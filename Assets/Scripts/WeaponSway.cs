using Unity.Netcode;
using UnityEngine;

public class WeaponSway : NetworkBehaviour
{
	[SerializeField] private float smooth;
    [SerializeField] private float multiplier;

    public void UpdateSway(Vector2 lookInput, float deltaTime)
    {
        float x = -lookInput.x * multiplier;
        float y = lookInput.y * multiplier * 1.5f;


        transform.localRotation = Quaternion.Slerp
        (
            a: transform.localRotation,
            b: Quaternion.Euler(y, x, 0),
            t: 1f - Mathf.Exp(-smooth * deltaTime)
        );
    }
}
