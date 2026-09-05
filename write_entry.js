const fs = require('fs');
const path = require('path');
const base = 'D:/Github/XiYouJi/Assets/Scripts/Start';

const content = `using UnityEngine;

/// <summary>
/// Start场景入口脚本 - 将此脚本添加到Start场景中的空GameObject上即可运行
/// 自动构建完整的UI界面（项目封面+介绍模块+体验模块选择界面）
/// </summary>
public class StartSceneEntry : MonoBehaviour
{
    private void Awake()
    {
        // 确保DOTween已初始化
        DG.Tweening.DOTween.Init();

        // 添加场景管理器
        StartSceneManager mgr = gameObject.AddComponent<StartSceneManager>();

        // 添加场景构建器
        StartSceneBuilder builder = gameObject.AddComponent<StartSceneBuilder>();
        builder.autoBuild = true;
        builder.BuildScene();
    }
}
`;

fs.writeFileSync(path.join(base, 'StartSceneEntry.cs'), content, 'utf8');
console.log('StartSceneEntry.cs written');
