using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.PLAYER)]
    public class Player : BaseUnit
    {
        private PlayerData _data;

        private PlayerController _playerController;
        private PlayerInputProvider _inputProvider;

        private void Awake()
        {
            _data = Root.sDataManager.GetData<PlayerData>(DataIDs.PlayerData);
            _playerController = Util.GetComponent<PlayerController>(this);
            _inputProvider = Util.GetComponent<PlayerInputProvider>(this);
        }

        public override void OnNetworkStart(string id, bool is_owner)
        {
            this.Id = id;
            IsOwner = is_owner;

            _playerController.Initialize(_data.MovementData, _data.CombatData, IsOwner);

            if (!IsOwner)
                return;

            _inputProvider.OnMove += HandleMove;
            _inputProvider.OnLook += HandleLook;
            _inputProvider.OnAttack += HandleAttack;
            _inputProvider.OnSprint += HandleSprint;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (!IsOwner)
                return;

            _inputProvider.OnMove -= HandleMove;
            _inputProvider.OnLook -= HandleLook;
            _inputProvider.OnAttack -= HandleAttack;
            _inputProvider.OnSprint -= HandleSprint;
        }

        private void HandleMove(Vector2 input)
        {
            _playerController.Move(input);
        }

        private void HandleLook(Vector2 input)
        {
            _playerController.Rotate(input, _data.MovementData.RotationSpeed);
        }

        private void HandleAttack()
        {
            _playerController.Attack(_data.CombatData);
        }

        private void HandleSprint(bool is_sprinting)
        {
            _playerController.Sprint(is_sprinting);
        }

        [Button]
        private void TestInit()
        {
            OnNetworkStart("Test", true);
        }
    }
}