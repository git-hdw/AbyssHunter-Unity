using UnityEngine;
using AbyssHunter.UI;

namespace AbyssHunter.Character
{
    public sealed class TopDownCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 10f, -8f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;
        [SerializeField] private bool environmentalFraming;
        private float openingTime;
        private Vector3 framedOffset;

        private Vector3 followVelocity;
        private Vector3 shakeOffset;
        private float shakeRemaining;
        private float shakeDuration;
        private float shakeElapsed;
        private float shakeIntensity;
        private float shakeRoll;

        private void Start()
        {
            if (target == null) return;
            framedOffset = environmentalFraming ? offset * .48f : offset;
            transform.position = target.position + framedOffset;
            transform.LookAt(target.position + Vector3.up);
        }

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

            Vector3 wantedOffset = offset;
            if (environmentalFraming)
            {
                var ui = GamePresentation.Current;
                bool intro = ui != null && ui.State == GamePresentation.ScreenState.Intro;
                if (intro)
                {
                    openingTime += GamePresentation.AnimationDelta;
                    wantedOffset *= Mathf.Lerp(.48f, 1, Mathf.SmoothStep(0, 1, openingTime / 2.6f));
                }
                else
                {
                    float z = target.position.z;
                    float bridge = Mathf.Max(1 - Mathf.Abs(z - 12) / 4, 1 - Mathf.Abs(z - 36) / 4);
                    float sanctum = Mathf.SmoothStep(0, 1, (z - 42) / 8);
                    wantedOffset += new Vector3(0, Mathf.Max(Mathf.Max(0, bridge) * 1.6f, sanctum * 1.8f), -sanctum * .8f);
                    if (ui != null && (ui.State == GamePresentation.ScreenState.Finishing || ui.State == GamePresentation.ScreenState.Result)) wantedOffset *= .85f;
                }
                framedOffset = Vector3.Lerp(framedOffset, wantedOffset, 1 - Mathf.Exp(-GamePresentation.AnimationDelta * 2.5f));
            }
            else framedOffset = offset;
            Vector3 desiredPosition = target.position + framedOffset;
            Vector3 basePosition = Vector3.SmoothDamp(
                transform.position - shakeOffset,
                desiredPosition,
                ref followVelocity,
                smoothTime,
                Mathf.Infinity,
                GamePresentation.AnimationDelta);

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
