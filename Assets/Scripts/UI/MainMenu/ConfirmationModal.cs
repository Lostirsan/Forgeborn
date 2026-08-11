using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeGame.UI.MainMenu
{
    /// <summary>
    /// A reusable yes/no dialog shown on top of the menu (new game, quit, ...).
    /// The caller supplies the message and callbacks through <see cref="Show"/>;
    /// the modal handles button wiring and default focus.
    /// </summary>
    public class ConfirmationModal : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private TMP_Text cancelLabel;

        private Action _onConfirm;
        private Action _onCancel;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(HandleCancel);
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(HandleConfirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(HandleCancel);
        }

        /// <summary>Opens the modal with the given text and callbacks.</summary>
        public void Show(string message, string confirmText, string cancelText,
            Action onConfirm, Action onCancel = null)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            if (messageText != null) messageText.text = message;
            if (confirmLabel != null && !string.IsNullOrEmpty(confirmText)) confirmLabel.text = confirmText;
            if (cancelLabel != null && !string.IsNullOrEmpty(cancelText)) cancelLabel.text = cancelText;

            if (root != null) root.SetActive(true);

            // Default focus on Cancel so an accidental confirm is less likely.
            if (cancelButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
        }

        /// <summary>Closes the modal without invoking any callback.</summary>
        public void Hide()
        {
            if (root != null) root.SetActive(false);
            _onConfirm = null;
            _onCancel = null;
        }

        /// <summary>Equivalent to pressing Cancel; used by the Escape key.</summary>
        public void CancelFromExternal()
        {
            HandleCancel();
        }

        private void HandleConfirm()
        {
            var cb = _onConfirm;
            Hide();
            cb?.Invoke();
        }

        private void HandleCancel()
        {
            var cb = _onCancel;
            Hide();
            cb?.Invoke();
        }
    }
}
