using UnityEngine;

namespace XiYouJi.Events
{
    // 某句对话“讲完”（文本效果播完）时广播。
    // 监听方收到后执行自己的任务，任务完成后调用 DialogueUIController.Instance.ResumeDialogue()
    // 作为回调，对话按钮恢复交互，流程继续。
    public struct DialogueLineFinished
    {
        public string EventKey;        // 对话数据里配置的事件 Key（finishEventKey）
        public int LineIndex;          // 讲完的是第几句
        public string Speaker;         // 说话人
        public string Content;         // 这句话的内容
        public DialogueData Container; // 这句对话的数据（方便取上下文）
    }
}
