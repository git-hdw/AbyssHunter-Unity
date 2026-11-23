using UnityEngine;

namespace AbyssHunter.Combat
{
    public enum HunterWeapon { Sword, Axe, Crossbow }

    public sealed class HunterWeapons : MonoBehaviour
    {
        [SerializeField] private GameObject sword;
        [SerializeField] private GameObject axe;
        [SerializeField] private GameObject crossbow;
        [SerializeField] private GameObject bolt;
        [SerializeField] private Transform supportGrip;
        private static readonly int SweepState = Animator.StringToHash("Base Layer.AxeSweep");
        private Animator animator;
        public bool UsesCrossbow => crossbow && crossbow.activeSelf;
        internal TrailRenderer BoltTrail { get; private set; }
        private void Awake()
        {
            var equipped = AbyssHunter.UI.SceneFlow.RunWeapon;
            axe.SetActive(equipped == HunterWeapon.Axe);
            if (crossbow) crossbow.SetActive(equipped == HunterWeapon.Crossbow);
            sword.SetActive(!UsesAxe && !UsesCrossbow);
            animator = GetComponent<Animator>();
            if (bolt) BoltTrail = bolt.GetComponent<TrailRenderer>();
        }

        private void LateUpdate()
        {
            if (!UsesAxe || !supportGrip || !animator) return;
            float weight = animator.GetCurrentAnimatorStateInfo(0).fullPathHash == SweepState ? 1f : 0f;
            if (animator.IsInTransition(0))
            {
                float next = animator.GetNextAnimatorStateInfo(0).fullPathHash == SweepState ? 1f : 0f;
                weight = Mathf.Lerp(weight, next, animator.GetAnimatorTransitionInfo(0).normalizedTime);
            }
            // The axe pivots at the right palm. Its shaft follows the second grip during the sweep.
            var handle = axe.transform;
            var grip = handle.parent.InverseTransformDirection(supportGrip.position - handle.position);
            handle.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, grip), weight);
        }

        internal Transform AxeHandle => axe.transform;
        internal Transform SupportGrip => supportGrip;
        internal GameObject Bolt => bolt;
        internal Vector3 Muzzle => crossbow.transform.TransformPoint(new Vector3(0, .08f, .90f));
        internal Vector3 BarrelDirection => crossbow.transform.forward;
        public bool UsesAxe => axe.activeSelf;
    }
}
