using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace Hakjang
{
    public class MovementData
    {
        [OdinSerialize] public float MovementSpeed { get; private set; }
        [OdinSerialize] public float SprintSpeed { get; private set; }
        [OdinSerialize] public float RotationSpeed { get; private set; }
    }
    public class CombatData
    {
        [OdinSerialize] public float AttackDuration { get; private set; }
        [OdinSerialize] public float AttackWidth { get; private set; }
        [OdinSerialize] public float AttackLength { get; private set; }
        [OdinSerialize] public float AttackHeight { get; private set; }
        [OdinSerialize] public float AttackDepth { get; private set; }
        [OdinSerialize] public float AttackTriggerTime { get; private set; }
    }

    [CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
    public class PlayerData : BaseData
    {
        [Title("Movement")]
        [OdinSerialize, InlineProperty, HideLabel] public MovementData MovementData { get; private set; } = new MovementData();

        [Title("Combat")]
        [OdinSerialize, InlineProperty, HideLabel] public CombatData CombatData { get; private set; } = new CombatData();
    }
}
