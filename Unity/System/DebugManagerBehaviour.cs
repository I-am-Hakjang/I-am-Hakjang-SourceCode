#if UNITY_EDITOR
using System.Collections;
using UnityEngine;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.DEBUG_MANAGER)]
    public class DebugManagerBehaviour : ManagerBehaviour
    {
        private int frameIndex;
        private int recordedFrameCount;
        
        private void LateUpdate()
        {
            SaveCurrentFrameToHistory();
            CalculateAverages();
            ResetCurrentFrameData();
        
            frameIndex = (frameIndex + 1) % DebugManager.MAX_FRAME_TIME_COUNT;
            if (recordedFrameCount < DebugManager.MAX_FRAME_TIME_COUNT) 
                recordedFrameCount++;
        }

        private void Start()
        {
            StartCoroutine(EndOfFrameCoroutine());
        }

        /// <summary>
        /// WaitForEndOfFrame을 사용하여 모든 Update/LateUpdate 이후에 실행
        /// </summary>
        private IEnumerator EndOfFrameCoroutine()
        {
            while (enabled)
            {
                yield return new WaitForEndOfFrame();

                SaveCurrentFrameToHistory();
                CalculateAverages();
                ResetCurrentFrameData();

                frameIndex = (frameIndex + 1) % DebugManager.MAX_FRAME_TIME_COUNT;
                if (recordedFrameCount < DebugManager.MAX_FRAME_TIME_COUNT)
                    recordedFrameCount++;
            }
        }

        /// <summary>
        /// 현재 프레임의 누적 데이터를 히스토리 배열에 저장
        /// </summary>
        private void SaveCurrentFrameToHistory()
        {
            // 전체 프레임 시간 저장
            DebugManager.TotalFrameTimes[frameIndex] = DebugManager.CurrentTotalFrameTime;

            // 타입별 프레임 시간 저장
            foreach (var (type, time) in DebugManager.PerTypeCurrentFrameTime)
            {
                if (!DebugManager.PerTypeFrameTimes.TryGetValue(type, out var history)) continue;
                
                history[frameIndex] = time;

                // 타입별 비율(0~1) 저장: 현재 프레임 내 차지한 비율
                float percent = DebugManager.CurrentTotalFrameTime > 0f
                    ? time / DebugManager.CurrentTotalFrameTime
                    : 0f;

                if (DebugManager.PerTypeFramePercents.TryGetValue(type, out var percentHistory))
                {
                    percentHistory[frameIndex] = percent;
                }

                DebugManager.PerTypeCurrentFramePercent[type] = percent;
            }
        }

        /// <summary>
        /// 히스토리 배열을 기반으로 평균값 계산
        /// </summary>
        private void CalculateAverages()
        {
            // 전체 프레임 평균 계산
            float totalSum = 0f;
            for (int i = 0; i < recordedFrameCount; i++)
            {
                totalSum += DebugManager.TotalFrameTimes[i];
            }
            DebugManager.AverageTotalFrameTime = recordedFrameCount > 0 ? totalSum / recordedFrameCount : 0f;

            // 타입별 평균 계산
            foreach (var (type, history) in DebugManager.PerTypeFrameTimes)
            {
                float typeSum = 0f;
                for (int i = 0; i < recordedFrameCount; i++)
                {
                    typeSum += history[i];
                }

                DebugManager.PerTypeAverageFrameTime[type] = recordedFrameCount > 0 ? typeSum / recordedFrameCount : 0f;

                // 타입별 비율 평균
                if (DebugManager.PerTypeFramePercents.TryGetValue(type, out var percentHistory))
                {
                    float percentSum = 0f;
                    for (int i = 0; i < recordedFrameCount; i++)
                    {
                        percentSum += percentHistory[i];
                    }

                    DebugManager.PerTypeAverageFramePercent[type] =
                        recordedFrameCount > 0 ? percentSum / recordedFrameCount : 0f;
                }
            }
        }

        /// <summary>
        /// 다음 프레임을 위해 현재 프레임 데이터 초기화
        /// </summary>
        private void ResetCurrentFrameData()
        {
            DebugManager.PerTypeCurrentFrameTime.Clear();
            DebugManager.CurrentTotalFrameTime = 0f;
            DebugManager.PerTypeCurrentFramePercent.Clear();
        }
    }
}
#endif