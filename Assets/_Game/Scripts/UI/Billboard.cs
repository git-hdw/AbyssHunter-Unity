using UnityEngine;

namespace AbyssHunter.UI
{
    public sealed class Billboard : MonoBehaviour
    {
        private Transform cameraTransform;

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform != null)
            {
                transform.rotation = cameraTransform.rotation;
            }
        }
    }
}
