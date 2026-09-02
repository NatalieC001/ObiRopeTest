using UnityEngine;

[RequireComponent(typeof(MovingTarget))]
public class PingPongMovement : MonoBehaviour
{
    [SerializeField]
    private float movementRadius = 2f;

    [SerializeField]
    private float speed = 1f;

    private Rigidbody rb;
    private MovingTarget target;

    private Vector3 startPosition;
    private float movementTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        target = GetComponent<MovingTarget>();

        startPosition = transform.position;

        // Randomize the starting position so each target moves independently
        movementTimer = Random.Range(0f, 100f);
    }

    private void FixedUpdate()
    {
        if (target != null && target.IsStopped)
        {
            return;
        }

        movementTimer += Time.fixedDeltaTime * speed;

        // Simple ping-pong motion along the right direction
        float offset = Mathf.PingPong(movementTimer, movementRadius * 2f) - movementRadius;
        Vector3 targetPosition = startPosition + transform.right * offset;

        if (rb != null)
        {
            rb.MovePosition(targetPosition);
        }
        else
        {
            transform.position = targetPosition;
        }
    }
}
