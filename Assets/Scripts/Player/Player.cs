using System;
using System.Threading.Tasks;
using KinematicCharacterController;
using Unity.Collections;
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
    [SerializeField] private PlayerWeapon playerWeapon;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [Space]
    [SerializeField] private Volume volume;
    [SerializeField] private StanceVignette stanceVignette;
    [Space]
    [SerializeField] private WeaponSway weaponSway;
    // private Weapon weapon;
    private bool isPlayerReady = false;
    private Camera playerCameraComponent;
    private PlayerInput playerInput;

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
    {
        if (ctx.started) weaponSwitchName = WeaponManager.Instance.GetRandomWeapon().weaponId;
    }

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
    
    public UnityEvent OnRespawnRequested = new();

    private string weaponSwitchName = null;

    private void Awake()
    {
        if (IsOwner && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.InitializeLocalPlayer(this);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer || IsHost)
        {

        }

        if (!IsLocalPlayer)
        {
            MakeRemote();

            return;
        }

        MakeLocalPlayer();
    }

    public async Task<bool> WaitForPlayerReady()
    {
        while (!isPlayerReady)
        {
            await Task.Yield();
        }

        return true;
    }

    private void MakeLocalPlayer()
    {
        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
        playerWeapon.Initialize();
        playerHealth.Initialize();
        cameraSpring.Initialize();
        cameraLean.Initialize();
        stanceVignette.Initialize(volume.profile);
        playerCameraComponent = playerCamera.GetComponentInChildren<Camera>();
        playerInput = GetComponent<PlayerInput>();

        gameObject.tag = "LocalPlayer";

        GetComponentInChildren<Camera>().enabled = false;
        GetComponentInChildren<AudioListener>().enabled = false;
        
        playerInput.enabled = true;
        playerInput.DeactivateInput();

        playerHealth.OnDeath.AddListener(OnDeath);

        isPlayerReady = true;
    }

    private void MakeRemote()
    {
        TagChildrenRecursive(gameObject);

        GetComponentInChildren<Camera>().enabled = false;
        GetComponentInChildren<AudioListener>().enabled = false;
        GetComponentInChildren<KinematicCharacterMotor>().enabled = false;
        GetComponentInChildren<PlayerCharacter>().enabled = false;
        GetComponentInChildren<PlayerWeapon>().enabled = false;
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

    public void OnDeath()
    {
        Debug.Log("Player.OnDeath: Deactivating input");
        playerInput.DeactivateInput();
    }

    public void OnSpawn()
    {
        Debug.Log("Player.OnSpawn: Activating input");
        playerInput.ActivateInput();
    }

    public void Update()
    {
        if (!isPlayerReady || !playerHealth.IsAlive())
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        bool isPaused = PauseManager.Instance.IsPaused();

        Vector2 lookInput = isPaused ? Vector2.zero : LookInput;

        CameraInput cameraInput = new()
        {
            Look = lookInput,
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

        // Get weapon input and update it
        PlayerWeaponInput weaponInput = new()
        {
            Fire = AttackInput,
            Reload = ReloadInput,
            SwitchWeapon = weaponSwitchName ?? null,
            Camera = playerCameraComponent
        };
        playerWeapon.UpdateWeapon(weaponInput);
    }

    public void LateUpdate()
    {
        if (!isPlayerReady)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        Transform cameraTarget = playerCharacter.GetCameraTarget();
        CharacterState state = playerCharacter.GetState();

        playerCamera.UpdatePosition(cameraTarget);
        playerHealth.UpdateHealthSlider();
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

        weaponSwitchName = null;
    }

    public PlayerHealth GetPlayerHealth() => playerHealth;
    public void SetPosition(Vector3 position) => playerCharacter.SetPosition(position);
}
