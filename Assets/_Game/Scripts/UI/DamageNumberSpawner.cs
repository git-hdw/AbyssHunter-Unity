using AbyssHunter.Combat;
using UnityEngine;

namespace AbyssHunter.UI
{
    [RequireComponent(typeof(Health))]
    public sealed class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new(0f, 2.4f, 0f);
        [SerializeField, Min(0f)] private float randomHorizontalOffset = 0.25f;
        [SerializeField] private Color damageColor = new(1f, 0.75f, 0.15f, 1f);
        [SerializeField] private Color healingColor = new(0.25f, 1f, 0.35f, 1f);
        [SerializeField, Min(0f)] private float riseSpeed = 1.2f;
        [SerializeField, Min(0.01f)] private float lifetime = 0.8f;

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.DamageTaken += ShowDamage;
            health.Healed += ShowHealing;
        }

        private void OnDisable()
        {
            health.DamageTaken -= ShowDamage;
            health.Healed -= ShowHealing;
        }

        private void ShowDamage(int damage)
        {
            ShowValue(-damage, damageColor);
        }

        private void ShowHealing(int healing)
        {
            ShowValue(healing, healingColor);
        }

        private void ShowValue(int value, Color color)
        {
            Vector2 randomOffset =
                Random.insideUnitCircle * randomHorizontalOffset;
            Vector3 position = transform.position + worldOffset;
            position += new Vector3(randomOffset.x, 0f, randomOffset.y);

            DamageNumber.Create(
                value,
                position,
                color,
                riseSpeed,
                lifetime);
        }
    }
}
