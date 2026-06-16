using UnityEngine;
using Utils;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.RAGDOLL_CONTROLLER)]
    public class RagDollController : MonoBehaviour
    {
        [SerializeField] private float _ragdollImpulseForce = 20f;
        [SerializeField] private float _ragdollImpulseUpwardBias = 0.05f;

        private void Awake()
        {
            Root.sNetworkManager.OnKilled += HandleKilled;
        }

        private void HandleKilled(BaseUnit killer_unit, BaseUnit target_unit)
        {
            Vector3 targetPosition = target_unit.transform.position;
            Quaternion targetRotation = target_unit.transform.rotation;
            Vector3 impulseDirection = ResolveKillImpulseDirection(killer_unit, target_unit);
            SpawnRagdoll(targetPosition, targetRotation, impulseDirection);
        }

        private void OnDestroy()
        {
            Root.sNetworkManager.OnKilled -= HandleKilled;
        }

        private Vector3 ResolveKillImpulseDirection(BaseUnit killer_unit, BaseUnit target_unit)
        {
            if (target_unit == null)
            {
                return Vector3.up;
            }

            Vector3 impulseDirection = -target_unit.transform.forward;

            Vector3 fromKillerToTarget = target_unit.transform.position - killer_unit.transform.position;
            fromKillerToTarget.y = 0f;

            if (fromKillerToTarget.sqrMagnitude > 0f)
            {
                impulseDirection = fromKillerToTarget.normalized;
            }

            impulseDirection.y += _ragdollImpulseUpwardBias;
            return impulseDirection.normalized;
        }

        private void SpawnRagdoll(Vector3 position, Quaternion rotation, Vector3 impulse_direction)
        {
            var ragdollGameObject = Root.sResourceManager.Instantiate(PrefabIDs.RagDoll, position, rotation);
            if (ragdollGameObject == null)
            {
                return;
            }

            var rigidbodies = Util.GetComponentsInChildren<Rigidbody>(ragdollGameObject, true);
            if (rigidbodies == null || rigidbodies.Length == 0)
            {
                return;
            }

            Vector3 impulse = impulse_direction * _ragdollImpulseForce;
            for (int index = 0; index < rigidbodies.Length; index++)
            {
                var rigidbody = rigidbodies[index];
                if (rigidbody == null)
                {
                    continue;
                }

                rigidbody.AddForce(impulse, ForceMode.VelocityChange);
            }
        }
    }
}