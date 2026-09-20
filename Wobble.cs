using UnityEngine;

public class Wobble : MonoBehaviour
{
    [Tooltip("How fast the object wobbles.")]
    public float wobbleSpeed = 2f;

    [Tooltip("How far the object wobbles in degrees.")]
    public float wobbleAmount = 15f;

    private Quaternion startRotation;

    void Start()
    {
        // Store the initial rotation so we wobble around it
        startRotation = transform.localRotation;
    }

    void Update()
    {
        // Calculate the wobble angle using Sine wave for smooth back-and-forth
        float angle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;

        // Apply the wobble to the Y axis relative to the starting rotation
        transform.localRotation = startRotation * Quaternion.Euler(0f, angle, 0f);
    }
}
