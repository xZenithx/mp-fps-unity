using UnityEngine;

public class SetPosition : MonoBehaviour
{
	public Transform[] ToMove;
    public Transform Place;

    void Update()
    {
        foreach (Transform t in ToMove)
        {
            t.position = Place.position;
        }
    }
}
