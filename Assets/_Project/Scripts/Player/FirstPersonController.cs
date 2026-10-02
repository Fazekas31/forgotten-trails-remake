using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenTrail.Gameplay.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float sprintSpeed = 4.8f;
        [SerializeField] private float lookSensitivity = 0.075f;
        [SerializeField] private float stepInterval = 0.55f;

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;
        private float _stepTimer;
        private bool _gameplayInputEnabled = true;

        public event Action<Vector3, float> NoiseEmitted;
        public Camera ViewCamera => viewCamera;
        public bool GameplayInputEnabled => _gameplayInputEnabled;

        public void SetGameplayInputEnabled(bool enabled)
        {
            _gameplayInputEnabled = enabled;
        }

        public void FaceTarget(Vector3 targetPosition)
        {
            if (viewCamera == null)
                return;

            var direction = targetPosition - viewCamera.transform.position;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            var horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (horizontal.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(horizontal.normalized, Vector3.up);

            var localDirection = transform.InverseTransformDirection(direction.normalized);
            var horizontalMagnitude = new Vector2(localDirection.x, localDirection.z).magnitude;
            _pitch = Mathf.Clamp(Mathf.Atan2(-localDirection.y, horizontalMagnitude) * Mathf.Rad2Deg, -82f, 82f);
            viewCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (viewCamera == null)
                viewCamera = GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || _controller == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                var locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }

            if (!_gameplayInputEnabled)
                return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                var delta = mouse.delta.ReadValue();
                transform.Rotate(Vector3.up, delta.x * lookSensitivity, Space.Self);
                _pitch = Mathf.Clamp(_pitch - delta.y * lookSensitivity, -82f, 82f);
                if (viewCamera != null)
                    viewCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            var move = Vector2.zero;
            if (keyboard.aKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed) move.x += 1f;
            if (keyboard.sKey.isPressed) move.y -= 1f;
            if (keyboard.wKey.isPressed) move.y += 1f;
            move = Vector2.ClampMagnitude(move, 1f);

            var sprinting = keyboard.leftShiftKey.isPressed && move.sqrMagnitude > 0f;
            var speed = sprinting ? sprintSpeed : walkSpeed;
            var planar = (transform.right * move.x + transform.forward * move.y) * speed;

            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
            _controller.Move((planar + Vector3.up * _verticalVelocity) * Time.deltaTime);

            EmitFootstepNoise(move.sqrMagnitude > 0f, sprinting);
        }

        private void EmitFootstepNoise(bool isMoving, bool sprinting)
        {
            if (!isMoving)
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer += Time.deltaTime;
            var interval = sprinting ? stepInterval * 0.68f : stepInterval;
            if (_stepTimer < interval)
                return;

            _stepTimer = 0f;
            NoiseEmitted?.Invoke(transform.position, sprinting ? 1f : 0.42f);
        }
    }
}
