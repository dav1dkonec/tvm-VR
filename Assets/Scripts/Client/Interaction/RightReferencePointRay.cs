using System;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Centers;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.InputSystem;

public class RightReferencePointRay : MonoBehaviour
{
    private const float MaxDistance = 8f;
    private const float OriginOffset = 0.04f;
    private const float StartWidth = 0.004f;
    private const float EndWidth = 0.0025f;

    private static readonly Color ValidColor = new(0.6862745f, 0.98039216f, 0.88235295f, 0.95f);
    private static readonly Color InvalidColor = new(0.40392157f, 0.8509804f, 0.7607843f, 0.28f);

    public GameObject rightHand;
    public InputActionProperty rightActivate;

    private EditingMethodRuntimeSettings methodSettings;
    private InflateDeflateUI inflateDeflateUi;
    private Sequence sequence;
    private CenterPool centerPool;
    private LineRenderer lineRenderer;
    private Material lineMaterial;
    private bool wasPressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var grabToMove = UnityEngine.Object.FindFirstObjectByType<GrabToMove>();
        var handObject = grabToMove != null ? grabToMove.rightHand : GameObject.Find("Right Hand");
        if (handObject == null || handObject.GetComponent<RightReferencePointRay>() != null)
            return;

        var component = handObject.AddComponent<RightReferencePointRay>();
        component.rightHand = handObject;

        if (grabToMove != null)
            component.rightActivate = grabToMove.rightSelect;
    }

    private void Awake()
    {
        if (rightHand == null)
            rightHand = gameObject;

        var grabToMove = UnityEngine.Object.FindFirstObjectByType<GrabToMove>();
        if (rightActivate.action == null && grabToMove != null)
            rightActivate = grabToMove.rightSelect;

        methodSettings = UnityEngine.Object.FindFirstObjectByType<EditingMethodRuntimeSettings>();
        inflateDeflateUi = UnityEngine.Object.FindFirstObjectByType<InflateDeflateUI>();
        sequence = UnityEngine.Object.FindFirstObjectByType<Sequence>();
        centerPool = UnityEngine.Object.FindFirstObjectByType<CenterPool>();

        CreateLaser();
        SetLaserActive(false);
    }

    private void Update()
    {
        if (!IsPickModeActive())
        {
            wasPressed = false;
            centerPool?.ClearPreview();
            SetLaserActive(false);
            return;
        }

        if (rightActivate.action == null || rightHand == null)
        {
            SetLaserActive(false);
            return;
        }

        var isPressed = rightActivate.action.IsPressed();

        if (isPressed)
            UpdatePreview();
        else if (wasPressed)
            TryCommitSelection();
        else
            SetLaserActive(false);

        wasPressed = isPressed;
    }

    private bool IsPickModeActive()
    {
        return inflateDeflateUi != null
            && inflateDeflateUi.IsPickingReferencePoint
            && methodSettings != null
            && methodSettings.CurrentMethod == MethodKind.InflateDeflate;
    }

    private void TryCommitSelection()
    {
        SetLaserActive(false);

        if (!TryGetValidSequenceHit(out var hit))
            return;

        centerPool?.ClearPreview();
        inflateDeflateUi?.ShowPickCompleted();
        sequence.CommitInflateDeflate(hit.point);
    }

    private void UpdatePreview()
    {
        SetLaserActive(true);

        var ray = BuildRay();
        if (!TryGetClosestSceneHit(ray, out var hit))
        {
            centerPool?.ClearPreview();
            UpdateLaserVisual(ray.origin, ray.origin + ray.direction * MaxDistance, InvalidColor);
            return;
        }

        var hitSequence = hit.collider != null ? hit.collider.GetComponentInParent<Sequence>() : null;
        var valid = hitSequence == sequence;

        if (valid)
            centerPool?.PreviewInflateDeflate(hit.point);
        else
            centerPool?.ClearPreview();

        UpdateLaserVisual(ray.origin, hit.point, valid ? ValidColor : InvalidColor);
    }

    private bool TryGetValidSequenceHit(out RaycastHit hit)
    {
        hit = default;

        if (sequence == null)
            return false;

        var ray = BuildRay();
        if (!TryGetClosestSceneHit(ray, out hit) || hit.collider == null)
            return false;

        var hitSequence = hit.collider.GetComponentInParent<Sequence>();
        return hitSequence == sequence;
    }

    private bool TryGetClosestSceneHit(Ray ray, out RaycastHit hit)
    {
        hit = default;

        var hits = Physics.RaycastAll(ray, MaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        Array.Sort(hits, static (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var candidate = hits[i];
            if (candidate.collider == null)
                continue;

            if (rightHand != null && candidate.collider.transform.IsChildOf(rightHand.transform))
                continue;

            hit = candidate;
            return true;
        }

        return false;
    }

    private Ray BuildRay()
    {
        var origin = rightHand.transform.position + rightHand.transform.forward * OriginOffset;
        var direction = rightHand.transform.forward;
        return new Ray(origin, direction);
    }

    private void CreateLaser()
    {
        var laserObject = new GameObject("Right Reference Ray Visual");
        laserObject.transform.SetParent(transform, false);
        laserObject.transform.localPosition = Vector3.zero;
        laserObject.transform.localRotation = Quaternion.identity;
        laserObject.transform.localScale = Vector3.one;

        lineRenderer = laserObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.numCapVertices = 6;
        lineRenderer.startWidth = StartWidth;
        lineRenderer.endWidth = EndWidth;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Hidden/Internal-Colored");

        if (shader == null)
        {
            Debug.LogWarning("RightReferencePointRay: No suitable shader found for the pick laser.");
            return;
        }

        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.DontSave;
        lineRenderer.sharedMaterial = lineMaterial;
        lineRenderer.enabled = false;
    }

    private void SetLaserActive(bool active)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = active;
    }

    private void UpdateLaserVisual(Vector3 start, Vector3 end, Color color)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}
