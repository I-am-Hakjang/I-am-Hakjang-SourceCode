using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Hakjang
{
    [System.Serializable]
    public abstract class BaseData : SerializedScriptableObject
    {
        [Title("Identity")]
        [OdinSerialize, ReadOnly]
        public int Id { get; set; }
    }
}