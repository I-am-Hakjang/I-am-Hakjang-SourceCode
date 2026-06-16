using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Hakjang
{
    public class ResourceManager
	{
		#region Property
		#endregion

		#region Field
		private Dictionary<int, (GameObject poolRoot, Queue<GameObject> objectQueue)> _pooledObjects = new();
		private PrefabList _prefabList;
        #endregion

        #region Method

        public void OnAwake(Transform system_object, PrefabList prefab_list)
		{
            if (system_object == null)
            {
                Debug.LogError("ResourceManager Init failed: system_object is null.");
                return;
            }
            if (prefab_list == null)
            {
                Debug.LogError("ResourceManager Init failed: prefab_list is null.");
                return;
            }
            if (prefab_list.Prefabs == null)
            {
                Debug.LogError("ResourceManager Init failed: prefab_list.Prefabs is null.");
                return;
            }

			_prefabList = prefab_list;
            var rootObject = new GameObject("PooledObject");
            rootObject.transform.parent = system_object;

            foreach (var prefab in _prefabList.Prefabs)
			{
                if (prefab == null) continue;

				if (!prefab.IsPooled)
				{
                    _pooledObjects[prefab.Id] = (prefab.gameObject, null);
                    continue;
				}

                var queuedRoot = new GameObject($"Pool_{prefab.Id}_{prefab.name}");
                queuedRoot.transform.parent = rootObject.transform;
				
                var objectQueue = new Queue<GameObject>();
                _pooledObjects[prefab.Id] = (queuedRoot, objectQueue);

				for (int i = 0; i < prefab.PoolCount; i++)
				{
					var pooledObject = GameObject.Instantiate(prefab.gameObject);
					pooledObject.transform.parent = queuedRoot.transform;
					objectQueue.Enqueue(pooledObject);
					pooledObject.SetActive(false);
                }
            }
        }

        public GameObject Instantiate(int id)
        {
            return Instantiate(id, Vector3.zero, Quaternion.identity);
        }
		
		public GameObject Instantiate(int id, Vector3 position, Quaternion rotation)
		{
            if (_prefabList == null || _prefabList.Prefabs == null)
            {
                Debug.LogError("ResourceManager Instantiate failed: PrefabList is not initialized.");
                return null;
            }

			var prefab = Array.Find(_prefabList.Prefabs, x => x != null && x.Id == id);

			if (prefab == null)
			{
				Debug.LogError($"Prefab with ID {id} not found in PrefabList.");
				return null;
			}

			if (_pooledObjects.TryGetValue(id, out var tuple))
			{
				if (tuple.objectQueue != null)
				{
                    if (tuple.objectQueue.Count > 0)
                    {
                        var pooledObject = tuple.objectQueue.Dequeue();
                        
                        // 큐에 있던 오브젝트가 외부에서 파괴되었을 경우(null) 안전하게 새로 생성
                        if (pooledObject != null)
                        {
                            pooledObject.transform.position = position;
                            pooledObject.transform.rotation = rotation;
                            pooledObject.SetActive(true);
                            return pooledObject;
                        }
                        else
                        {
                            var newObject = GameObject.Instantiate(prefab.gameObject, position, rotation);
                            if (tuple.poolRoot != null) newObject.transform.parent = tuple.poolRoot.transform;
                            return newObject;
                        }
                    }
                    else
                    {
                        var newObject = GameObject.Instantiate(prefab.gameObject, position, rotation);
                        return newObject;
                    }
                }

                // Pooling이 설정되지 않은 프리팹인 경우
                else
                {
					var newObject = GameObject.Instantiate(prefab.gameObject, position, rotation);
					return newObject;
				}
			}
			else
			{
				Debug.LogError($"No pool found for prefab ID {id}. Creating new pool.");
				return null;
            }
        }

		public void Destroy(GameObject game_object)
		{
            if (game_object == null) return;

			// 2. 일반 프리팹 객체 처리
			if (Util.TryGetComponent<Prefabable>(game_object, out var prefab))
			{
				if (prefab.IsPooled)
				{
                    game_object.SetActive(false);
                    if (_pooledObjects.TryGetValue(prefab.Id, out var tuple))
                    {
                        if (tuple.poolRoot != null) game_object.transform.parent = tuple.poolRoot.transform;
                        tuple.objectQueue?.Enqueue(game_object);
                    }
                    else
                    {
                        // 풀을 찾을 수 없는 경우 경고 후 파괴 (메모리 누수 방지)
                        Debug.LogWarning($"No pool found for prefab ID {prefab.Id}. Destroying object.");
                        GameObject.Destroy(game_object);
                    }
                }

				else
				{
					GameObject.Destroy(game_object);
                }
			}
            else
            {
                // Prefab 컴포넌트가 없는 경우 일반 파괴
                GameObject.Destroy(game_object);
            }
		}

        #endregion
    }
}
