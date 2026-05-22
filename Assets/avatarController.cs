using UnityEngine;

public class avatarController : MonoBehaviour
{
    private Animator animator;
    private CharacterController characterController;

    private Vector3 targetDestination;
    private bool isMoving = false;

    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 5.0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        targetDestination = transform.position;
    }

    void Update()
    {
        if (isMoving)
        {
            // Lock the Y-axis so the avatar doesn't tilt up/down
            Vector3 targetFloorPos = new Vector3(targetDestination.x, transform.position.y, targetDestination.z);
            float distance = Vector3.Distance(transform.position, targetFloorPos);

            if (distance > 0.2f) // Stop 20cm before the exact point
            {
                // 1. Face the destination
                Vector3 direction = (targetFloorPos - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

                // 2. Move safely via physics (Automatically respects Task 2 walls!)
                Vector3 motion = direction * moveSpeed * Time.deltaTime;
                motion.y = 0f;
                characterController.Move(motion);
                transform.position = new Vector3(transform.position.x, -0.35f, transform.position.z);
            }
            else
            {
                // Arrived! Return to idle
                isMoving = false;
                animator.SetBool("Walking", false);
            }
        }
    }

    public void GoToLocation(Vector3 newDestination)
    {
        targetDestination = newDestination;
        isMoving = true;
        animator.SetBool("Walking", true); // Flips your Mixamo clip from Idle to Walk
    }
}
