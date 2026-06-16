using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Hakjang
{
    [CreateAssetMenu(fileName = "PrefabList", menuName = "Scriptable Objects/PrefabList")]
    [Searchable]
    public class PrefabList : SerializedScriptableObject
    {
        [OdinSerialize, ReadOnly, ListDrawerSettings(NumberOfItemsPerPage = 10, ListElementLabelName = "Id"), PropertyOrder(0)]
        public Prefabable[] Prefabs { get; private set; }

        [BoxGroup("Settings"), PropertyOrder(1)]
        [OdinSerialize, FolderPath] private string _prefabPath;
        [BoxGroup("Settings"), PropertyOrder(1)]
        [OdinSerialize, FolderPath] private string _generatedScriptFolderPath;

#if UNITY_EDITOR
        [BoxGroup("Settings"), PropertyOrder(2)]
        [Button("Refresh Prefab List", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1)]
        private void RefreshPrefabList()
        {
            Prefabs = null;

            if (string.IsNullOrEmpty(_prefabPath))
            {
                Debug.LogError("Prefab Path가 설정되지 않았습니다.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets($"t=prefab", new[] { _prefabPath });
            Prefabs = new Prefabable[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (gameObject != null)
                {
                    if (!gameObject.TryGetComponent<Prefabable>(out var prefab))
                        prefab = gameObject.AddComponent<Prefabable>();

                    prefab.Id = gameObject.name.GetHashCode();
                    Prefabs[i] = prefab;
                    EditorUtility.SetDirty(prefab);
                }
            }

            // 종류순 -> id순 정렬
            Array.Sort(Prefabs, (x, y) => x.gameObject.name.CompareTo(y.gameObject.name));

            EditorUtility.SetDirty(this);
            Debug.Log($"총 {Prefabs.Length}개의 데이터가 리스트에 등록되었습니다.");
        }

        [BoxGroup("Settings"), PropertyOrder(2)]
        [Button("GeneratePrefabId", ButtonSizes.Large), GUIColor("green")]
        private void GeneratePrefabIdStaticClass()
        {
            if (string.IsNullOrEmpty(_generatedScriptFolderPath))
            {
                Debug.LogWarning("스크립트 생성 경로가 설정되지 않아 클래스를 생성하지 않습니다.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("namespace Hakjang");
            sb.AppendLine("{");
            sb.AppendLine("\t// 이 파일은 PrefabList에 의해 자동으로 생성되었습니다. 수동으로 수정하지 마세요.");
            sb.AppendLine("\tpublic static class PrefabIDs");
            sb.AppendLine("\t{");

            foreach (var prefab in Prefabs)
            {
                string originalName = prefab.gameObject.name;
                // 변수명으로 사용할 수 있게 이름 정제 (공백, 특수문자 제거)
                string safeName = Regex.Replace(originalName, @"[^a-zA-Z0-9_]", "_");

                // 숫자로 시작할 경우 앞에 언더바 추가
                if (char.IsDigit(safeName[0])) safeName = "_" + safeName;

                sb.AppendLine($"\t\tpublic static readonly int {safeName} = {prefab.Id}; // {originalName}");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            string fullPath = Path.Combine(_generatedScriptFolderPath, "PrefabIDs.cs");
            File.WriteAllText(fullPath, sb.ToString());
        }
#endif
    }
}