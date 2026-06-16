using System;
using UnityEditor;
using UnityEngine;

namespace Hakjang.Editor
{
    /// <summary>
    /// 유니티가 컴파일 될 때마다 이 클래스가 자동으로 초기화
    /// Manager들을 모두 SystemObject에 추가시켜줌.
    /// </summary>
    [InitializeOnLoad]
    public class ManagerAutoAdder
    {
        private const string FOLDER_PATH = "Assets/1. Hakjang/Prefabs";

        static ManagerAutoAdder()
        {
            string[] guids = AssetDatabase.FindAssets("SystemObject", new[] { FOLDER_PATH });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SystemObjectController systemObject = AssetDatabase.LoadAssetAtPath<SystemObjectController>(path);

                if (systemObject == null)
                    continue;

                var managerTypes = TypeCache.GetTypesDerivedFrom<ManagerBehaviour>();

                foreach (Type type in managerTypes)
                {
                    if (typeof(ManagerBehaviour).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                    {
                        if (systemObject.GetComponent(type) == null)
                        {
                            systemObject.gameObject.AddComponent(type);
                            Debug.Log($"[AutoAdder] 새 매니저 발견: {type.Name} 추가");
                        }
                    }
                }

                // 씬이 변경되었음을 유니티에 알려 저장 가능하게 함.
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }
            }             
        }
    }
}