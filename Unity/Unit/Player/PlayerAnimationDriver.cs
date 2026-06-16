using UnityEngine;

namespace Hakjang
{
    internal sealed class PlayerAnimationDriver
    {
        private const float DEFAULT_TRANSITION_DURATION = 0.2f;

        private Animator _animator;
        private int _currentStateHash;

        public void Initialize(Animator animator)
        {
            _animator = animator;
            _currentStateHash = 0;
        }

        public void PlayIdle()
        {
            Play(AnimationParams.Idle);
        }

        public void PlayMove(bool is_sprinting)
        {
            Play(is_sprinting ? AnimationParams.Run : AnimationParams.Walk);
        }

        public void PlayAttack()
        {
            Play(AnimationParams.Attack);
        }

        private void Play(int state_hash, float transition_duration = DEFAULT_TRANSITION_DURATION, int layer = 0)
        {
            if (_animator == null || _currentStateHash == state_hash)
            {
                return;
            }

            _currentStateHash = state_hash;
            _animator.CrossFadeInFixedTime(state_hash, transition_duration, layer);
        }
    }
}
