using System;
using UnityEngine;
using HeroVR.Combat;

namespace HeroVR.Abilities
{
    public abstract class HeroAbility : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float cooldown = .5f;

        private Damageable ownerHealth;
        private RespawnOnDeath respawn;
        private float nextReadyTime;

        public bool IsReady => Time.time >= nextReadyTime;
        public float Cooldown => cooldown;
        public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);
        public float NormalizedCooldown => cooldown <= 0f
            ? 0f
            : Mathf.Clamp01(CooldownRemaining / cooldown);

        public event Action<HeroAbility> Activated;

        protected GameObject Owner => ownerHealth != null
            ? ownerHealth.gameObject
            : transform.root.gameObject;

        protected virtual void Awake()
        {
            ownerHealth = GetComponentInParent<Damageable>();
            respawn = GetComponentInParent<RespawnOnDeath>();
        }

        protected virtual void OnEnable()
        {
            if (ownerHealth != null)
                ownerHealth.Died += HandleOwnerDied;
            if (respawn != null)
                respawn.Respawned += HandleOwnerRespawned;
        }

        protected virtual void OnDisable()
        {
            if (ownerHealth != null)
                ownerHealth.Died -= HandleOwnerDied;
            if (respawn != null)
                respawn.Respawned -= HandleOwnerRespawned;
            CancelActiveState();
        }

        public bool TryActivate()
        {
            if (!isActiveAndEnabled || !IsReady ||
                (ownerHealth != null && ownerHealth.IsDead) ||
                !CanActivate())
            {
                return false;
            }

            if (!Activate())
                return false;

            nextReadyTime = Time.time + cooldown;
            Activated?.Invoke(this);
            return true;
        }

        public void SetCooldown(float duration)
        {
            cooldown = Mathf.Max(0f, duration);
        }

        public void ResetCooldown()
        {
            nextReadyTime = 0f;
        }

        protected virtual bool CanActivate()
        {
            return true;
        }

        protected abstract bool Activate();

        protected virtual void CancelActiveState()
        {
        }

        private void HandleOwnerDied()
        {
            CancelActiveState();
        }

        private void HandleOwnerRespawned()
        {
            CancelActiveState();
            ResetCooldown();
        }

        protected virtual void OnValidate()
        {
            cooldown = Mathf.Max(0f, cooldown);
        }
    }
}
