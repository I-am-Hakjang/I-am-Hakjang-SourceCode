#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using Component = UnityEngine.Component;

namespace Hakjang
{
    public class DebugManager
    {
        public const int MAX_FRAME_TIME_COUNT = 600;
        internal static Dictionary<Type, float> PerTypeCurrentFrameTime { get; } = new();
        // Editor 전용 창에서 읽기 전용으로 접근할 수 있도록 공개 ReadOnly 뷰 제공
        public static IReadOnlyDictionary<Type, float> PerTypeCurrentFrameTimeReadOnly => PerTypeCurrentFrameTime;
        public static float CurrentTotalFrameTime { get; internal set; }
        
        internal static Dictionary<Type, float> PerTypeAverageFrameTime { get; } = new();
        public static IReadOnlyDictionary<Type, float> PerTypeAverageFrameTimeReadOnly => PerTypeAverageFrameTime;
        public static float AverageTotalFrameTime { get; internal set; }
        
        internal static Dictionary<Type, float[]> PerTypeFrameTimes { get; } = new();
        public static IReadOnlyDictionary<Type, float[]> PerTypeFrameTimesReadOnly => PerTypeFrameTimes;
        public static float[] TotalFrameTimes { get; private set; } = new float[MAX_FRAME_TIME_COUNT];

        private static Dictionary<Type, float> PerTypeMaxFrameTimes { get; } = new();
        public static IReadOnlyDictionary<Type, float> PerTypeMaxFrameTimesReadOnly => PerTypeMaxFrameTimes;
        public static float MaxTotalFrameTime { get; private set; }

        // 비율(퍼센트, 0~1) 기록용
        internal static Dictionary<Type, float> PerTypeCurrentFramePercent { get; } = new();
        public static IReadOnlyDictionary<Type, float> PerTypeCurrentFramePercentReadOnly => PerTypeCurrentFramePercent;

        internal static Dictionary<Type, float> PerTypeAverageFramePercent { get; } = new();
        public static IReadOnlyDictionary<Type, float> PerTypeAverageFramePercentReadOnly => PerTypeAverageFramePercent;

        internal static Dictionary<Type, float[]> PerTypeFramePercents { get; } = new();
        public static IReadOnlyDictionary<Type, float[]> PerTypeFramePercentsReadOnly => PerTypeFramePercents;

        private static void RecordMetrics(Type type, float duration)
        {
            // 플레이 모드에서만 누적 통계를 갱신
            if (!Application.isPlaying)
                return;

            // 총합
            CurrentTotalFrameTime += duration;
            if (duration > MaxTotalFrameTime)
                MaxTotalFrameTime = duration;

            // 타입별
            if (!PerTypeCurrentFrameTime.TryGetValue(type, out var current))
            {
                current = 0f;
                PerTypeFrameTimes[type] = new float[MAX_FRAME_TIME_COUNT];
                PerTypeAverageFrameTime[type] = 0f;
                PerTypeMaxFrameTimes[type] = 0f;
                // 비율 관련 초기화
                PerTypeFramePercents[type] = new float[MAX_FRAME_TIME_COUNT];
                PerTypeAverageFramePercent[type] = 0f;
                PerTypeCurrentFramePercent[type] = 0f;
            }

            current += duration;
            PerTypeCurrentFrameTime[type] = current;

            if (!PerTypeMaxFrameTimes.TryGetValue(type, out var tMax) || duration > tMax)
                PerTypeMaxFrameTimes[type] = duration;
        }

        public static T GetComponent<T>(MonoBehaviour mono)
        {
            var start = Time.realtimeSinceStartup;
            var component = mono.GetComponent<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return component;
        }

        // GameObject overloads
        public static T GetComponent<T>(GameObject go)
        {
            var start = Time.realtimeSinceStartup;
            var component = go.GetComponent<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return component;
        }

        public static T[] GetComponents<T>(MonoBehaviour mono)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponents<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static T[] GetComponents<T>(GameObject go)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponents<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static Component GetComponent(MonoBehaviour mono, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var component = mono.GetComponent(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return component;
        }

        public static Component GetComponent(GameObject go, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var component = go.GetComponent(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return component;
        }

        public static Component[] GetComponents(MonoBehaviour mono, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponents(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static Component[] GetComponents(GameObject go, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponents(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static T GetComponentInChildren<T>(MonoBehaviour mono)
        {
            var start = Time.realtimeSinceStartup;
            var component = mono.GetComponentInChildren<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return component;
        }

        public static T GetComponentInChildren<T>(GameObject go)
        {
            var start = Time.realtimeSinceStartup;
            var component = go.GetComponentInChildren<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return component;
        }

        public static T GetComponentInChildren<T>(MonoBehaviour mono, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var component = mono.GetComponentInChildren<T>(include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return component;
        }

        public static T GetComponentInChildren<T>(GameObject go, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var component = go.GetComponentInChildren<T>(include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return component;
        }

        public static Component GetComponentInChildren(MonoBehaviour mono, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var component = mono.GetComponentInChildren(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return component;
        }

        public static Component GetComponentInChildren(GameObject go, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var component = go.GetComponentInChildren(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return component;
        }

        public static Component GetComponentInChildren(MonoBehaviour mono, Type type, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var component = mono.GetComponentInChildren(type, include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return component;
        }

        public static Component GetComponentInChildren(GameObject go, Type type, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var component = go.GetComponentInChildren(type, include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return component;
        }

        public static T[] GetComponentsInChildren<T>(MonoBehaviour mono)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponentsInChildren<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static T[] GetComponentsInChildren<T>(GameObject go)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponentsInChildren<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static T[] GetComponentsInChildren<T>(MonoBehaviour mono, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponentsInChildren<T>(include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static T[] GetComponentsInChildren<T>(GameObject go, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponentsInChildren<T>(include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static Component[] GetComponentsInChildren(MonoBehaviour mono, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponentsInChildren(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static Component[] GetComponentsInChildren(GameObject go, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponentsInChildren(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static Component[] GetComponentsInChildren(MonoBehaviour mono, Type type, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponentsInChildren(type, include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static Component[] GetComponentsInChildren(GameObject go, Type type, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponentsInChildren(type, include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static T GetComponentInParent<T>(MonoBehaviour mono)
        {
            var start = Time.realtimeSinceStartup;
            var component = mono.GetComponentInParent<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return component;
        }

        public static T GetComponentInParent<T>(GameObject go)
        {
            var start = Time.realtimeSinceStartup;
            var component = go.GetComponentInParent<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return component;
        }

        public static Component GetComponentInParent(MonoBehaviour mono, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var component = mono.GetComponentInParent(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return component;
        }

        public static Component GetComponentInParent(GameObject go, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var component = go.GetComponentInParent(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return component;
        }

        public static T[] GetComponentsInParent<T>(MonoBehaviour mono)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponentsInParent<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static T[] GetComponentsInParent<T>(GameObject go)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponentsInParent<T>();
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static T[] GetComponentsInParent<T>(MonoBehaviour mono, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponentsInParent<T>(include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static T[] GetComponentsInParent<T>(GameObject go, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponentsInParent<T>(include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return components;
        }

        public static Component[] GetComponentsInParent(MonoBehaviour mono, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponentsInParent(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static Component[] GetComponentsInParent(GameObject go, Type type)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponentsInParent(type);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static Component[] GetComponentsInParent(MonoBehaviour mono, Type type, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var components = mono.GetComponentsInParent(type, include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static Component[] GetComponentsInParent(GameObject go, Type type, bool include_inactive)
        {
            var start = Time.realtimeSinceStartup;
            var components = go.GetComponentsInParent(type, include_inactive);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return components;
        }

        public static bool TryGetComponent<T>(MonoBehaviour mono, out T component)
        {
            var start = Time.realtimeSinceStartup;
            var result = mono.TryGetComponent<T>(out component);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return result;
        }

        public static bool TryGetComponent<T>(GameObject go, out T component)
        {
            var start = Time.realtimeSinceStartup;
            var result = go.TryGetComponent<T>(out component);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(typeof(T), dt);
            return result;
        }

        public static bool TryGetComponent(MonoBehaviour mono, Type type, out Component component)
        {
            var start = Time.realtimeSinceStartup;
            var result = mono.TryGetComponent(type, out component);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return result;
        }

        public static bool TryGetComponent(GameObject go, Type type, out Component component)
        {
            var start = Time.realtimeSinceStartup;
            var result = go.TryGetComponent(type, out component);
            var dt = Time.realtimeSinceStartup - start;
            RecordMetrics(type, dt);
            return result;
        }
    }
}
#endif