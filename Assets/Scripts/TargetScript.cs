using UnityEngine;

public class TargetScript : MonoBehaviour
{
	public Transform origin;

    // Do a raycast forward from origin and set position of hit to this object, if nothing is hit set it to far forward

    public void Update() {
        
        if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, Mathf.Infinity))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = origin.position + origin.forward * 100f;
        }
    }
}
