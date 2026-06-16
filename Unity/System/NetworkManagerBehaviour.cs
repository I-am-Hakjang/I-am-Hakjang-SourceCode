using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.NETWORK_MANAGER)]
    public class NetworkManagerBehaviour : ManagerBehaviour
    {
        #region Field
        [BoxGroup("Network Settings")]
        [OdinSerialize]
        private string _serverUrl = "ws://localhost:3000/room";

        [BoxGroup("Network Settings")]
        [OdinSerialize]
        private string _gameUrl = "ws://localhost:3000/games";

        #endregion

        #region Method
        private void Awake()
        {
            Root.sNetworkManager = new();
            Root.sNetworkManager.OnAwake(_serverUrl, _gameUrl);
        }

        private void Update()
        {
            Root.sNetworkManager.OnUpdate();
        }

        private void OnDestroy()
        {
            Root.sNetworkManager.OnDestroy();
        }
        #endregion
    }
}
