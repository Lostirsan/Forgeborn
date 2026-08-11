using System.Collections.Generic;
using UnityEngine;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// Tracks which interactables the player is currently overlapping (via 2D
    /// triggers, so there is no per-frame scene search) and exposes the nearest
    /// available one. The controller reads <see cref="Current"/> to drive the prompt
    /// and the interact key.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        private readonly List<IInteractable> _inRange = new List<IInteractable>();

        public IInteractable Current { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var interactable = other.GetComponentInParent<IInteractable>();
            if (interactable != null && !_inRange.Contains(interactable))
                _inRange.Add(interactable);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var interactable = other.GetComponentInParent<IInteractable>();
            if (interactable != null)
                _inRange.Remove(interactable);
        }

        private void Update()
        {
            // Only a handful of stations are ever in range, so this is cheap.
            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = _inRange.Count - 1; i >= 0; i--)
            {
                var it = _inRange[i];
                if (it == null || it.Transform == null)
                {
                    _inRange.RemoveAt(i);
                    continue;
                }
                if (!it.CanInteract) continue;
                float d = Mathf.Abs(it.Transform.position.x - transform.position.x);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = it;
                }
            }
            Current = best;
        }
    }
}
