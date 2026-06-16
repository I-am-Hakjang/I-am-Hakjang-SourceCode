using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Hakjang
{
	[DefaultExecutionOrder((int)UpdateOrderGroup.RESOURCE_MANAGER)]
	public class ResourceManagerBehaviour : ManagerBehaviour
	{
        #region Property
        [OdinSerialize, InlineEditor(InlineEditorObjectFieldModes.Boxed, Expanded = true)] public PrefabList PrefabList { get; private set; }
        #endregion

        #region Field
        #endregion

        #region Method
        private void Awake()
		{
			Root.sResourceManager.OnAwake(this.transform, PrefabList);
        }
        #endregion
    }
}
