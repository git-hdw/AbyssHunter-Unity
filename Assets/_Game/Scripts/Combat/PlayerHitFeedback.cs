using System.Collections;
using AbyssHunter.Character;
using UnityEngine;

namespace AbyssHunter.Combat
{
    [RequireComponent(typeof(PlayerMeleeAttack))]
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [Header("Hit Stop")]
        [SerializeField, Min(0f)] private float normalHitStopDuration = 0.04f;
        [SerializeField, Min(0f)] private float killHitStopDuration = 0.07f;
        [SerializeField, Range(0.01f, 1f)] private float hitStopTimeScale = 0.05f;

        [Header("Camera Shake")]
        [SerializeField, Min(0f)] private float normalShakeDuration = 0.1f;
        [SerializeField, Min(0f)] private float normalShakeIntensity = 0.08f;
        [SerializeField, Min(0f)] private float killShakeDuration = 0.16f;
        [SerializeField, Min(0f)] private float killShakeIntensity = 0.13f;

        private PlayerMeleeAttack meleeAttack;
        private TopDownCamera topDownCamera;
        private Coroutine hitStopCoroutine;
        private float timeScaleBeforeHitStop = 1f;

        private void Awake()
        {
            meleeAttack = GetComponent<PlayerMeleeAttack>();
            FindCamera();
        }

        private void OnEnable()
        {
            meleeAttack.HitConfirmed += PlayFeedback;
        }

        private void OnDisable()
        {
            meleeAttack.HitConfirmed -= PlayFeedback;

            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
                hitStopCoroutine = null;
                RestoreTimeScale();
            }
        }

        private void PlayFeedback(bool killedTarget)
        {
            if (topDownCamera == null)
            {
                FindCamera();
            }

            topDownCamera?.Shake(
                killedTarget ? killShakeDuration : normalShakeDuration,
                killedTarget ? killShakeIntensity : normalShakeIntensity);

            if (hitStopCoroutine == null && Time.timeScale > 0f)
            {
                float duration = killedTarget
                    ? killHitStopDuration
                    : normalHitStopDuration;
                hitStopCoroutine = StartCoroutine(HitStop(duration));
            }
        }

        private IEnumerator HitStop(float duration)
        {
            if (duration <= 0f)
            {
                hitStopCoroutine = null;
                yield break;
            }

            timeScaleBeforeHitStop = Time.timeScale;
            Time.timeScale = hitStopTimeScale;

            yield return new WaitForSecondsRealtime(duration);

            RestoreTimeScale();
            hitStopCoroutine = null;
        }

        private void RestoreTimeScale()
        {
            if (Mathf.Approximately(Time.timeScale, hitStopTimeScale))
            {
                Time.timeScale = timeScaleBeforeHitStop;
            }
        }

        private void FindCamera()
        {
            if (Camera.main != null)
            {
                topDownCamera = Camera.main.GetComponent<TopDownCamera>();
            }
        }
    }
}
