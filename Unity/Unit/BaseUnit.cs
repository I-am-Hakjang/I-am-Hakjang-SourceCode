using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Hakjang
{
    public abstract class BaseUnit : SerializedMonoBehaviour
    {
        [ShowInInspector, ReadOnly] public string Id { get; protected set; }
        [ShowInInspector, ReadOnly] public bool IsOwner { get; protected set; }
        [ShowInInspector, ReadOnly] public bool IsDead { get; protected set; }

        public abstract void OnNetworkStart(string id, bool is_owner);
    }
}