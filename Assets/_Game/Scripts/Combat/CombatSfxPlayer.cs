using UnityEngine;

namespace AbyssHunter.Combat
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(PlayerMeleeAttack))]
    [RequireComponent(typeof(Health))]
    public sealed class CombatSfxPlayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

        private AudioSource audioSource;
        private PlayerMeleeAttack meleeAttack;
        private Health health;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            meleeAttack = GetComponent<PlayerMeleeAttack>();
            health = GetComponent<Health>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = volume;
        }

        private void OnEnable()
        {
            meleeAttack.Attacked += PlaySwing;
            meleeAttack.BoltReleased += PlayBowstring;
            meleeAttack.HitConfirmed += PlayHit;
            health.Damaged += PlayHurt;
        }

        private void OnDisable()
        {
            meleeAttack.Attacked -= PlaySwing;
            meleeAttack.BoltReleased -= PlayBowstring;
            meleeAttack.HitConfirmed -= PlayHit;
            health.Damaged -= PlayHurt;
        }

        private void PlaySwing()
        {
            if (!meleeAttack.UsesCrossbow) CombatSfx.PlaySwing(audioSource);
        }

        private void PlayBowstring() => CombatSfx.PlayBowstring(audioSource);

        private void PlayHit(bool killedTarget)
        {
            if (killedTarget)
            {
                CombatSfx.PlayKill(audioSource);
                return;
            }

            if (meleeAttack.LastHitStaggeredHeavy) CombatSfx.PlayCounter(audioSource);
            else CombatSfx.PlayHit(audioSource);
        }

        private void PlayHurt()
        {
            CombatSfx.PlayHurt(audioSource);
        }
    }
}
