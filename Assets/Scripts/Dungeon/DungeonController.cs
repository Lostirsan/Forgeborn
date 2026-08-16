using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ForgeGame.Dungeon
{
    /// <summary>
    /// Dungeon vertical slice: a fixed camera looks down a painted cave corridor, the hero is
    /// drawn from behind near the bottom. Holding W (or ↑) walks forward — the corridor scrolls
    /// down past him (segments recycled for an endless tunnel). Everything is dark except the
    /// circle lit by the torch in the hero's hand, so the way ahead fades into blackness.
    /// Esc returns to the smithy.
    /// </summary>
    public class DungeonController : MonoBehaviour
    {
        [SerializeField] private Transform worldRoot;
        [SerializeField] private Transform player;
        [SerializeField] private Camera cam;
        [SerializeField] private Sprite floorSprite;
        [SerializeField] private Sprite wallSprite;   // 1u wide × 6u tall, lit on the right (inner) edge
        [SerializeField] private TMPro.TMP_Text depthLabel;
        [SerializeField] private string smithySceneName = "Smithy";

        [Header("Corridor / movement")]
        [SerializeField] private float segmentHeight = 6f;         // = wall sprite native height (no vertical stretch)
        [SerializeField] private float wallScreenFraction = 0.05f; // each wall ≤ this share of screen width
        [SerializeField] private float floorStretch = 1.8f;        // floor tiles elongated into the distance (perspective feel)
        [SerializeField] private float moveSpeed = 4f;

        private float _halfW, _wallW, _floorW;

        [Header("Torch (flicker)")]
        [SerializeField] private SpriteRenderer torchGlow;
        [SerializeField] private Transform torchFlame;
        [SerializeField] private Transform visionTransform; // the darkness mask — its lit hole flickers like flame

        [Header("Ore / inventory")]
        [SerializeField] private Sprite oreSprite;
        [SerializeField] private DungeonHotbar hotbar;
        [SerializeField] private GameObject inventoryPanel; // 3×3 panel, toggled with I
        [SerializeField] private string oreItemId = "iron_ore";
        [SerializeField] private float oreSpawnChance = 0.5f;  // chance a spawned segment carries ore
        [SerializeField] private float sideSpeed = 5f;         // how fast the hero steps sideways toward ore
        [SerializeField] private float pickupRadius = 1.0f;

        private float _nextLocalY;
        private readonly Queue<GameObject> _segments = new Queue<GameObject>();
        private float _depth;
        private Vector3 _glowBase, _flameBase, _visionBase, _visionPos, _glowPos, _flamePos;
        private float _playerX, _playerBaseY;
        private DungeonOre _targetOre;
        private int _segIndex;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
            _halfW = cam.orthographicSize * cam.aspect;         // half screen width in world units
            _wallW = wallScreenFraction * (2f * _halfW);        // each wall ≤ 5% of screen width
            _floorW = 2f * _halfW - 2f * _wallW;                // floor fills the ~90% middle
            if (player != null) { _playerX = player.position.x; _playerBaseY = player.position.y; }
            if (torchGlow != null) { _glowBase = torchGlow.transform.localScale; _glowPos = torchGlow.transform.localPosition; }
            if (torchFlame != null) { _flameBase = torchFlame.localScale; _flamePos = torchFlame.localPosition; }
            if (visionTransform != null) { _visionBase = visionTransform.localScale; _visionPos = visionTransform.localPosition; }
        }

        private void Start()
        {
            _nextLocalY = -cam.orthographicSize;
            FillAhead();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            HandleClick();

            bool walking = false;
            if (_targetOre != null)
            {
                walking = WalkToTarget();
            }
            else
            {
                // Always step back to the centre lane (e.g. right after grabbing off-centre ore).
                float newX = Mathf.MoveTowards(_playerX, 0f, sideSpeed * Time.deltaTime);
                if (!Mathf.Approximately(newX, _playerX)) walking = true;
                _playerX = newX;

                if (kb != null && (kb.wKey.isPressed || kb.upArrowKey.isPressed))
                {
                    float d = moveSpeed * Time.deltaTime;
                    worldRoot.position += Vector3.down * d;
                    _depth += d;
                    walking = true;
                }
            }

            if (player != null)
            {
                float bob = walking ? Mathf.Sin(Time.time * 10f) * 0.06f : 0f;
                player.position = new Vector3(_playerX, _playerBaseY + bob, 0f);
            }

            if (kb != null && kb.iKey.wasPressedThisFrame && inventoryPanel != null)
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (inventoryPanel != null && inventoryPanel.activeSelf) inventoryPanel.SetActive(false);
                else SceneManager.LoadScene(smithySceneName);
            }

            FlickerTorch();
            FillAhead();
            CullBehind();
            if (depthLabel != null) depthLabel.text = $"Глубина: {_depth:0} м";
        }

        // Click an ore in the world → target it so the hero walks over to collect.
        private void HandleClick()
        {
            var m = Mouse.current;
            if (m == null || !m.leftButton.wasPressedThisFrame) return;
            Vector3 wp = cam.ScreenToWorldPoint(m.position.ReadValue());
            var hit = Physics2D.OverlapPoint(new Vector2(wp.x, wp.y));
            if (hit != null)
            {
                var ore = hit.GetComponent<DungeonOre>();
                if (ore != null) _targetOre = ore;
            }
        }

        // Walk to the targeted ore: scroll the corridor so it comes level with the hero and step
        // sideways toward it; collect when close. Returns true while moving.
        private bool WalkToTarget()
        {
            Vector3 op = _targetOre.transform.position;
            float dy = op.y - _playerBaseY;
            if (dy > 0.05f)
            {
                float d = Mathf.Min(moveSpeed * Time.deltaTime, dy);
                worldRoot.position += Vector3.down * d;
                _depth += d;
            }
            _playerX = Mathf.MoveTowards(_playerX, op.x, sideSpeed * Time.deltaTime);

            op = _targetOre.transform.position; // moved with the scroll
            if (Vector2.Distance(new Vector2(_playerX, _playerBaseY), new Vector2(op.x, op.y)) < pickupRadius)
            {
                if (hotbar != null) hotbar.Add(_targetOre.itemId, oreSprite, _targetOre.amount);
                Destroy(_targetOre.gameObject);
                _targetOre = null;
            }
            return true;
        }

        private void FlickerTorch()
        {
            float t = Time.time;
            // Flame intensity: a slow breathe + a fast jitter, with the occasional sharper flare.
            float slow = Mathf.PerlinNoise(t * 1.6f, 0.5f);
            float fast = Mathf.PerlinNoise(t * 11f, 5.5f);
            float flare = Mathf.Pow(Mathf.PerlinNoise(t * 4f, 9f), 3f); // rare bright spikes
            float flick = Mathf.Clamp01(0.45f * slow + 0.35f * fast + 0.35f * flare);

            // The lit hole itself flickers: radius breathes, shape wobbles unevenly, centre drifts —
            // so the light reads as fire, not a steady lamp.
            // The whole torch (light circle, glow, flame) rides with the hero's X position.
            if (visionTransform != null)
            {
                float sx = 0.90f + flick * 0.16f + (Mathf.PerlinNoise(t * 7f, 20f) - 0.5f) * 0.08f;
                float sy = 0.90f + flick * 0.16f + (Mathf.PerlinNoise(t * 9f, 40f) - 0.5f) * 0.08f;
                visionTransform.localScale = new Vector3(_visionBase.x * sx, _visionBase.y * sy, 1f);
                visionTransform.localPosition = new Vector3(
                    _playerX + _visionPos.x + (Mathf.PerlinNoise(t * 8f, 1f) - 0.5f) * 0.35f,
                    _visionPos.y + (Mathf.PerlinNoise(t * 8f, 2f) - 0.5f) * 0.30f,
                    _visionPos.z);
            }
            if (torchGlow != null)
            {
                torchGlow.transform.localScale = _glowBase * (0.85f + flick * 0.30f);
                torchGlow.transform.localPosition = new Vector3(_playerX + _glowPos.x, _glowPos.y, _glowPos.z);
                var col = torchGlow.color; col.a = 0.45f + flick * 0.35f; torchGlow.color = col;
            }
            if (torchFlame != null)
            {
                float wob = (Mathf.PerlinNoise(t * 10f, 7f) - 0.5f) * 0.12f;
                torchFlame.localScale = new Vector3(_flameBase.x * (0.9f + fast * 0.18f) + wob, _flameBase.y * (0.8f + flick * 0.4f), 1f);
                torchFlame.localPosition = new Vector3(_playerX + _flamePos.x, _flamePos.y, _flamePos.z);
            }
        }

        private void FillAhead()
        {
            float top = cam.orthographicSize + segmentHeight;
            int guard = 0;
            while (worldRoot.position.y + _nextLocalY < top && guard++ < 100) SpawnSegment();
        }

        private void SpawnSegment()
        {
            var seg = new GameObject("Segment").transform;
            seg.SetParent(worldRoot, false);
            seg.localPosition = new Vector3(0f, _nextLocalY, 0f);

            // Floor: one tiled renderer filling the middle. The Y tiling is stretched so the
            // flagstones read as elongated INTO the distance (a road at an angle, not top-down).
            var floor = new GameObject("Floor");
            floor.transform.SetParent(seg, false);
            floor.transform.localScale = new Vector3(1f, floorStretch, 1f);
            var fsr = floor.AddComponent<SpriteRenderer>();
            fsr.sprite = floorSprite; fsr.drawMode = SpriteDrawMode.Tiled;
            fsr.size = new Vector2(_floorW, segmentHeight / floorStretch); fsr.sortingOrder = 0;

            // Thin walls at the far left/right edges (right one mirrored so its lit edge faces in).
            AddWall(seg, -(_halfW - _wallW * 0.5f), +_wallW);
            AddWall(seg, +(_halfW - _wallW * 0.5f), -_wallW);

            // Some blocks carry ore lying on the floor (skip the first couple near the start).
            if (oreSprite != null && _segIndex >= 2 && Random.value < oreSpawnChance)
            {
                float margin = 1.2f;
                float ox = Random.Range(-_floorW * 0.5f + margin, _floorW * 0.5f - margin);
                float oy = Random.Range(-segmentHeight * 0.35f, segmentHeight * 0.35f);
                var ore = new GameObject("Ore");
                ore.transform.SetParent(seg, false);
                ore.transform.localPosition = new Vector3(ox, oy, 0f);
                ore.transform.localScale = Vector3.one * 0.85f;
                var osr = ore.AddComponent<SpriteRenderer>();
                osr.sprite = oreSprite; osr.sortingOrder = 2;
                var col = ore.AddComponent<CircleCollider2D>();
                col.isTrigger = true; col.radius = 0.6f;
                var doc = ore.AddComponent<DungeonOre>();
                doc.itemId = oreItemId; doc.amount = Random.Range(1, 4);
            }
            _segIndex++;

            _segments.Enqueue(seg.gameObject);
            _nextLocalY += segmentHeight;
        }

        private void AddWall(Transform seg, float x, float scaleX)
        {
            var wall = new GameObject("Wall");
            wall.transform.SetParent(seg, false);
            wall.transform.localPosition = new Vector3(x, 0f, 0f);
            wall.transform.localScale = new Vector3(scaleX, segmentHeight / 6f, 1f); // wall sprite native = 6u tall
            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = wallSprite; sr.sortingOrder = 1;
        }

        private void CullBehind()
        {
            float bottom = -cam.orthographicSize - segmentHeight;
            while (_segments.Count > 0)
            {
                var s = _segments.Peek();
                if (worldRoot.position.y + s.transform.localPosition.y < bottom) Destroy(_segments.Dequeue());
                else break;
            }
        }
    }
}
