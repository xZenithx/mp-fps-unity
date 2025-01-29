using System.Threading.Tasks;
using KinematicCharacterController;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Player : NetworkBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerHealth playerHealth;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [Space]
    [SerializeField] private Volume volume;
    [SerializeField] private StanceVignette stanceVignette;
    [Space]
    [SerializeField] private WeaponSway weaponSway;
    private Weapon weapon;

    /*
        * Input actions
    */
    private Vector2 LookInput;
    public void OnLook(InputAction.CallbackContext ctx) => LookInput = ctx.ReadValue<Vector2>();

    private Vector2 MoveInput;
    public void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    private bool AttackInput;
    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.started) AttackInput = true;
        if (ctx.canceled) AttackInput = false;
    }

    private bool ReloadInput;
    public void OnReload(InputAction.CallbackContext ctx)
    {
        if (ctx.started) ReloadInput = true;
    }

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

    public void OnPause(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            PauseManager.Instance.TogglePause();
        }
    }

    /*
        * Unity methods
    */
    // public void Start()
    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
        {
            MakeRemote();

            return;
        }

        GetComponent<PlayerInput>().enabled = true;

        Cursor.lockState = CursorLockMode.Locked;

        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
        cameraSpring.Initialize();
        cameraLean.Initialize();
        stanceVignette.Initialize(volume.profile);

        SubscribeToEvents();
    }
    private void SubscribeToEvents()
    {
        
    }

    public void Update()
    {
        if (!IsLocalPlayer)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        bool isPaused = PauseManager.Instance.IsPaused();

        Vector2 lookInput = isPaused ? Vector2.zero : LookInput;

        CameraInput cameraInput = new()
        {
            Look = lookInput,
            weapon = weapon
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

        weaponSway.UpdateSway(lookInput, deltaTime);

        if (isPaused)
        {
            return;
        }

        if (AttackInput)
        {
            RequestAttack();
        }

        if (ReloadInput)
        {
            RequestReload();
        }
    }

    public void LateUpdate()
    {
        if (!IsLocalPlayer)
        {
            return;
        }

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
        ReloadInput = false;
    }

    private void MakeRemote()
    {
        TagChildrenRecursive(gameObject);

        GetComponentInChildren<Camera>().enabled = false;
        GetComponentInChildren<AudioListener>().enabled = false;
        GetComponentInChildren<KinematicCharacterMotor>().enabled = false;
        GetComponentInChildren<PlayerCharacter>().enabled = false;
        GetComponentInChildren<Weapon>().enabled = false;

        GameObject.FindGameObjectWithTag("MainMenuCamera").SetActive(false);
    }

    private void TagChildrenRecursive(GameObject obj)
    {
        obj.tag = "Remote Player";
        for(int i = 0; i < obj.transform.childCount; i++)
        {
            Transform Go = obj.transform.GetChild(i);

            TagChildrenRecursive(Go.gameObject);
        }
    }

    private void RequestAttack()
    {
        if (weapon == null)
        {
            weapon = GetComponentInChildren<Weapon>();
        }
        
        if (weapon == null)
        {
            return;
        }

        weapon.ShootServerRpc();
    }

    private void RequestReload()
    {
        if (weapon == null)
        {
            weapon = GetComponentInChildren<Weapon>();
        }
        
        if (weapon == null)
        {
            return;
        }

        weapon.StartReloadServerRpc();
    }

    public PlayerHealth GetPlayerHealth() => playerHealth;
}
