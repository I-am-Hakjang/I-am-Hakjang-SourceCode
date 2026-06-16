using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hakjang
{
    public class DataManager
    {
        #region Property    

        #endregion

        #region Field
        private readonly Dictionary<Type, Dictionary<int, BaseData>> dataDictionary = new();
        #endregion

        #region Method

        /// <summary>
        /// 스크립터블 오브젝트들을 로드하여 데이터 딕셔너리에 저장.
        /// </summary>
        /// <param name="json_path"></param>
        /// <param name="so_path"></param>
        /// <param name="all_data"></param>
        public void OnAwake(IEnumerable<BaseData> all_data)
        {
            dataDictionary.Clear();

            if (all_data == null) 
                return;

            foreach (var data in all_data)
            {
                if (data == null) 
                    continue;

                Type type = data.GetType();

                if (!dataDictionary.ContainsKey(type))
                {
                    dataDictionary.Add(type, new Dictionary<int, BaseData>());
                }

                if (dataDictionary[type].ContainsKey(data.Id))
                {
                    Debug.LogWarning($"[DataManager] 중복된 ID 발견: {type.Name} - ID:{data.Id} ({data.name})");
                    continue;
                }

                dataDictionary[type][data.Id] = data;
            }
        }

        public T GetData<T>(int id) where T : BaseData
        {
            if (!dataDictionary.ContainsKey(typeof(T)))
            {
                Debug.LogError($"해당 타입의 데이터를 찾을 수 없습니다: {typeof(T).Name}");
                return null;
            }

            if (!dataDictionary[typeof(T)].ContainsKey(id))
            {
                Debug.LogError($"해당 id를 찾을 수 없습니다: {id}");
                return null;
            }

            return dataDictionary[typeof(T)][id] as T;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// 모든 스크립터블 오브젝트를 JSON 파일로 저장.
        /// </summary>
        /// <param name="json_path"></param>
        /// <param name="so_path"></param>
        public void SaveAllSOsToJson(string json_path, string so_path)
        {
            if (!IsValidPath(json_path, so_path))
            {
                return;
            }

            var dataTypes = TypeCache.GetTypesDerivedFrom<BaseData>();

            foreach (Type type in dataTypes)
            {
                if (typeof(BaseData).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                {
                    var assetGuids = AssetDatabase.FindAssets($"t:{type.Name}", new string[] { so_path });
                    string[] assetPaths = Array.ConvertAll<string, string>(assetGuids, AssetDatabase.GUIDToAssetPath);

                    foreach (string path in assetPaths)
                    {
                        var so = AssetDatabase.LoadAssetAtPath<BaseData>(path);

                        if (so != null)
                        {
                            string json = JsonUtility.ToJson(so, true);

                            string filePath = Path.Combine(json_path, type.Name);

                            if (!Directory.Exists(filePath))
                                Directory.CreateDirectory(filePath);

                            File.WriteAllText(Path.Combine(filePath, $"{so.name}.json"), json);
                        }
                    }
                }


            }

            AssetDatabase.Refresh();
            Debug.Log("데이터 저장 완료");
        }

        /// <summary>
        /// 모든 JSON 파일을 스크립터블 오브젝트로 불러오기.
        /// </summary>
        /// <param name="json_path"></param>
        /// <param name="so_path"></param>
        public void LoadAllSOsFromJson(string json_path, string so_path)
        {
            if (!IsValidPath(json_path, so_path))
            {
                return;
            }

            string[] folderPaths = Directory.GetDirectories(json_path);
            foreach (string folderPath in folderPaths)
            {
                string[] filePaths = Directory.GetFiles(folderPath, "*.json");

                foreach (string filePath in filePaths)
                {
                    string jsonText = File.ReadAllText(filePath);
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string typeName = Path.GetFileName(folderPath);

                    var assetGuids = AssetDatabase.FindAssets($"t:{typeName} {fileName}", new string[] { so_path });

                    if (assetGuids.Length == 0)
                    {
                        Debug.LogWarning($"해당 이름과 타입의 ScriptableObject가 존재하지 않습니다: {typeName} - {fileName}");
                        continue;
                    }
                    else if (assetGuids.Length > 1)
                    {
                        Debug.LogWarning($"해당 이름과 타입의 ScriptableObject가 여러 개 존재합니다: {typeName} - {fileName}");
                        continue;
                    }

                    var path = AssetDatabase.GUIDToAssetPath(assetGuids[0]);
                    var so = AssetDatabase.LoadAssetAtPath<BaseData>(path);

                    if (so != null)
                    {
                        // 2. 핵심: 기존 SO 객체 위에 JSON 내용을 덮어씌움
                        JsonUtility.FromJsonOverwrite(jsonText, so);

                        // 3. 변경사항 저장
                        EditorUtility.SetDirty(so);
                    }
                }
            }

            Debug.Log("데이터 불러오기 완료");
        }


        private bool IsValidPath(string json_path, string so_path)
        {
            if (!Directory.Exists(json_path) || !Directory.Exists(so_path))
            {
                Debug.LogError("유효하지 않은 경로입니다. 경로를 확인해주세요.");
                return false;
            }
            return true;
        }
#endif
        #endregion
    }
}
