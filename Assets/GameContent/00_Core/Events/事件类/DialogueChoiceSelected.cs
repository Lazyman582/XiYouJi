using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XiYouJi.Events
{
    // 选项被选中时广播
    public struct DialogueChoiceSelected
    {
        public string ChoiceText;       // 玩家选了什么
        public int TargetIndex;         // 跳转目标 (-1 = 结束对话)
        public int CurrentIndex;        // 触发时的当前对话索引
        public DialogueData Container; // 所属对话容器（方便取上下文）

        public DialogueChoiceSelected(string choiceText, int targetIndex, int currentIndex, DialogueData container)
        {
            ChoiceText = choiceText;
            TargetIndex = targetIndex;
            CurrentIndex = currentIndex;
            Container = container;
        }
    }

    public struct SceneTriggerEvent
    {
        public string EventKey;   // 事件标识符，例如 "WaterAppear"、"DoorOpen"
        // 如果需要，可以增加其他字段，例如携带数值或对象引用
        // public int IntParam;
        // public GameObject TargetObject;
    }
    public struct DialogueStarted { public DialogueData Container; }
    public struct DialogueEnded { }
}
