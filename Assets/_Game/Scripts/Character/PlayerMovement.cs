using System;
using AbyssHunter.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.Character
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float rotationSpeed = 12f;
        [SerializeField] private Transform cameraTransform;

        [Header("Dodge")]
        [SerializeField, Min(0f)] private float dodgeSpeed = 12f;
        [SerializeField, Min(0.01f)] private float dodgeDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float dodgeCooldown = 0.8f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedVelocity = -2f;

        [Header("Fall Recovery")]
        [SerializeField] private float fallThreshold = -10f;

        private CharacterController characterController;
        private Health health;
        private Vector3 spawnPosition;
        private Vector3 lastMoveDirection;
        private float verticalVelocity;
        private float dodgeEndTime;
        private float nextDodgeTime;
        private bool isDodging;

        public event Action DodgeStarted;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            spawnPosition = transform.position;
            lastMoveDirection = transform.forward;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (transform.position.y < fallThreshold)
            {
                RespawnAfterFall();
                return;
            }

            Vector2 input = ReadMovementInput();
            Vector3 moveDirection = GetCameraRelativeDirection(input);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                lastMoveDirection = moveDirection;
            }

            if (!isDodging && ReadDodgeInput() && Time.time >= nextDodgeTime)
            {
                StartDodge();
            }

            ApplyGravity();

            if (isDodging)
            {
                UpdateDodge();
                return;
            }

            Move(moveDirection, moveSpeed);
            Rotate(moveDirection);
        }

        private static Vector2 ReadMovementInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed) horizontal += 1f;
            if (keyboard.sKey.isPressed) vertical -= 1f;
            if (keyboard.wKey.isPressed) vertical += 1f;

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private static bool ReadDodgeInput()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (cameraTransform == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        private void ApplyGravity()
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedVelocity;
                return;
            }

            verticalVelocity += gravity * Time.deltaTime;
        }

        private void Move(Vector3 moveDirection, float speed)
        {
            Vector3 velocity = moveDirection * speed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void StartDodge()
        {
            isDodging = true;
            dodgeEndTime = Time.time + dodgeDuration;
            nextDodgeTime = Time.time + dodgeCooldown;
            health?.SetInvulnerable(true);
            DodgeStarted?.Invoke();
        }

        private void UpdateDodge()
        {
            Move(lastMoveDirection, dodgeSpeed);
            Rotate(lastMoveDirection);

            if (Time.time >= dodgeEndTime)
            {
                isDodging = false;
                health?.SetInvulnerable(false);
            }
        }

        private void OnDisable()
        {
            health?.SetInvulnerable(false);
        }

        private void RespawnAfterFall()
        {
            characterController.enabled = false;
            transform.position = spawnPosition;
            characterController.enabled = true;

            verticalVelocity = 0f;
            isDodging = false;
            health?.SetInvulnerable(false);
        }

        private void Rotate(Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }
}
