using UnityEngine;

namespace AbyssHunter.Character
{
    public sealed class TopDownCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 10f, -8f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;

        private Vector3 followVelocity;
        private Vector3 shakeOffset;
        private float shakeRemaining;
        private float shakeDuration;
        private float shakeElapsed;
        private float shakeIntensity;
        private float shakeRoll;

        public void Shake(float duration, float intensity)
        {
            if (duration >= shakeRemaining || intensity >= shakeIntensity)
            {
                shakeRemaining = duration;
                shakeDuration = Mathf.Max(0.01f, duration);
                shakeElapsed = 0f;
                shakeIntensity = intensity;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            Vector3 basePosition = Vector3.SmoothDamp(
                transform.position - shakeOffset,
                desiredPosition,
                ref followVelocity,
                smoothTime);

            UpdateShake();
            transform.position = basePosition + shakeOffset;
            transform.LookAt(target.position + Vector3.up);
            transform.Rotate(0f, 0f, shakeRoll, Space.Self);
        }

        private void UpdateShake()
        {
            if (shakeRemaining <= 0f)
            {
                shakeOffset = Vector3.zero;
                shakeIntensity = 0f;
                shakeRoll = 0f;
                return;
            }

            shakeRemaining -= Time.unscaledDeltaTime;
            shakeElapsed += Time.unscaledDeltaTime;

            float envelope = Mathf.Clamp01(shakeRemaining / shakeDuration);
            envelope *= envelope;

            float horizontalWave = Mathf.Sin(shakeElapsed * 52f);
            float verticalWave = Mathf.Sin(shakeElapsed * 43f + 1.4f);
            float rollWave = Mathf.Sin(shakeElapsed * 47f + 0.5f);

            shakeOffset =
                (transform.right * horizontalWave +
                 transform.up * verticalWave * 0.6f) *
                (shakeIntensity * envelope);
            shakeRoll = rollWave * shakeIntensity * 2.5f * envelope;
        }
    }
}
