using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssHunter.Combat
{
    [RequireComponent(typeof(Health))]
    public sealed class DamageFlash : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;

        private readonly List<RendererEntry> rendererEntries = new();
        private Health health;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            CacheRenderers();
        }

        private void OnEnable()
        {
            health.Damaged += PlayFlash;
        }

        private void OnDisable()
        {
            health.Damaged -= PlayFlash;

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }

            RestoreColors();
        }

        private void CacheRenderers()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer targetRenderer in renderers)
            {
                Material material = targetRenderer.sharedMaterial;
                if (material == null)
                {
                    continue;
                }

                rendererEntries.Add(new RendererEntry
                {
                    Renderer = targetRenderer,
                    PropertyBlock = new MaterialPropertyBlock(),
                    BaseColor = material.HasProperty(BaseColorId)
                        ? material.GetColor(BaseColorId)
                        : Color.white,
                    Color = material.HasProperty(ColorId)
                        ? material.GetColor(ColorId)
                        : Color.white
                });
            }
        }

        private void PlayFlash()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            ApplyFlashColor();
            flashCoroutine = StartCoroutine(RestoreAfterDelay());
        }

        private IEnumerator RestoreAfterDelay()
        {
            yield return new WaitForSecondsRealtime(flashDuration);
            RestoreColors();
            flashCoroutine = null;
        }

        private void ApplyFlashColor()
        {
            foreach (RendererEntry entry in rendererEntries)
            {
                entry.Renderer.GetPropertyBlock(entry.PropertyBlock);
                entry.PropertyBlock.SetColor(BaseColorId, flashColor);
                entry.PropertyBlock.SetColor(ColorId, flashColor);
                entry.Renderer.SetPropertyBlock(entry.PropertyBlock);
            }
        }

        private void RestoreColors()
        {
            foreach (RendererEntry entry in rendererEntries)
            {
                if (entry.Renderer == null)
                {
                    continue;
                }

                entry.Renderer.GetPropertyBlock(entry.PropertyBlock);
                entry.PropertyBlock.SetColor(BaseColorId, entry.BaseColor);
                entry.PropertyBlock.SetColor(ColorId, entry.Color);
                entry.Renderer.SetPropertyBlock(entry.PropertyBlock);
            }
        }

        private sealed class RendererEntry
        {
            public Renderer Renderer;
            public MaterialPropertyBlock PropertyBlock;
            public Color BaseColor;
            public Color Color;
        }
    }
}
