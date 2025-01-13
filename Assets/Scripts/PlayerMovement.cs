// using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	private PlayerReferences playerReferences;
    private Vector2 movementInput;

    public void OnMove(InputAction.CallbackContext ctx) => movementInput = ctx.ReadValue<Vector2>();

    // public override void OnNetworkSpawn()
    public void Start()
    {

        playerReferences = GetComponent<PlayerReferences>();
        

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GetComponent<PlayerInput>().enabled = true;
    }

    public void Update()
    {

        float moveSpeed = 5f;
        float gravity = -9.81f;
        Vector3 velocity = Vector3.zero;

        CharacterController controller = playerReferences.characterController;

        float moveX = movementInput.x;
        float moveZ = movementInput.y;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        _ = controller.Move(move * moveSpeed * Time.deltaTime);

        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = 0f;
        }

        controller.Move(velocity * Time.deltaTime);

        Quaternion rotation = Quaternion.Euler(0f, playerReferences.playerCamera.transform.eulerAngles.y, 0f);
        transform.rotation = rotation;
    }


}