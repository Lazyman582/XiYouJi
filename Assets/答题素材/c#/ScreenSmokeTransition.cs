using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Attach to a full-screen UI Image on a topmost Canvas.</summary>
[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public sealed class ScreenSmokeTransition : MonoBehaviour
{
    [Header("转场进度：0 完全透明，1 完全黑屏")]
    [Range(0f, 1f)] public float progress;
    [Header("时间（秒）")]
    [Min(0.05f)] public float coverDuration = 1.6f;
    [Min(0f)] public float holdDuration = 0.35f;
    [Min(0.05f)] public float revealDuration = 1.6f;
    [Header("烟雾外观")]
    public Material materialTemplate;
    [Range(1f, 20f)] public float noiseScale = 5f;
    [Range(0.02f, 0.4f)] public float edgeSoftness = 0.16f;
    [Range(0.2f, 1f)] public float density = 0.9f;
    public Vector2 scrollSpeed = new Vector2(0.08f, -0.22f);
    [Tooltip("完全黑屏时调用：可在这里切换画面或启动场景加载。异步加载请用 Cover，加载完成后 Reveal。")]
    public UnityEvent onCovered = new UnityEvent();

    public bool IsPlaying { get; private set; }
    public bool BlocksInteraction => IsPlaying || progress > 0.01f;
    private Material material;
    private Material previousMaterial;
    private Image image;
    private CanvasGroup group;
    private Coroutine routine;
    private float smokeTime;

    private void Awake() { Initialize(); }
    private void Update()
    {
        smokeTime += Time.unscaledDeltaTime;
        Apply();
    }

    private void Initialize()
    {
        if (material != null) return;
        image = GetComponent<Image>();
        group = GetComponent<CanvasGroup>();
        Shader shader = materialTemplate != null ? materialTemplate.shader : Shader.Find("UI/Black Smoke Cover");
        if (shader == null) { Debug.LogError("缺少 UI/Black Smoke Cover Shader", this); return; }
        previousMaterial = image.material;
        material = materialTemplate != null ? new Material(materialTemplate) : new Material(shader);
        material.name = "Screen Smoke Transition (Instance)";
        material.hideFlags = HideFlags.HideAndDontSave;
        image.material = material;
        image.color = Color.white;
        image.raycastTarget = true;
        Apply();
    }

    public void PlayAndDisable()
    {
        if (IsPlaying) return;
        Begin(0);                              // 复用已有的完整时序（盖→停留→揭）
        StartCoroutine(WaitFinishAndDisable());
    }
    private IEnumerator WaitFinishAndDisable()
    {
        while (IsPlaying)                      // 等内部 Animate 跑完
            yield return null;
        gameObject.SetActive(false);           // 播完一轮后失活自身
    }
    private void Apply()
    {
        if (material == null) return;
        progress = Mathf.Clamp01(progress);
        material.SetFloat("_OverlayMode", 1f);
        material.SetFloat("_SmokeProgress", progress);
        material.SetFloat("_SmokeOpacity", density);
        material.SetFloat("_SmokeNoiseScale", noiseScale);
        material.SetFloat("_SmokeSoftness", edgeSoftness);
        material.SetVector("_SmokeScrollSpeed", scrollSpeed);
        material.SetColor("_SmokeColor", Color.black);
        material.SetFloat("_SmokeTime", smokeTime);
        material.SetFloat("_SmokeAspect", (float)Screen.width / Mathf.Max(1, Screen.height));
        // Transparent at rest, and completely opaque at 1 regardless of density.
        image.enabled = progress > 0f;
        group.blocksRaycasts = IsPlaying || progress >= 0.999f;
        group.interactable = false;
    }

    public void SetProgress(float value)
    {
        Cancel();
        Initialize();
        progress = Mathf.Clamp01(value);
        Apply();
    }

    public void PlayTransition() { Begin(0); }
    public void Cover() { Begin(1); }
    public void Reveal() { Begin(2); }

    private void Begin(int mode)
    {
        Cancel();
        Initialize();
        if (!isActiveAndEnabled) return;
        IsPlaying = true;
        routine = StartCoroutine(Animate(mode));
    }

    private IEnumerator Animate(int mode)
    {
        if (mode != 2)
        {
            yield return MoveTo(1f, coverDuration);
            // Render at least one fully black frame before changing the content.
            yield return null;
            onCovered.Invoke();
        }
        if (mode == 0) yield return new WaitForSecondsRealtime(holdDuration);
        if (mode != 1) yield return MoveTo(0f, revealDuration);
        IsPlaying = false;
        routine = null;
        Apply();
    }

    private IEnumerator MoveTo(float target, float seconds)
    {
        float from = progress;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, seconds) * Mathf.Abs(target - from);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            progress = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            Apply();
            yield return null;
        }
        progress = target;
        Apply();
    }

    private void Cancel()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        IsPlaying = false;
    }

    private void OnDisable()
    {
        Cancel();
        progress = 0f;
        Apply();
    }

    private void OnDestroy()
    {
        if (image != null && image.material == material) image.material = previousMaterial;
        if (material != null) Destroy(material);
    }
}
