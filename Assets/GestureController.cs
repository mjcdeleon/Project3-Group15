using UnityEngine;

/// <summary>
/// Task 3 - Gesture Controller
///
/// GESTURE TO MOVE AGENT:
/// - Pinch BOTH hands simultaneously and hold 0.5 sec
/// - Agent walks in the direction you are LOOKING (camera forward)
/// - Release either pinch to reset
/// </summary>
public class GestureController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private avatarController avatarController;
    [SerializeField] private Transform rightHandTransform;
    [SerializeField] private OVRHand rightHand;
    [SerializeField] private OVRHand leftHand;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 2.0f;
    [SerializeField] private float holdTime = 0.5f;
    [SerializeField] private float moveCooldown = 2.0f;

    private float bothPinchTimer = 0f;
    private bool hasTriggered = false;
    private float lastTriggerTime = -999f;
    private Vector3 lastDestination;

    void Start()
    {
        Debug.Log("[GestureController] Started!");
    }

    void Update()
    {
        if (avatarController == null || rightHand == null || leftHand == null) return;

        bool rightPinch = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool leftPinch = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool bothPinch = rightPinch && leftPinch;

        if (Time.frameCount % 60 == 0)
            Debug.Log($"[GestureController] rightPinch={rightPinch}, leftPinch={leftPinch}");

        if (bothPinch)
        {
            bothPinchTimer += Time.deltaTime;
            Debug.Log($"[GestureController] Both pinching! Timer={bothPinchTimer:F1}s");

            if (bothPinchTimer >= holdTime && !hasTriggered)
            {
                if (Time.time - lastTriggerTime >= moveCooldown)
                {
                    TriggerMovement();
                    hasTriggered = true;
                    lastTriggerTime = Time.time;
                }
            }
        }
        else
        {
            bothPinchTimer = 0f;
            hasTriggered = false;
        }
    }

    private void TriggerMovement()
    {
        // Use camera forward (where you're LOOKING) as movement direction
        // Much more reliable than hand forward direction
        Vector3 direction = Camera.main.transform.forward;
        direction.y = 0f;

        if (direction.magnitude < 0.01f)
        {
            Debug.LogWarning("[GestureController] Direction too small, skipping.");
            return;
        }

        direction.Normalize();

        // Start from avatar's current position
        Vector3 avatarPos = avatarController.transform.position;
        Vector3 destination = avatarPos + direction * moveDistance;
        destination.y = avatarPos.y;

        // Raycast from avatar to check for walls
        Ray ray = new Ray(new Vector3(avatarPos.x, avatarPos.y + 0.5f, avatarPos.z), direction);
        if (Physics.Raycast(ray, out RaycastHit hit, moveDistance))
        {
            destination = new Vector3(hit.point.x, avatarPos.y, hit.point.z) - direction * 0.3f;
            Debug.Log($"[GestureController] Wall hit, stopping before it.");
        }

        lastDestination = destination;
        avatarController.GoToLocation(destination);
        Debug.Log($"[GestureController] Moving to {destination}");
    }

    void OnDrawGizmos()
    {
        if (lastDestination == Vector3.zero) return;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(lastDestination, 0.15f);
    }
}