using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 0.1f;
    [SerializeField] private float cameraXLimit = 75f;
    private Vector3 _eulerAngles;

	public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.rotation = target.rotation;

        transform.eulerAngles = _eulerAngles = target.eulerAngles;
    }

    public void UpdateRotation(CameraInput input)
    {
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x, 0) * _sensitivity;
        _eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -cameraXLimit, cameraXLimit);
        transform.eulerAngles = _eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
}
