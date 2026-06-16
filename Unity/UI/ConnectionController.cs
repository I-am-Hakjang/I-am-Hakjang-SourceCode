using System;
using UnityEngine;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.CONNECTION_CONTROLLER)]
    public class ConnectionController : MonoBehaviour
    {
        #region Definition
        public enum FlowState
        {
            CONNECT,
            LOBBY,
            IN_GAME,
        }

        public class LobbyViewData
        {
            public string roomId;
            public string[] playerDisplayNames;
            public bool isHost;
            public int playerCount;
            public int maxPlayer;
        }
        #endregion

        #region Property
        public string LocalPlayerUid => _localPlayerUid;
        #endregion

        #region Field
        private const string STATUS_READY = "닉네임을 입력하고 연결하세요.";
        private const string STATUS_CONNECTING = "연결 중...";
        private const string STATUS_CONNECTED = "연결 성공";
        private const string STATUS_DISCONNECTED = "연결이 종료되었습니다.";
        private const string STATUS_INVALID_NICKNAME = "닉네임을 입력해주세요.";
        private const string STATUS_ONLY_HOST_CAN_START = "방장만 게임 시작이 가능합니다.";
        private const string STATUS_ERROR_PREFIX = "오류: ";
        private const string UNKNOWN_PLAYER = "Unknown";
        private const string READY_TEXT = "READY";
        private const string NOT_READY_TEXT = "NOT READY";

        private string _localPlayerId = string.Empty;
        private string _localPlayerUid = string.Empty;
        private NetworkManager.Room _currentRoom;

        private readonly LobbyViewData _lobbyViewData = new LobbyViewData();
        #endregion

        #region Event
        public event Action<FlowState> OnFlowChanged;
        public event Action<string> OnStatusChanged;
        public event Action<LobbyViewData> OnLobbyUpdated;
        #endregion

        #region Method
        private void Awake()
        {
            PublishFlow(FlowState.CONNECT);
            PublishStatus(STATUS_READY);
        }

        private void OnEnable()
        {
            Root.sNetworkManager.OnQuickMatch += HandleQuickMatch;
            Root.sNetworkManager.OnRoomUpdate += HandleRoomUpdate;
            Root.sNetworkManager.OnGameStart += HandleGameStart;
            Root.sNetworkManager.OnDisconnected += HandleDisconnected;
            Root.sNetworkManager.OnError += HandleError;
        }

        private void OnDisable()
        {
            Root.sNetworkManager.OnQuickMatch -= HandleQuickMatch;
            Root.sNetworkManager.OnRoomUpdate -= HandleRoomUpdate;
            Root.sNetworkManager.OnGameStart -= HandleGameStart;
            Root.sNetworkManager.OnDisconnected -= HandleDisconnected;
            Root.sNetworkManager.OnError -= HandleError;
        }

        public void RequestConnect(string player_id)
        {
            if (string.IsNullOrEmpty(player_id))
            {
                PublishStatus(STATUS_INVALID_NICKNAME);
                return;
            }

            _localPlayerId = player_id;
            PublishStatus(STATUS_CONNECTING);
            Root.sNetworkManager.Connect(player_id);
        }

        public void RequestLeaveRoom()
        {
            if (_currentRoom != null && !string.IsNullOrEmpty(_currentRoom.id) && !string.IsNullOrEmpty(_localPlayerUid))
            {
                Root.sNetworkManager.SendLeaveRoom(_currentRoom.id, _localPlayerUid);
            }

            Root.sNetworkManager.Disconnect();
        }

        public void RequestStartGame()
        {
            if (_currentRoom == null || string.IsNullOrEmpty(_currentRoom.id) || string.IsNullOrEmpty(_localPlayerUid))
            {
                return;
            }

            if (!IsLocalPlayerHost())
            {
                PublishStatus(STATUS_ONLY_HOST_CAN_START);
                return;
            }

            Root.sNetworkManager.SendStartGame(_currentRoom.id, _localPlayerUid);
        }

        private void HandleQuickMatch(NetworkManager.QuickMatchResponse quick_match_response)
        {
            if (quick_match_response == null || quick_match_response.room == null)
            {
                return;
            }

            _localPlayerUid = quick_match_response.uid ?? string.Empty;
            _currentRoom = quick_match_response.room;

            PublishStatus(STATUS_CONNECTED);
            PublishFlow(FlowState.LOBBY);
            PublishLobby(_currentRoom);
        }

        private void HandleRoomUpdate(NetworkManager.Room room)
        {
            if (room == null)
            {
                return;
            }

            _currentRoom = room;
            PublishLobby(room);
        }

        private void HandleGameStart(NetworkManager.GameStartData game_start_data)
        {
            PublishFlow(FlowState.IN_GAME);
        }

        private void HandleDisconnected()
        {
            _currentRoom = null;
            _localPlayerUid = string.Empty;

            PublishFlow(FlowState.CONNECT);
            PublishStatus(STATUS_DISCONNECTED);
        }

        private void HandleError(NetworkManager.ErrorData error_data)
        {
            if (error_data == null || string.IsNullOrEmpty(error_data.message))
            {
                return;
            }

            PublishStatus(STATUS_ERROR_PREFIX + error_data.message);
        }

        private void PublishFlow(FlowState flow_state)
        {
            OnFlowChanged?.Invoke(flow_state);
        }

        private void PublishStatus(string status_message)
        {
            OnStatusChanged?.Invoke(status_message);
        }

        private void PublishLobby(NetworkManager.Room room)
        {
            if (room == null)
            {
                return;
            }

            _lobbyViewData.roomId = room.id;
            _lobbyViewData.playerCount = room.players != null ? room.players.Length : 0;
            _lobbyViewData.maxPlayer = room.maxPlayer;
            _lobbyViewData.isHost = IsHostUid(room.hostUid, _localPlayerUid);

            if (room.players == null || room.players.Length == 0)
            {
                _lobbyViewData.playerDisplayNames = Array.Empty<string>();
            }
            else
            {
                int playerCount = room.players.Length;

                if (_lobbyViewData.playerDisplayNames == null || _lobbyViewData.playerDisplayNames.Length != playerCount)
                {
                    _lobbyViewData.playerDisplayNames = new string[playerCount];
                }

                for (int i = 0; i < playerCount; i++)
                {
                    var player = room.players[i];
                    if (player == null)
                    {
                        _lobbyViewData.playerDisplayNames[i] = UNKNOWN_PLAYER;
                        continue;
                    }

                    string playerName = !string.IsNullOrEmpty(player.id) ? player.id : UNKNOWN_PLAYER;
                    string readyText = player.isReady ? READY_TEXT : NOT_READY_TEXT;
                    _lobbyViewData.playerDisplayNames[i] = playerName + " (" + readyText + ")";
                }
            }

            OnLobbyUpdated?.Invoke(_lobbyViewData);
        }

        private bool IsLocalPlayerHost()
        {
            if (_currentRoom == null)
            {
                return false;
            }

            return IsHostUid(_currentRoom.hostUid, _localPlayerUid);
        }

        private bool IsHostUid(string host_uid, string local_uid)
        {
            if (string.IsNullOrEmpty(host_uid) || string.IsNullOrEmpty(local_uid))
            {
                return false;
            }

            return host_uid == local_uid;
        }
        #endregion
    }
}
