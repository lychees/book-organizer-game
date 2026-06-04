using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 8, 6);
    public float followSpeed = 8f;
    public float lookAtHeight = 1f;

    [Header("Orbit Controls")]
    [Tooltip("Mouse button for orbiting (0=left, 1=right, 2=middle). -1 disables mouse drag.")]
    public int orbitMouseButton = 1; // Right mouse button
    public float rotationSpeedX = 3f; // Horizontal rotation speed
    public float rotationSpeedY = 2f; // Vertical rotation speed
    public float pitchMin = 5f;
    public float pitchMax = 85f;

    [Header("Zoom Controls")]
    public float zoomSpeed = 8f;
    public float minDistance = 4f;
    public float maxDistance = 35f;

    private float yaw;
    private float pitch;
    private float currentDistance;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Initialize orbit angles from the configured offset
        currentDistance = offset.magnitude;
        yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(Mathf.Clamp(offset.y / currentDistance, -1f, 1f)) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleInput();

        // Calculate desired camera position from spherical coordinates
        float pitchRad = pitch * Mathf.Deg2Rad;
        float yawRad = yaw * Mathf.Deg2Rad;

        Vector3 desiredOffset = new Vector3(
            currentDistance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
            currentDistance * Mathf.Sin(pitchRad),
            currentDistance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
        );

        Vector3 targetPos = target.position + desiredOffset;

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        // Look at player
        Vector3 lookPos = target.position + Vector3.up * lookAtHeight;
        transform.LookAt(lookPos);
    }

    void HandleInput()
    {
        // Orbit with right mouse button drag
        if (orbitMouseButton >= 0 && Input.GetMouseButton(orbitMouseButton))
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeedX;
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeedY;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        // Zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }
}
