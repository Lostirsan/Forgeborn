using System;
using UnityEngine;

namespace ForgeGame.Smithy
{
    /// <summary>
    /// Owns the single active <see cref="ForgeSession"/> and broadcasts changes.
    /// Stage controllers read and mutate the session through this object rather than
    /// holding their own copies, which keeps the multi-station flow consistent and
    /// makes persistence a matter of exporting one object.
    /// </summary>
    public class ForgeSessionController : MonoBehaviour
    {
        private ForgeSession _current;

        public ForgeSession Current => _current;

        public bool HasActiveSession =>
            _current != null &&
            _current.currentStage != ForgeStage.None &&
            _current.currentStage != ForgeStage.Completed &&
            _current.currentStage != ForgeStage.Failed;

        public event Action<ForgeSession> SessionChanged;
        public event Action<ForgeStage> StageChanged;

        public ForgeSession StartNewSession()
        {
            _current = ForgeSession.CreateNew();
            SessionChanged?.Invoke(_current);
            return _current;
        }

        public void SetStage(ForgeStage stage)
        {
            if (_current == null) return;
            _current.currentStage = stage;
            StageChanged?.Invoke(stage);
            SessionChanged?.Invoke(_current);
        }

        public void ClearSession()
        {
            _current = null;
            SessionChanged?.Invoke(null);
        }

        public void RaiseChanged()
        {
            SessionChanged?.Invoke(_current);
        }

        // ---- Persistence ----

        public ForgeSession Export() => _current;

        public void LoadFrom(ForgeSession session)
        {
            _current = session;
            SessionChanged?.Invoke(_current);
            if (_current != null) StageChanged?.Invoke(_current.currentStage);
        }
    }
}
