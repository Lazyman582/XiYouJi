using System.Collections;
using UnityEngine;
using XiYouJi.Events;

namespace XiYouJi.Gameplay
{
    public sealed class WalkAnimationButton : MonoBehaviour
    {
        private const float WalkDuration = 3f;
        private const string WalkStateName = "Walk";

        private Animator animator;
        private PlayerAnimationDriver animationDriver;
        private Coroutine walkRoutine;
        private int walkStateHash;

        private void Awake()
        {
            walkStateHash = Animator.StringToHash(WalkStateName);
        }

        private void OnEnable()
        {
            EventBus<WalkAnimationRequested>.Subscribe(OnWalkRequested, 0);
        }

        private void OnDisable()
        {
            EventBus<WalkAnimationRequested>.Unsubscribe(OnWalkRequested);

            if (walkRoutine != null)
            {
                StopCoroutine(walkRoutine);
                walkRoutine = null;
            }

            RestoreAnimation();
        }

        public void OnWalkButtonClicked()
        {
            EventBus<WalkAnimationRequested>.Publish(new WalkAnimationRequested());
        }

        private void OnWalkRequested(WalkAnimationRequested eventData)
        {
            if (walkRoutine != null)
            {
                return;
            }

            if (!FindPlayer())
            {
                Debug.LogError("Player not found.");
                return;
            }

            walkRoutine = StartCoroutine(PlayWalk());
        }

        private bool FindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return false;
            }

            animator = player.GetComponentInChildren<Animator>(true);
            animationDriver = player.GetComponent<PlayerAnimationDriver>();
            return animator != null;
        }

        private IEnumerator PlayWalk()
        {
            if (animationDriver != null)
            {
                animationDriver.enabled = false;
            }

            animator.enabled = true;
            animator.speed = 1f;
            animator.Play(walkStateHash, 0, 0f);

            yield return new WaitForSeconds(WalkDuration);

            animator.speed = 0f;
            Debug.LogError("hi");

            if (animationDriver != null)
            {
                animationDriver.enabled = true;
            }

            walkRoutine = null;
        }

        private void RestoreAnimation()
        {
            if (animator != null)
            {
                animator.speed = 1f;
            }

            if (animationDriver != null)
            {
                animationDriver.enabled = true;
            }
        }

        private sealed class WalkAnimationRequested
        {
        }
    }
}
