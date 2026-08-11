using UnityEngine;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// Base class for every overlay panel. The controller opens and closes panels
    /// through here so behaviour (focus, dimming, movement lock) is uniform, while
    /// each subclass only implements its own <see cref="OnOpened"/>/<see cref="OnClosed"/>.
    /// </summary>
    public abstract class SmithyPanel : MonoBehaviour
    {
        [SerializeField] protected SmithyController controller;
        [SerializeField] private PanelId panelId;
        [SerializeField] private GameObject firstSelected;

        public PanelId Id => panelId;
        public GameObject FirstSelected => firstSelected;
        public SmithyController Controller => controller;

        /// <summary>Set by the generator; also settable at runtime for safety.</summary>
        public void Bind(SmithyController owner) => controller = owner;
        public void SetId(PanelId id) => panelId = id;
        public void SetFirstSelected(GameObject go) => firstSelected = go;

        public void OpenInternal()
        {
            gameObject.SetActive(true);
            OnOpened();
        }

        public void CloseInternal()
        {
            OnClosed();
            gameObject.SetActive(false);
        }

        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
    }
}
