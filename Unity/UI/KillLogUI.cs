using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.KILL_LOG_UI)]
    public class KillLogUI : MonoBehaviour
    {
        private struct KillLogData
        {
            public string KillerName;
            public string VictimName;

            public KillLogData(string killer_name, string victim_name)
            {
                KillerName = killer_name;
                VictimName = victim_name;
            }
        }

        [SerializeField] private TextMeshProUGUI[] _texts;
        [SerializeField] private float _visibleDuration = 2f;
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private string _killLogFormat = "{0} -> {1}";

        private readonly Queue<TextMeshProUGUI> _inactiveTextQueue = new Queue<TextMeshProUGUI>();
        private readonly Queue<KillLogData> _pendingLogQueue = new Queue<KillLogData>();

        private void Awake()
        {
            InitializeTextQueue();
            Root.sNetworkManager.OnKilled += HandleKilled;
        }

        private void HandleKilled(BaseUnit killer_unit, BaseUnit target_unit)
        {
            if (target_unit is NPC)
                return;

            var killerNickName = Root.sNetworkManager.GetPlayerNickName(killer_unit.Id);
            var targetNickName = Root.sNetworkManager.GetPlayerNickName(target_unit.Id);

            SetKillLog(killerNickName, targetNickName);
        }

        private void InitializeTextQueue()
        {
            _inactiveTextQueue.Clear();

            if (_texts == null)
            {
                return;
            }

            for (var i = 0; i < _texts.Length; i++)
            {
                var text = _texts[i];
                if (text == null)
                {
                    continue;
                }

                text.DOKill();

                var color = text.color;
                color.a = 1f;
                text.color = color;
                text.gameObject.SetActive(false);

                _inactiveTextQueue.Enqueue(text);
            }
        }

        public void SetKillLog(string killer_name, string victim_name)
        {
            _pendingLogQueue.Enqueue(new KillLogData(killer_name, victim_name));
            TryShowNextLog();
        }

        private void TryShowNextLog()
        {
            while (_pendingLogQueue.Count > 0 && _inactiveTextQueue.Count > 0)
            {
                var logData = _pendingLogQueue.Dequeue();
                var text = _inactiveTextQueue.Dequeue();

                ShowLog(text, logData);
            }
        }

        private void ShowLog(TextMeshProUGUI text, KillLogData log_data)
        {
            text.DOText(string.Format(_killLogFormat, log_data.KillerName, log_data.VictimName), 0.5f);

            var color = text.color;
            color.a = 1f;
            text.color = color;
            text.gameObject.SetActive(true);

            var sequence = DOTween.Sequence();
            sequence.AppendInterval(_visibleDuration);
            sequence.Append(text.DOFade(0f, _fadeOutDuration));
            sequence.OnComplete(() =>
            {
                if (text == null)
                {
                    return;
                }

                var completedColor = text.color;
                completedColor.a = 1f;
                text.color = completedColor;
                text.gameObject.SetActive(false);
                _inactiveTextQueue.Enqueue(text);

                TryShowNextLog();
            });
        }

        private void OnDestroy()
        {
            if (_texts != null)
            {
                for (var i = 0; i < _texts.Length; i++)
                {
                    var text = _texts[i];
                    if (text == null)
                    {
                        continue;
                    }

                    text.DOKill();
                }
            }

            Root.sNetworkManager.OnKilled -= HandleKilled;
        }
    }
}