using ForgeGame.Data;
using UnityEngine;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// A single weapon component as a real 2D physics body in the world-space assembly
    /// stage (guard / handle / pommel). It can be held (kinematic, follows the cursor,
    /// rotates with the wheel/keys), dropped (dynamic, falls under gravity, collides with
    /// the tang and the parts below), and finally committed (frozen static where it came
    /// to rest). Nothing here snaps or straightens — where the body ends up IS the result.
    /// Colliders are child objects (see the builder) leaving a central channel so the tang
    /// can pass through a well-aligned part but catch a crooked one.
    /// </summary>
    public class AssemblyPhysicsPart : MonoBehaviour
    {
        [SerializeField] private ComponentSlot slot;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private float maxRotation = 40f;

        private Collider2D[] _cols;

        public ComponentSlot Slot => slot;
        public Rigidbody2D Body => body;
        public bool Committed { get; private set; }
        public float HeldRotation { get; private set; }

        // The loose part carries its own selection until it commits (only then → session).
        public string ComponentId { get; set; }
        public int VariantIndex { get; set; }

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (sprite == null) sprite = GetComponent<SpriteRenderer>();
            _cols = GetComponentsInChildren<Collider2D>(true);
        }

        public void SetSprite(Sprite s) { if (sprite != null && s != null) sprite.sprite = s; }
        public void SetTint(Color c) { if (sprite != null) sprite.color = c; }

        /// <summary>Reset to a fresh, free-falling state at a spawn position (upright by default).</summary>
        public void Spawn(Vector2 worldPos, float rotation = 0f)
        {
            gameObject.SetActive(true);
            Committed = false;
            HeldRotation = Mathf.Clamp(rotation, -maxRotation, maxRotation);
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
            transform.rotation = Quaternion.Euler(0f, 0f, HeldRotation);
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        /// <summary>Player grabbed it — stop physics, follow the cursor.</summary>
        public void Hold()
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        public void FollowCursor(Vector2 worldPos)
        {
            body.position = worldPos;
            body.rotation = HeldRotation;
        }

        public void SetHeldRotation(float rotation)
        {
            HeldRotation = Mathf.Clamp(rotation, -maxRotation, maxRotation);
            body.rotation = HeldRotation;
        }

        /// <summary>Let go — gravity takes over from exactly here.</summary>
        public void Release()
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        /// <summary>Freeze permanently where it settled (still a collider for the next part).</summary>
        public void Commit()
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Static;
            Committed = true;
        }

        public bool ContainsPoint(Vector2 world)
        {
            if (_cols == null) return false;
            foreach (var c in _cols)
                if (c != null && c.OverlapPoint(world)) return true;
            return false;
        }

        public bool IsResting(float maxSpeed, float maxAngularSpeed)
            => body.linearVelocity.magnitude < maxSpeed && Mathf.Abs(body.angularVelocity) < maxAngularSpeed;
    }
}
