using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

/// <summary>Built-in renderer: raycast the model, then draw only its outer silhouette.</summary>
public sealed class ModelHoverOutline : MonoBehaviour
{
    public Camera viewCamera;
    public Material outlineTemplate;
    public ScreenSmokeTransition transition;
    public Color outlineColor = new Color(1f, 0.77f, 0.15f, 1f);
    [Range(1f, 10f)] public float outlineWidth = 4f;
    [Tooltip("没有碰撞体时，为静止人物按当前网格创建精确碰撞体。动画人物请配置跟随骨骼的碰撞体。")]
    public bool createStaticMeshColliders = true;
    public bool IsHovered { get; private set; }
    private Renderer[] renderers;
    private Material material;
    private CommandBuffer commands;
    private readonly List<Mesh> bakedMeshes = new List<Mesh>();
    private readonly List<Collider> generatedColliders = new List<Collider>();

    private void OnEnable()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null || outlineTemplate == null) return;
        renderers = GetComponentsInChildren<Renderer>();
        material = new Material(outlineTemplate) { hideFlags = HideFlags.HideAndDontSave };
        commands = new CommandBuffer { name = "Character hover outline" };
        viewCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, commands);
        if (createStaticMeshColliders && GetComponentInChildren<Collider>() == null)
            CreateColliders();
    }

    private void CreateColliders()
    {
        foreach (Renderer renderer in renderers)
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skin)
            {
                mesh = new Mesh { name = "Hover collider (current pose)" };
                skin.BakeMesh(mesh);
                bakedMeshes.Add(mesh);
            }
            else if (renderer.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
            if (mesh == null) continue;
            var collider = renderer.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            generatedColliders.Add(collider);
        }
    }

    private void Update()
    {
        bool hovered = false;
        if (viewCamera != null && (transition == null || !transition.BlocksInteraction)
            && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            && viewCamera.pixelRect.Contains(Input.mousePosition))
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, viewCamera.farClipPlane, viewCamera.cullingMask, QueryTriggerInteraction.Ignore))
                hovered = hit.transform == transform || hit.transform.IsChildOf(transform);
        }
        SetHighlighted(hovered);
    }

    // Also usable by selection logic; Update normally follows the mouse.
    public void SetHighlighted(bool highlighted)
    {
        IsHovered = highlighted;
        if (commands == null || material == null) return;
        commands.Clear();
        if (!highlighted) return;
        material.SetColor("_OutlineColor", outlineColor);
        material.SetFloat("_OutlineWidth", outlineWidth);
        // Mark all original surfaces first, then draw expanded backs outside that mask.
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                Mesh mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh
                    : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    commands.DrawRenderer(renderer, material, submesh, pass);
            }
        }
    }

    private void OnDisable()
    {
        IsHovered = false;
        if (viewCamera != null && commands != null)
            viewCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, commands);
        commands?.Release();
        commands = null;
        if (material != null) Destroy(material);
        foreach (Collider collider in generatedColliders) if (collider != null) Destroy(collider);
        foreach (Mesh mesh in bakedMeshes) if (mesh != null) Destroy(mesh);
        generatedColliders.Clear();
        bakedMeshes.Clear();
    }
}
