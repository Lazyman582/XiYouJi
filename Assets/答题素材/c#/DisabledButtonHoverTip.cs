using UnityEngine;
using UnityEngine.UI;

public class DisabledButtonHoverTip : MonoBehaviour
{
    [Header("目标Button")]
    public Button targetBtn;
    [Header("禁用时悬浮显示的提示图片")]
    public Image tipImage;

    private RectTransform _btnRect;
    private Canvas _canvas;

    void Start()
    {
        _btnRect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        if (tipImage != null) tipImage.gameObject.SetActive(false);
    }

    void Update()
    {
        if (targetBtn == null || tipImage == null) return;

        // 只有按钮不可用的时候，才检测悬浮
        if (!targetBtn.interactable)
        {
            bool isMouseOver = IsPointerOverUIElement(_btnRect);
            if (isMouseOver)
            {
                tipImage.gameObject.SetActive(true);
            }
            else
            {
                tipImage.gameObject.SetActive(false);
            }
        }
        else
        {
            // 按钮可用时，隐藏提示图
            if (tipImage.gameObject.activeSelf)
                tipImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 判断鼠标是否在指定RectTransform UI上
    /// </summary>
    bool IsPointerOverUIElement(RectTransform rt)
    {
        Vector2 localPoint;
        return RectTransformUtility.RectangleContainsScreenPoint(
            rt,
            Input.mousePosition,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera);
    }
}