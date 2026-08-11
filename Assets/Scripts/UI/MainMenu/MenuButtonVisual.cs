using ForgeGame.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeGame.UI.MainMenu
{
    /// <summary>
    /// Drives the visual state of a menu button: a dark translucent panel that
    /// grows a warm orange glow on hover / keyboard-or-gamepad selection, and
    /// shrinks slightly while pressed. It reads state from Unity's UI event
    /// interfaces so mouse, keyboard and gamepad all behave the same. The colour
    /// transitions are lerped every frame toward a target, which is cheap for the
    /// handful of buttons in a menu.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButtonVisual : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("Targets")]
        [SerializeField] private RectTransform scaleTarget;
        [SerializeField] private Image background;
        [SerializeField] private Image border;
        [SerializeField] private Graphic glow;
        [SerializeField] private TMP_Text label;

        [Header("Background colours")]
        [SerializeField] private Color normalBackground = new Color(0.07f, 0.06f, 0.05f, 0.72f);
        [SerializeField] private Color highlightBackground = new Color(0.16f, 0.11f, 0.07f, 0.85f);

        [Header("Border colours")]
        [SerializeField] private Color normalBorder = new Color(0.45f, 0.28f, 0.16f, 0.55f);
        [SerializeField] private Color highlightBorder = new Color(0.95f, 0.55f, 0.22f, 0.95f);

        [Header("Text colours")]
        [SerializeField] private Color normalText = new Color(0.86f, 0.80f, 0.68f, 1f);
        [SerializeField] private Color highlightText = new Color(1f, 0.93f, 0.78f, 1f);
        [SerializeField] private Color disabledText = new Color(0.55f, 0.50f, 0.44f, 0.6f);

        [Header("Animation")]
        [SerializeField] private float transitionSpeed = 12f;
        [SerializeField] private float pressedScale = 0.95f;

        [Header("Audio (optional)")]
        [SerializeField] private AudioManager audioManager;

        private Button _button;
        private bool _pointerInside;
        private bool _selected;
        private bool _pressed;
        private float _highlight; // 0..1 lerped weight toward the highlighted look
        private float _scale = 1f;

        private bool Highlighted => _button != null && _button.interactable && (_pointerInside || _selected);

        private void Awake()
        {
            _button = GetComponent<Button>();
            // We paint the visuals ourselves, so disable Unity's built-in colour tint.
            _button.transition = Selectable.Transition.None;
            if (scaleTarget == null) scaleTarget = transform as RectTransform;
            ApplyImmediate();
        }

        private void OnEnable()
        {
            _pointerInside = false;
            _selected = false;
            _pressed = false;
            ApplyImmediate();
        }

        private void Update()
        {
            float targetHighlight = Highlighted ? 1f : 0f;
            _highlight = Mathf.MoveTowards(_highlight, targetHighlight, transitionSpeed * Time.unscaledDeltaTime);

            float targetScale = (_pressed && _button.interactable) ? pressedScale : 1f;
            _scale = Mathf.MoveTowards(_scale, targetScale, transitionSpeed * Time.unscaledDeltaTime);

            Paint();
        }

        private void Paint()
        {
            bool interactable = _button == null || _button.interactable;

            if (background != null)
                background.color = Color.Lerp(normalBackground, highlightBackground, _highlight);

            if (border != null)
                border.color = Color.Lerp(normalBorder, highlightBorder, _highlight);

            if (glow != null)
            {
                Color c = glow.color;
                c.a = _highlight;
                glow.color = c;
            }

            if (label != null)
                label.color = interactable ? Color.Lerp(normalText, highlightText, _highlight) : disabledText;

            if (scaleTarget != null)
                scaleTarget.localScale = new Vector3(_scale, _scale, 1f);
        }

        private void ApplyImmediate()
        {
            _highlight = Highlighted ? 1f : 0f;
            _scale = 1f;
            Paint();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerInside = true;
            if (_button != null && _button.interactable)
            {
                // Move keyboard/gamepad focus to whatever the mouse hovers.
                EventSystem.current?.SetSelectedGameObject(gameObject);
                audioManager?.PlayHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerInside = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && _button.interactable)
                _pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            if (_button != null && _button.interactable)
                audioManager?.PlayHover();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            _pressed = false;
        }
    }
}
