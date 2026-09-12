using AbyssHunter.Enemy;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssHunter.UI
{
    [RequireComponent(typeof(Text))]
    public sealed class WaveStatusUI : MonoBehaviour
    {
        [SerializeField] private EnemyWaveSpawner waveSpawner;

        private Text statusText;
        private bool allWavesCompleted;

        private void Awake()
        {
            statusText = GetComponent<Text>();
        }

        private void OnEnable()
        {
            if (waveSpawner == null)
            {
                return;
            }

            waveSpawner.WaveStarted += HandleWaveStarted;
            waveSpawner.RemainingEnemiesChanged += HandleRemainingChanged;
            waveSpawner.AllWavesCompleted += HandleAllWavesCompleted;
        }

        private void Start()
        {
            RefreshText();
        }

        private void OnDisable()
        {
            if (waveSpawner == null)
            {
                return;
            }

            waveSpawner.WaveStarted -= HandleWaveStarted;
            waveSpawner.RemainingEnemiesChanged -= HandleRemainingChanged;
            waveSpawner.AllWavesCompleted -= HandleAllWavesCompleted;
        }

        private void HandleWaveStarted(int currentWave, int totalWaves)
        {
            RefreshText();
        }

        private void HandleRemainingChanged(int remainingEnemies)
        {
            RefreshText();
        }

        private void HandleAllWavesCompleted()
        {
            allWavesCompleted = true;
            statusText.text = "全部波次完成！";
        }

        private void RefreshText()
        {
            if (waveSpawner == null)
            {
                statusText.text = "Wave Spawner 未配置";
                return;
            }

            if (allWavesCompleted)
            {
                statusText.text = "全部波次完成！";
                return;
            }

            if (waveSpawner.CurrentWave == 0)
            {
                statusText.text = "准备战斗...";
                return;
            }

            statusText.text =
                $"第 {waveSpawner.CurrentWave}/{waveSpawner.TotalWaves} 波  " +
                $"剩余敌人：{waveSpawner.ActiveEnemies}";
        }
    }
}
