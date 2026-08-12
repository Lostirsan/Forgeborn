using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// A recipe (mold) card in the foundry. Clicking it tells the foundry which weapon the
    /// player wants to cast, then the ore selection appears. The blueprint id is baked in by
    /// the scene builder so no per-card wiring is needed at runtime.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class FoundryRecipeButton : MonoBehaviour
    {
        [SerializeField] private string blueprintId;
        [SerializeField] private FoundryPanelController foundry;

        public string BlueprintId => blueprintId;

        public void Configure(string id, FoundryPanelController owner)
        {
            blueprintId = id;
            foundry = owner;
        }

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (foundry != null) foundry.SelectRecipe(blueprintId);
            });
        }
    }
}
