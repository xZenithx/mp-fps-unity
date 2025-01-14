using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [Space]
    [SerializeField] private Volume volume;
    [SerializeField] private StanceVignette stanceVignette;

    private Vector2 LookInput;
    public void OnLook(InputAction.CallbackContext ctx) => LookInput = ctx.ReadValue<Vector2>();

    private Vector2 MoveInput;
    public void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {}

    public void OnInteract(InputAction.CallbackContext ctx)
    {}

    private bool JumpInput;
    private bool JumpHeldInput;
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started) JumpInput = true;
        JumpHeldInput = ctx.performed;
    }

    private bool InputCrouch;
    public void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (ctx.started) InputCrouch = true;
    }

    public void OnSprint(InputAction.CallbackContext ctx)
    {}

    public void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
        cameraSpring.Initialize();
        cameraLean.Initialize();
        stanceVignette.Initialize(volume.profile);
    }

    public void Update()
    {
        float deltaTime = Time.deltaTime;

        CameraInput cameraInput = new()
        {
            Look = LookInput
        };
        playerCamera.UpdateRotation(cameraInput);

        // Get character input and update it
        CharacterInput characterInput = new()
        {
            Rotation = playerCamera.transform.rotation,
            Move = MoveInput,
            Jump = JumpInput,
            JumpSustain = JumpHeldInput,
            Crouch = InputCrouch ? CrouchInput.Toggle : CrouchInput.None
        };
        playerCharacter.UpdateInput(characterInput);
        playerCharacter.UpdateBody(deltaTime);
    }

    public void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        Transform cameraTarget = playerCharacter.GetCameraTarget();
        CharacterState state = playerCharacter.GetState();

        playerCamera.UpdatePosition(cameraTarget);
        cameraSpring.UpdateSpring(deltaTime, cameraTarget.up);
        cameraLean.UpdateLean
        (
            deltaTime, 
            state.Stance is Stance.Slide, 
            state.Acceleration, 
            cameraTarget.up
        );
        stanceVignette.UpdateVignette(deltaTime, state.Stance);

        JumpInput = false;
        InputCrouch = false;
    }
}
