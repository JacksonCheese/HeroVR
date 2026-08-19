using UnityEngine;
using UnityEngine.InputSystem;
using HeroVR.Combat;
using HeroVR.Abilities;

namespace HeroVR.Prototype
{
    [RequireComponent(typeof(CharacterController), typeof(Damageable))]
    public class DesktopHeroController : MonoBehaviour
    {
        public float moveSpeed = 7f, jumpHeight = 2.6f, gravity = -22f;
        public float punchDamage = 22f, punchRange = 1.6f, punchRadius = .65f, punchForce = 9f;
        public float blastSpeed = 24f, dashDistance = 5f, smashRadius = 4f, smashDamage = 32f, smashForce = 14f;

        CharacterController controller;
        Damageable health;
        Camera cam;
        TrainingBot opponent;
        Vector3 verticalVelocity, spawn;
        float pitch, nextPunch, nextBlast, nextDash, nextSmash;

        public Damageable Health => health;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Damageable>();
            spawn = transform.position;

            GameObject c = new GameObject("HeroCamera");
            c.transform.SetParent(transform);
            c.transform.localPosition = new Vector3(0, 1.55f, 0);
            cam = c.AddComponent<Camera>();
            c.AddComponent<AudioListener>();
            c.tag = "MainCamera";
        }

        void Start() => LockCursor();
        public void SetOpponent(TrainingBot bot) => opponent = bot;

        void Update()
        {
            if (Keyboard.current == null || Mouse.current == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) LockCursor();
                return;
            }

            if (health.IsDead)
            {
                controller.enabled = false;
                transform.position = spawn;
                controller.enabled = true;
                health.ResetHealth();
                return;
            }

            Look();
            Move();

            if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextPunch) Punch();
            if (Mouse.current.rightButton.wasPressedThisFrame && Time.time >= nextBlast) Blast();
            if (Keyboard.current.leftShiftKey.wasPressedThisFrame && Time.time >= nextDash) Dash();
            if (Keyboard.current.eKey.wasPressedThisFrame && Time.time >= nextSmash) Smash();
        }

        void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Look()
        {
            Vector2 d = Mouse.current.delta.ReadValue() * .12f;
            transform.Rotate(Vector3.up * d.x);
            pitch = Mathf.Clamp(pitch - d.y, -85, 85);
            cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        void Move()
        {
            Vector2 i = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) i.y++;
            if (Keyboard.current.sKey.isPressed) i.y--;
            if (Keyboard.current.dKey.isPressed) i.x++;
            if (Keyboard.current.aKey.isPressed) i.x--;
            i = Vector2.ClampMagnitude(i, 1);

            if (controller.isGrounded && verticalVelocity.y < 0) verticalVelocity.y = -2;
            if (controller.isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity.y += gravity * Time.deltaTime;
            Vector3 horizontal = (transform.right * i.x + transform.forward * i.y) * moveSpeed;
            controller.Move((horizontal + verticalVelocity) * Time.deltaTime);
        }

        void Punch()
        {
            nextPunch = Time.time + .35f;
            Vector3 center = cam.transform.position + cam.transform.forward * punchRange;

            foreach (Collider hit in Physics.OverlapSphere(center, punchRadius))
            {
                if (hit.transform.root == transform.root) continue;
                Damageable d = hit.GetComponentInParent<Damageable>();
                if (d == null) continue;

                d.TakeDamage(punchDamage);
                Rigidbody rb = hit.GetComponentInParent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                    rb.AddForce(cam.transform.forward * punchForce, ForceMode.Impulse);
                break;
            }
        }

        void Blast()
        {
            nextBlast = Time.time + .55f;
            Vector3 dir = cam.transform.forward;

            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "EnergyBlast";
            p.transform.position = cam.transform.position + dir * 1.1f;
            p.transform.localScale = Vector3.one * .32f;
            p.GetComponent<Renderer>().material.color = new Color(.15f, .75f, 1f);

            Rigidbody rb = p.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            EnergyProjectile projectile = p.AddComponent<EnergyProjectile>();
            projectile.Launch(dir * blastSpeed);
        }

        void Dash()
        {
            nextDash = Time.time + 1.5f;
            controller.Move(transform.forward * dashDistance);
        }

        void Smash()
        {
            nextSmash = Time.time + 4f;

            foreach (Collider hit in Physics.OverlapSphere(transform.position, smashRadius))
            {
                if (hit.transform.root == transform.root) continue;

                Damageable d = hit.GetComponentInParent<Damageable>();
                if (d != null) d.TakeDamage(smashDamage);

                Rigidbody rb = hit.GetComponentInParent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 dir = (rb.worldCenterOfMass - transform.position).normalized;
                    dir.y = Mathf.Max(dir.y, .35f);
                    rb.AddForce(dir.normalized * smashForce, ForceMode.Impulse);
                }
            }
        }

        void OnGUI()
        {
            GUI.Box(new Rect(18, 18, 330, 150), "HERO VR — PROTOTYPE 0.1");
            GUI.Label(new Rect(32, 48, 300, 22), $"Health: {Mathf.CeilToInt(health.CurrentHealth)}");
            if (opponent != null)
                GUI.Label(new Rect(32, 70, 300, 22), $"Enemy: {Mathf.CeilToInt(opponent.Health.CurrentHealth)}");
            GUI.Label(new Rect(32, 96, 300, 60),
                "WASD Move | Mouse Look | Space Super Jump\nLMB Punch | RMB Blast | E Super Smash\nShift Dash | Esc Release Mouse");
            GUI.Label(new Rect(Screen.width/2f - 5, Screen.height/2f - 12, 20, 25), "+");
        }
    }
}