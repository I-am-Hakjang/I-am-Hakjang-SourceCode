using System;
using UnityEngine;

namespace Hakjang
{
    public partial class NetworkManager
    {
        #region DTO

        [Serializable]
        public class RoomPlayer
        {
            public string id;
            public string uid;
            public bool isReady;
        }

        [Serializable]
        public class Room
        {
            public string id;
            public string hostUid;
            public RoomPlayer[] players;
            public int maxPlayer;
            public string status;
        }

        [Serializable]
        private class EventHeader
        {
            public string @event;
        }

        [Serializable]
        private class ConnectedData
        {
            public string playerId;
        }

        [Serializable]
        private class QuickMatchRequest
        {
            public string playerId;
        }

        [Serializable]
        private class LeaveRoomRequest
        {
            public string roomId;
            public string uid;
        }

        [Serializable]
        private class SetReadyRequest
        {
            public string roomId;
            public string uid;
        }

        [Serializable]
        private class StartGameRequest
        {
            public string roomId;
            public string uid;
        }

        [Serializable]
        private class GetRoomRequest
        {
            public string roomId;
        }

        [Serializable]
        private class AttackRequest
        {
            public string target;
        }

        [Serializable]
        private class WorldEventData
        {
            public string type;
            public float x;
            public float y;
            public float z;
            public long timestamp;
        }

        [Serializable]
        public class QuickMatchResponse
        {
            public Room room;
            public string uid;
        }

        [Serializable]
        public class GameStartData
        {
            public string sessionId;
            public Vector3[] playerPoses;
            public Vector3[] npcPoses;
        }

        [Serializable]
        public class ErrorData
        {
            public string message;
        }

        [Serializable]
        public class KillData
        {
            public string killer;
            public string target;
        }

        [Serializable]
        public class KillStat
        {
            public int playerKillCount;
            public int npcKillCount;
        }

        [Serializable]
        public class LeaderBoard
        {
            public PlayerResultData[] items;
        }

        [Serializable]
        public class PlayerResultData
        {
            public string playerUid;
            public int rank;
            public int playerKillCount;
            public int npcKillCount;
        }

        [Serializable]
        private class PositionSyncData
        {
            public string uid;
            public int state;
            public float x;
            public float y;
            public float z;
            public float r;
        }

        [Serializable]
        private class PositionSyncArrayWrapper
        {
            public PositionSyncData[] items;
        }

        [Serializable]
        private class RoomArrayWrapper
        {
            public Room[] items;
        }

        private sealed class OutgoingMessage
        {
            public string eventName;
            public string jsonText;
            public int connectionGeneration;
        }

        private sealed class SmokeEffectLifecycle
        {
            public GameObject gameObject;
            public long despawnTimestamp;
        }

        #endregion
    }
}
