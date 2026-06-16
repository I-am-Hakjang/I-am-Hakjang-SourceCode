using TMPro;
using UnityEngine;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.GameUI)]
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private GameOverUI _gameOverUI;
        [SerializeField] private TextMeshProUGUI _killStat;

        private void Awake()
        {
            Root.sNetworkManager.OnGameOver += HandleGameOver;
            Root.sNetworkManager.OnDisconnected += HandleDisConnected;
            Root.sNetworkManager.OnGameStart += HandleGameStart;
            Root.sNetworkManager.OnKillStatChanged += HandleKillStatChanged;
        }

        private void HandleGameOver(NetworkManager.LeaderBoard board)
        {
            _gameOverUI.gameObject.SetActive(true);
            _gameOverUI.SetResultText(board.items);
            _killStat.gameObject.SetActive(false);
        }

        private void HandleGameStart(NetworkManager.GameStartData _)
        {
            _killStat.gameObject.SetActive(true);
            _killStat.text = $"PLAYER KILLS: 0 / NPC KILLS: 0";
        }
        private void HandleKillStatChanged(NetworkManager.KillStat stat)
        {
            _killStat.text = $"PLAYER KILLS: {stat.playerKillCount} / NPC KILLS: {stat.npcKillCount}";
        }

        private void HandleDisConnected()
        {
            _gameOverUI.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Root.sNetworkManager.OnGameOver -= HandleGameOver;
            Root.sNetworkManager.OnDisconnected -= HandleDisConnected;
            Root.sNetworkManager.OnGameStart -= HandleGameStart;
            Root.sNetworkManager.OnKillStatChanged -= HandleKillStatChanged;

        }
    }
}