using System;
using UnityEngine;

namespace ForgeGame.Smithy.Shop
{
    /// <summary>
    /// A minimal visual customer: it walks horizontally to a target, then idles with
    /// a gentle bob, and can leave. No animations required, and it never changes
    /// scale so it reads correctly on the frontal street. Purely presentational —
    /// it holds no game state and is never written to the save file.
    /// </summary>
    public class CustomerView : MonoBehaviour
    {
        private enum State { Hidden, Walking, Idle }

        [SerializeField] private Transform body;
        [SerializeField] private Transform shadow;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed = 3.5f;

        private State _state = State.Hidden;
        private Vector3 _target;
        private Action _onArrive;
        private float _bobTime;
        private float _bodyBaseY;

        public bool IsIdle => _state == State.Idle;

        private void Awake()
        {
            if (body != null) _bodyBaseY = body.localPosition.y;
        }

        public void Hide()
        {
            _state = State.Hidden;
            gameObject.SetActive(false);
        }

        public void SpawnAt(Vector3 worldPos)
        {
            transform.position = worldPos;
            gameObject.SetActive(true);
            _state = State.Idle;
        }

        public void WalkTo(Vector3 worldPos, Action onArrive)
        {
            _target = new Vector3(worldPos.x, transform.position.y, transform.position.z);
            _onArrive = onArrive;
            _state = State.Walking;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_state == State.Walking)
            {
                Vector3 p = transform.position;
                p.x = Mathf.MoveTowards(p.x, _target.x, moveSpeed * Time.deltaTime);
                transform.position = p;

                if (Mathf.Abs(p.x - _target.x) < 0.02f)
                {
                    _state = State.Idle;
                    var cb = _onArrive;
                    _onArrive = null;
                    cb?.Invoke();
                }
            }

            if (body != null && _state != State.Hidden)
            {
                _bobTime += Time.deltaTime * bobSpeed;
                var lp = body.localPosition;
                lp.y = _bodyBaseY + Mathf.Sin(_bobTime) * bobAmplitude;
                body.localPosition = lp;
            }
        }
    }
}
