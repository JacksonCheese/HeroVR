using UnityEngine;

namespace HeroVR.Abilities
{
    [DisallowMultipleComponent]
    public sealed class TransformAimProvider : MonoBehaviour, IAimProvider
    {
        [SerializeField] private Transform originTransform;
        [SerializeField] private Transform directionTransform;

        public Vector3 Origin =>
            originTransform != null ? originTransform.position : transform.position;
        public Vector3 Direction =>
            directionTransform != null ? directionTransform.forward : transform.forward;
        public Vector3 Up =>
            directionTransform != null ? directionTransform.up : transform.up;

        public void Configure(Transform origin, Transform directionSource)
        {
            originTransform = origin;
            directionTransform = directionSource;
        }
    }
}
