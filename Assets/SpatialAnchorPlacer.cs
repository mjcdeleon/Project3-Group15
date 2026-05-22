using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Task 2 - Spatial Anchor Placer
///
/// PLACING ANCHORS:
/// - LEFT pinch only (hold 1 sec)  = place FLOOR plane (blue)
/// - RIGHT pinch only (hold 1 sec) = place WALL plane (orange)
///
/// FIX: Uses a grace period so brief accidental left pinch doesn't
/// cancel the right pinch timer.
/// </summary>
public class SpatialAnchorPlacer : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;

    [Header("Plane Sizes")]
    [SerializeField] private Vector2 floorSize = new Vector2(4f, 4f);
    [SerializeField] private Vector2 wallSize = new Vector2(3f, 2.5f);

    [Header("Gesture Tuning")]
    [SerializeField] private float holdTime = 1.0f;
    [SerializeField] private float cooldown = 1.5f;

    [Tooltip("How long the other hand can briefly pinch before cancelling (grace period)")]
    [SerializeField] private float gracePeriod = 0.3f;

    private float leftPinchTimer = 0f;
    private float rightPinchTimer = 0f;
    private float leftGraceTimer = 0f;
    private float rightGraceTimer = 0f;
    private float lastPlacementTime = -999f;
    private bool leftPlacedThisGesture = false;
    private bool rightPlacedThisGesture = false;

    private List<GameObject> placedAnchors = new List<GameObject>();
    private int anchorCount = 0;

    void Start()
    {
        Debug.Log("[SpatialAnchorPlacer] Started!");
    }

    void Update()
    {
        if (leftHand == null || rightHand == null) return;

        bool leftPinch = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool rightPinch = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        if (Time.frameCount % 60 == 0)
            Debug.Log($"[SpatialAnchorPlacer] leftPinch={leftPinch}, rightPinch={rightPinch}");

        // Grace period: if other hand briefly pinches, don't immediately cancel
        // Left hand placement
        if (leftPinch)
        {
            if (rightPinch)
            {
                // Right hand also pinching - count grace period
                rightGraceTimer += Time.deltaTime;
                if (rightGraceTimer < gracePeriod)
                {
                    // Still within grace - treat as left only
                    HandleLeftPlacement(true);
                }
                else
                {
                    // Both pinching too long - it's a movement gesture, reset
                    leftPinchTimer = 0f;
                    leftPlacedThisGesture = false;
                }
            }
            else
            {
                rightGraceTimer = 0f;
                HandleLeftPlacement(true);
            }
        }
        else
        {
            rightGraceTimer = 0f;
            HandleLeftPlacement(false);
        }

        // Right hand placement
        if (rightPinch)
        {
            if (leftPinch)
            {
                // Left hand also pinching - count grace period
                leftGraceTimer += Time.deltaTime;
                if (leftGraceTimer < gracePeriod)
                {
                    // Still within grace - treat as right only
                    HandleRightPlacement(true);
                }
                else
                {
                    // Both pinching too long - it's a movement gesture, reset
                    rightPinchTimer = 0f;
                    rightPlacedThisGesture = false;
                }
            }
            else
            {
                leftGraceTimer = 0f;
                HandleRightPlacement(true);
            }
        }
        else
        {
            leftGraceTimer = 0f;
            HandleRightPlacement(false);
        }
    }

    private void HandleLeftPlacement(bool active)
    {
        if (leftHandTransform == null) return;

        if (active)
        {
            leftPinchTimer += Time.deltaTime;
            Debug.Log($"[SpatialAnchorPlacer] Left pinch held {leftPinchTimer:F1}s");

            if (leftPinchTimer >= holdTime && !leftPlacedThisGesture)
            {
                if (Time.time - lastPlacementTime >= cooldown)
                {
                    PlaceFloorAnchor(leftHandTransform.position);
                    leftPlacedThisGesture = true;
                    lastPlacementTime = Time.time;
                }
            }
        }
        else
        {
            leftPinchTimer = 0f;
            leftPlacedThisGesture = false;
        }
    }

    private void HandleRightPlacement(bool active)
    {
        if (rightHandTransform == null) return;

        if (active)
        {
            rightPinchTimer += Time.deltaTime;
            Debug.Log($"[SpatialAnchorPlacer] Right pinch held {rightPinchTimer:F1}s");

            if (rightPinchTimer >= holdTime && !rightPlacedThisGesture)
            {
                if (Time.time - lastPlacementTime >= cooldown)
                {
                    PlaceWallAnchor(rightHandTransform.position);
                    rightPlacedThisGesture = true;
                    lastPlacementTime = Time.time;
                }
            }
        }
        else
        {
            rightPinchTimer = 0f;
            rightPlacedThisGesture = false;
        }
    }

    private void PlaceFloorAnchor(Vector3 handPosition)
    {
        anchorCount++;
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floor.name = $"SpatialAnchor_Floor_{anchorCount}";

        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        floor.transform.position = new Vector3(
            Camera.main.transform.position.x + camForward.x,
            handPosition.y,
            Camera.main.transform.position.z + camForward.z
        );
        floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        floor.transform.localScale = new Vector3(floorSize.x, floorSize.y, 1f);

        ApplyURPMaterial(floor, new Color(0.2f, 0.5f, 1f, 0.4f));
        Destroy(floor.GetComponent<MeshCollider>());
        BoxCollider box = floor.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 0.02f, 1f);

        placedAnchors.Add(floor);
        Debug.Log($"[SpatialAnchorPlacer] Floor placed at Y={handPosition.y:F2}m");
    }

    private void PlaceWallAnchor(Vector3 handPosition)
    {
        anchorCount++;
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wall.name = $"SpatialAnchor_Wall_{anchorCount}";

        wall.transform.position = new Vector3(
            handPosition.x,
            handPosition.y + wallSize.y * 0.25f,
            handPosition.z
        );

        Vector3 toCamera = Camera.main.transform.position - handPosition;
        toCamera.y = 0f;
        if (toCamera.magnitude < 0.01f) toCamera = Vector3.forward;
        wall.transform.rotation = Quaternion.LookRotation(toCamera.normalized);
        wall.transform.localScale = new Vector3(wallSize.x, wallSize.y, 1f);

        ApplyURPMaterial(wall, new Color(1f, 0.4f, 0.1f, 0.4f));
        Destroy(wall.GetComponent<MeshCollider>());
        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 1f, 0.05f);

        placedAnchors.Add(wall);
        Debug.Log($"[SpatialAnchorPlacer] Wall placed at {handPosition}");
    }

    private void ApplyURPMaterial(GameObject obj, Color color)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        Material mat = null;

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader != null)
        {
            mat = new Material(urpShader);
            mat.color = color;
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_AlphaClip", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
        else
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }

        rend.material = mat;
    }

    [ContextMenu("Clear All Anchors")]
    public void ClearAllAnchors()
    {
        foreach (GameObject anchor in placedAnchors)
            if (anchor != null) Destroy(anchor);
        placedAnchors.Clear();
        anchorCount = 0;
        Debug.Log("[SpatialAnchorPlacer] All anchors cleared.");
    }

    void OnDrawGizmos()
    {
        foreach (GameObject anchor in placedAnchors)
        {
            if (anchor == null) continue;
            Gizmos.color = anchor.name.Contains("Floor") ? Color.blue : Color.red;
            Gizmos.DrawWireCube(anchor.transform.position, anchor.transform.localScale);
        }
    }
}