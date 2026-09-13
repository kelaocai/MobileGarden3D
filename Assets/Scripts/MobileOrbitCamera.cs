using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mobile-friendly orbit camera: one-finger drag rotates, two-finger pinch zooms.
/// Mouse controls are included for testing in the Unity Editor.
/// </summary>
[DisallowMultipleComponent]
public sealed class MobileOrbitCamera : MonoBehaviour
{
    [Header("Focus")]
    [SerializeField] private Vector3 focusPoint = new(0f, 1.5f, 1.5f);

    [Header("Rotation")]
    [SerializeField, Min(0.01f)] private float dragSensitivity = 0.12f;
    [SerializeField] private float minimumPitch = 20f;
    [SerializeField] private float maximumPitch = 75f;

    [Header("Zoom")]
    [SerializeField] private float minimumDistance = 8f;
    [SerializeField] private float maximumDistance = 24f;
    [SerializeField, Min(0.001f)] private float pinchSensitivity = 0.015f;

    private float yaw;
    private float pitch;
    private float distance;
    private bool mouseDragging;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null &&
            mainCamera.GetComponent<LockedArenaCamera>() == null &&
            mainCamera.GetComponent<MobileOrbitCamera>() == null)
        {
            mainCamera.gameObject.AddComponent<MobileOrbitCamera>();
        }
    }

    private void OnEnable()
    {
        Vector3 offset = transform.position - focusPoint;
        distance = Mathf.Clamp(offset.magnitude, minimumDistance, maximumDistance);

        Vector3 direction = offset.sqrMagnitude > 0.001f ? offset.normalized : Vector3.back;
        pitch = Mathf.Clamp(Mathf.Asin(direction.y) * Mathf.Rad2Deg, minimumPitch, maximumPitch);
        yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        ApplyTransform();
    }

    private void Update()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            HandleTouches(touchscreen);
        }

        HandleMouse();
    }

    private void HandleTouches(Touchscreen touchscreen)
    {
        bool firstPressed = touchscreen.touches[0].press.isPressed;
        bool secondPressed = touchscreen.touches[1].press.isPressed;

        if (firstPressed && secondPressed)
        {
            Vector2 firstPosition = touchscreen.touches[0].position.ReadValue();
            Vector2 secondPosition = touchscreen.touches[1].position.ReadValue();
            Vector2 firstDelta = touchscreen.touches[0].delta.ReadValue();
            Vector2 secondDelta = touchscreen.touches[1].delta.ReadValue();

            float previousSeparation = Vector2.Distance(firstPosition - firstDelta, secondPosition - secondDelta);
            float currentSeparation = Vector2.Distance(firstPosition, secondPosition);
            distance = Mathf.Clamp(
                distance - (currentSeparation - previousSeparation) * pinchSensitivity,
                minimumDistance,
                maximumDistance);
        }
        else if (firstPressed)
        {
            RotateBy(touchscreen.touches[0].delta.ReadValue());
        }
    }

    private void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            mouseDragging = true;
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            mouseDragging = false;
        }

        if (mouseDragging)
        {
            RotateBy(mouse.delta.ReadValue());
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance = Mathf.Clamp(distance - scroll * 0.01f, minimumDistance, maximumDistance);
        }
    }

    private void RotateBy(Vector2 screenDelta)
    {
        yaw += screenDelta.x * dragSensitivity;
        pitch = Mathf.Clamp(pitch - screenDelta.y * dragSensitivity, minimumPitch, maximumPitch);
    }

    private void LateUpdate()
    {
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        float yawRadians = yaw * Mathf.Deg2Rad;
        float pitchRadians = Mathf.Clamp(pitch, minimumPitch, maximumPitch) * Mathf.Deg2Rad;
        float horizontalDistance = Mathf.Cos(pitchRadians) * distance;

        // Build the orbit position explicitly so its vertical component is always
        // positive. This prevents the camera from ever moving below the ground.
        Vector3 orbitOffset = new(
            Mathf.Sin(yawRadians) * horizontalDistance,
            Mathf.Sin(pitchRadians) * distance,
            Mathf.Cos(yawRadians) * horizontalDistance);

        transform.position = focusPoint + orbitOffset;
        transform.LookAt(focusPoint, Vector3.up);
    }
}
