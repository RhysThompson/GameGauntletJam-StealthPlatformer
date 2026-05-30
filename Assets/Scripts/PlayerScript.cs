using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerScript : MonoBehaviour
{
    [Header("References")]
    public InputActionAsset ActionAsset;
    InputAction MoveAction;
    InputAction JumpAction;
    InputAction DiveAction;

    public GameObject PlayerSpriteObj;
    Material PlayerSpriteMat;

    // Run Animation variables
    static float[] RunAnimOffsetList = { 0f, 0.25f };
    int NumberOfRunFrames = 2;
    int CurrentRunFrame = 0;
    float TimeSinceRunFrameChanged = 0;
    float RunAnimFrameRate = 0.5f;
    
    float CurrentDirectionOffset = 0.75f; // default is away

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float airControlPercentage = 0.001f;
    public float diveControlPercentage = 0.1f;
    public float AirFrictionPercentage = 0.99f;
    public float GroundFrictionPercentage = 0.9f;
    public float maxGroundSpeed = 8f;
    public float maxAirSpeed = 5f;

    [Header("Jumping")]
    public float jumpHeight = 2f;
    public float gravity = -20f;

    [Header("Dive")]
    public float diveSpeed = 10f;
    public float diveDuration = 0.4f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 lastMovementDirection = Vector3.forward;
    private bool isDiving = false;
    private bool canDive = false;
    private float diveZeroGravTimer = 0f;


    void OnEnable()
    {
        ActionAsset.FindActionMap("Player").Enable();
    }

    void OnDisable()
    {
        ActionAsset.FindActionMap("Player").Disable();
    }

    void Awake()
    {
        MoveAction = InputSystem.actions.FindAction("Move");
        JumpAction = InputSystem.actions.FindAction("Jump");
        DiveAction = InputSystem.actions.FindAction("Dive");
        PlayerSpriteMat = PlayerSpriteObj.GetComponent<Renderer>().material;
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        GetVelocity();

        HandleMovement();
        HandleGravity();
        HandleJump();
        HandleDive();
        HandleSprite();

        SetVelocity();
    }

    void GetVelocity()
    {
        velocity.x = controller.velocity.x;
        velocity.z = controller.velocity.z;
    }
    void SetVelocity()
    {
        controller.Move(velocity * Time.deltaTime);
    }

    void OrientToCameraAngle()
    {
        // Rotates the player to be facing in the same direction as the camera
        this.transform.LookAt(Camera.main.transform, Vector3.up);
        Vector3 rot = this.transform.rotation.eulerAngles;
        rot.x = 0;
        rot.y += 180;
        this.transform.rotation = Quaternion.Euler(rot);
    }
    void HandleMovement()
    {
        // Apply friction
        velocity.x *= controller.isGrounded ? GroundFrictionPercentage : AirFrictionPercentage;
        velocity.z *= controller.isGrounded ? GroundFrictionPercentage : AirFrictionPercentage;

        if (velocity.x < 0.01f && velocity.x > -0.01f) velocity.x = 0;
        if (velocity.z < 0.01f && velocity.z > -0.01f) velocity.z = 0;

        Vector2 moveInput = MoveAction.ReadValue<Vector2>();
        Vector3 input = new Vector3(moveInput.x, 0, moveInput.y);

        if (input.magnitude > 0)
        {
            OrientToCameraAngle();

            float control = controller.isGrounded ? 1f : (isDiving ? diveControlPercentage : airControlPercentage);
            Vector3 move = transform.TransformDirection(input) * moveSpeed * control;
            
            if(controller.isGrounded)
            {
                velocity.x += move.x * Time.deltaTime;
                velocity.z += move.z * Time.deltaTime;

                // cap move speed
                Vector3 vel = new Vector3(velocity.x, 0, velocity.z);

                if (vel.magnitude > maxGroundSpeed)
                {
                    vel = vel.normalized * maxGroundSpeed;
                }

                velocity = new Vector3(vel.x, velocity.y, vel.z);
            }
            else
            {
                velocity.x += move.x * Time.deltaTime;
                velocity.z += move.z * Time.deltaTime;

                // cap move speed
                Vector3 vel = new Vector3(velocity.x, 0, velocity.z);

                if (vel.magnitude > maxAirSpeed && !isDiving)
                {
                    vel = vel.normalized * maxAirSpeed;
                }

                velocity = new Vector3(vel.x, velocity.y, vel.z);
            }
        }

        if (velocity.x + velocity.z != 0)
            lastMovementDirection = velocity.normalized;
    }

    void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f; // Keeps player grounded

        if (diveZeroGravTimer <= 0)
            velocity.y += gravity * Time.deltaTime;
    }

    void HandleJump()
    {
        if (controller.isGrounded && JumpAction.WasPressedThisFrame() && !isDiving)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void HandleDive()
    {
        // Reset dive when on the ground
        if (controller.isGrounded) canDive = true;
        
        // Check for and do the dive
        if (DiveAction.WasPressedThisFrame() && !controller.isGrounded && !isDiving && canDive)
        {
            StartDive();
        }

        // Count down the timer for the dive
        if (isDiving)
        {
            diveZeroGravTimer -= Time.deltaTime;

            if (controller.isGrounded)
                EndDive();
        }
    }

    void HandleSprite()
    {
        // Increment run animation
        if (MoveAction.IsPressed() && controller.isGrounded)
        {
            if (TimeSinceRunFrameChanged > 0)
                TimeSinceRunFrameChanged -= Time.fixedDeltaTime;
            else
            {
                TimeSinceRunFrameChanged = RunAnimFrameRate;

                CurrentRunFrame++;
                if (CurrentRunFrame == NumberOfRunFrames) CurrentRunFrame = 0;
            }
        }

        // Work out the direction the player is facing relative to the camera
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        float forwardDot = Vector3.Dot(lastMovementDirection.normalized, camForward);
        float rightDot = Vector3.Dot(lastMovementDirection.normalized, camRight);

        if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
        {
            if (forwardDot > 0) 
                CurrentDirectionOffset = 0.75f; // away
            else 
                CurrentDirectionOffset = 0.25f; //towards
        }
        else
        {
            if (rightDot > 0) 
                CurrentDirectionOffset = 0.5f; // right
            else 
                CurrentDirectionOffset = 0f; // left
        }

        float frameOffset = 0.1f;

        if(controller.isGrounded)
        {
            if (MoveAction.IsPressed())
                frameOffset = RunAnimOffsetList[CurrentRunFrame];
            else
                frameOffset = 0.5f;
        }
        else
        {
            if (isDiving)
                frameOffset = 0.75f;
            else
                frameOffset = 0.5f;
        }
        PlayerSpriteMat.SetTextureOffset("_MainTex", new Vector2(frameOffset, CurrentDirectionOffset));
    }

    void StartDive()
    {
        isDiving = true;
        canDive = false;
        diveZeroGravTimer = diveDuration;

        Vector3 dir = velocity;
        dir.y = 0;
        dir.Normalize();

        // if the player isn't moving, dive away from the camera instead
        if (dir.x + dir.z == 0)
        {
            OrientToCameraAngle();
            dir = this.transform.forward;
        }
        velocity = dir * diveSpeed;
    }

    void EndDive()
    {
        isDiving = false;
        diveZeroGravTimer = 0;
    }
}
