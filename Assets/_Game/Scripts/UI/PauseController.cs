using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.UI
{
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || MatchResult.HasEnded)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetPaused(!IsPaused);
            }

            if (IsPaused && keyboard.mKey.wasPressedThisFrame)
            {
                SceneFlow.LoadMenu();
            }
        }

        private void OnDisable()
        {
            if (IsPaused)
            {
                SetPaused(false);
            }
        }

        private void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(paused);
            }
        }
    }
}
