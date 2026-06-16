using UnityEngine;

namespace Hakjang
{
    public class AnimationParams
    {
        public static readonly int Idle = Animator.StringToHash("Idle");
        public static readonly int Walk = Animator.StringToHash("Walk");
        public static readonly int Run = Animator.StringToHash("Run");
        public static readonly int Attack = Animator.StringToHash("Attack");
    }
}