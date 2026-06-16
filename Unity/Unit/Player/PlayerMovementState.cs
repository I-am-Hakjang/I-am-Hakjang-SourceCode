using UnityEngine;

namespace Hakjang
{
    internal sealed class PlayerMovementState
    {
        private Vector3 _localDirection;
        private Vector3 _lastMoveInputDirection;
        private float _speed;

        public bool IsMoving { get; private set; }
        public bool IsSprinting { get; private set; }

        public void SetMoveInput(Vector2 input, MovementData movement_data)
        {
            IsMoving = input != Vector2.zero;

            if (!IsMoving)
            {
                Stop();
                return;
            }

            _lastMoveInputDirection = new Vector3(input.x, 0f, input.y);
            _localDirection = _lastMoveInputDirection;
            _speed = ResolveSpeed(movement_data);
        }

        public void SetSprint(bool is_sprinting, MovementData movement_data)
        {
            IsSprinting = is_sprinting;

            if (!IsMoving)
            {
                return;
            }

            _localDirection = _lastMoveInputDirection;
            _speed = ResolveSpeed(movement_data);
        }

        public void Stop()
        {
            IsMoving = false;
            IsSprinting = false;
            _localDirection = Vector3.zero;
            _speed = 0f;
        }

        public Vector3 GetWorldVelocity(Transform transform, bool can_move)
        {
            if (!can_move || transform == null || _localDirection == Vector3.zero)
            {
                return Vector3.zero;
            }

            return ((transform.right * _localDirection.x) + (transform.forward * _localDirection.z)).normalized * _speed;
        }

        private float ResolveSpeed(MovementData movement_data)
        {
            if (movement_data == null)
            {
                return 0f;
            }

            return IsSprinting ? movement_data.SprintSpeed : movement_data.MovementSpeed;
        }
    }
}
