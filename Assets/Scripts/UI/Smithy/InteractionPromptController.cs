using TMPro;
using UnityEngine;

namespace ForgeGame.UI.Smithy
{
    /// <summary>Shows "[E] &lt;action&gt;" while the player stands by an interactable.</summary>
    public class InteractionPromptController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string keyHint = "E";

        private void Awake() => Hide();

        public void Show(string action)
        {
            if (root != null) root.SetActive(true);
            if (label != null) label.text = $"[{keyHint}]  {action}";
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
