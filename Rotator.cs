using UnityEngine;

[RequireComponent(typeof(MovingTarget))]
public class Rotator : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second around the Y axis.")]
    [SerializeField]
    private float rotationSpeed = 90f;

    private Rigidbody rb;
    private MovingTarget target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        target = GetComponent<MovingTarget>();
    }

    private void FixedUpdate()
    {
        if (target != null && target.IsStopped)
        {
            return;
        }

        // Calculate rotation for this frame
        float rotationAmount = rotationSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);

        if (rb != null)
        {
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
        else
        {
            transform.Rotate(0f, rotationAmount, 0f, Space.World);
        }
    }
}
