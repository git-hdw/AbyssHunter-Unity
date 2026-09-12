using System;
using UnityEngine;

namespace AbyssHunter.Combat
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField, Min(0f)] private float destroyDelay = 1f;

        public int CurrentHealth { get; private set; }
        public int MaximumHealth => maximumHealth;
        public bool IsDead => CurrentHealth <= 0;
        public bool IsInvulnerable { get; private set; }

        public event Action<int, int> HealthChanged;
        public event Action Damaged;
        public event Action<int> DamageTaken;
        public event Action<int> Healed;
        public event Action Died;

        private void Awake()
        {
            CurrentHealth = maximumHealth;
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || IsDead || IsInvulnerable)
            {
                return;
            }

            int previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            int appliedDamage = previousHealth - CurrentHealth;
            HealthChanged?.Invoke(CurrentHealth, maximumHealth);
            Damaged?.Invoke();
            DamageTaken?.Invoke(appliedDamage);

            Debug.Log($"{name} 受到 {damage} 点伤害，剩余生命：{CurrentHealth}", this);

            if (IsDead)
            {
                Die();
            }
        }

        public void SetInvulnerable(bool isInvulnerable)
        {
            IsInvulnerable = isInvulnerable;
        }

        public bool Heal(int amount)
        {
            if (amount <= 0 || IsDead || CurrentHealth >= maximumHealth)
            {
                return false;
            }

            int previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(maximumHealth, CurrentHealth + amount);
            int restoredHealth = CurrentHealth - previousHealth;

            HealthChanged?.Invoke(CurrentHealth, maximumHealth);
            Healed?.Invoke(restoredHealth);
            Debug.Log(
                $"{name} 恢复 {restoredHealth} 点生命，当前生命：{CurrentHealth}",
                this);

            return restoredHealth > 0;
        }

        public void RestoreFullHealth()
        {
            IsInvulnerable = false;
            CurrentHealth = maximumHealth;
            HealthChanged?.Invoke(CurrentHealth, maximumHealth);
        }

        public void RestoreHealth(int health)
        {
            IsInvulnerable = false;
            CurrentHealth = Mathf.Clamp(health, 1, maximumHealth);
            HealthChanged?.Invoke(CurrentHealth, maximumHealth);
        }

        public void SetDestroyOnDeath(bool shouldDestroy)
        {
            destroyOnDeath = shouldDestroy;
        }

        private void Die()
        {
            Died?.Invoke();
            Debug.Log($"{name} 已死亡", this);

            if (destroyOnDeath)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }
}
