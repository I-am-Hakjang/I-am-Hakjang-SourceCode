using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using Utils;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.PLAYER_CONTROLLER)]
    public class PlayerController : MonoBehaviour
    {
        private const float GIZMO_ATTACK_CENTER_ALPHA = 0.35f;
        private const float GIZMO_ATTACK_SWEEP_ALPHA = 0.2f;

        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private CinemachineCamera _firstPersonCamera;
        [SerializeField] private CinemachineCamera _thirdPersonCamera;
        [SerializeField] private CinemachineBrain _cinemachineBrain;
        [SerializeField] private GameObject _headObject;
        [SerializeField] private LayerMask _attackTargetLayerMask = ~0;
        [SerializeField] private LayerMask _stepCollisionLayerMask = ~0;
        [SerializeField] private float _stepCheckDistance = 0.35f;
        [SerializeField] private float _stepLowerRayOffset = 0.05f;
        [SerializeField] private float _maxStepHeight = 0.4f;
        [SerializeField] private float _stepUpSpeed = 3f;

        private BaseUnit _unit;
        private Rigidbody _rigidbody;
        private Collider _collider;

        private MovementData _movementData;

        private CombatData _combatData;
        private float _currentAttackTime;
        private bool _isAttacking;
        private bool _hasSentAttack;
        private bool _isFirstPersonRequested = true;
        private bool _isHeadVisible = true;
        private int _firstPersonRequestFrame = -1;
        private bool _waitForFirstPersonBlendEnd;
        private bool _hasFirstPersonBlendStarted;

        private readonly PlayerAttackDetector _attackDetector = new PlayerAttackDetector();
        private readonly List<string> _attackTargetUids = new List<string>();
        private readonly PlayerAnimationDriver _animationDriver = new PlayerAnimationDriver();
        private readonly PlayerCameraRig _cameraRig = new PlayerCameraRig();
        private readonly PlayerMovementState _movementState = new PlayerMovementState();

        private void Awake()
        {
            _unit = Util.GetComponent<BaseUnit>(this);
            _rigidbody = Util.GetComponent<Rigidbody>(this);
            _collider = Util.GetComponent<Collider>(this);

            if (_headObject != null)
                _isHeadVisible = _headObject.activeSelf;

            _animationDriver.Initialize(Util.GetComponentInChildren<Animator>(this));
            _cameraRig.Initialize(_cameraTransform, _firstPersonCamera, _thirdPersonCamera);

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                _cinemachineBrain = Util.GetComponent<CinemachineBrain>(mainCamera.gameObject);
        }

        public void Initialize(MovementData movement_data, CombatData combat_data, bool is_owner)
        {
            _movementData = movement_data;
            _combatData = combat_data;
            _cameraRig.SetOwnerActive(is_owner);

            if (!is_owner)
            {
                SetHeadObjectActive(true);
                return;
            }

            RequestFirstPersonView();
        }

        private void Update()
        {
            if (!_unit.IsOwner)
                return;

            UpdateHeadObjectVisibility();

            if (_movementData == null || _combatData == null)
                return;

            if (_isAttacking)
            {
                _currentAttackTime += Time.deltaTime;
                if (_currentAttackTime >= _combatData.AttackTriggerTime && !_hasSentAttack)
                {
                    CastAttackAndSendTargets();
                    _hasSentAttack = true;
                }
                else if (_currentAttackTime >= _combatData.AttackDuration)
                {
                    _isAttacking = false;
                    RequestFirstPersonView();
                    PlayMovementAnimation();
                }
            }
        }

        private void FixedUpdate()
        {
            if (!_unit.IsOwner)
                return;

            if (_movementData == null || _combatData == null)
                return;

            Vector3 currentVelocity = _rigidbody.linearVelocity;

            if (_isAttacking)
            {
                _rigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
                return;
            }

            Vector3 moveDirection = _movementState.GetWorldVelocity(transform, !_isAttacking);
            _rigidbody.linearVelocity = new Vector3(moveDirection.x, currentVelocity.y, moveDirection.z);
        }
   
        public void Move(Vector2 input)
        {
            _movementState.SetMoveInput(input, _movementData);

            if (_isAttacking)
                return;

            if (_movementState.IsMoving)
            {
                PlayMovementAnimation();
            }
            else
            {
                StopMove();
            }
        }

        public void Sprint(bool is_sprinting)
        {
            _movementState.SetSprint(is_sprinting, _movementData);

            if (_isAttacking)
                return;

            if (_movementState.IsMoving)
            {
                PlayMovementAnimation();
            }
        }

        private void StopMove()
        {
            _movementState.Stop();
            _animationDriver.PlayIdle();
        }

        public void Rotate(Vector2 look_delta, float rotation_speed)
        {
            if (_isAttacking)
                return;

            _cameraRig.Rotate(transform, look_delta, rotation_speed);
        }

        public void Attack(CombatData combat_data)
        {
            if (_isAttacking)
                return;

            if (combat_data != null)
                _combatData = combat_data;

            _isAttacking = true;
            _hasSentAttack = false;

            _currentAttackTime = 0;

            _animationDriver.PlayAttack();
            RequestThirdPersonView();
        }

        private void RequestFirstPersonView()
        {
            _isFirstPersonRequested = true;
            _cameraRig.ShowFirstPersonView();
            _firstPersonRequestFrame = Time.frameCount;
            _waitForFirstPersonBlendEnd = true;
            _hasFirstPersonBlendStarted = false;
            TryHideHeadObjectForFirstPerson();
        }

        private void RequestThirdPersonView()
        {
            _isFirstPersonRequested = false;
            _waitForFirstPersonBlendEnd = false;
            _hasFirstPersonBlendStarted = false;
            _cameraRig.ShowThirdPersonView();
            SetHeadObjectActive(true);
        }

        private void UpdateHeadObjectVisibility()
        {
            if (_isFirstPersonRequested)
            {
                TryHideHeadObjectForFirstPerson();
                return;
            }

            SetHeadObjectActive(true);
        }

        private void TryHideHeadObjectForFirstPerson()
        {
            if (!_isFirstPersonRequested)
                return;

            if (_cinemachineBrain == null)
            {
                SetHeadObjectActive(false);
                _waitForFirstPersonBlendEnd = false;
                return;
            }

            if (!_waitForFirstPersonBlendEnd)
            {
                SetHeadObjectActive(false);
                return;
            }

            if (Time.frameCount <= _firstPersonRequestFrame)
                return;

            if (_cinemachineBrain.IsBlending)
            {
                _hasFirstPersonBlendStarted = true;
                return;
            }

            if (!_hasFirstPersonBlendStarted && !IsFirstPersonCameraLive())
                return;

            SetHeadObjectActive(false);
            _waitForFirstPersonBlendEnd = false;
            _hasFirstPersonBlendStarted = false;
        }

        private bool IsFirstPersonCameraLive()
        {
            return _cinemachineBrain.ActiveVirtualCamera == _firstPersonCamera;
        }

        private void SetHeadObjectActive(bool is_active)
        {
            if (_headObject == null || _isHeadVisible == is_active)
                return;

            _headObject.SetActive(is_active);
            _isHeadVisible = is_active;
        }

        private void CastAttackAndSendTargets()
        {
            if (!_attackDetector.TryCollectTargetUids(_unit, transform, _combatData, _attackTargetLayerMask, _attackTargetUids))
            {
                return;
            }

            for (int index = 0; index < _attackTargetUids.Count; index++)
            {
                Root.sNetworkManager.SendAttack(_attackTargetUids[index]);
            }
        }

        private void OnDrawGizmos()
        {
            if (!PlayerAttackDetector.IsValidCombatData(_combatData))
                return;

            Vector3 castCenter = PlayerAttackDetector.GetAttackCastCenter(transform, _combatData);
            Vector3 halfExtents = PlayerAttackDetector.GetAttackHalfExtents(_combatData);
            Vector3 boxSize = halfExtents * 2f;
            Vector3 endCenter = castCenter + (transform.forward * _combatData.AttackLength);

            Gizmos.matrix = Matrix4x4.TRS(castCenter, transform.rotation, Vector3.one);
            Gizmos.color = new Color(1f, 0.3f, 0.3f, GIZMO_ATTACK_CENTER_ALPHA);
            Gizmos.DrawCube(Vector3.zero, boxSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, boxSize);

            Gizmos.matrix = Matrix4x4.TRS(endCenter, transform.rotation, Vector3.one);
            Gizmos.color = new Color(1f, 0.6f, 0.2f, GIZMO_ATTACK_SWEEP_ALPHA);
            Gizmos.DrawCube(Vector3.zero, boxSize);
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);

            Gizmos.matrix = Matrix4x4.identity;
        }

        private void PlayMovementAnimation()
        {
            if (_movementState.IsMoving)
                _animationDriver.PlayMove(_movementState.IsSprinting);
            else
                _animationDriver.PlayIdle();
        }
    }

}
