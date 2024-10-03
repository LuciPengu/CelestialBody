using UnityEngine;

public class OrreryCameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float zoomSpeed = 10f;
    public float rotationSpeed = 100f;
    public float smoothTime = 0.3f;

    private Vector3 targetPosition;
    private float targetZoom;
    private Vector3 currentVelocity;
    private float zoomVelocity;

    private void Start()
    {
        targetPosition = transform.position;
        targetZoom = Camera.main.orthographicSize;
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        targetPosition += transform.TransformDirection(movement);

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        targetZoom -= scroll * zoomSpeed;
        targetZoom = Mathf.Clamp(targetZoom, 1f, 100f);

        Camera.main.orthographicSize = Mathf.SmoothDamp(Camera.main.orthographicSize, targetZoom, ref zoomVelocity, smoothTime);
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

            transform.RotateAround(Vector3.zero, transform.right, -mouseY);
            transform.RotateAround(Vector3.zero, Vector3.up, mouseX);
        }
    }
}