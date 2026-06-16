using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Hakjang
{
	[DefaultExecutionOrder((int)UpdateOrderGroup.PREFABABLE)]
	public class Prefabable : SerializedMonoBehaviour
    {
        #region Property
        [OdinSerialize, ReadOnly] public int Id { get; set; }
        [OdinSerialize] public bool IsPooled { get; private set; }
        [OdinSerialize, ShowIf("IsPooled")] public int PoolCount { get; private set; }
        #endregion

        #region Field
        #endregion

        #region Method
        #endregion
    }
}
