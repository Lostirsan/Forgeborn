using System.Collections;
using System.Collections.Generic;
using ForgeGame.Data;
using ForgeGame.Inventory;
using ForgeGame.Items;
using ForgeGame.Research;
using ForgeGame.Save;
using ForgeGame.Settings;
using ForgeGame.UI.Common;
using ForgeGame.UI.Smithy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// The scene coordinator. It owns the shared services, routes input and station
    /// interactions, manages the single-open-panel rule with focus handling, drives
    /// screen shake, and handles saving/loading and scene transitions. Mechanics
    /// live in the individual panels and evaluators, not here.
    /// </summary>
    public class SmithyController : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private SmithyDatabase database;
        [SerializeField] private InventoryService inventory;
        [SerializeField] private ResearchService research;
        [SerializeField] private ForgeSessionController sessionController;
        [SerializeField] private SettingsService settings;

        [Header("Scene refs")]
        [SerializeField] private ScreenFader fader;
        [SerializeField] private SmithyAudio audioAdapter;
        [SerializeField] private NotificationController notifications;
        [SerializeField] private InteractionPromptController prompt;
        [SerializeField] private HudController hud;
        [SerializeField] private SmithyPlayerController player;
        [SerializeField] private InteractionDetector detector;
        [SerializeField] private Transform cameraTransform;

        [Header("Panels")]
        [SerializeField] private List<SmithyPanel> panels = new List<SmithyPanel>();

        [Header("Scene names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string dungeonSceneName = "Dungeon";

        private readonly ISaveGameService _saveService = new LocalSaveGameService();
        private readonly Dictionary<PanelId, SmithyPanel> _panelMap = new Dictionary<PanelId, SmithyPanel>();
        private PanelId _current = PanelId.None;
        private GameObject _selectionBeforePanel;
        private bool _isLoading;
        private Vector3 _cameraBaseLocalPos;
        private Coroutine _shakeRoutine;
        private string _equippedWeaponId = string.Empty;
        private bool _bronzeSeeded;

        public SmithyDatabase Database => database;
        public InventoryService Inventory => inventory;
        public ResearchService Research => research;
        public ForgeSessionController Session => sessionController;
        public SmithyAudio Audio => audioAdapter;
        public bool IsUIOpen => _current != PanelId.None;
        public float EffectsIntensity => settings != null ? settings.Current.effectsIntensity : 1f;

        private void Awake()
        {
            _panelMap.Clear();
            foreach (var p in panels)
            {
                if (p == null) continue;
                p.Bind(this);
                _panelMap[p.Id] = p;
            }
        }

        private void Start()
        {
            if (cameraTransform != null) _cameraBaseLocalPos = cameraTransform.localPosition;

            foreach (var p in panels)
                if (p != null) p.gameObject.SetActive(false);

            LoadGame();
            UpdateObjective();
            fader?.FadeIn();
        }

        private void Update()
        {
            if (_isLoading) return;
            HandleHotkeys();
            UpdatePrompt();
        }

        // ---- Input ----

        private void HandleHotkeys()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;

            bool cancel = (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                          (gp != null && gp.startButton.wasPressedThisFrame);
            bool inventoryKey = kb != null && (kb.tabKey.wasPressedThisFrame || kb.iKey.wasPressedThisFrame);
            bool journalKey = kb != null && kb.jKey.wasPressedThisFrame;
            bool interactKey = (kb != null && kb.eKey.wasPressedThisFrame) ||
                               (gp != null && gp.buttonSouth.wasPressedThisFrame);

            if (cancel)
            {
                if (_current != PanelId.None) ClosePanel();
                else OpenPanel(PanelId.Pause);
                return;
            }

            if (inventoryKey)
            {
                if (_current == PanelId.Inventory) ClosePanel();
                else if (_current == PanelId.None) OpenPanel(PanelId.Inventory);
                return;
            }

            if (journalKey)
            {
                if (_current == PanelId.Journal) ClosePanel();
                else if (_current == PanelId.None) OpenPanel(PanelId.Journal);
                return;
            }

            if (interactKey && _current == PanelId.None && detector != null &&
                detector.Current != null && detector.Current.CanInteract)
            {
                detector.Current.Interact(this);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (kb != null && kb.f1Key.wasPressedThisFrame)
            {
                if (_current == PanelId.Debug) ClosePanel();
                else if (_current == PanelId.None) OpenPanel(PanelId.Debug);
            }
#endif
        }

        private void UpdatePrompt()
        {
            if (prompt == null) return;
            if (_current == PanelId.None && detector != null && detector.Current != null)
                prompt.Show(detector.Current.PromptText);
            else
                prompt.Hide();
        }

        // ---- Station routing ----

        public void ActivateStation(SmithyStation station)
        {
            switch (station)
            {
                case SmithyStation.Storage: OpenPanel(PanelId.Inventory); break;
                case SmithyStation.Foundry: OpenPanel(PanelId.Foundry); break;
                case SmithyStation.Anvil: OpenPanel(PanelId.Anvil); break;
                case SmithyStation.AssemblyTable: OpenPanel(PanelId.Assembly); break;
                case SmithyStation.DungeonDoor: GoToDungeon(); break;
                case SmithyStation.MainMenuDoor: OpenPanel(PanelId.Pause); break;
            }
        }

        // ---- Panels ----

        public void OpenPanel(PanelId id)
        {
            if (_isLoading || !_panelMap.TryGetValue(id, out var panel)) return;
            if (_current == id) return;

            if (_current != PanelId.None && _panelMap.TryGetValue(_current, out var cur))
                cur.CloseInternal();
            else
                _selectionBeforePanel = CurrentSelection();

            audioAdapter?.PlayUiOpen();
            panel.OpenInternal();
            _current = id;
            SelectGameObject(panel.FirstSelected);
        }

        public void ClosePanel()
        {
            if (_current == PanelId.None) return;
            if (_panelMap.TryGetValue(_current, out var cur)) cur.CloseInternal();
            _current = PanelId.None;
            SelectGameObject(_selectionBeforePanel);
            UpdateObjective();
        }

        public T GetPanel<T>() where T : SmithyPanel
        {
            foreach (var p in panels)
                if (p is T typed) return typed;
            return null;
        }

        public void ShowWeaponResult(WeaponInstance weapon)
        {
            var panel = GetPanel<ForgeGame.UI.Smithy.ItemResultPanelController>();
            if (panel == null) return;
            panel.Configure(weapon);
            OpenPanel(PanelId.ItemResult);
        }

        // ---- Feedback ----

        public void Notify(string message) => notifications?.Show(message);

        public void EquipWeapon(string weaponId)
        {
            _equippedWeaponId = weaponId ?? string.Empty;
            SaveGame(silent: true);
        }

        public void Shake(float strength)
        {
            if (settings != null && !settings.Current.screenShake) return;
            strength *= EffectsIntensity;
            if (strength <= 0.001f || cameraTransform == null) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(strength));
        }

        private IEnumerator ShakeRoutine(float strength)
        {
            const float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float damp = 1f - (t / duration);
                Vector2 offset = Random.insideUnitCircle * strength * damp;
                cameraTransform.localPosition = _cameraBaseLocalPos + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }
            cameraTransform.localPosition = _cameraBaseLocalPos;
            _shakeRoutine = null;
        }

        public void UpdateObjective()
        {
            if (hud == null) return;
            string text = (inventory != null && inventory.CastBlanks.Count > 0)
                ? "Отнесите заготовку к наковальне (или отлейте ещё в плавильне)."
                : "Расплавьте бронзу в плавильне.";
            var s = sessionController != null ? sessionController.Current : null;
            if (s != null)
            {
                switch (s.currentStage)
                {
                    case ForgeStage.Melting: text = "Нагрейте бронзу до рабочего диапазона."; break;
                    case ForgeStage.Pouring: text = "Заливайте бронзу тонкой струёй: удерживайте и тяните тигель вправо."; break;
                    case ForgeStage.Cooling: text = "Дождитесь остывания формы."; break;
                    case ForgeStage.CastBlankReady: text = "Отнесите литую заготовку к наковальне."; break;
                    case ForgeStage.EdgeForging: text = "Проковывайте кромки клинка на наковальне."; break;
                    case ForgeStage.Assembly: text = "Соберите меч на сборочном столе."; break;
                }
            }
            hud.SetObjective(text);
        }

        // ---- Save / load ----

        private void LoadGame()
        {
            if (inventory != null) inventory.SetDatabase(database);

            SaveData data = _saveService.Load();
            var smithy = data?.smithy;
            if (smithy != null && smithy.hasData)
            {
                inventory?.LoadFrom(smithy.inventory);
                research?.LoadFrom(smithy.research);
                sessionController?.LoadFrom(smithy.activeSession);
                // Drop any incompatible pre-bronze in-progress session without breaking the rest of the save.
                var loaded = sessionController != null ? sessionController.Current : null;
                if (loaded != null && loaded.selectedMaterialId != SmithyIds.Bronze)
                    sessionController.ClearSession();
                _equippedWeaponId = smithy.equippedWeaponId ?? string.Empty;
                if (player != null && Mathf.Abs(smithy.playerX) > 0.001f) player.SetPositionX(smithy.playerX);

                // One-time bronze grant (also tops up pre-bronze saves so the smithy is playable).
                _bronzeSeeded = smithy.bronzeSeeded;
                if (!_bronzeSeeded && inventory != null)
                {
                    inventory.AddItem(SmithyIds.Bronze, 12);
                    _bronzeSeeded = true;
                    SaveGame(silent: true);
                }
            }
            else
            {
                SeedNewGame();
            }
        }

        private void SeedNewGame()
        {
            if (inventory == null) return;
            // Enough bronze for several test swords.
            inventory.AddItem(SmithyIds.Bronze, 12);
            _bronzeSeeded = true;
            SaveGame(silent: true);
        }

        public void SaveGame() => SaveGame(false);

        public void SaveGame(bool silent)
        {
            SaveData data = _saveService.Load() ?? SaveData.CreateNew();
            data.sceneName = "Smithy";
            data.smithy ??= new SmithySaveData();
            data.smithy.hasData = true;
            data.smithy.bronzeSeeded = _bronzeSeeded;
            if (inventory != null) data.smithy.inventory = inventory.Export();
            if (research != null) data.smithy.research = research.Export();
            if (sessionController != null) data.smithy.activeSession = sessionController.Export();
            data.smithy.equippedWeaponId = _equippedWeaponId;
            if (player != null) data.smithy.playerX = player.PositionX;
            _saveService.Save(data);
            if (!silent) Notify("Игра сохранена.");
        }

        // ---- Transitions ----

        public void ReturnToMainMenu()
        {
            LoadSceneWithFade(mainMenuSceneName);
        }

        public void GoToDungeon()
        {
            if (string.IsNullOrEmpty(dungeonSceneName) || !Application.CanStreamedLevelBeLoaded(dungeonSceneName))
            {
                Notify("Подземелье пока недоступно.");
                return;
            }
            LoadSceneWithFade(dungeonSceneName);
        }

        private void LoadSceneWithFade(string sceneName)
        {
            if (_isLoading) return;
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Smithy] Scene '{sceneName}' is not in Build Settings.");
                Notify("Сцена недоступна.");
                return;
            }
            _isLoading = true;
            if (fader != null) fader.LoadSceneWithFade(sceneName, () => _isLoading = false);
            else SceneManager.LoadScene(sceneName);
        }

        // ---- Focus helpers ----

        private static GameObject CurrentSelection() =>
            EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        private static void SelectGameObject(GameObject go)
        {
            if (go != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(go);
        }
    }
}
