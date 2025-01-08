using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CameraTrackTarget : NetworkBehaviour
{
	public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        GameObject.FindGameObjectWithTag("CinemachineCamera").GetComponent<CinemachineCamera>().Follow = transform;
    }
}