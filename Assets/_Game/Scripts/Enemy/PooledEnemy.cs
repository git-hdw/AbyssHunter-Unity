using System;
using System.Collections;
using AbyssHunter.Combat;
using UnityEngine;

namespace AbyssHunter.Enemy
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyController))]
    public sealed class PooledEnemy : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float deathReleaseDelay = 1f;

        private Health health;
        private Action<PooledEnemy> releaseAction;
        private Coroutine releaseCoroutine;

        public EnemyController Controller { get; private set; }
        public event Action<PooledEnemy> Defeated;

        private void Awake()
        {
            health = GetComponent<Health>();
            Controller = GetComponent<EnemyController>();
            health.SetDestroyOnDeath(false);
        }

        private void OnEnable()
        {
            health.Died += BeginRelease;
            health.RestoreFullHealth();
            Controller.enabled = true;
        }

        private void OnDisable()
        {
            health.Died -= BeginRelease;

            if (releaseCoroutine != null)
            {
                StopCoroutine(releaseCoroutine);
                releaseCoroutine = null;
            }
        }

        public void Initialize(Action<PooledEnemy> onRelease)
        {
            releaseAction = onRelease;
        }

        private void BeginRelease()
        {
            Controller.enabled = false;
            releaseCoroutine = StartCoroutine(ReleaseAfterDelay());
        }

        private IEnumerator ReleaseAfterDelay()
        {
            yield return new WaitForSeconds(deathReleaseDelay);

            Defeated?.Invoke(this);
            gameObject.SetActive(false);
            releaseAction?.Invoke(this);
        }
    }
}
