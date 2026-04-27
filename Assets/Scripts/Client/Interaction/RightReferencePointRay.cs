using System;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Centers;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class RightReferencePointRay : MonoBehaviour
{
    private const float MaxDistance = 8f;
    private const float OriginOffset = 0.04f;
    private const float StartWidth = 0.007f;
    private const float EndWidth = 0.0045f;
    private const float PressThreshold = 0.15f;

    private static readonly Color ValidColor = new(0.6862745f, 0.98039216f, 0.88235295f, 0.95f);
    private static readonly Color InvalidColor = new(0.40392157f, 0.8509804f, 0.7607843f, 0.55f);

    public GameObject rightHand;
    public InputActionProperty rightActivate;
    public InputActionProperty rightActivateValue;

    private EditingMethodRuntimeSettings methodSettings;
    private InflateDeflateUI inflateDeflateUi;
    private Sequence sequence;
    private CenterPool centerPool;
    private LineRenderer lineRenderer;
    private Material lineMaterial;
    private Transform aimTransform;
    private bool wasPressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static RightReferencePointRay EnsureExists()
    {
        var existing = UnityEngine.Object.FindFirstObjectByType<RightReferencePointRay>();
        if (existing != null)
        {
            existing.RefreshSceneReferences();
            return existing;
        }

        var grabToMove = UnityEngine.Object.FindFirstObjectByType<GrabToMove>();
        var handObject = grabToMove != null ? grabToMove.rightHand : GameObject.Find("Right Hand");
        if (handObject == null)
            return null;

        var component = handObject.GetComponent<RightReferencePointRay>();
        if (component == null)
            component = handObject.AddComponent<RightReferencePointRay>();

        component.rightHand = handObject;
        component.RefreshSceneReferences();
        return component;
    }

    private void Awake()
    {
        if (rightHand == null)
            rightHand = gameObject;

        RefreshSceneReferences();

        CreateLaser();
        SetLaserActive(false);
    }

    private void Update()
    {
        RefreshSceneReferences();

        if (!IsPickModeActive())
        {
            wasPressed = false;
            centerPool?.ClearPreview();
            SetLaserActive(false);
            return;
        }

        if (rightHand == null)
        {
            SetLaserActive(false);
            return;
        }

        ResolveAimTransform();
        var isPressed = IsActivationPressed();
        UpdatePreview();

        if (isPressed && !wasPressed)
            TryCommitSelection();

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
        var sourceTransform = aimTransform != null ? aimTransform : rightHand.transform;
        var direction = sourceTransform.forward;
        if (Vector3.Dot(direction.normalized, Vector3.up) > 0.55f)
            direction = sourceTransform.up;
        if (Vector3.Dot(direction.normalized, Vector3.up) > 0.55f)
            direction = -sourceTransform.right;

        direction = direction.normalized;
        var origin = sourceTransform.position + direction * OriginOffset;
        return new Ray(origin, direction);
    }

    private void RefreshSceneReferences()
    {
        if (rightHand == null)
        {
            var grabToMove = UnityEngine.Object.FindFirstObjectByType<GrabToMove>();
            if (grabToMove != null && grabToMove.rightHand != null)
                rightHand = grabToMove.rightHand;
            else
                rightHand = GameObject.Find("Right Hand");
        }

        if (methodSettings == null)
            methodSettings = UnityEngine.Object.FindFirstObjectByType<EditingMethodRuntimeSettings>();

        if (inflateDeflateUi == null)
            inflateDeflateUi = UnityEngine.Object.FindFirstObjectByType<InflateDeflateUI>();

        if (sequence == null)
            sequence = UnityEngine.Object.FindFirstObjectByType<Sequence>();

        if (centerPool == null)
            centerPool = UnityEngine.Object.FindFirstObjectByType<CenterPool>();

        if (rightHand != null)
        {
            var actionBasedController = rightHand.GetComponent<ActionBasedController>();
            if (rightActivate.action == null && actionBasedController != null && actionBasedController.activateAction.action != null)
            {
                rightActivate = actionBasedController.activateAction;
            }

            if (rightActivateValue.action == null && actionBasedController != null && actionBasedController.activateActionValue.action != null)
            {
                rightActivateValue = actionBasedController.activateActionValue;
            }

            if (rightActivate.action == null)
            {
                var grabToMove = UnityEngine.Object.FindFirstObjectByType<GrabToMove>();
                if (grabToMove != null && grabToMove.rightSelect.action != null)
                    rightActivate = grabToMove.rightSelect;
            }
        }

        ResolveAimTransform();
    }

    private bool IsActivationPressed()
    {
        if (IsButtonActionPressed(rightActivate.action))
            return true;

        if (IsAxisActionPressed(rightActivateValue.action))
            return true;

        return false;
    }

    private static bool IsButtonActionPressed(InputAction action)
    {
        if (action == null)
            return false;

        if (action.WasPressedThisFrame() || action.IsPressed())
            return true;

        return IsAxisActionPressed(action);
    }

    private static bool IsAxisActionPressed(InputAction action)
    {
        if (action == null)
            return false;

        try
        {
            return action.ReadValue<float>() > PressThreshold;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void ResolveAimTransform()
    {
        if (rightHand == null)
            return;

        var playbackMenu = rightHand.transform.Find("Playback Menu");
        if (playbackMenu != null)
        {
            aimTransform = playbackMenu;
            return;
        }

        aimTransform = rightHand.transform;
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
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 6;
        lineRenderer.startWidth = StartWidth;
        lineRenderer.endWidth = EndWidth;
        lineRenderer.sortingOrder = 1000;

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
        if (lineMaterial.HasProperty("_BaseColor"))
            lineMaterial.SetColor("_BaseColor", Color.white);
        if (lineMaterial.HasProperty("_Color"))
            lineMaterial.SetColor("_Color", Color.white);
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
