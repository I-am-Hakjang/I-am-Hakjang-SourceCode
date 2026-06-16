#if UNITY_EDITOR
using Hakjang;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HakJang.Editor.Windows
{
    public class DebugManagerWindow : EditorWindow
    {
        private Vector2 _scroll;

        // 섹션 폴드아웃 상태
        private bool _showPerTypeCurrent;
        private bool _showPerTypeAverage;
        private bool _showPerTypeMax;
        private bool _showPerTypeFrames;
        private bool _showPerTypeCurrentPercent;
        private bool _showPerTypeAveragePercent;
        private bool _showPerTypePercents;

        // 타입별 프레임 타임(배열) 상세 폴드아웃 상태
        private readonly Dictionary<Type, bool> _perTypeFramesFoldout = new();

        [MenuItem("RT/Debug Manager")] 
        public static void ShowWindow()
        {
            var win = GetWindow<DebugManagerWindow>(false, "Debug Manager", true);
            win.minSize = new Vector2(420, 260);
            win.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // 플레이 중에는 갱신 주기가 빨라야 하므로 매 프레임 리페인트
            if (EditorApplication.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("DebugManager Metrics", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Current Total Frame Time", DebugManager.CurrentTotalFrameTime);
                EditorGUILayout.FloatField("Average Total Frame Time", DebugManager.AverageTotalFrameTime);
                EditorGUILayout.FloatField("Max Total Frame Time", DebugManager.MaxTotalFrameTime);
                EditorGUILayout.IntField("TotalFrameTimes Length", DebugManager.TotalFrameTimes?.Length ?? 0);
            }

            EditorGUILayout.Space(6);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawDictionarySection(
                "Per-Type Current Frame Time",
                ref _showPerTypeCurrent,
                DebugManager.PerTypeCurrentFrameTimeReadOnly,
                (type, value) => EditorGUILayout.FloatField(type.FullName, value)
            );

            DrawDictionarySection(
                "Per-Type Average Frame Time",
                ref _showPerTypeAverage,
                DebugManager.PerTypeAverageFrameTimeReadOnly,
                (type, value) => EditorGUILayout.FloatField(type.FullName, value)
            );

            DrawDictionarySection(
                "Per-Type Max Frame Time",
                ref _showPerTypeMax,
                DebugManager.PerTypeMaxFrameTimesReadOnly,
                (type, value) => EditorGUILayout.FloatField(type.FullName, value)
            );

            // 비율(%) 관련 섹션
            DrawDictionarySection(
                "Per-Type Current Frame Percent (0~1)",
                ref _showPerTypeCurrentPercent,
                DebugManager.PerTypeCurrentFramePercentReadOnly,
                (type, value) => EditorGUILayout.FloatField(type.FullName, value)
            );

            DrawDictionarySection(
                "Per-Type Average Frame Percent (0~1)",
                ref _showPerTypeAveragePercent,
                DebugManager.PerTypeAverageFramePercentReadOnly,
                (type, value) => EditorGUILayout.FloatField(type.FullName, value)
            );

            // PerTypeFrameTimes는 타입별로 배열이 크므로 타입 항목 자체를 폴드할 수 있도록 별도 처리
            _showPerTypeFrames = EditorGUILayout.Foldout(_showPerTypeFrames, "Per-Type Frame Times (history)", true);
            if (_showPerTypeFrames)
            {
                EditorGUI.indentLevel++;
                var dict = DebugManager.PerTypeFrameTimesReadOnly;
                if (dict != null && dict.Count > 0)
                {
                    foreach (var kvp in dict.OrderBy(k => k.Key.FullName))
                    {
                        var t = kvp.Key;
                        var arr = kvp.Value;
                        if (!_perTypeFramesFoldout.ContainsKey(t)) _perTypeFramesFoldout[t] = false;
                        _perTypeFramesFoldout[t] = EditorGUILayout.Foldout(_perTypeFramesFoldout[t], t.FullName, true);
                        if (_perTypeFramesFoldout[t])
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.IntField("Length", arr?.Length ?? 0);
                            if (arr != null && arr.Length > 0)
                            {
                                // 미니 그리드로 앞 20개만 미리보기
                                int preview = Mathf.Min(20, arr.Length);
                                for (int i = 0; i < preview; i++)
                                {
                                    EditorGUILayout.FloatField($"[{i}]", arr[i]);
                                }
                                if (arr.Length > preview)
                                {
                                    EditorGUILayout.LabelField($"... (+{arr.Length - preview} more)");
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("(empty)");
                }
                EditorGUI.indentLevel--;
            }

            // 비율 히스토리 표시
            _showPerTypePercents = EditorGUILayout.Foldout(_showPerTypePercents, "Per-Type Frame Percents (history 0~1)", true);
            if (_showPerTypePercents)
            {
                EditorGUI.indentLevel++;
                var dictP = DebugManager.PerTypeFramePercentsReadOnly;
                if (dictP != null && dictP.Count > 0)
                {
                    foreach (var kvp in dictP.OrderBy(k => k.Key.FullName))
                    {
                        var t = kvp.Key;
                        var arr = kvp.Value;
                        if (!_perTypeFramesFoldout.ContainsKey(t)) _perTypeFramesFoldout[t] = false;
                        _perTypeFramesFoldout[t] = EditorGUILayout.Foldout(_perTypeFramesFoldout[t], t.FullName, true);
                        if (_perTypeFramesFoldout[t])
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.IntField("Length", arr?.Length ?? 0);
                            if (arr != null && arr.Length > 0)
                            {
                                int preview = Mathf.Min(20, arr.Length);
                                for (int i = 0; i < preview; i++)
                                {
                                    EditorGUILayout.FloatField($"[{i}]", arr[i]);
                                }
                                if (arr.Length > preview)
                                {
                                    EditorGUILayout.LabelField($"... (+{arr.Length - preview} more)");
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("(empty)");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawDictionarySection<T>(
            string title,
            ref bool foldout,
            IReadOnlyDictionary<Type, T> dict,
            Action<Type, T> drawRow)
        {
            foldout = EditorGUILayout.Foldout(foldout, title, true);
            if (!foldout) return;

            EditorGUI.indentLevel++;
            if (dict != null && dict.Count > 0)
            {
                foreach (var kvp in dict.OrderBy(k => k.Key.FullName))
                {
                    drawRow(kvp.Key, kvp.Value);
                }
            }
            else
            {
                EditorGUILayout.LabelField("(empty)");
            }
            EditorGUI.indentLevel--;
        }
    }
}
#endif
