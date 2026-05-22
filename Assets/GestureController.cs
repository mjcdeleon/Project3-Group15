using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Task 3 - Gesture Controller
/// Uses Unity's built-in XR InputDevice system - no OVR types needed.
///
/// SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject called "GestureController"
/// 2. Attach this script to it
/// 3. In the Inspector, drag "Erika Archer@T-Pose" into Avatar Controller
/// 4. Drag the "[BuildingBlock] Hand Tracking right" into Right Hand Transform
///
/// GESTURE DESIGN:
/// - Open hand              = idle
/// - Closed fist            = activate control mode
/// - Fist + index pointing  = agent moves in hand's forward direction
/// </summary>
public class GestureController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your Erika Archer@T-Pose GameObject here")]
    [SerializeField] private avatarController avatarController;

    [Tooltip("Drag the right hand tracking GameObject here (e.g. RightHandAnchor)")]
    [SerializeField] private Transform rightHandTransform;

    [Header("Movement")]
    [Tooltip("How far ahead the agent moves per gesture trigger (meters)")]
    [SerializeField] private float moveDistance = 3.0f;

    [Header("Gesture Tuning")]
    [Tooltip("Grip threshold to count as a fist (0=open, 1=fully closed)")]
    [SerializeField] private float fistGripThreshold = 0.7f;

    [Tooltip("Index curl threshold to count as pointing (0=curled, 1=straight)")]
    [SerializeField] private float pointingThreshold = 0.3f;

    [Tooltip("Seconds gesture must be held before triggering movement")]
    [SerializeField] private float gestureHoldTime = 0.5f;

    // Internal
    private float gestureTimer = 0f;
    private bool hasTriggeredMove = false;
    private Vector3 lastDestination;

    // XR device
    private InputDevice rightHandDevice;
    private bool deviceFound = false;

    void Update()
    {
        // Try to find the right hand device if not found yet
        if (!deviceFound)
        {
            TryFindRightHand();
            if (!deviceFound) return;
        }

        bool fistClosed = IsFistClosed();
        bool isPointing = IsPointing();

        if (fistClosed && isPointing)
        {
            gestureTimer += Time.deltaTime;

            if (gestureTimer >= gestureHoldTime && !hasTriggeredMove)
            {
                TriggerMovement();
                hasTriggeredMove = true;
            }
        }
        else
        {
            gestureTimer = 0f;
            hasTriggeredMove = false;
        }
    }

    private void TryFindRightHand()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.HandTracking,
            devices
        );

        if (devices.Count > 0)
        {
            rightHandDevice = devices[0];
            deviceFound = true;
            Debug.Log("[GestureController] Right hand device found.");
        }
    }

    /// <summary>
    /// Fist = high grip value on the hand device.
    /// </summary>
    private bool IsFistClosed()
    {
        if (rightHandDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            return gripValue >= fistGripThreshold;
        }
        return false;
    }

    /// <summary>
    /// Pointing = index finger is extended (low curl value).
    /// Falls back to trigger axis if index curl not available.
    /// </summary>
    private bool IsPointing()
    {
        // Try index finger curl (available with hand tracking)
        if (rightHandDevice.TryGetFeatureValue(
            new InputFeatureUsage<float>("IndexFinger"), out float indexCurl))
        {
            return indexCurl <= pointingThreshold;
        }

        // Fallback: use trigger value (lower = more extended)
        if (rightHandDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            return triggerValue <= pointingThreshold;
        }

        return false;
    }

    /// <summary>
    /// Sends the agent toward where the hand is pointing.
    /// </summary>
    private void TriggerMovement()
    {
        if (avatarController == null || rightHandTransform == null) return;

        // Get forward direction of the hand, flattened to horizontal
        Vector3 pointDirection = rightHandTransform.forward;
        pointDirection.y = 0f;
        pointDirection.Normalize();

        // Raycast to check for walls placed by SpatialAnchorPlacer
        Ray ray = new Ray(rightHandTransform.position, pointDirection);
        Vector3 destination;

        if (Physics.Raycast(ray, out RaycastHit hit, moveDistance))
        {
            // Wall in the way - stop just before it
            destination = hit.point - pointDirection * 0.3f;
        }
        else
        {
            // Clear path - move forward
            destination = avatarController.transform.position + pointDirection * moveDistance;
        }

        destination.y = avatarController.transform.position.y;
        lastDestination = destination;
        avatarController.GoToLocation(destination);

        Debug.Log($"[GestureController] Moving to {destination}");
    }

    void OnDrawGizmos()
    {
        if (lastDestination == Vector3.zero) return;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(lastDestination, 0.15f);
        if (rightHandTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rightHandTransform.position, lastDestination);
        }
    }
}