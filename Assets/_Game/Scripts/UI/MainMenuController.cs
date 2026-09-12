using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private void Awake()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame)
            {
                StartGame();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                QuitGame();
            }
        }

        public void StartGame()
        {
            SceneFlow.LoadGame();
        }

        public void QuitGame()
        {
            SceneFlow.Quit();
        }
    }
}
