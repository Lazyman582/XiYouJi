using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiYouJi.Events;
using XiYouJi.Gameplay;

public sealed class DialogueEndAnimator : MonoBehaviour
{
    [Header("对话结束要播放的动画状态，须与 Animator 内的状态名一致")]
    [SerializeField] private string endStateName = "Idle";

    [Header("动画是否播固定时长后自动恢复（<=0 表示交给状态机，不自动恢复）")]
    [SerializeField] private float endDuration = 0f;

    private Animator animator;
    private PlayerAnimationDriver animationDriver;
    private Coroutine endRoutine;
    private int endStateHash;

    private void Awake()
    {
        endStateHash = Animator.StringToHash(endStateName);
    }

    private void OnEnable()
    {
        EventBus<DialogueChoiceSelected>.Subscribe(OnDialogueChoice, 0);
    }

    private void OnDisable()
    {
        EventBus<DialogueChoiceSelected>.Unsubscribe(OnDialogueChoice);

        if (endRoutine != null)
        {
            StopCoroutine(endRoutine);
            endRoutine = null;
        }

        RestoreAnimation();
    }

    // ---------- 重载1：收到广播，用 Inspector 配置的时长 ----------
    private void OnDialogueChoice(DialogueChoiceSelected evt)
    {
        if (evt.TargetIndex != -1)
            return;

        PlayEndAnimation(endDuration);
    }

    // ---------- 重载2：代码调用，直接指定动画名和时长 ----------
    public void PlayEndAnimation(string stateName)
    {
        if (endRoutine != null)
        {
            StopCoroutine(endRoutine);
            endRoutine = null;
        }

        endStateHash = Animator.StringToHash(stateName);
        PlayEndAnimation(0f);   // 交给状态机，不自动恢复
    }

    public void PlayEndAnimation(string stateName, float duration)
    {
        if (endRoutine != null)
        {
            StopCoroutine(endRoutine);
            endRoutine = null;
        }

        endStateHash = Animator.StringToHash(stateName);
        PlayEndAnimation(duration);
    }

    // ---------- 核心：真正播放 ----------
    private void PlayEndAnimation(float duration)
    {
        if (!FindPlayer())
        {
            Debug.LogError("Player not found while playing end animation.");
            return;
        }

        if (animationDriver != null)
        {
            animationDriver.enabled = false;
        }

        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(endStateHash, 0, 0f);

        // 指定了固定时长：播完后自动恢复驱动
        if (duration > 0f)
        {
            endRoutine = StartCoroutine(RestoreAfter(duration));
        }
    }

    private IEnumerator RestoreAfter(float duration)
    {
        yield return new WaitForSeconds(duration);

        animator.speed = 0f;

        if (animationDriver != null)
        {
            animationDriver.enabled = true;
        }

        endRoutine = null;
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

    private void RestoreAnimation()
    {
        if (endRoutine != null)
        {
            StopCoroutine(endRoutine);
            endRoutine = null;
        }

        if (animationDriver != null)
        {
            animationDriver.enabled = true;
        }

        if (animator != null)
        {
            animator.speed = 1f;
        }
    }
}