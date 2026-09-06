using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 实现鼠标进入、离开接口
public class HoverShowImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("悬浮要显示的图片")]
    public Image tipImage;

    // 鼠标移入
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tipImage != null)
        {
            tipImage.gameObject.SetActive(true);
        }
    }

    // 鼠标移出
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tipImage != null)
        {
            tipImage.gameObject.SetActive(false);
        }
    }
}