using TMPro;
using UnityEngine;

namespace ForgeGame.Localization
{
    /// <summary>
    /// Binds a TMP text to a translation key: sets the text on enable and re-applies whenever
    /// the language changes. The scene builder attaches this (via <see cref="SetKey"/>) to every
    /// static label/button so switching languages updates all baked text instantly.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            Loc.LocaleChanged += Apply;
            Apply();
        }

        private void OnDisable() => Loc.LocaleChanged -= Apply;

        public void SetKey(string newKey)
        {
            key = newKey;
            if (_text == null) _text = GetComponent<TMP_Text>();
            Apply();
        }

        private void Apply()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (_text != null && !string.IsNullOrEmpty(key)) _text.text = Loc.Tr(key);
        }
    }
}
