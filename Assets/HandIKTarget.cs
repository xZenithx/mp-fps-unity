using UnityEngine;

public class HandIKTarget : MonoBehaviour
{
	public string targetTag;
    private GameObject _target;
    public GameObject _targetObject;
    
    public void Update()
    {
        if (_target != null && _target.transform != null)
        {
            // Lerp the position of the target object to the target object
            _targetObject.transform.position = Vector3.Lerp(_targetObject.transform.position, _target.transform.position, 0.1f);
        }
    }

    public void FixedUpdate()
    {
        if (_target == null || _target.transform == null)
        {
            _target = GameObject.FindGameObjectWithTag(targetTag);
        }
    }
}
