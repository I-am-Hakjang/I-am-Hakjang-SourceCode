using Unity.Cinemachine;
using UnityEngine;

namespace Hakjang
{
    internal sealed class PlayerCameraRig
    {
        private const int ACTIVE_CAMERA_PRIORITY = 10;
        private const int INACTIVE_CAMERA_PRIORITY = 0;

        private Transform _cameraTransform;
        private CinemachineCamera _firstPersonCamera;
        private CinemachineCamera _thirdPersonCamera;
        private float _xRotation;

        public void Initialize(Transform camera_transform, CinemachineCamera first_person_camera, CinemachineCamera third_person_camera)
        {
            _cameraTransform = camera_transform;
            _firstPersonCamera = first_person_camera;
            _thirdPersonCamera = third_person_camera;
            _xRotation = 0f;
        }

        public void SetOwnerActive(bool is_owner)
        {
            SetCameraActive(_firstPersonCamera, is_owner);
            SetCameraActive(_thirdPersonCamera, is_owner);
        }

        public void Rotate(Transform player_transform, Vector2 look_delta, float rotation_speed)
        {
            if (player_transform == null)
            {
                return;
            }

            float mouseX = look_delta.x * rotation_speed * Time.deltaTime;
            float mouseY = look_delta.y * rotation_speed * Time.deltaTime;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

            if (_cameraTransform != null)
            {
                _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            }

            player_transform.Rotate(Vector3.up * mouseX);
        }

        public void ShowFirstPersonView()
        {
            SetCameraPriority(_firstPersonCamera, ACTIVE_CAMERA_PRIORITY);
            SetCameraPriority(_thirdPersonCamera, INACTIVE_CAMERA_PRIORITY);
        }

        public void ShowThirdPersonView()
        {
            //AlignThirdPersonYawToFollowTarget();
            SetCameraPriority(_firstPersonCamera, INACTIVE_CAMERA_PRIORITY);
            SetCameraPriority(_thirdPersonCamera, ACTIVE_CAMERA_PRIORITY);
        }

        private void AlignThirdPersonYawToFollowTarget()
        {
            if (_thirdPersonCamera == null)
            {
                return;
            }

            Transform followTarget = _thirdPersonCamera.Follow;
            if (followTarget == null)
            {
                return;
            }

            Vector3 eulerAngles = _thirdPersonCamera.transform.eulerAngles;
            eulerAngles.y = followTarget.eulerAngles.y;
            _thirdPersonCamera.transform.eulerAngles = eulerAngles;
        }

        private void SetCameraActive(CinemachineCamera camera, bool is_active)
        {
            if (camera == null)
            {
                return;
            }

            camera.gameObject.SetActive(is_active);
        }

        private void SetCameraPriority(CinemachineCamera camera, int priority)
        {
            if (camera == null)
            {
                return;
            }

            camera.Priority = priority;
        }
    }
}
