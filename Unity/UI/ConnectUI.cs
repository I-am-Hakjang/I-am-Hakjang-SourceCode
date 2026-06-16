using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hakjang
{
    [DefaultExecutionOrder((int)UpdateOrderGroup.CONNECT_UI)]
    public class ConnectUI : MonoBehaviour
    {
        #region Field
        [SerializeField] private ConnectionController _connectionController;
        [SerializeField] private GameObject _rootObject;
        [SerializeField] private TMP_InputField _nicknameInputField;
        [SerializeField] private Button _connectButton;
        [SerializeField] private TextMeshProUGUI _statusText;

        private string _cachedNickname = string.Empty;
        #endregion

        #region Method
        private void Awake()
        {
            if (_connectButton != null)
            {
                _connectButton.onClick.AddListener(OnClickConnectButton);
            }

            if (_nicknameInputField != null)
            {
                _nicknameInputField.onValueChanged.AddListener(OnNicknameChanged);
            }
        }

        private void OnEnable()
        {
            if (_connectionController == null)
            {
                return;
            }

            _connectionController.OnFlowChanged += HandleFlowChanged;
            _connectionController.OnStatusChanged += HandleStatusChanged;
        }

        private void OnDisable()
        {
            if (_connectionController == null)
            {
                return;
            }

            _connectionController.OnFlowChanged -= HandleFlowChanged;
            _connectionController.OnStatusChanged -= HandleStatusChanged;
        }

        private void OnDestroy()
        {
            if (_connectButton != null)
            {
                _connectButton.onClick.RemoveListener(OnClickConnectButton);
            }

            if (_nicknameInputField != null)
            {
                _nicknameInputField.onValueChanged.RemoveListener(OnNicknameChanged);
            }
        }

        private void OnClickConnectButton()
        {
            if (_connectionController == null)
            {
                return;
            }

            string nickname = GetNickname();
            _connectionController.RequestConnect(nickname);
        }

        private void OnNicknameChanged(string nickname)
        {
            _cachedNickname = nickname;
        }

        private string GetNickname()
        {
            if (_nicknameInputField != null)
            {
                _cachedNickname = _nicknameInputField.text;
            }

            if (string.IsNullOrWhiteSpace(_cachedNickname))
            {
                return string.Empty;
            }

            return _cachedNickname.Trim();
        }

        private void HandleFlowChanged(ConnectionController.FlowState flow_state)
        {
            bool is_connect = flow_state == ConnectionController.FlowState.CONNECT;

            if (_rootObject != null)
            {
                _rootObject.SetActive(is_connect);
            }
            else
            {
                gameObject.SetActive(is_connect);
            }
        }

        private void HandleStatusChanged(string status_message)
        {
            if (_statusText == null)
            {
                return;
            }

            _statusText.text = status_message;
        }
        #endregion
    }
}
