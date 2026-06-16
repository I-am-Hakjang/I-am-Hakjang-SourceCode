using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Hakjang
{
    [CreateAssetMenu(fileName = "Database", menuName = "Scriptable Objects/Database")]
    public class Database : SerializedScriptableObject
    {
        [Title("Preloaded Data"), PropertyOrder(0)]
        [InfoBox("빌드 시 사용될 데이터 목록입니다. 'Refresh Data List' 버튼을 눌러 갱신하세요.", InfoMessageType.Info)]
        [OdinSerialize, ListDrawerSettings(NumberOfItemsPerPage = 20), ReadOnly]
        public List<BaseData> AllData { get; private set; }

        [BoxGroup("Settings"), PropertyOrder(1)]
        [OdinSerialize, FolderPath] private string _soPath;
        [BoxGroup("Settings"), PropertyOrder(1)]
        [OdinSerialize, FolderPath] private string _generatedScriptFolderPath;

#if UNITY_EDITOR
        [BoxGroup("Settings"), PropertyOrder(2)]
        [Button("Refresh Data List", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1)]
        private void RefreshDataList()
        {
            AllData = new();

            if (string.IsNullOrEmpty(_soPath))
            {
                Debug.LogError("SO Path가 설정되지 않았습니다.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:BaseData", new[] { _soPath });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BaseData data = AssetDatabase.LoadAssetAtPath<BaseData>(path);
                if (data != null)
                {
                    AllData.Add(data);
                    data.Id = data.name.GetHashCode();
                    EditorUtility.SetDirty(data);
                }
            }

            // 종류순 -> id순 정렬
            AllData.Sort((x, y) =>
            {
                int typeComparison = x.GetType().Name.CompareTo(y.GetType().Name);
                if (typeComparison != 0) return typeComparison;

                return x.Id.CompareTo(y.Id);
            });

            EditorUtility.SetDirty(this);
            Debug.Log($"총 {AllData.Count}개의 데이터가 리스트에 등록되었습니다.");
        }

        [BoxGroup("Settings"), PropertyOrder(2)]
        [Button("GenerateDataIds", ButtonSizes.Large), GUIColor("green")]
        private void GenerateDataIdsStaticClass()
        {
            if (string.IsNullOrEmpty(_generatedScriptFolderPath))
            {
                Debug.LogWarning("스크립트 생성 경로가 설정되지 않아 클래스를 생성하지 않습니다.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("namespace Hakjang");
            sb.AppendLine("{");
            sb.AppendLine("\t// 이 파일은 Database에 의해 자동으로 생성되었습니다. 수동으로 수정하지 마세요.");
            sb.AppendLine("\tpublic static class DataIDs");
            sb.AppendLine("\t{");

            foreach (var data in AllData)
            {
                string originalName = data.name;
                // 변수명으로 사용할 수 있게 이름 정제 (공백, 특수문자 제거)
                string safeName = Regex.Replace(originalName, @"[^a-zA-Z0-9_]", "_");

                // 숫자로 시작할 경우 앞에 언더바 추가
                if (char.IsDigit(safeName[0])) safeName = "_" + safeName;

                sb.AppendLine($"\t\tpublic static readonly int {safeName} = {data.Id}; // {originalName}");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            string fullPath = Path.Combine(_generatedScriptFolderPath, "DataIDs.cs");
            File.WriteAllText(fullPath, sb.ToString());
        }
#endif
    }
}