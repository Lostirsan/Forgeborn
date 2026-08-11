using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Exposes whether a pointer is currently held down on this UI element. Used for
    /// "hold to heat" and "hold to pour" in the foundry. Reliable with the Input
    /// System UI module and works for mouse, touch and gamepad submit-drag.
    /// </summary>
    public class PointerHoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IDragHandler
    {
        public bool IsHeld { get; private set; }
        public float HeldDuration { get; private set; }
        public float HorizontalDrag01 { get; private set; }
        public event Action Down;
        public event Action Up;

        private RectTransform _rectTransform;
        private Vector2 _lastLocalPointer;

        private void Awake() => _rectTransform = transform as RectTransform;

        private void Update()
        {
            if (IsHeld) HeldDuration += Time.unscaledDeltaTime;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            HeldDuration = 0f;
            HorizontalDrag01 = 0f;
            UpdateLocalPointer(eventData, out _lastLocalPointer);
            Down?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsHeld || _rectTransform == null) return;
            if (!UpdateLocalPointer(eventData, out Vector2 localPointer)) return;

            float width = Mathf.Max(1f, _rectTransform.rect.width);
            HorizontalDrag01 = Mathf.Clamp01(HorizontalDrag01 + (localPointer.x - _lastLocalPointer.x) / width * 1.8f);
            _lastLocalPointer = localPointer;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();
        private void OnDisable() => Release();

        private void Release()
        {
            if (!IsHeld) return;
            IsHeld = false;
            HeldDuration = 0f;
            HorizontalDrag01 = 0f;
            Up?.Invoke();
        }

        private bool UpdateLocalPointer(PointerEventData eventData, out Vector2 localPointer)
        {
            if (_rectTransform == null)
            {
                localPointer = Vector2.zero;
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, eventData.position, eventData.pressEventCamera, out localPointer);
        }
    }
}
