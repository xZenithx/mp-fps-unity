using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CameraTrackTarget : MonoBehaviour
{
	public void Start()
    {
        GameObject.FindGameObjectWithTag("CinemachineCamera").GetComponent<CinemachineCamera>().Follow = transform;
    }
}