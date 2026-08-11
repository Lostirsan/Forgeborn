using UnityEngine;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// Anything the player can interact with by pressing the interact key while in
    /// range. A single detector drives every interactable, so stations don't each
    /// need bespoke input code.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short hint shown in the interaction prompt (without the key).</summary>
        string PromptText { get; }

        /// <summary>Whether the interaction is currently allowed.</summary>
        bool CanInteract { get; }

        /// <summary>World transform, used to pick the nearest target.</summary>
        Transform Transform { get; }

        /// <summary>Performs the interaction.</summary>
        void Interact(SmithyController controller);
    }
}
