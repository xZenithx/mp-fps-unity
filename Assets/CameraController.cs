using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 100f;
    private Vector2 mouseInput;
    private float pitch;
    public Transform TargetObject;

    public void OnMouseMove(InputAction.CallbackContext ctx) => mouseInput = ctx.ReadValue<Vector2>();

    public void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Update()
    {
        transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity * Time.deltaTime);

        pitch -= mouseInput.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, 0f);

        // Raycast forward until hit and set position of hit to TargetObject
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Mathf.Infinity))
        {
            TargetObject.position = hit.point;
        }
    }
	
}
