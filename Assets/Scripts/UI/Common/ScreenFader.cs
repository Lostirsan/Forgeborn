using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForgeGame.UI.Common
{
    /// <summary>
    /// A full-screen black overlay used for menu fade-in and scene transitions.
    /// It drives a <see cref="CanvasGroup"/> alpha with a coroutine (no external
    /// tween libraries) and blocks input while a transition is in progress.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.6f;

        private Coroutine _routine;

        /// <summary>True while a fade or a scene load is running; used to block interaction.</summary>
        public bool IsBusy { get; private set; }

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            // Start fully opaque so the very first frame is covered, then fade in.
            SetAlpha(1f);
        }

        private void SetAlpha(float a)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = a;
            canvasGroup.blocksRaycasts = a > 0.001f;
            canvasGroup.interactable = false;
        }

        /// <summary>Fades from black to fully transparent, revealing the menu.</summary>
        public void FadeIn(Action onComplete = null)
        {
            StartFade(0f, onComplete);
        }

        /// <summary>Fades to solid black.</summary>
        public void FadeOut(Action onComplete = null)
        {
            StartFade(1f, onComplete);
        }

        private void StartFade(float target, Action onComplete)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeRoutine(target, onComplete));
        }

        private IEnumerator FadeRoutine(float target, Action onComplete)
        {
            IsBusy = true;
            if (canvasGroup == null)
            {
                IsBusy = false;
                onComplete?.Invoke();
                yield break;
            }

            // Cover the screen for any target > 0 immediately (raycast block).
            canvasGroup.blocksRaycasts = true;

            float start = canvasGroup.alpha;
            float t = 0f;
            float duration = Mathf.Max(0.01f, fadeDuration);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }

            SetAlpha(target);
            IsBusy = false;
            _routine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Fades out, then asynchronously loads a scene. If the scene is not in the
        /// build settings, it logs an error, fades back in and invokes
        /// <paramref name="onInvalidScene"/> instead of throwing.
        /// </summary>
        public void LoadSceneWithFade(string sceneName, Action onInvalidScene = null)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(LoadRoutine(sceneName, onInvalidScene));
        }

        private IEnumerator LoadRoutine(string sceneName, Action onInvalidScene)
        {
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[ForgeGame] Scene '{sceneName}' is not in Build Settings and cannot be loaded.");
                onInvalidScene?.Invoke();
                yield break;
            }

            IsBusy = true;
            yield return FadeRoutine(1f, null);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[ForgeGame] Failed to start loading scene '{sceneName}'.");
                IsBusy = false;
                FadeIn();
                yield break;
            }

            while (!op.isDone)
                yield return null;
        }
    }
}
