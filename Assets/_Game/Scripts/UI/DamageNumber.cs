using UnityEngine;

namespace AbyssHunter.UI
{
    public sealed class DamageNumber : MonoBehaviour
    {
        private TextMesh textMesh;
        private Transform cameraTransform;
        private Color startColor;
        private float riseSpeed;
        private float lifetime;
        private float elapsed;

        public static void Create(
            int value,
            Vector3 position,
            Color color,
            float riseSpeed,
            float lifetime)
        {
            GameObject instance = new($"FloatingValue_{value}");
            instance.transform.position = position;

            TextMesh text = instance.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            text.text = value.ToString("+0;-0;0");
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = 0.08f;
            text.color = color;
            text.font = font;

            MeshRenderer renderer = text.GetComponent<MeshRenderer>();
            if (font != null)
            {
                renderer.sharedMaterial = font.material;
            }

            renderer.sortingOrder = 100;

            DamageNumber number = instance.AddComponent<DamageNumber>();
            number.Initialize(text, riseSpeed, lifetime);
        }

        private void Initialize(TextMesh text, float speed, float duration)
        {
            textMesh = text;
            riseSpeed = speed;
            lifetime = Mathf.Max(0.01f, duration);
            startColor = text.color;

            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            if (cameraTransform != null)
            {
                transform.rotation = cameraTransform.rotation;
            }

            float progress = Mathf.Clamp01(elapsed / lifetime);
            Color color = startColor;
            color.a = 1f - progress;
            textMesh.color = color;

            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
