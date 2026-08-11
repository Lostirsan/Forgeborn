using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Small transient toast for feedback ("Слиток создан", "Подземелье пока
    /// недоступно"). Messages queue and fade; safe to call every frame.
    /// </summary>
    public class NotificationController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text text;
        [SerializeField] private float holdSeconds = 2.2f;
        [SerializeField] private float fadeSeconds = 0.4f;

        private readonly Queue<string> _queue = new Queue<string>();
        private Coroutine _routine;

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
        }

        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _queue.Enqueue(message);
            _routine ??= StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (_queue.Count > 0)
            {
                string msg = _queue.Dequeue();
                if (text != null) text.text = msg;

                yield return Fade(0f, 1f);
                yield return new WaitForSecondsRealtime(holdSeconds);
                yield return Fade(1f, 0f);
            }
            _routine = null;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (group == null) yield break;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
