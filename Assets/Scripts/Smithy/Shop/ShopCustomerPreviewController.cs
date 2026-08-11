using UnityEngine;

namespace ForgeGame.Smithy.Shop
{
    /// <summary>
    /// Drives a single test customer through the Shop view to validate composition
    /// and movement: enter from the left, walk to the talk point, idle, and (on
    /// request) leave. This is a visual prototype only — no economy, no rewards, and
    /// nothing here is persisted.
    /// </summary>
    public class ShopCustomerPreviewController : MonoBehaviour
    {
        [SerializeField] private CustomerView customer;
        [SerializeField] private Transform entryPointLeft;
        [SerializeField] private Transform talkPoint;
        [SerializeField] private Transform exitPointLeft;
        [SerializeField] private bool autoStart = true;

        private void Start()
        {
            if (autoStart) Begin();
            else customer?.Hide();
        }

        /// <summary>Spawns at the entry point and walks to the talk point.</summary>
        public void Begin()
        {
            if (customer == null || entryPointLeft == null || talkPoint == null) return;
            customer.SpawnAt(entryPointLeft.position);
            customer.WalkTo(talkPoint.position, null);
        }

        /// <summary>Walks the customer off to the left, then hides it.</summary>
        public void Dismiss()
        {
            if (customer == null) return;
            Vector3 exit = exitPointLeft != null ? exitPointLeft.position
                : (entryPointLeft != null ? entryPointLeft.position : customer.transform.position);
            customer.WalkTo(exit, () => customer.Hide());
        }

        /// <summary>Debug helper: leave then re-enter.</summary>
        public void Cycle()
        {
            if (customer != null && customer.IsIdle) Dismiss();
            else Begin();
        }
    }
}
