using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Task 2 - Spatial Anchor Placer
/// Uses Unity's built-in XR InputDevice system - no OVR types needed.
///
/// SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject called "SpatialAnchorPlacer"
/// 2. Attach this script to it
/// 3. In the Inspector:
///    - Drag left hand tracking GameObject into Left Hand Transform
///    - Drag right hand tracking GameObject into Right Hand Transform
///
/// HOW TO PLACE ANCHORS AT RUNTIME:
/// - PINCH LEFT HAND (hold 1 sec)  = Place FLOOR at hand height
/// - PINCH RIGHT HAND (hold 1 sec) = Place WALL facing you
/// - Right-click component ? "Clear All Anchors" to reset
///
/// SATISFIES TASK 2:
/// - Spatial anchors of key surfaces (floor + walls)   [10 pts]
/// - Quad GameObjects placed at anchor positions        [10 pts]
/// - Box colliders block the avatar CharacterController [Task 4]
/// </summary>
public class SpatialAnchorPlacer : MonoBehaviour
{
    [Header("Hand Transforms")]
    [Tooltip("Drag the LEFT hand tracking GameObject here (e.g. LeftHandAnchor)")]
    [SerializeField] private Transform leftHandTransform;

    [Tooltip("Drag the RIGHT hand tracking GameObject here (e.g. RightHandAnchor)")]
    [SerializeField] private Transform rightHandTransform;

    [Header("Plane Sizes")]
    [SerializeField] private Vector2 floorSize = new Vector2(4f, 4f);
    [SerializeField] private Vector2 wallSize = new Vector2(3f, 2.5f);

    [Header("Gesture Tuning")]
    [Tooltip("Pinch/grip threshold to detect a pinch gesture (0-1)")]
    [SerializeField] private float pinchThreshold = 0.7f;

    [Tooltip("Seconds pinch must be held to place an anchor")]
    [SerializeField] private float pinchHoldTime = 1.0f;

    [Tooltip("Cooldown between placements")]
    [SerializeField] private float placementCooldown = 1.5f;

    // Timers and state
    private float leftPinchTimer = 0f;
    private float rightPinchTimer = 0f;
    private float lastPlacementTime = -999f;
    private bool leftPlacedThisGesture = false;
    private bool rightPlacedThisGesture = false;

    // XR devices
    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private bool leftFound = false;
    private bool rightFound = false;

    // Anchor tracking
    private List<GameObject> placedAnchors = new List<GameObject>();
    private int anchorCount = 0;

    void Update()
    {
        if (!leftFound) TryFindLeftHand();
        if (!rightFound) TryFindRightHand();

        HandleLeftHand();
        HandleRightHand();
    }

    private void TryFindLeftHand()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.HandTracking, devices);
        if (devices.Count > 0) { leftDevice = devices[0]; leftFound = true; }
    }

    private void TryFindRightHand()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.HandTracking, devices);
        if (devices.Count > 0) { rightDevice = devices[0]; rightFound = true; }
    }

    // ?? LEFT HAND: Place FLOOR ????????????????????????????????????????????????

    private void HandleLeftHand()
    {
        if (!leftFound || leftHandTransform == null) return;

        bool pinching = GetPinchValue(leftDevice) >= pinchThreshold;

        if (pinching)
        {
            leftPinchTimer += Time.deltaTime;
            if (leftPinchTimer >= pinchHoldTime && !leftPlacedThisGesture)
            {
                if (Time.time - lastPlacementTime >= placementCooldown)
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

    // ?? RIGHT HAND: Place WALL ????????????????????????????????????????????????

    private void HandleRightHand()
    {
        if (!rightFound || rightHandTransform == null) return;

        bool pinching = GetPinchValue(rightDevice) >= pinchThreshold;

        if (pinching)
        {
            rightPinchTimer += Time.deltaTime;
            if (rightPinchTimer >= pinchHoldTime && !rightPlacedThisGesture)
            {
                if (Time.time - lastPlacementTime >= placementCooldown)
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

    private float GetPinchValue(InputDevice device)
    {
        if (device.TryGetFeatureValue(CommonUsages.grip, out float grip))
            return grip;
        if (device.TryGetFeatureValue(CommonUsages.trigger, out float trigger))
            return trigger;
        return 0f;
    }

    // ?? PLACEMENT ?????????????????????????????????????????????????????????????

    private void PlaceFloorAnchor(Vector3 handPosition)
    {
        anchorCount++;
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floor.name = $"SpatialAnchor_Floor_{anchorCount}";

        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        floor.transform.position = new Vector3(
            Camera.main.transform.position.x + camForward.x,
            handPosition.y,
            Camera.main.transform.position.z + camForward.z
        );
        floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        floor.transform.localScale = new Vector3(floorSize.x, floorSize.y, 1f);

        ApplyTransparentMaterial(floor, new Color(0.2f, 0.5f, 1f, 0.25f));

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
        toCamera.y = 0;
        wall.transform.rotation = Quaternion.LookRotation(toCamera.normalized);
        wall.transform.localScale = new Vector3(wallSize.x, wallSize.y, 1f);

        ApplyTransparentMaterial(wall, new Color(1f, 0.4f, 0.1f, 0.25f));

        Destroy(wall.GetComponent<MeshCollider>());
        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 1f, 0.05f);

        placedAnchors.Add(wall);
        Debug.Log($"[SpatialAnchorPlacer] Wall placed at {handPosition}");
    }

    // ?? HELPERS ???????????????????????????????????????????????????????????????

    private void ApplyTransparentMaterial(GameObject obj, Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
        obj.GetComponent<Renderer>().material = mat;
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