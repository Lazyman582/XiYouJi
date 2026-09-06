using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiYouJi.Gameplay;

public class DialogueToggle : MonoBehaviour
{
    [SerializeField] private Behaviour target; // 拖拽任意组件（Collider、MonoBehaviour等）




    private void Start()
    {
        var found = FindObjectOfType<PointClickNavController>();
        target = found;
    }
    private void OnEnable()
    {
        DialogueController.OnDialogueStart += OnStart;
        DialogueUIController.OnDialogueEnd += OnEnd;
        var found = FindObjectOfType<PointClickNavController>();
        target = found;
    }
    
    private void OnDestroy()
    {
        DialogueController.OnDialogueStart -= OnStart;
        DialogueUIController.OnDialogueEnd -= OnEnd;
    }

    private void OnStart()
    {
      
            SetTarget(false);
    }

    private void OnEnd()
    {
     
            SetTarget(true);
    }

    private void SetTarget(bool state)
    {
        if (target != null) target.enabled = state;
    }
}
