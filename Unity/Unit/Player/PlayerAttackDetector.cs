using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Hakjang
{
    internal sealed class PlayerAttackDetector
    {
        private const int ATTACK_HIT_BUFFER_SIZE = 16;

        private readonly RaycastHit[] _hitBuffer = new RaycastHit[ATTACK_HIT_BUFFER_SIZE];
        private readonly HashSet<string> _targetUidSet = new HashSet<string>();

        public bool TryCollectTargetUids(BaseUnit attacker, Transform attack_origin, CombatData combat_data, LayerMask target_layer_mask, List<string> target_uids)
        {
            if (target_uids == null)
            {
                return false;
            }

            target_uids.Clear();

            if (attacker == null || attack_origin == null || string.IsNullOrEmpty(attacker.Id))
            {
                return false;
            }

            if (!IsValidCombatData(combat_data))
            {
                return false;
            }

            _targetUidSet.Clear();

            Vector3 castCenter = GetAttackCastCenter(attack_origin, combat_data);
            Vector3 halfExtents = GetAttackHalfExtents(combat_data);

            int hitCount = Physics.BoxCastNonAlloc(
                castCenter,
                halfExtents,
                attack_origin.forward,
                _hitBuffer,
                attack_origin.rotation,
                combat_data.AttackLength,
                target_layer_mask,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < hitCount; index++)
            {
                var collider = _hitBuffer[index].collider;
                if (collider == null)
                {
                    continue;
                }

                BaseUnit targetUnit = Util.GetComponentInParent<BaseUnit>(collider.gameObject);
                if (targetUnit == null || targetUnit == attacker || targetUnit.IsDead || string.IsNullOrEmpty(targetUnit.Id))
                {
                    continue;
                }

                if (!_targetUidSet.Add(targetUnit.Id))
                {
                    continue;
                }

                target_uids.Add(targetUnit.Id);
            }

            return target_uids.Count > 0;
        }

        public static bool IsValidCombatData(CombatData combat_data)
        {
            return combat_data != null
                && combat_data.AttackWidth > 0f
                && combat_data.AttackLength > 0f
                && combat_data.AttackHeight > 0f
                && combat_data.AttackDepth > 0f;
        }

        public static Vector3 GetAttackCastCenter(Transform attack_origin, CombatData combat_data)
        {
            if (attack_origin == null || combat_data == null)
            {
                return Vector3.zero;
            }

            return attack_origin.position + (Vector3.up * (combat_data.AttackHeight * 0.5f));
        }

        public static Vector3 GetAttackHalfExtents(CombatData combat_data)
        {
            if (combat_data == null)
            {
                return Vector3.zero;
            }

            return new Vector3(combat_data.AttackWidth * 0.5f, combat_data.AttackHeight * 0.5f, combat_data.AttackDepth * 0.5f);
        }
    }
}
