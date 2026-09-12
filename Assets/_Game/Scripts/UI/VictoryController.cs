using AbyssHunter.Combat;
using AbyssHunter.Enemy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.UI
{
    public sealed class VictoryController : MonoBehaviour
    {
        [SerializeField] private EnemyWaveSpawner waveSpawner;
        [SerializeField] private Health playerHealth;
        [SerializeField] private GameObject victoryPanel;

        private bool isVictory;

        private void Awake()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (waveSpawner != null)
            {
                waveSpawner.AllWavesCompleted += ShowVictory;
            }
        }

        private void OnDisable()
        {
            if (waveSpawner != null)
            {
                waveSpawner.AllWavesCompleted -= ShowVictory;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (!isVictory || keyboard == null)
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

        private void ShowVictory()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                return;
            }

            if (!MatchResult.TryEnd())
            {
                return;
            }

            isVictory = true;

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            Time.timeScale = 0f;
        }
    }
}
