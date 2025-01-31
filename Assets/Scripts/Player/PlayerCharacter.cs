using System.Threading.Tasks;
using KinematicCharacterController;
using UnityEngine;

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch, Slide
}

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;
}

public struct CharacterState
{
    public bool Grounded;
    public Stance Stance;
    public Vector3 Velocity;
    public Vector3 Acceleration;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    
    private KinematicCharacterMotor motor;
    private Player player;
    [SerializeField] private Transform root;
    [Space]
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTarget;
    [Space]
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 20f;
    [Space]
    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float coyoteTime = 0.2f;
    [Range(0, 1)]
    [SerializeField] private float JumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float slideStartSpeed = 25f;
    [SerializeField] private float slideEndSpeed = 15f;
    [SerializeField] private float slideFriction = 0.8f;
    [SerializeField] private float slideSteerAcceleration = 5f;
    [SerializeField] private float slideGravity = -90f;
    [Space]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0, 1)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0, 1)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;
    
    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedCrouch;
    private bool _requestedCrouchInAir;

    private float _timeSinceUngrounded;
    private float _timeSinceJumpRequest;
    private bool _ungroundedDueToJump;

    private Collider[] _uncrouchOverlapResults;

    public void Initialize()
    {
        motor = GetComponent<KinematicCharacterMotor>();
        player = GetComponentInParent<Player>();
        motor.CharacterController = this;
        motor.enabled = true;
        _state.Stance = Stance.Stand;
        _lastState = _state;
        _uncrouchOverlapResults = new Collider[8];
    }

    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;
        _requestedMovement = new Vector3(input.Move.x, 0, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);

        _requestedMovement = input.Rotation * _requestedMovement;

        bool wasRequestingJump = _requestedJump;
        _requestedJump = _requestedJump || input.Jump;
        if (_requestedJump && !wasRequestingJump) 
        {
            _timeSinceJumpRequest = 0f;
        }

        _requestedSustainedJump = input.JumpSustain;

        bool wasRequestingCrouch = _requestedCrouch;
        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch
        };
        if (_requestedCrouch && !wasRequestingCrouch) 
        {
            _requestedCrouchInAir = !_state.Grounded;
        }
        else if (!_requestedCrouch && wasRequestingCrouch)
        {
            _requestedCrouchInAir = false;
        }
    }

    public void UpdateBody(float deltaTime)
    {
        float currentHeight = motor.Capsule.height;
        float normalizedHeight = currentHeight / standHeight;
        float cameraTargetHeight = currentHeight * (_state.Stance is Stance.Stand ? standCameraTargetHeight : crouchCameraTargetHeight);
        Vector3 rootTargetScale = new(1f, normalizedHeight, 1f);

        cameraTarget.localPosition = Vector3.Lerp
        (
            cameraTarget.localPosition,
            new Vector3(0, cameraTargetHeight, 0),
            1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
        root.localScale = Vector3.Lerp
        (
            root.localScale,
            rootTargetScale,
            1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        Vector3 forward = Vector3.ProjectOnPlane
        (
            _requestedRotation * Vector3.forward,
            motor.CharacterUp
        );

        if (forward != Vector3.zero) {
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _state.Acceleration = Vector3.zero;
        // If on the ground
        if (motor.GroundingStatus.IsStableOnGround) 
        {
            _timeSinceUngrounded = 0f;
            _ungroundedDueToJump = false;

            Vector3 groundedMovement = motor.GetDirectionTangentToSurface(
                direction: _requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            ) * _requestedMovement.magnitude;

            // Slide
            {
                bool moving = groundedMovement.sqrMagnitude > 0f;
                bool crouching = _state.Stance is Stance.Crouch;
                bool wasStanding = _lastState.Stance is Stance.Stand;
                bool wasInAir = !_lastState.Grounded;

                if (moving && crouching && (wasStanding || wasInAir)) 
                {
                    _state.Stance = Stance.Slide;

                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane(
                            vector: _lastState.Velocity,
                            planeNormal: motor.GroundingStatus.GroundNormal
                        );
                    }

                    float effectiveSlideStartSpeed = slideStartSpeed;
                    if (!_lastState.Grounded && !_requestedCrouchInAir)
                    {
                        effectiveSlideStartSpeed = 0f;
                        _requestedCrouchInAir = false;
                    }

                    float slideSpeed = Mathf.Max(effectiveSlideStartSpeed, currentVelocity.magnitude);
                    currentVelocity = motor.GetDirectionTangentToSurface(
                        direction: currentVelocity,
                        surfaceNormal: motor.GroundingStatus.GroundNormal
                    ).normalized * slideSpeed;
                }
            }

            // Movement
            if (_state.Stance is Stance.Stand or Stance.Crouch)
            {
                float speed = _state.Stance is Stance.Stand ? walkSpeed : crouchSpeed;

                float response = _state.Stance is Stance.Stand ? walkResponse : crouchResponse;

                Vector3 targetVelocity = groundedMovement * speed;
                Vector3 moveVelocity = Vector3.Lerp
                (
                    currentVelocity,
                    targetVelocity,
                    1f - Mathf.Exp(-response * deltaTime)
                );

                _state.Acceleration = (moveVelocity - currentVelocity) / deltaTime;
                currentVelocity = moveVelocity;
            }
            // Continue sliding
            else
            {
                // Friction
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);

                // Slope
                {
                    Vector3 force = Vector3.ProjectOnPlane(
                        vector: -motor.CharacterUp,
                        planeNormal: motor.GroundingStatus.GroundNormal
                    ) * slideGravity * deltaTime;

                    currentVelocity -= force;
                }

                // Steer
                {
                    float currentSpeed = currentVelocity.magnitude;
                    Vector3 targetVelocity = groundedMovement * currentSpeed;
                    Vector3 steerVelocity = currentVelocity;
                    Vector3 steerForce = deltaTime * slideSteerAcceleration * (targetVelocity - steerVelocity);

                    steerVelocity += steerForce;
                    steerVelocity = Vector3.ClampMagnitude(steerVelocity, currentSpeed);

                    _state.Acceleration = (steerVelocity - currentVelocity) / deltaTime;
                    currentVelocity = steerVelocity;
                }

                // Stop
                if (currentVelocity.magnitude < slideEndSpeed) {
                    _state.Stance = Stance.Crouch;
                }
            }
        }
        // Else not grounded
        else
        {
            _timeSinceUngrounded += deltaTime;

            // Air movement
            if (_requestedMovement.sqrMagnitude > 0f)
            {
                Vector3 planarMovement = Vector3.ProjectOnPlane(
                    _requestedMovement, 
                    motor.CharacterUp
                ).normalized * _requestedMovement.magnitude;

                Vector3 currentPlanarVelocity = Vector3.ProjectOnPlane(
                    currentVelocity, 
                    motor.CharacterUp
                );

                Vector3 movementForce = planarMovement * airAcceleration * deltaTime;

                if (currentPlanarVelocity.magnitude < airSpeed)
                {
                    Vector3 targetPlanarVelocity = currentPlanarVelocity + movementForce;

                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);

                    movementForce = targetPlanarVelocity - currentPlanarVelocity;
                }
                else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f)
                {
                    Vector3 constrainedMovementForce = Vector3.ProjectOnPlane(
                        movementForce, 
                        currentPlanarVelocity.normalized
                    );

                    movementForce = constrainedMovementForce;
                }

                // Prevent air-climbing steep slopes
                if (motor.GroundingStatus.FoundAnyGround)
                {
                    if (Vector3.Dot(currentVelocity, currentVelocity + movementForce) > 0f)
                    {
                        Vector3 obstructionNormal = Vector3.Cross
                        (
                            motor.CharacterUp,
                            Vector3.Cross(
                                motor.CharacterUp,
                                motor.GroundingStatus.GroundNormal
                            )
                        ).normalized;

                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                    }
                }

                currentVelocity += movementForce;
            }

            // Not grounded, apply gravity
            float effectiveGravity = gravity;
            float verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);

            if (_requestedSustainedJump && verticalSpeed > 0f) {
                effectiveGravity *= JumpSustainGravity;
            }

            currentVelocity += deltaTime * effectiveGravity * motor.CharacterUp;
        }

        if (_requestedJump) {
            bool grounded = motor.GroundingStatus.IsStableOnGround;
            bool canCoyoteJump = _timeSinceUngrounded <= coyoteTime && !_ungroundedDueToJump;

            if (grounded || canCoyoteJump)
            {
                _requestedJump = false;
                _requestedCrouch = false;
                _requestedCrouchInAir = false;

                motor.ForceUnground(time: 0f);
                _ungroundedDueToJump = true;

                float currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                float targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);

                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
            }
            else
            {
                _timeSinceJumpRequest += deltaTime;

                bool canJumpLater = _timeSinceJumpRequest <= coyoteTime;
                _requestedJump = canJumpLater;
            }
        }
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _tempState = _state;
        // Crouch
        if (_requestedCrouch && _state.Stance is Stance.Stand) {
            
            _state.Stance = Stance.Crouch;
            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
            );
        }
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        // Uncrouch
        if (!_requestedCrouch && _state.Stance is not Stance.Stand) {
            
            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
            );

            Vector3 pos = motor.TransientPosition;
            Quaternion rot = motor.TransientRotation;
            LayerMask mask = motor.CollidableLayers;

            if (motor.CharacterOverlap(pos, rot, _uncrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0) {
                _requestedCrouch = true;
                motor.SetCapsuleDimensions(
                    radius: motor.Capsule.radius,
                    height: crouchHeight,
                    yOffset: crouchHeight * 0.5f
                );
            } else {
                _state.Stance = Stance.Stand;
            }
        }

        _state.Grounded = motor.GroundingStatus.IsStableOnGround;
        _state.Velocity = motor.Velocity;
        _lastState = _tempState;
    }


    public void PostGroundingUpdate(float deltaTime)
    {
        if (!motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide) {
            _state.Stance = Stance.Crouch;
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return coll != null && coll.enabled;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {}

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {}

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {}

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {}

    public Transform GetCameraTarget() => cameraTarget;
    public CharacterState GetState() => _state;
    public CharacterState GetLastState() => _lastState;

    private async Task<bool> WaitForPlayerReference()
    {
        while (player == null)
        {
            await Task.Yield();
        }

        return true;
    }
    public async void SetPosition(Vector3 position, bool killVelocity = true)
    {
        await WaitForPlayerReference();
        await player.WaitForPlayerReady();

        motor.SetPosition(position);
        if (killVelocity)
        {
            motor.BaseVelocity = Vector3.zero;
        }
    }

}
