using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.DATA_MANAGER)]
    public class DataManagerBehaviour : ManagerBehaviour
    {
        #region Property
        #endregion

        #region Field

        [BoxGroup("Settings")]
        [OdinSerialize, FolderPath] private string jsonPath;
        [BoxGroup("Settings")]
        [OdinSerialize, FolderPath] private string soPath;

        [Title("Preloaded Data"), PropertyOrder(5)]
        [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
        [OdinSerialize] private Database database;

        #endregion

        #region Method
        private void Awake()
        {
            if (database)
                Root.sDataManager.OnAwake(database.AllData);
            else
                Debug.LogError("Database가 할당되지 않았습니다.");
        }

#if UNITY_EDITOR
        [BoxGroup("Actions")]
        [Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void SaveAllSOsToJson()
        {
            Root.sDataManager.SaveAllSOsToJson(soPath, jsonPath);
        }

        [BoxGroup("Actions")]
        [Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void LoadAllSOsFromJson()
        {
            Root.sDataManager.LoadAllSOsFromJson(jsonPath, soPath);
        }
#endif
        #endregion
    }
}
