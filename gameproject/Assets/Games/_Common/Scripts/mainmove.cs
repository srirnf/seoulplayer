using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class mainmove : MonoBehaviour
{
    [Header("Move Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 12f;

    [Header("Ground Lock")]
    public bool lockYPosition = true;
    public float fixedY = 0f;

    private Rigidbody rb;
    private Vector3 inputDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        inputDirection = new Vector3(h, 0f, v).normalized;
    }

    private void FixedUpdate()
    {
        Move();
        Rotate();
        ClampY();
    }

    private void Move()
    {
        Vector3 nextPosition = rb.position + inputDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);
    }

    private void Rotate()
    {
        if (inputDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }

    private void ClampY()
    {
        if (!lockYPosition)
            return;

        Vector3 pos = rb.position;
        pos.y = fixedY;
        rb.position = pos;
    }
}

