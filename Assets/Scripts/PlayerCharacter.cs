using KinematicCharacterController;
using UnityEngine;

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch
}

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    
    private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [Space]
    private Transform cameraTarget;
    [Space]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]
    [SerializeField] private float jumpSpeed = 20f;
    [Range(0, 1)]
    [SerializeField] private float JumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0, 1)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0, 1)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    private Stance _stance;
    
    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedCrouch;

    private Collider[] _uncrouchOverlapResults;

    public void Initialize()
    {
        motor = GetComponent<KinematicCharacterMotor>();
        motor.CharacterController = this;
        cameraTarget = GameObject.FindGameObjectWithTag("CameraTarget").transform;
        _stance = Stance.Stand;
        _uncrouchOverlapResults = new Collider[8];
    }

    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;
        _requestedMovement = new Vector3(input.Move.x, 0, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);

        _requestedMovement = input.Rotation * _requestedMovement;

        _requestedJump = _requestedJump || input.Jump;
        _requestedSustainedJump = input.JumpSustain;

        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch
        };
    }

    public void UpdateBody(float deltaTime)
    {
        float currentHeight = motor.Capsule.height;
        float normalizedHeight = currentHeight / standHeight;
        float cameraTargetHeight = currentHeight * (_stance is Stance.Stand ? standCameraTargetHeight : crouchCameraTargetHeight);
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

        if (motor.GroundingStatus.IsStableOnGround) {
            Vector3 groundedMovement = motor.GetDirectionTangentToSurface(
                direction: _requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            ) * _requestedMovement.magnitude;

            float speed = _stance is Stance.Stand ? walkSpeed : crouchSpeed;

            float response = _stance is Stance.Stand ? walkResponse : crouchResponse;

            Vector3 targetVelocity = groundedMovement * speed;
            currentVelocity = Vector3.Lerp
            (
                currentVelocity,
                targetVelocity,
                1f - Mathf.Exp(-response * deltaTime)
            );
        } else {
            // Not grounded, apply gravity
            float effectiveGravity = gravity;
            float verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);

            if (_requestedSustainedJump && verticalSpeed > 0f) {
                effectiveGravity *= JumpSustainGravity;
            }

            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;
        }

        if (_requestedJump && motor.GroundingStatus.IsStableOnGround) {
            motor.ForceUnground(time: 0f);

            float currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            float targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);

            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
        }
        _requestedJump = false;
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Crouch
        if (_requestedCrouch && _stance is Stance.Stand) {
            Debug.Log("Crouch");
            _stance = Stance.Crouch;
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
        if (!_requestedCrouch && _stance is not Stance.Stand) {
            Debug.Log("Uncrouch");
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
                _stance = Stance.Stand;
            }
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

    public void PostGroundingUpdate(float deltaTime)
    {}

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {}

    public Transform GetCameraTarget() => cameraTarget;
}
