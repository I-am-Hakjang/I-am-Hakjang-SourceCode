using UnityEngine;
using Utils;

namespace Hakjang
{


    public class NetworkedTransform : MonoBehaviour
    {
        #region Field
        private const float SEND_INTERVAL = 0.016f;
        private const float POSITION_INTERPOLATION_SPEED = 12f;
        private const float ROTATION_INTERPOLATION_SPEED = 16f;
        private const float SNAP_DISTANCE_SQR = 9f;

        private BaseUnit _baseUnit;
        private bool _isRegistered;
        private bool _isInitialized;
        private bool _isOwner;
        private bool _hasTargetPose;
        private string _playerUid = string.Empty;
        private float _sendTimer;
        private Vector3 _targetPosition;
        private float _targetRotationY;
        private Rigidbody _rigidbody;
        #endregion

        #region Method
        private void Awake()
        {
            _baseUnit = Util.GetComponent<BaseUnit>(this);
            _rigidbody = Util.GetComponent<Rigidbody>(this);
        }

        private void OnEnable()
        {
            TryInitializeFromBaseUnit();
        }

        private void Update()
        {
            TryInitializeFromBaseUnit();

            if (!_isOwner)
            {
                InterpolateRemotePose();
                return;
            }

            if (string.IsNullOrEmpty(_playerUid))
            {
                return;
            }

            if (!Root.sNetworkManager.IsConnected)
            {
                return;
            }

            _sendTimer += Time.deltaTime;
            if (_sendTimer < SEND_INTERVAL)
            {
                return;
            }

            _sendTimer = 0f;

            Vector3 currentPosition = transform.position;
            float currentRotationY = transform.eulerAngles.y;

            Root.sNetworkManager.SendPlayerPose(_playerUid, currentPosition, currentRotationY);
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        public void ApplyNetworkPose(Vector3 position, float rotation_y)
        {
            if (_isOwner)
            {
                return;
            }

            position.y = transform.position.y;
            _targetPosition = position;
            _targetRotationY = rotation_y;

            if (!_hasTargetPose)
            {
                _rigidbody.position = _targetPosition;
                transform.rotation = Quaternion.Euler(0f, _targetRotationY, 0f);
            }

            _hasTargetPose = true;
        }

        private void TryInitializeFromBaseUnit()
        {
            if (_baseUnit == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_baseUnit.Id))
            {
                return;
            }

            if (_isInitialized && _playerUid == _baseUnit.Id && _isOwner == _baseUnit.IsOwner)
            {
                return;
            }

            if (_isRegistered)
            {
                Unregister();
            }

            _playerUid = _baseUnit.Id;
            _isOwner = _baseUnit.IsOwner;
            _isInitialized = true;
            _sendTimer = 0f;
            _hasTargetPose = false;

            Root.sNetworkManager.RegisterNetworkedTransform(_playerUid, this);
            _isRegistered = true;
        }

        private void InterpolateRemotePose()
        {
            if (!_hasTargetPose)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            Vector3 targetPosition = _targetPosition;

            if ((targetPosition - currentPosition).sqrMagnitude > SNAP_DISTANCE_SQR)
            {
                transform.position = targetPosition;
            }
            else
            {
                float positionLerpFactor = Time.deltaTime * POSITION_INTERPOLATION_SPEED;
                transform.position = Vector3.Lerp(currentPosition, targetPosition, positionLerpFactor);
            }

            float currentRotationY = transform.eulerAngles.y;
            float rotationLerpFactor = Time.deltaTime * ROTATION_INTERPOLATION_SPEED;
            float interpolatedRotationY = Mathf.LerpAngle(currentRotationY, _targetRotationY, rotationLerpFactor);
            transform.rotation = Quaternion.Euler(0f, interpolatedRotationY, 0f);
        }

        private void Unregister()
        {
            if (!_isRegistered || string.IsNullOrEmpty(_playerUid))
            {
                return;
            }

            Root.sNetworkManager.UnregisterNetworkedTransform(_playerUid, this);
            _isRegistered = false;
        }
        #endregion
    }
}