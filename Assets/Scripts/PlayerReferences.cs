using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerReferences : MonoBehaviour
{
	[HideInInspector] public CharacterController characterController;
    [HideInInspector] public PlayerMovement playerMovement;
    [HideInInspector] public PlayerInput playerInput;
    [HideInInspector] public Transform _transform;
    [HideInInspector] public GameObject playerCamera;

    void Start()
    {
        playerCamera = GameObject.FindGameObjectWithTag("CinemachineCamera");
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>();
        _transform = transform;
    }
}
