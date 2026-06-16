using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Hakjang
{
    public partial class NetworkManager
    {
        #region Definition
        private enum ConnectionState
        {
            DISCONNECTED,
            CONNECTING,
            CONNECTED,
            DISCONNECTING,
        }

        #endregion

        #region Property
        public bool IsConnected => _connectionState == ConnectionState.CONNECTED;
        public IReadOnlyDictionary<string, Player> PlayerObjects => _playerObjects;

        public string GetPlayerNickName(string player_uid)
        {
            if (string.IsNullOrEmpty(player_uid))
                return string.Empty;

            if (_playerNickNames.TryGetValue(player_uid, out var nickName))
                return nickName;

            return string.Empty;
        }
        #endregion

        #region Field
        private const int RECEIVE_BUFFER_SIZE = 8192;
        private const string DATA_KEY = "\"data\"";
        private const string CONNECTED_EVENT = "connected";
        private const string QUICK_MATCH_EVENT = "quickMatch";
        private const string AVAILABLE_ROOMS_EVENT = "availableRooms";
        private const string ROOM_UPDATE_EVENT = "roomUpdate";
        private const string GAME_START_EVENT = "gameStart";
        private const string ERROR_EVENT = "error";
        private const string ATTACK_EVENT = "attack";
        private const string KILL_EVENT = "kill";
        private const string KILL_STAT_EVENT = "killStat";
        private const string GAME_OVER_EVENT = "gameOver";
        private const string WORLD_EVENT = "event";
        private const string SMOKE_EVENT_TYPE = "SMOKE";
        private const string POS_EVENT = "unitSync";
        private const long SMOKE_EFFECT_DURATION_MILLISECONDS = 10_000;

        private readonly Queue<Action> _mainThreadActions = new Queue<Action>();
        private readonly Queue<OutgoingMessage> _sendQueue = new Queue<OutgoingMessage>();
        private readonly object _mainThreadLockObject = new object();
        private readonly object _sendQueueLockObject = new object();
        private readonly object _positionSyncLockObject = new object();
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _sendQueueSignal = new SemaphoreSlim(0);
        private readonly Dictionary<string, Player> _playerObjects = new Dictionary<string, Player>();
        private readonly Dictionary<string, NPC> _npcObjects = new();
        private readonly Dictionary<string, string> _playerNickNames = new();

        private readonly Dictionary<string, NetworkedTransform> _networkedTransforms = new Dictionary<string, NetworkedTransform>();
        private readonly Dictionary<string, NetworkedAnimation> _networkedAnimations = new Dictionary<string, NetworkedAnimation>();
        private readonly Dictionary<string, PositionSyncData> _pendingPositionSyncByUid = new Dictionary<string, PositionSyncData>();
        private readonly List<PositionSyncData> _positionSyncApplyBuffer = new List<PositionSyncData>();
        private readonly List<SmokeEffectLifecycle> _activeSmokeEffects = new List<SmokeEffectLifecycle>();

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _receiveCancellationTokenSource;
        private CancellationTokenSource _sendCancellationTokenSource;
        private OutgoingMessage _pendingPoseSendMessage;
        private ConnectionState _connectionState = ConnectionState.DISCONNECTED;
        private int _connectionGeneration;
        private string _serverUrl = string.Empty;
        private string _gameUrl = string.Empty;
        private string _localPlayerUid = string.Empty;
        private Room _currentRoom;
        private bool _isSwitchingToGameServer;
        #endregion

        #region Event
        public event Action<string> OnConnected;
        public event Action OnDisconnected;
        public event Action<QuickMatchResponse> OnQuickMatch;
        public event Action<Room[]> OnAvailableRooms;
        public event Action<Room> OnRoomUpdate;
        public event Action<GameStartData> OnGameStart;
        public event Action<ErrorData> OnError;
        public event Action<BaseUnit, BaseUnit> OnKilled;
        public event Action<KillStat> OnKillStatChanged;
        public event Action<LeaderBoard> OnGameOver;
        #endregion

        #region Method
        public void OnAwake(string server_url, string game_url)
        {
            ResetConnectionResources();

            _serverUrl = server_url ?? string.Empty;
            _gameUrl = game_url ?? string.Empty;

            lock (_mainThreadLockObject)
            {
                _mainThreadActions.Clear();
            }

            ClearSendQueue();
            ClearPendingPositionSync();
            ClearPlayerObjects();
            ClearNpcObjects();
            ClearSmokeEffects();
        }

        public void OnUpdate()
        {
            FlushMainThreadQueue();
            FlushPendingPositionSync();
            FlushExpiredSmokeEffects();
        }

        public void OnDestroy()
        {
            Disconnect();
        }

        public async void Connect(string player_id)
        {
            if (string.IsNullOrEmpty(_serverUrl))
            {
                Debug.LogError("[NetworkManager] 서버 URL이 비어 있습니다.");
                return;
            }

            if (_connectionState == ConnectionState.CONNECTING || _connectionState == ConnectionState.CONNECTED)
            {
                return;
            }

            bool isConnected = await ConnectCore(_serverUrl);
            if (!isConnected)
            {
                return;
            }

            if (!string.IsNullOrEmpty(player_id))
            {
                SendQuickMatch(player_id);
            }
        }

        public async void Disconnect()
        {
            await DisconnectCore(true, true, true);
        }

        public void RegisterNetworkedTransform(string player_uid, NetworkedTransform networked_transform)
        {
            if (string.IsNullOrEmpty(player_uid) || networked_transform == null)
            {
                return;
            }

            _networkedTransforms[player_uid] = networked_transform;
        }

        public void RegisterNetworkedAnimation(string player_uid, NetworkedAnimation networked_animation)
        {
            if (string.IsNullOrEmpty(player_uid) || networked_animation == null)
            {
                return;
            }

            _networkedAnimations[player_uid] = networked_animation;
        }

        public void UnregisterNetworkedTransform(string player_uid, NetworkedTransform networked_transform)
        {
            if (string.IsNullOrEmpty(player_uid) || networked_transform == null)
            {
                return;
            }

            if (!_networkedTransforms.TryGetValue(player_uid, out var registered_transform))
            {
                return;
            }

            if (registered_transform != networked_transform)
            {
                return;
            }

            _networkedTransforms.Remove(player_uid);
        }

        public void UnregisterNetworkedAnimation(string player_uid, NetworkedAnimation networked_animation)
        {
            if (string.IsNullOrEmpty(player_uid) || networked_animation == null)
            {
                return;
            }

            if (!_networkedAnimations.TryGetValue(player_uid, out var registered_animation))
            {
                return;
            }

            if (registered_animation != networked_animation)
            {
                return;
            }

            _networkedAnimations.Remove(player_uid);
        }

        public void SendPlayerPose(string player_uid, Vector3 position, float rotation_y)
        {
            if (string.IsNullOrEmpty(player_uid))
            {
                return;
            }

            int state = (int)State.IDLE;
            if (_networkedAnimations.TryGetValue(player_uid, out var networkedAnimation) && networkedAnimation != null)
            {
                state = networkedAnimation.CurrentState;
            }

            var payload = new PositionSyncData
            {
                uid = player_uid,
                state = state,
                x = position.x,
                y = position.y,
                z = position.z,
                r = rotation_y,
            };

            EnqueueSendEvent(POS_EVENT, JsonUtility.ToJson(payload), true);
        }

        public Player SpawnPlayer(string player_uid, string player_nick_name, Vector3 spawn_position)
        {
            if (string.IsNullOrEmpty(player_uid))
            {
                return null;
            }

            if (_playerObjects.TryGetValue(player_uid, out var existing_player) && existing_player != null)
            {
                return existing_player;
            }

            var playerGameObject = Root.sResourceManager.Instantiate(PrefabIDs.Hakjang, spawn_position, Quaternion.identity);
            if (playerGameObject == null)
            {
                return null;
            }

            var player = Util.GetComponent<Player>(playerGameObject);
            if (player == null)
            {
                Root.sResourceManager.Destroy(playerGameObject);
                return null;
            }

            bool isLocalPlayer = IsLocalPlayerUid(player_uid);
            player.OnNetworkStart(player_uid, isLocalPlayer);

            _playerObjects[player_uid] = player;
            _playerNickNames[player_uid] = player_nick_name;
            return player;
        }

        public NPC SpawnNPC(string uid, Vector3 spawn_position)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return null;
            }

            if (_npcObjects.TryGetValue(uid, out var existingNpc) && existingNpc != null)
            {
                return existingNpc;
            }

            var npcGameObject = Root.sResourceManager.Instantiate(PrefabIDs.NPC, spawn_position, Quaternion.identity);
            if (npcGameObject == null)
                return null;

            var npc = Util.GetComponent<NPC>(npcGameObject);
            if (npc == null)
            {
                Root.sResourceManager.Destroy(npcGameObject);
                return null;
            }

            npc.OnNetworkStart(uid, false);
            _npcObjects[uid] = npc;

            return npc;
        }

        public void SendQuickMatch(string player_id)
        {
            var payload = new QuickMatchRequest
            {
                playerId = player_id,
            };

            SendEvent(QUICK_MATCH_EVENT, JsonUtility.ToJson(payload));
        }

        public void SendLeaveRoom(string room_id, string uid)
        {
            var payload = new LeaveRoomRequest
            {
                roomId = room_id,
                uid = uid,
            };

            SendEvent("leaveRoom", JsonUtility.ToJson(payload));
        }

        public void SendSetReady(string room_id, string uid)
        {
            var payload = new SetReadyRequest
            {
                roomId = room_id,
                uid = uid,
            };

            SendEvent("setReady", JsonUtility.ToJson(payload));
        }

        public void SendStartGame(string room_id, string uid)
        {
            var payload = new StartGameRequest
            {
                roomId = room_id,
                uid = uid,
            };

            SendEvent("startGame", JsonUtility.ToJson(payload));
        }

        public void SendGetAvailableRooms()
        {
            SendEvent("getAvailableRooms", "{}");
        }

        public void SendGetRoom(string room_id)
        {
            var payload = new GetRoomRequest
            {
                roomId = room_id,
            };

            SendEvent("getRoom", JsonUtility.ToJson(payload));
        }

        public void SendAttack(string target_uid)
        {
            if (string.IsNullOrEmpty(target_uid))
            {
                return;
            }

            var payload = new AttackRequest
            {
                target = target_uid,
            };

            SendEvent(ATTACK_EVENT, JsonUtility.ToJson(payload));
        }

        private void SendEvent(string event_name, string data_json)
        {
            EnqueueSendEvent(event_name, data_json, false);
        }

        private void EnqueueSendEvent(string event_name, string data_json, bool coalesce_pose)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                Debug.LogWarning("[NetworkManager] Send failed: not connected (" + event_name + ")");
                return;
            }

            string message = BuildEventMessageJson(event_name, data_json);
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogWarning("[NetworkManager] Send failed: invalid message (" + event_name + ")");
                return;
            }

            var outgoingMessage = new OutgoingMessage
            {
                eventName = event_name,
                jsonText = message,
                connectionGeneration = _connectionGeneration,
            };

            lock (_sendQueueLockObject)
            {
                if (coalesce_pose)
                {
                    _pendingPoseSendMessage = outgoingMessage;
                }
                else
                {
                    _sendQueue.Enqueue(outgoingMessage);
                }
            }

            _sendQueueSignal.Release();
        }

        private async Task StartSendLoop(ClientWebSocket web_socket, CancellationToken cancellation_token, int connection_generation)
        {
            while (web_socket != null && !cancellation_token.IsCancellationRequested)
            {
                try
                {
                    await _sendQueueSignal.WaitAsync(cancellation_token).ConfigureAwait(false);

                    while (TryDequeueSendMessage(connection_generation, out var outgoingMessage))
                    {
                        await SendRawMessageAsync(web_socket, outgoingMessage, cancellation_token, connection_generation).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    Debug.LogError("[NetworkManager] Send loop failed: " + exception.Message);
                    return;
                }
            }
        }

        private bool TryDequeueSendMessage(int connection_generation, out OutgoingMessage outgoing_message)
        {
            outgoing_message = null;

            lock (_sendQueueLockObject)
            {
                while (_sendQueue.Count > 0)
                {
                    var queuedMessage = _sendQueue.Dequeue();
                    if (queuedMessage != null && queuedMessage.connectionGeneration == connection_generation)
                    {
                        outgoing_message = queuedMessage;
                        return true;
                    }
                }

                if (_pendingPoseSendMessage != null)
                {
                    if (_pendingPoseSendMessage.connectionGeneration == connection_generation)
                    {
                        outgoing_message = _pendingPoseSendMessage;
                    }

                    _pendingPoseSendMessage = null;
                }
            }

            return outgoing_message != null;
        }

        private async Task SendRawMessageAsync(ClientWebSocket web_socket, OutgoingMessage outgoing_message, CancellationToken cancellation_token, int connection_generation)
        {
            if (web_socket == null || outgoing_message == null)
            {
                return;
            }

            if (connection_generation != _connectionGeneration || outgoing_message.connectionGeneration != connection_generation)
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(outgoing_message.jsonText);
            var segment = new ArraySegment<byte>(bytes);
            bool isSendLockAcquired = false;

            try
            {
                await _sendSemaphore.WaitAsync(cancellation_token).ConfigureAwait(false);
                isSendLockAcquired = true;

                if (connection_generation != _connectionGeneration || web_socket.State != WebSocketState.Open || _connectionState != ConnectionState.CONNECTED)
                {
                    return;
                }

                await web_socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellation_token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError("[NetworkManager] Send failed (" + outgoing_message.eventName + "): " + exception.Message);
            }
            finally
            {
                if (isSendLockAcquired)
                {
                    _sendSemaphore.Release();
                }
            }
        }

        private string BuildEventMessageJson(string event_name, string data_json)
        {
            if (string.IsNullOrEmpty(event_name))
            {
                return string.Empty;
            }

            string normalizedDataJson = data_json;
            if (string.IsNullOrEmpty(normalizedDataJson))
            {
                normalizedDataJson = "{}";
            }

            return "{\"event\":\"" + EscapeJsonString(event_name) + "\",\"data\":" + normalizedDataJson + "}";
        }

        private string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);

            for (int index = 0; index < value.Length; index++)
            {
                char currentChar = value[index];

                if (currentChar == '\\')
                {
                    builder.Append("\\\\");
                    continue;
                }

                if (currentChar == '"')
                {
                    builder.Append("\\\"");
                    continue;
                }

                builder.Append(currentChar);
            }

            return builder.ToString();
        }

        private async Task<bool> ConnectCore(string server_url)
        {
            try
            {
                _connectionState = ConnectionState.CONNECTING;
                var webSocket = new ClientWebSocket();
                _webSocket = webSocket;
                _receiveCancellationTokenSource = new CancellationTokenSource();
                _sendCancellationTokenSource = new CancellationTokenSource();
                int connectionGeneration = Interlocked.Increment(ref _connectionGeneration);

                await webSocket.ConnectAsync(new Uri(server_url), _receiveCancellationTokenSource.Token).ConfigureAwait(false);
                _connectionState = ConnectionState.CONNECTED;

                _ = StartReceiveLoop(webSocket, _receiveCancellationTokenSource.Token, connectionGeneration);
                _ = StartSendLoop(webSocket, _sendCancellationTokenSource.Token, connectionGeneration);
                return true;
            }
            catch (Exception exception)
            {
                ResetConnectionResources();
                Debug.LogError("[NetworkManager] Connect 실패: " + exception.Message);
                return false;
            }
        }

        private async Task DisconnectCore(bool notify_disconnect, bool clear_player_objects, bool reset_room_state)
        {
            if (_webSocket == null)
            {
                _connectionState = ConnectionState.DISCONNECTED;
                return;
            }

            if (_connectionState == ConnectionState.DISCONNECTING || _connectionState == ConnectionState.DISCONNECTED)
            {
                return;
            }

            _connectionState = ConnectionState.DISCONNECTING;
            bool isSendLockAcquired = false;

            try
            {
                await _sendSemaphore.WaitAsync(CancellationToken.None);
                isSendLockAcquired = true;

                if (_receiveCancellationTokenSource != null)
                {
                    _receiveCancellationTokenSource.Cancel();
                }

                if (_sendCancellationTokenSource != null)
                {
                    _sendCancellationTokenSource.Cancel();
                }

                if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "disconnect", CancellationToken.None);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[NetworkManager] Disconnect 경고: " + exception.Message);
            }
            finally
            {
                if (clear_player_objects)
                {
                    ClearPlayerObjects();
                }

                if (isSendLockAcquired)
                {
                    _sendSemaphore.Release();
                }

                _webSocket.Dispose();
                _webSocket = null;

                if (_receiveCancellationTokenSource != null)
                {
                    _receiveCancellationTokenSource.Dispose();
                    _receiveCancellationTokenSource = null;
                }

                if (_sendCancellationTokenSource != null)
                {
                    _sendCancellationTokenSource.Dispose();
                    _sendCancellationTokenSource = null;
                }

                _connectionState = ConnectionState.DISCONNECTED;
                Interlocked.Increment(ref _connectionGeneration);

                if (reset_room_state)
                {
                    _localPlayerUid = string.Empty;
                    _currentRoom = null;
                }

                ClearSendQueue();
                ClearPendingPositionSync();
                ClearSmokeEffects();

                if (notify_disconnect)
                {
                    EnqueueMainThreadAction(() => OnDisconnected?.Invoke());
                }
            }
        }

        private void SwitchToGameServer()
        {
            _ = SwitchToGameServerAsync();
        }

        private async Task SwitchToGameServerAsync()
        {
            if (_isSwitchingToGameServer)
            {
                return;
            }

            string gameServerUrl = BuildGameServerUrl();
            if (string.IsNullOrEmpty(gameServerUrl))
            {
                Debug.LogWarning("[NetworkManager] /games URL 생성 실패");
                return;
            }

            _isSwitchingToGameServer = true;

            try
            {
                await DisconnectCore(false, false, false);
                await ConnectCore(gameServerUrl);
            }
            finally
            {
                _isSwitchingToGameServer = false;
            }
        }

        private string BuildGameServerUrl()
        {
            if (string.IsNullOrEmpty(_gameUrl) || _currentRoom == null || string.IsNullOrEmpty(_currentRoom.id) || string.IsNullOrEmpty(_localPlayerUid))
            {
                return string.Empty;
            }

            string roomId = Uri.EscapeDataString(_currentRoom.id);
            string playerUid = Uri.EscapeDataString(_localPlayerUid);
            return _gameUrl + "?sessionId=" + roomId + "&playerUid=" + playerUid;
        }

        private async Task StartReceiveLoop(ClientWebSocket web_socket, CancellationToken cancellation_token, int connection_generation)
        {
            var receiveBuffer = new byte[RECEIVE_BUFFER_SIZE];

            while (web_socket != null && web_socket.State == WebSocketState.Open && !cancellation_token.IsCancellationRequested && connection_generation == _connectionGeneration)
            {
                try
                {
                    using (var stream = new MemoryStream(RECEIVE_BUFFER_SIZE))
                    {
                        WebSocketReceiveResult receiveResult;

                        do
                        {
                            var segment = new ArraySegment<byte>(receiveBuffer);
                            receiveResult = await web_socket.ReceiveAsync(segment, cancellation_token).ConfigureAwait(false);

                            if (receiveResult.MessageType == WebSocketMessageType.Close)
                            {
                                await web_socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "server_close", CancellationToken.None).ConfigureAwait(false);
                                if (connection_generation == _connectionGeneration)
                                {
                                    EnqueueMainThreadAction(Disconnect);
                                }
                                return;
                            }

                            stream.Write(receiveBuffer, 0, receiveResult.Count);
                        }
                        while (!receiveResult.EndOfMessage);

                        string text = Encoding.UTF8.GetString(stream.ToArray());
                        if (connection_generation == _connectionGeneration)
                        {
                            HandleIncomingMessage(text);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    Debug.LogError("[NetworkManager] 수신 실패: " + exception.Message);
                    return;
                }
            }
        }

        private void HandleIncomingMessage(string json_text)
        {
            if (string.IsNullOrEmpty(json_text))
            {
                return;
            }

            EventHeader header;
            try
            {
                header = JsonUtility.FromJson<EventHeader>(json_text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[NetworkManager] 헤더 파싱 실패: " + exception.Message);
                return;
            }

            if (header == null || string.IsNullOrEmpty(header.@event))
            {
                return;
            }

            if (!TryExtractDataJson(json_text, out string data_json) || string.IsNullOrEmpty(data_json))
            {
                return;
            }

            switch (header.@event)
            {
                case CONNECTED_EVENT:
                    var connectedData = JsonUtility.FromJson<ConnectedData>(data_json);
                    EnqueueMainThreadAction(() => OnConnected?.Invoke(connectedData != null ? connectedData.playerId : string.Empty));
                    break;

                case QUICK_MATCH_EVENT:
                    var quickMatchResponse = JsonUtility.FromJson<QuickMatchResponse>(data_json);
                    EnqueueMainThreadAction(() =>
                    {
                        _localPlayerUid = quickMatchResponse != null && !string.IsNullOrEmpty(quickMatchResponse.uid)
                            ? quickMatchResponse.uid
                            : string.Empty;
                        _currentRoom = quickMatchResponse != null ? quickMatchResponse.room : null;
                        OnQuickMatch?.Invoke(quickMatchResponse);
                    });
                    break;

                case AVAILABLE_ROOMS_EVENT:
                    var wrappedRooms = JsonUtility.FromJson<RoomArrayWrapper>(WrapArrayJson(data_json));
                    EnqueueMainThreadAction(() => OnAvailableRooms?.Invoke(wrappedRooms != null ? wrappedRooms.items : null));
                    break;

                case ROOM_UPDATE_EVENT:
                    var room = JsonUtility.FromJson<Room>(data_json);
                    EnqueueMainThreadAction(() =>
                    {
                        _currentRoom = room;
                        OnRoomUpdate?.Invoke(room);
                    });
                    break;

                case GAME_START_EVENT:
                    var gameStartData = JsonUtility.FromJson<GameStartData>(data_json);
                    EnqueueMainThreadAction(() =>
                    {
                        SpawnPlayersFromCurrentRoom(gameStartData.playerPoses);
                        SpawnNpcsFromCurrentRoom(gameStartData.npcPoses);
                        OnGameStart?.Invoke(gameStartData);
                        SwitchToGameServer();
                    });
                    break;

                case ERROR_EVENT:
                    var errorData = JsonUtility.FromJson<ErrorData>(data_json);
                    EnqueueMainThreadAction(() => OnError?.Invoke(errorData));
                    break;

                case KILL_EVENT:
                    var killData = JsonUtility.FromJson<KillData>(data_json);
                    EnqueueMainThreadAction(() => HandleKillEvent(killData));
                    break;

                case POS_EVENT:
                    var wrappedPositions = JsonUtility.FromJson<PositionSyncArrayWrapper>(WrapArrayJson(data_json));
                    EnqueuePositionSyncBatch(wrappedPositions != null ? wrappedPositions.items : null);
                    break;

                case GAME_OVER_EVENT:
                    var leaderBoard = JsonUtility.FromJson<LeaderBoard>(WrapArrayJson(data_json));
                    EnqueueMainThreadAction(() => OnGameOver?.Invoke(leaderBoard));
                    break;

                case KILL_STAT_EVENT:
                    var killStat = JsonUtility.FromJson<KillStat>(data_json);
                    EnqueueMainThreadAction(() => OnKillStatChanged?.Invoke(killStat));
                    break;

                case WORLD_EVENT:
                    var worldEventData = JsonUtility.FromJson<WorldEventData>(data_json);
                    EnqueueMainThreadAction(() => HandleWorldEvent(worldEventData));
                    break;
            }
        }

        private void HandleWorldEvent(WorldEventData world_event_data)
        {
            if (world_event_data == null || string.IsNullOrEmpty(world_event_data.type))
            {
                return;
            }

            switch (world_event_data.type)
            {
                case SMOKE_EVENT_TYPE:
                    break;
                default:
                    break;
            }

            if (world_event_data.type != SMOKE_EVENT_TYPE)
            {
                return;
            }

            long despawnTimestamp = world_event_data.timestamp + SMOKE_EFFECT_DURATION_MILLISECONDS;
            if (despawnTimestamp <= GetCurrentUnixTimestampMilliseconds())
            {
                return;
            }

            var spawnPosition = new Vector3(world_event_data.x, world_event_data.y, world_event_data.z);
            var smokeEffectObject = Root.sResourceManager.Instantiate(PrefabIDs.SmokeEffect, spawnPosition, Quaternion.identity);
            if (smokeEffectObject == null)
            {
                return;
            }

            _activeSmokeEffects.Add(new SmokeEffectLifecycle
            {
                gameObject = smokeEffectObject,
                despawnTimestamp = despawnTimestamp,
            });
        }

        private void FlushExpiredSmokeEffects()
        {
            if (_activeSmokeEffects.Count == 0)
            {
                return;
            }

            long currentTimestamp = GetCurrentUnixTimestampMilliseconds();

            for (int i = _activeSmokeEffects.Count - 1; i >= 0; i--)
            {
                var smokeEffect = _activeSmokeEffects[i];
                if (smokeEffect == null)
                {
                    _activeSmokeEffects.RemoveAt(i);
                    continue;
                }

                if (smokeEffect.gameObject == null)
                {
                    _activeSmokeEffects.RemoveAt(i);
                    continue;
                }

                if (smokeEffect.despawnTimestamp > currentTimestamp)
                {
                    continue;
                }

                Root.sResourceManager.Destroy(smokeEffect.gameObject);
                _activeSmokeEffects.RemoveAt(i);
            }
        }

        private void ClearSmokeEffects()
        {
            if (_activeSmokeEffects.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _activeSmokeEffects.Count; i++)
            {
                var smokeEffect = _activeSmokeEffects[i];
                if (smokeEffect == null || smokeEffect.gameObject == null)
                {
                    continue;
                }

                Root.sResourceManager.Destroy(smokeEffect.gameObject);
            }

            _activeSmokeEffects.Clear();
        }

        private long GetCurrentUnixTimestampMilliseconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void EnqueuePositionSyncBatch(PositionSyncData[] position_sync_data_array)
        {
            if (position_sync_data_array == null || position_sync_data_array.Length == 0)
            {
                return;
            }

            lock (_positionSyncLockObject)
            {
                for (int i = 0; i < position_sync_data_array.Length; i++)
                {
                    var positionSyncData = position_sync_data_array[i];
                    if (positionSyncData == null || string.IsNullOrEmpty(positionSyncData.uid))
                    {
                        continue;
                    }

                    _pendingPositionSyncByUid[positionSyncData.uid] = positionSyncData;
                }
            }
        }

        private void FlushPendingPositionSync()
        {
            _positionSyncApplyBuffer.Clear();

            lock (_positionSyncLockObject)
            {
                if (_pendingPositionSyncByUid.Count == 0)
                {
                    return;
                }

                foreach (var pair in _pendingPositionSyncByUid)
                {
                    _positionSyncApplyBuffer.Add(pair.Value);
                }

                _pendingPositionSyncByUid.Clear();
            }

            for (int i = 0; i < _positionSyncApplyBuffer.Count; i++)
            {
                ApplyPositionSync(_positionSyncApplyBuffer[i]);
            }

            _positionSyncApplyBuffer.Clear();
        }

        private void ClearSendQueue()
        {
            lock (_sendQueueLockObject)
            {
                _sendQueue.Clear();
                _pendingPoseSendMessage = null;
            }

            while (_sendQueueSignal.Wait(0))
            {
            }
        }

        private void ClearPendingPositionSync()
        {
            lock (_positionSyncLockObject)
            {
                _pendingPositionSyncByUid.Clear();
            }

            _positionSyncApplyBuffer.Clear();
        }

        private void ApplyPositionSync(PositionSyncData position_sync_data)
        {
            if (position_sync_data == null || string.IsNullOrEmpty(position_sync_data.uid))
            {
                return;
            }

            if (!_networkedTransforms.TryGetValue(position_sync_data.uid, out var networkedTransform) || networkedTransform == null)
            {
                return;
            }

            var position = new Vector3(position_sync_data.x, position_sync_data.y, position_sync_data.z);
            networkedTransform.ApplyNetworkPose(position, position_sync_data.r);

            if (_networkedAnimations.TryGetValue(position_sync_data.uid, out var networkedAnimation) && networkedAnimation != null)
            {
                networkedAnimation.ApplyNetworkState(position_sync_data.state);
            }
        }

        private bool TryExtractDataJson(string json_text, out string data_json)
        {
            data_json = string.Empty;

            if (string.IsNullOrEmpty(json_text))
            {
                return false;
            }

            int dataKeyIndex = json_text.IndexOf(DATA_KEY, StringComparison.Ordinal);
            if (dataKeyIndex < 0)
            {
                return false;
            }

            int colonIndex = json_text.IndexOf(':', dataKeyIndex + DATA_KEY.Length);
            if (colonIndex < 0)
            {
                return false;
            }

            int valueStartIndex = colonIndex + 1;
            while (valueStartIndex < json_text.Length && char.IsWhiteSpace(json_text[valueStartIndex]))
            {
                valueStartIndex++;
            }

            if (valueStartIndex >= json_text.Length)
            {
                return false;
            }

            char valueStartChar = json_text[valueStartIndex];
            if (valueStartChar != '{' && valueStartChar != '[')
            {
                return false;
            }

            int depth = 0;
            bool in_string = false;
            bool isEscaped = false;

            for (int index = valueStartIndex; index < json_text.Length; index++)
            {
                char currentChar = json_text[index];

                if (in_string)
                {
                    if (isEscaped)
                    {
                        isEscaped = false;
                        continue;
                    }

                    if (currentChar == '\\')
                    {
                        isEscaped = true;
                        continue;
                    }

                    if (currentChar == '"')
                    {
                        in_string = false;
                    }

                    continue;
                }

                if (currentChar == '"')
                {
                    in_string = true;
                    continue;
                }

                if (currentChar == '{' || currentChar == '[')
                {
                    depth++;
                    continue;
                }

                if (currentChar == '}' || currentChar == ']')
                {
                    depth--;

                    if (depth == 0)
                    {
                        data_json = json_text.Substring(valueStartIndex, index - valueStartIndex + 1);
                        return true;
                    }
                }
            }

            return false;
        }

        private string WrapArrayJson(string array_json)
        {
            if (string.IsNullOrEmpty(array_json))
            {
                return "{\"items\":[]}";
            }

            return "{\"items\":" + array_json + "}";
        }

        private void EnqueueMainThreadAction(Action action)
        {
            if (action == null)
            {
                return;
            }

            lock (_mainThreadLockObject)
            {
                _mainThreadActions.Enqueue(action);
            }
        }

        private void FlushMainThreadQueue()
        {
            while (true)
            {
                Action pendingAction = null;

                lock (_mainThreadLockObject)
                {
                    if (_mainThreadActions.Count > 0)
                    {
                        pendingAction = _mainThreadActions.Dequeue();
                    }
                }

                if (pendingAction == null)
                {
                    return;
                }

                pendingAction.Invoke();
            }
        }

        private void SpawnPlayersFromCurrentRoom(Vector3[] playerPoses)
        {
            if (_currentRoom == null || _currentRoom.players == null || _currentRoom.players.Length == 0)
            {
                return;
            }

            ClearPlayerObjects();

            int playerCount = playerPoses.Length;
            for (int i = 0; i < playerCount; i++)
            {
                var player = _currentRoom.players[i];
                if (player == null || string.IsNullOrEmpty(player.uid))
                {
                    continue;
                }

                SpawnPlayer(player.uid, player.id, playerPoses[i]);
            }
        }

        private void SpawnNpcsFromCurrentRoom(Vector3[] npcPoses)
        {
            ClearNpcObjects();

            for (int i = 0; i < npcPoses.Length; i++)
            {
                string npcUid = "npc_" + i;
                SpawnNPC(npcUid, npcPoses[i]);
            }
        }

        private void HandleKillEvent(KillData kill_data)
        {
            if (kill_data == null || string.IsNullOrEmpty(kill_data.target))
            {
                return;
            }

            if (!TryGetUnitByUid(kill_data.target, out BaseUnit targetUnit) || targetUnit == null)
            {
                return;
            }

            if (!TryGetUnitByUid(kill_data.killer, out BaseUnit killerUnit) || killerUnit == null)
            {
                return;
            }

            RemoveUnitByUid(kill_data.target);
            OnKilled?.Invoke(killerUnit, targetUnit);
        }
    
        private bool TryGetUnitByUid(string unit_uid, out BaseUnit unit)
        {
            unit = null;

            if (string.IsNullOrEmpty(unit_uid))
            {
                return false;
            }

            if (_playerObjects.TryGetValue(unit_uid, out var player) && player != null)
            {
                unit = player;
                return true;
            }

            if (_npcObjects.TryGetValue(unit_uid, out var npc) && npc != null)
            {
                unit = npc;
                return true;
            }

            return false;
        }

        private void RemoveUnitByUid(string unit_uid)
        {
            if (string.IsNullOrEmpty(unit_uid))
            {
                return;
            }

            if (_playerObjects.TryGetValue(unit_uid, out var player) && player != null)
            {
                _playerObjects.Remove(unit_uid);
                _networkedTransforms.Remove(unit_uid);
                _networkedAnimations.Remove(unit_uid);
                Root.sResourceManager.Destroy(player.gameObject);
                return;
            }

            if (_npcObjects.TryGetValue(unit_uid, out var npc) && npc != null)
            {
                _npcObjects.Remove(unit_uid);
                Root.sResourceManager.Destroy(npc.gameObject);
            }
        }

        private void ClearPlayerObjects()
        {
            if (_playerObjects.Count == 0)
            {
                _networkedTransforms.Clear();
                _networkedAnimations.Clear();
                return;
            }

            foreach (var pair in _playerObjects)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                Root.sResourceManager.Destroy(pair.Value.gameObject);
            }

            _playerObjects.Clear();
            _networkedTransforms.Clear();
            _networkedAnimations.Clear();
        }

        private void ClearNpcObjects()
        {
            if (_npcObjects.Count == 0)
            {
                return;
            }
            foreach (var pair in _npcObjects)
            {
                if (pair.Value == null)
                {
                    continue;
                }
                Root.sResourceManager.Destroy(pair.Value.gameObject);
            }

            _npcObjects.Clear();
        }

        private bool IsLocalPlayerUid(string player_uid)
        {
            if (string.IsNullOrEmpty(player_uid) || string.IsNullOrEmpty(_localPlayerUid))
            {
                return false;
            }

            return player_uid == _localPlayerUid;
        }

        private void ResetConnectionResources()
        {
            if (_receiveCancellationTokenSource != null)
            {
                _receiveCancellationTokenSource.Cancel();
                _receiveCancellationTokenSource.Dispose();
                _receiveCancellationTokenSource = null;
            }

            if (_sendCancellationTokenSource != null)
            {
                _sendCancellationTokenSource.Cancel();
                _sendCancellationTokenSource.Dispose();
                _sendCancellationTokenSource = null;
            }

            if (_webSocket != null)
            {
                _webSocket.Dispose();
                _webSocket = null;
            }

            Interlocked.Increment(ref _connectionGeneration);
            ClearSendQueue();
            _connectionState = ConnectionState.DISCONNECTED;
        }

        #endregion
    }
}
