using AbyssHunter.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssHunter.UI
{
    [RequireComponent(typeof(Slider))]
    public sealed class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Health targetHealth;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (targetHealth != null)
            {
                targetHealth.HealthChanged += UpdateValue;
                UpdateValue(targetHealth.CurrentHealth, targetHealth.MaximumHealth);
            }
        }

        private void OnDisable()
        {
            if (targetHealth != null)
            {
                targetHealth.HealthChanged -= UpdateValue;
            }
        }

        private void UpdateValue(int currentHealth, int maximumHealth)
        {
            slider.minValue = 0f;
            slider.maxValue = maximumHealth;
            slider.value = currentHealth;
        }
    }
}
