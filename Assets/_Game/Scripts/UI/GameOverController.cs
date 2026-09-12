using System.Collections;
using AbyssHunter.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.UI
{
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField, Min(0f)] private float gameOverDelay = 0.9f;

        private bool isGameOver;
        private Coroutine showGameOverCoroutine;

        private void Awake()
        {
            MatchResult.Reset();
            Time.timeScale = 1f;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died += ShowGameOver;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= ShowGameOver;
            }

            if (showGameOverCoroutine != null)
            {
                StopCoroutine(showGameOverCoroutine);
                showGameOverCoroutine = null;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (!isGameOver || keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }

            if (keyboard.mKey.wasPressedThisFrame)
            {
                ReturnToMenu();
            }
        }

        public void RestartGame()
        {
            SceneFlow.LoadGame();
        }

        public void ReturnToMenu()
        {
            SceneFlow.LoadMenu();
        }

        private void ShowGameOver()
        {
            if (!MatchResult.TryEnd())
            {
                return;
            }

            isGameOver = true;
            showGameOverCoroutine = StartCoroutine(ShowGameOverAfterDelay());
        }

        private IEnumerator ShowGameOverAfterDelay()
        {
            yield return new WaitForSeconds(gameOverDelay);

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            Time.timeScale = 0f;
        }
    }
}
