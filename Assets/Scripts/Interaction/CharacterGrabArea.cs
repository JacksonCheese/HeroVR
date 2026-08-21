using UnityEngine;

namespace HeroVR.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class CharacterGrabArea : MonoBehaviour
    {
        [SerializeField] private GrabbableCharacter character;
        [SerializeField] private Rigidbody preferredBody;

        public GrabbableCharacter Character => character;
        public Rigidbody PreferredBody => preferredBody;

        public void Configure(
            GrabbableCharacter owner,
            Rigidbody body = null)
        {
            character = owner;
            preferredBody = body;
        }

        private void Awake()
        {
            if (character == null)
                character = GetComponentInParent<GrabbableCharacter>();
        }
    }
}
