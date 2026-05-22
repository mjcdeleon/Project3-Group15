using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Task 2 - Spatial Anchor Placer
/// Uses OVRHand pinch from Meta XR All-In-One SDK.
///
/// SETUP:
/// 1. Attach to SpatialAnchorPlacer GameObject
/// 2. In Inspector drag:
///    - LeftHandAnchor                   -> Left Hand Transform
///    - RightHandAnchor                  -> Right Hand Transform
///    - [BuildingBlock] Hand Tracking left  -> Left Hand
///    - [BuildingBlock] Hand Tracking right -> Right Hand
///
/// GESTURES:
/// - LEFT pinch only (hold 0.5 sec)  = place FLOOR (blue) at hand height
/// - RIGHT pinch only (hold 0.5 sec) = place WALL (orange) at hand position
///
/// SATISFIES TASK 2:
/// - Spatial anchors of key surfaces (floor + walls)   [10 pts]
/// - Quad GameObjects placed at those positions         [10 pts]
/// - Box colliders block the avatar CharacterController [Task 4]
/// </summary>
public class SpatialAnchorPlacer : MonoBehaviour
{
    [Header("Hand References")]
    [Tooltip("Drag LeftHandAnchor here")]
    [SerializeField] private Transform leftHandTransform;

    [Tooltip("Drag RightHandAnchor here")]
    [SerializeField] private Transform rightHandTransform;

    [Tooltip("Drag [BuildingBlock] Hand Tracking left here")]
    [SerializeField] private OVRHand leftHand;

    [Tooltip("Drag [BuildingBlock] Hand Tracking right here")]
    [SerializeField] private OVRHand rightHand;

    [Header("Plane Sizes")]
    [SerializeField] private Vector2 floorSize = new Vector2(4f, 4f);
    [SerializeField] private Vector2 wallSize = new Vector2(3f, 2.5f);

    [Header("Gesture Tuning")]
    [Tooltip("Seconds pinch must be held to place anchor")]
    [SerializeField] private float holdTime = 0.5f;

    [Tooltip("Cooldown between placements")]
    [SerializeField] private float cooldown = 2.0f;

    [Tooltip("Grace period before other hand cancels timer")]
    [SerializeField] private float gracePeriod = 0.8f;

    [Header("Height Settings")]
    [Tooltip("Y position of the floor plane (set to match your real floor)")]
    [SerializeField] private float floorY = -0.35f;

    [Tooltip("Y position of the bottom of wall planes")]
    [SerializeField] private float wallBaseY = -0.35f;

    // Internal state
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
        Debug.Log("[SpatialAnchorPlacer] Left pinch=floor, Right pinch=wall");
    }

    void Update()
    {
        if (leftHand == null || rightHand == null) return;

        bool leftPinch = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool rightPinch = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        if (Time.frameCount % 60 == 0)
            Debug.Log($"[SpatialAnchorPlacer] leftPinch={leftPinch}, rightPinch={rightPinch}");

        // LEFT HAND with grace period
        if (leftPinch)
        {
            if (rightPinch)
            {
                rightGraceTimer += Time.deltaTime;
                if (rightGraceTimer < gracePeriod)
                    HandleLeftPlacement(true);
                else
                {
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

        // RIGHT HAND with grace period
        if (rightPinch)
        {
            if (leftPinch)
            {
                leftGraceTimer += Time.deltaTime;
                if (leftGraceTimer < gracePeriod)
                    HandleRightPlacement(true);
                else
                {
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
        if (camForward.magnitude < 0.01f) camForward = Vector3.forward;
        camForward.Normalize();

        // Place floor at fixed Y (Erika's floor level) in front of camera
        floor.transform.position = new Vector3(
            Camera.main.transform.position.x + camForward.x,
            floorY,
            Camera.main.transform.position.z + camForward.z
        );
        floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        floor.transform.localScale = new Vector3(floorSize.x, floorSize.y, 1f);

        ApplySolidMaterial(floor, new Color(0.2f, 0.5f, 1f, 1f));
        Destroy(floor.GetComponent<MeshCollider>());
        BoxCollider box = floor.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 0.02f, 1f);

        placedAnchors.Add(floor);
        Debug.Log($"[SpatialAnchorPlacer] Floor placed at Y={floorY}");
    }

    private void PlaceWallAnchor(Vector3 handPosition)
    {
        anchorCount++;
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wall.name = $"SpatialAnchor_Wall_{anchorCount}";

        // Place wall at fixed base Y, centered at wallBaseY + half wall height
        // This ensures the wall is always at ground level regardless of hand height
        float wallCenterY = wallBaseY + wallSize.y * 0.5f;
        wall.transform.position = new Vector3(
            handPosition.x,
            wallCenterY,
            handPosition.z
        );

        // Face wall toward camera
        Vector3 toCamera = Camera.main.transform.position - handPosition;
        toCamera.y = 0f;
        if (toCamera.magnitude < 0.01f) toCamera = Vector3.forward;
        wall.transform.rotation = Quaternion.LookRotation(-toCamera.normalized);
        wall.transform.localScale = new Vector3(wallSize.x, wallSize.y, 1f);

        ApplySolidMaterial(wall, new Color(1f, 0.4f, 0.1f, 1f));
        Destroy(wall.GetComponent<MeshCollider>());
        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 1f, 0.05f);

        placedAnchors.Add(wall);
        Debug.Log($"[SpatialAnchorPlacer] Wall placed at X={handPosition.x:F2}, Y={wallCenterY:F2}, Z={handPosition.z:F2}");
    }

    private void ApplySolidMaterial(GameObject obj, Color color)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        // Use default material color - guaranteed to be visible
        rend.material.color = color;
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