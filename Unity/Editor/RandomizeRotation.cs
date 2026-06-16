using UnityEngine;
using UnityEditor;

namespace Hakjang.Editor
{
    public class RandomizeRotation : EditorWindow
    {
        private float _randomRotationOffset = 0f;
        private Vector3 _randomPositionOffset = Vector3.zero;

        [MenuItem("RT/Randomize Rotation")]
        public static void ShowWindow()
        {
            GetWindow<RandomizeRotation>("랜덤 회전 추가");
        }

        private void OnGUI()
        {
            GUILayout.Label("선택된 오브젝트의 현재 회전에 랜덤 값을 더합니다.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            _randomRotationOffset = EditorGUILayout.FloatField("최대 랜덤 회전 오프셋 (+/-)", _randomRotationOffset);
            _randomPositionOffset = EditorGUILayout.Vector3Field("최대 랜덤 위치 오프셋 (+/-)", _randomPositionOffset);

            GUILayout.Space(10);

            if (GUILayout.Button("적용 (Apply)"))
            {
                ApplyRandomOffset();
            }
        }

        private void ApplyRandomOffset()
        {
            // 선택된 오브젝트가 없으면 실행하지 않음
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("오브젝트를 먼저 선택해주세요.");
                return;
            }

            foreach (GameObject obj in Selection.gameObjects)
            {
                // Ctrl+Z 로 되돌리기(Undo)를 지원하기 위한 기록
                Undo.RecordObject(obj.transform, "Add Random Transform");

                float rotationOffset = Random.Range(-_randomRotationOffset, _randomRotationOffset);
                Vector3 positionOffset = new Vector3(
                    Random.Range(-_randomPositionOffset.x, _randomPositionOffset.x),
                    Random.Range(-_randomPositionOffset.y, _randomPositionOffset.y),
                    Random.Range(-_randomPositionOffset.z, _randomPositionOffset.z)
                );

                // 기존 위치에 랜덤 오프셋 더하기
                obj.transform.rotation = Quaternion.Euler(obj.transform.rotation.eulerAngles.x, obj.transform.rotation.eulerAngles.y + rotationOffset, obj.transform.rotation.eulerAngles.z);
                obj.transform.position += positionOffset;
            }
        }
    }
}