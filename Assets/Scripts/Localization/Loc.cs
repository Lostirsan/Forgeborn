using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ForgeGame.Localization
{
    /// <summary>
    /// Thin synchronous facade over Unity Localization so the (code-generated) UI can pull
    /// translated strings by key without async/await or per-object components everywhere.
    /// Falls back to the key itself if the table/entry is missing, so nothing ever renders blank.
    /// </summary>
    public static class Loc
    {
        public const string Table = "GameText";

        /// <summary>Raised after the active language changes (re-bind your text here).</summary>
        public static event Action LocaleChanged;

        private static bool _hooked;

        private static void EnsureHook()
        {
            if (_hooked) return;
            _hooked = true;
            LocalizationSettings.SelectedLocaleChanged += _ => LocaleChanged?.Invoke();
        }

        /// <summary>Translate a key in the GameText table. Returns the key if unavailable.</summary>
        public static string Tr(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            EnsureHook();
            try
            {
                var s = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
                return string.IsNullOrEmpty(s) ? key : s;
            }
            catch
            {
                return key;
            }
        }

        /// <summary>Translate then string.Format with args (for "Готовность: {0}%" style strings).</summary>
        public static string Format(string key, params object[] args)
        {
            var s = Tr(key);
            try { return string.Format(s, args); }
            catch { return s; }
        }

        public static string CurrentCode =>
            LocalizationSettings.SelectedLocale != null
                ? LocalizationSettings.SelectedLocale.Identifier.Code
                : "";

        /// <summary>Switch language by locale code (en/es/hi/ru). Persists via the settings' selectors.</summary>
        public static void SetLocale(string code)
        {
            EnsureHook();
            foreach (var l in LocalizationSettings.AvailableLocales.Locales)
            {
                if (l.Identifier.Code == code)
                {
                    LocalizationSettings.SelectedLocale = l;
                    return;
                }
            }
        }
    }
}
