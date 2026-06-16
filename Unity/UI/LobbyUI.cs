using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.LOBBY_UI)]
    public class LobbyUI : MonoBehaviour
    {
        #region Field
        [SerializeField] private ConnectionController _connectionController;
        [SerializeField] private GameObject _rootObject;
        [SerializeField] private TextMeshProUGUI _playerListText;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private Button _startGameButton;

        private readonly StringBuilder _stringBuilder = new StringBuilder(256);
        #endregion

        #region Method
        private void Awake()
        {
            if (_leaveButton != null)
            {
                _leaveButton.onClick.AddListener(OnClickLeaveButton);
            }

            if (_startGameButton != null)
            {
                _startGameButton.onClick.AddListener(OnClickStartGameButton);
            }
        }

        private void OnEnable()
        {
            if (_connectionController == null)
            {
                return;
            }

            _connectionController.OnFlowChanged += HandleFlowChanged;
            _connectionController.OnLobbyUpdated += HandleLobbyUpdated;
        }

        private void OnDisable()
        {
            if (_connectionController == null)
            {
                return;
            }

            _connectionController.OnFlowChanged -= HandleFlowChanged;
            _connectionController.OnLobbyUpdated -= HandleLobbyUpdated;
        }

        private void OnDestroy()
        {
            if (_leaveButton != null)
            {
                _leaveButton.onClick.RemoveListener(OnClickLeaveButton);
            }

            if (_startGameButton != null)
            {
                _startGameButton.onClick.RemoveListener(OnClickStartGameButton);
            }
        }

        private void OnClickLeaveButton()
        {
            if (_connectionController == null)
            {
                return;
            }

            _connectionController.RequestLeaveRoom();
        }

        private void OnClickStartGameButton()
        {
            if (_connectionController == null)
            {
                return;
            }

            _connectionController.RequestStartGame();
        }

        private void HandleFlowChanged(ConnectionController.FlowState flow_state)
        {
            bool is_lobby = flow_state == ConnectionController.FlowState.LOBBY;

            if (_rootObject != null)
            {
                _rootObject.SetActive(is_lobby);
            }
            else
            {
                gameObject.SetActive(is_lobby);
            }
        }

        private void HandleLobbyUpdated(ConnectionController.LobbyViewData lobby_view_data)
        {
            if (lobby_view_data == null)
            {
                return;
            }

            if (_playerListText != null)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append("Room: ");
                _stringBuilder.Append(lobby_view_data.roomId);
                _stringBuilder.Append('\n');
                _stringBuilder.Append("Players: ");
                _stringBuilder.Append(lobby_view_data.playerCount);
                _stringBuilder.Append('/');
                _stringBuilder.Append(lobby_view_data.maxPlayer);
                _stringBuilder.Append('\n');

                if (lobby_view_data.playerDisplayNames != null)
                {
                    int playerCount = lobby_view_data.playerDisplayNames.Length;
                    for (int i = 0; i < playerCount; i++)
                    {
                        _stringBuilder.Append(i + 1);
                        _stringBuilder.Append(". ");
                        _stringBuilder.Append(lobby_view_data.playerDisplayNames[i]);
                        _stringBuilder.Append('\n');
                    }
                }

                _playerListText.text = _stringBuilder.ToString();
            }

            if (_startGameButton != null)
            {
                _startGameButton.gameObject.SetActive(lobby_view_data.isHost);
            }
        }
        #endregion
    }
}
