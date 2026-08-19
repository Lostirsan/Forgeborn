using System.Collections.Generic;
using ForgeGame.Localization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ForgeGame.Dungeon
{
    /// <summary>
    /// Side-view (diorama) dungeon: the party stands in a line on the left, the cave scrolls
    /// past as they walk right (W/D/→). A torch on the leader lights the party and a little
    /// ahead; the rest is dark. Ore lies on the ground — click it and the party advances to it
    /// and picks it up into the 9-slot inventory (I opens it). Esc returns to the smithy.
    /// This is the exploration slice; row-based combat is layered on next.
    /// </summary>
    public class DungeonSideController : MonoBehaviour
    {
        [SerializeField] private Transform worldRoot;
        [SerializeField] private Transform party;
        [SerializeField] private Camera cam;
        [SerializeField] private Sprite wallSprite;
        [SerializeField] private Sprite floorSprite;
        [SerializeField] private Sprite ceilingSprite;
        [SerializeField] private TMPro.TMP_Text depthLabel;
        [SerializeField] private string smithySceneName = "Smithy";

        [Header("Layout (world units)")]
        [SerializeField] private float groundY = -3.2f;    // top of the floor / where the party stands
        [SerializeField] private float floorBottom = -5.4f;
        [SerializeField] private float ceilBottom = 3.4f;
        [SerializeField] private float ceilTop = 5.4f;
        [SerializeField] private float segmentWidth = 4f;
        [SerializeField] private float moveSpeed = 4f;

        [Header("Torch (flicker)")]
        [SerializeField] private SpriteRenderer torchGlow;
        [SerializeField] private Transform torchFlame;
        [SerializeField] private Transform visionTransform;

        [Header("Ore / inventory")]
        [SerializeField] private Sprite oreSprite;
        [SerializeField] private DungeonHotbar hotbar;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private string oreItemId = "iron_ore";
        [SerializeField] private float oreSpawnChance = 0.55f;
        [SerializeField] private float pickupRadius = 1.2f;

        [Header("Combat")]
        [SerializeField] private DungeonCombat combat;
        [SerializeField] private DungeonPauseController pause;
        [SerializeField] private Transform leaderView;    // hidden if the player left the leader behind
        [SerializeField] private Transform companionView; // hidden if the player left the companion behind
        [SerializeField] private float firstEncounterDepth = 6f; // no fights before this depth
        [SerializeField] private float encounterMinGap = 12f;
        [SerializeField] private float encounterMaxGap = 22f;

        private float _halfW, _partyX, _nextLocalX, _dist;
        private readonly Queue<GameObject> _segments = new Queue<GameObject>();
        private Vector3 _partyHome, _glowBase, _flameBase, _visionBase, _visionPos;
        private DungeonOre _targetOre;
        private int _segIndex;
        private float _nextEncounterAt;
        private float _visionMult = 1f, _visionShiftX;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
            _halfW = cam.orthographicSize * cam.aspect;
            if (party != null) { _partyHome = party.position; _partyX = party.position.x; }
            if (torchGlow != null) _glowBase = torchGlow.transform.localScale;
            if (torchFlame != null) _flameBase = torchFlame.localScale;
            if (visionTransform != null) { _visionBase = visionTransform.localScale; _visionPos = visionTransform.localPosition; }

            // Honor the smithy loadout: hide any hero the player left behind for the run.
            if (ExpeditionLoadout.configured)
            {
                if (!ExpeditionLoadout.takeLeader && leaderView != null) leaderView.gameObject.SetActive(false);
                if (!ExpeditionLoadout.takeCompanion && companionView != null) companionView.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            _nextLocalX = Mathf.Floor((-_halfW - segmentWidth) / segmentWidth) * segmentWidth;
            _nextEncounterAt = firstEncounterDepth + Random.Range(0f, encounterMinGap);
            FillAhead();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;

            // ESC menu takes priority. Open it pauses the run (and any fight) via timeScale;
            // while open, ESC steps back out (settings → pause → resume). Inventory closes first.
            if (pause != null && pause.IsOpen)
            {
                if (esc) { if (pause.SettingsOpen) pause.CloseSettings(); else pause.Resume(); }
                return;
            }
            if (esc)
            {
                if (inventoryPanel != null && inventoryPanel.activeSelf) inventoryPanel.SetActive(false);
                else if (pause != null) pause.Open();
                return;
            }

            // While a fight is on, the world is frozen: only the torch keeps living and the
            // inventory can still be toggled. Combat runs itself in DungeonCombat.
            if (combat != null && combat.InCombat)
            {
                if (kb != null && kb.iKey.wasPressedThisFrame && inventoryPanel != null)
                    inventoryPanel.SetActive(!inventoryPanel.activeSelf);
                FlickerTorch();
                return;
            }

            HandleClick();

            bool walking = false;
            if (_targetOre != null) walking = WalkToOre();
            else if (kb != null && (kb.wKey.isPressed || kb.dKey.isPressed || kb.rightArrowKey.isPressed))
            {
                worldRoot.position += Vector3.left * (moveSpeed * Time.deltaTime);
                _dist += moveSpeed * Time.deltaTime;
                walking = true;
                if (combat != null && _dist >= _nextEncounterAt) { combat.StartEncounter(Mathf.RoundToInt(_dist)); return; }
            }

            if (party != null)
            {
                float bob = walking ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.06f : 0f;
                party.position = new Vector3(_partyX, _partyHome.y + bob, _partyHome.z);
            }

            if (kb != null && kb.iKey.wasPressedThisFrame && inventoryPanel != null)
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);

            FlickerTorch();
            FillAhead();
            CullBehind();
            if (depthLabel != null) depthLabel.text = Loc.Format("dungeon.depth", Mathf.RoundToInt(_dist));
        }

        private void HandleClick()
        {
            var m = Mouse.current;
            if (m == null || !m.leftButton.wasPressedThisFrame) return;
            Vector3 wp = cam.ScreenToWorldPoint(m.position.ReadValue());
            var hit = Physics2D.OverlapPoint(new Vector2(wp.x, wp.y));
            if (hit != null) { var ore = hit.GetComponent<DungeonOre>(); if (ore != null) _targetOre = ore; }
        }

        private bool WalkToOre()
        {
            float dx = _targetOre.transform.position.x - _partyX;
            if (dx > pickupRadius * 0.5f)
            {
                worldRoot.position += Vector3.left * Mathf.Min(moveSpeed * Time.deltaTime, dx);
                _dist += moveSpeed * Time.deltaTime;
            }
            if (Mathf.Abs(_targetOre.transform.position.x - _partyX) < pickupRadius)
            {
                if (hotbar != null) hotbar.Add(_targetOre.itemId, oreSprite, _targetOre.amount);
                ExpeditionResult.Add(_targetOre.itemId, _targetOre.amount); // banked into the smithy on return
                Destroy(_targetOre.gameObject);
                _targetOre = null;
            }
            return true;
        }

        // Combat widens and pushes the torchlight forward so the battlefield is visible;
        // exploration restores the tight torch circle.
        public void SetCombatMode(bool on)
        {
            _visionMult = on ? 2.1f : 1f;
            _visionShiftX = on ? 2.6f : 0f;
        }

        // Called by DungeonCombat when a fight ends. Victory → keep delving (schedule the next
        // ambush); defeat → the run is over, back to the smithy.
        public void OnCombatEnd(bool victory)
        {
            if (!victory) { SceneManager.LoadScene(smithySceneName); return; }
            _nextEncounterAt = _dist + Random.Range(encounterMinGap, encounterMaxGap);
        }

        private void FlickerTorch()
        {
            float t = Time.time;
            float slow = Mathf.PerlinNoise(t * 1.6f, 0.5f);
            float fast = Mathf.PerlinNoise(t * 11f, 5.5f);
            float flare = Mathf.Pow(Mathf.PerlinNoise(t * 4f, 9f), 3f);
            float flick = Mathf.Clamp01(0.45f * slow + 0.35f * fast + 0.35f * flare);
            if (visionTransform != null)
            {
                float sx = 0.90f + flick * 0.16f + (Mathf.PerlinNoise(t * 7f, 20f) - 0.5f) * 0.08f;
                float sy = 0.90f + flick * 0.16f + (Mathf.PerlinNoise(t * 9f, 40f) - 0.5f) * 0.08f;
                visionTransform.localScale = new Vector3(_visionBase.x * sx * _visionMult, _visionBase.y * sy * _visionMult, 1f);
                visionTransform.localPosition = _visionPos + new Vector3(
                    _visionShiftX + (Mathf.PerlinNoise(t * 8f, 1f) - 0.5f) * 0.30f,
                    (Mathf.PerlinNoise(t * 8f, 2f) - 0.5f) * 0.26f, 0f);
            }
            if (torchGlow != null)
            {
                torchGlow.transform.localScale = _glowBase * (0.85f + flick * 0.30f);
                var col = torchGlow.color; col.a = 0.45f + flick * 0.35f; torchGlow.color = col;
            }
            if (torchFlame != null)
            {
                float wob = (Mathf.PerlinNoise(t * 10f, 7f) - 0.5f) * 0.1f;
                torchFlame.localScale = new Vector3(_flameBase.x * (0.9f + fast * 0.18f) + wob, _flameBase.y * (0.8f + flick * 0.4f), 1f);
            }
        }

        private void FillAhead()
        {
            float right = _halfW + segmentWidth;
            int guard = 0;
            while (worldRoot.position.x + _nextLocalX < right && guard++ < 100) SpawnSegment();
        }

        private void SpawnSegment()
        {
            var seg = new GameObject("Segment").transform;
            seg.SetParent(worldRoot, false);
            float cx = _nextLocalX + segmentWidth * 0.5f;
            seg.localPosition = new Vector3(cx, 0f, 0f);

            AddTiled(seg, floorSprite, segmentWidth, groundY - floorBottom, (groundY + floorBottom) * 0.5f, 0);
            AddTiled(seg, wallSprite, segmentWidth, ceilBottom - groundY, (groundY + ceilBottom) * 0.5f, -1);
            AddTiled(seg, ceilingSprite, segmentWidth, ceilTop - ceilBottom, (ceilBottom + ceilTop) * 0.5f, 0);

            if (oreSprite != null && _segIndex >= 2 && Random.value < oreSpawnChance)
            {
                var ore = new GameObject("Ore");
                ore.transform.SetParent(seg, false);
                ore.transform.localPosition = new Vector3(Random.Range(-segmentWidth * 0.35f, segmentWidth * 0.35f), groundY + 0.35f, 0f);
                ore.transform.localScale = Vector3.one * 0.8f;
                var osr = ore.AddComponent<SpriteRenderer>(); osr.sprite = oreSprite; osr.sortingOrder = 2;
                var col = ore.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.6f;
                var doc = ore.AddComponent<DungeonOre>(); doc.itemId = oreItemId; doc.amount = Random.Range(1, 4);
            }
            _segIndex++;

            _segments.Enqueue(seg.gameObject);
            _nextLocalX += segmentWidth;
        }

        private void AddTiled(Transform seg, Sprite sprite, float width, float height, float centerY, int order)
        {
            var go = new GameObject("Layer");
            go.transform.SetParent(seg, false);
            go.transform.localPosition = new Vector3(0f, centerY, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height); sr.sortingOrder = order;
        }

        private void CullBehind()
        {
            float left = -_halfW - segmentWidth;
            while (_segments.Count > 0)
            {
                var s = _segments.Peek();
                if (worldRoot.position.x + s.transform.localPosition.x + segmentWidth * 0.5f < left) Destroy(_segments.Dequeue());
                else break;
            }
        }
    }
}
