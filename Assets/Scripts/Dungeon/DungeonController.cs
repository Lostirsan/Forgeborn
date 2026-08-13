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

        private float _nextLocalY;
        private readonly Queue<GameObject> _segments = new Queue<GameObject>();
        private float _depth;
        private Vector3 _playerHome, _glowBase, _flameBase;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
            _halfW = cam.orthographicSize * cam.aspect;         // half screen width in world units
            _wallW = wallScreenFraction * (2f * _halfW);        // each wall ≤ 5% of screen width
            _floorW = 2f * _halfW - 2f * _wallW;                // floor fills the ~90% middle
            if (player != null) _playerHome = player.localPosition;
            if (torchGlow != null) _glowBase = torchGlow.transform.localScale;
            if (torchFlame != null) _flameBase = torchFlame.localScale;
        }

        private void Start()
        {
            _nextLocalY = -cam.orthographicSize;
            FillAhead();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                {
                    float d = moveSpeed * Time.deltaTime;
                    worldRoot.position += Vector3.down * d;
                    _depth += d;
                    if (player != null)
                    {
                        var lp = _playerHome; lp.y += Mathf.Sin(Time.time * 10f) * 0.06f; // walk bob
                        player.localPosition = lp;
                    }
                }
                else if (player != null) player.localPosition = _playerHome;

                if (kb.escapeKey.wasPressedThisFrame) SceneManager.LoadScene(smithySceneName);
            }

            FlickerTorch();
            FillAhead();
            CullBehind();
            if (depthLabel != null) depthLabel.text = $"Глубина: {_depth:0} м";
        }

        private void FlickerTorch()
        {
            if (torchGlow != null)
            {
                float n = Mathf.PerlinNoise(Time.time * 5f, 1.3f);
                torchGlow.transform.localScale = _glowBase * (0.97f + n * 0.06f); // barely breathes
                var col = torchGlow.color; col.a = 0.6f + n * 0.08f; torchGlow.color = col;
            }
            if (torchFlame != null)
            {
                float n2 = Mathf.PerlinNoise(Time.time * 8f, 4f);
                torchFlame.localScale = new Vector3(_flameBase.x * (0.95f + n2 * 0.08f), _flameBase.y * (0.92f + n2 * 0.12f), 1f);
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
