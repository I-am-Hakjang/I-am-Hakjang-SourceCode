using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Hakjang
{
    public enum State : int
    {
        IDLE,
        WALK,
        RUN,
        ATTACK,
    }

    public class NetworkedAnimation : MonoBehaviour
    {
        [ShowInInspector, ReadOnly] private int _currentStateHash;

        private BaseUnit _baseUnit;
        private Animator _animator;
        private bool _isRegistered;
        private bool _isInitialized;
        private bool _isOwner;
        private string _playerUid = string.Empty;
        private int _currentState = (int)State.IDLE;

        public int CurrentState => _currentState;

        private void Awake()
        {
            _baseUnit = Util.GetComponent<BaseUnit>(this);
            _animator = Util.GetComponentInChildren<Animator>(this);
        }

        private void OnEnable()
        {
            TryInitializeFromBaseUnit();
        }

        private void Update()
        {
            TryInitializeFromBaseUnit();

            if (!_isOwner || _animator == null)
            {
                return;
            }

            _currentState = ResolveStateFromAnimator();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        public void Play(int state_hash, float transition_duration = 0.2f, int layer = 0)
        {
            if (_currentStateHash == state_hash)
                return;

            _currentStateHash = state_hash;
            _animator.CrossFadeInFixedTime(state_hash, transition_duration, layer);
            _currentState = ResolveStateFromHash(state_hash);
        }

        public void ApplyNetworkState(int state)
        {
            if (_isOwner)
            {
                return;
            }

            int stateHash = ResolveHashFromState(state);
            if (stateHash == 0)
            {
                return;
            }

            Play(stateHash);
        }

        public void SetFloat(int hash, float value)
        {
            _animator.SetFloat(hash, value);
        }

        public void SetBool(int hash, bool value)
        {
            _animator.SetBool(hash, value);
        }

        public void SetTrigger(int hash)
        {
            _animator.SetTrigger(hash);
        }

        private void TryInitializeFromBaseUnit()
        {
            if (_baseUnit == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_baseUnit.Id))
            {
                return;
            }

            if (_isInitialized && _playerUid == _baseUnit.Id && _isOwner == _baseUnit.IsOwner)
            {
                return;
            }

            if (_isRegistered)
            {
                Unregister();
            }

            _playerUid = _baseUnit.Id;
            _isOwner = _baseUnit.IsOwner;
            _isInitialized = true;

            Root.sNetworkManager.RegisterNetworkedAnimation(_playerUid, this);
            _isRegistered = true;
        }

        private void Unregister()
        {
            if (!_isRegistered || string.IsNullOrEmpty(_playerUid))
            {
                return;
            }

            Root.sNetworkManager.UnregisterNetworkedAnimation(_playerUid, this);
            _isRegistered = false;
        }

        private int ResolveStateFromAnimator()
        {
            AnimatorStateInfo currentStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return ResolveStateFromHash(currentStateInfo.shortNameHash);
        }

        private int ResolveStateFromHash(int state_hash)
        {
            if (state_hash == AnimationParams.Walk)
            {
                return (int)State.WALK;
            }

            if (state_hash == AnimationParams.Run)
            {
                return (int)State.RUN;
            }

            if (state_hash == AnimationParams.Attack)
            {
                return (int)State.ATTACK;
            }

            return (int)State.IDLE;
        }

        private int ResolveHashFromState(int state)
        {
            if (state == (int)State.WALK)
            {
                return AnimationParams.Walk;
            }

            if (state == (int)State.RUN)
            {
                return AnimationParams.Run;
            }

            if (state == (int)State.ATTACK)
            {
                return AnimationParams.Attack;
            }

            return AnimationParams.Idle;
        }
    }
}
