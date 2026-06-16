using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.PLAYER_INPUT_PROVIDER)]
    public class PlayerInputProvider : MonoBehaviour
    {
        public event Action<Vector2> OnMove;
        public event Action<Vector2> OnLook;
        public event Action OnAttack;
        public event Action<bool> OnSprint;

        InputSystem_Actions _inputActions;

        private void Awake()
        {
            _inputActions = new();
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();

            _inputActions.Player.Move.performed += HandleMove;
            _inputActions.Player.Move.canceled += HandleMove;
            _inputActions.Player.Look.performed += HandleLook;
            _inputActions.Player.Look.canceled += HandleLook;
            _inputActions.Player.Attack.performed += HandleAttack;
            _inputActions.Player.Sprint.performed += HandleSprintPerformed;
            _inputActions.Player.Sprint.canceled += HandleSprintCanceled;
        }

        private void OnDisable()
        {
            _inputActions.Player.Move.performed -= HandleMove;
            _inputActions.Player.Move.canceled -= HandleMove;
            _inputActions.Player.Look.performed -= HandleLook;
            _inputActions.Player.Look.canceled -= HandleLook;
            _inputActions.Player.Attack.performed -= HandleAttack;
            _inputActions.Player.Sprint.performed -= HandleSprintPerformed;
            _inputActions.Player.Sprint.canceled -= HandleSprintCanceled;

            _inputActions.Player.Disable();
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            OnMove?.Invoke(context.ReadValue<Vector2>());
        }

        private void HandleLook(InputAction.CallbackContext context)
        {
            OnLook?.Invoke(context.ReadValue<Vector2>());
        }

        private void HandleAttack(InputAction.CallbackContext context)
        {
            OnAttack?.Invoke();
        }

        private void HandleSprintPerformed(InputAction.CallbackContext context)
        {
            OnSprint?.Invoke(true);
        }

        private void HandleSprintCanceled(InputAction.CallbackContext context)
        {
            OnSprint?.Invoke(false);
        }
    }
}