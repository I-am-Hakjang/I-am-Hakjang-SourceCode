using TMPro;
using UnityEngine;

namespace Hakjang
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] _resultTexts;

        public void SetResultText(NetworkManager.PlayerResultData[] results)
        {
            for (int i = 0; i < results.Length && i < _resultTexts.Length; i++)
            {
                _resultTexts[i].text = $"{i + 1}. {Root.sNetworkManager.GetPlayerNickName(results[i].playerUid)} - {results[i].playerKillCount} kills, {results[i].npcKillCount} NPC kills";
            }
        }
    }
}