using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

enum SPRITE_FRAMES
{
    RUN1,
    RUN2,
    RUN3,
    RUN4,
    RUN5,
    RUN6,
    RUN_END = RUN6,
    IDLE1,
    IDLE_END = IDLE1,
    INAIR = IDLE1,
    INAIR_END = INAIR,
    DIVE1 = IDLE1,
    DIVE_END = DIVE1,
    GLIDING1 = IDLE1,
    GLIDING_END = GLIDING1,
}
enum SPRITE_DIRECTION
{
    AWAY,
    TOWARDS,
    RIGHT,
    LEFT,
    NUM_DIRECTIONS
}

enum PLAYER_STATES
{
    IDLE,
    RUNNING,
    IN_AIR,
    DIVING,
    GLIDING
}
public class PlayerScript : MonoBehaviour
{
    [Header("References")]
    public InputActionAsset ActionAsset;
    InputAction MoveAction;
    InputAction JumpAction;
    InputAction DiveAction;

    public GameObject PlayerSpriteObj;
    Material PlayerSpriteMat;

    public Texture2D[] SpriteSheets;

    // Run Animation variables
    public int SpriteSheetWidth = 7; // how many sprite columns
    public int SpriteSheetHeight = 1; // how many sprite rows
    float TimeSinceFrameChanged = 0;
    public float AnimFrameRate = 0.07f;
    
    SPRITE_DIRECTION CurrentlyDisplayedDirection;

    SPRITE_FRAMES CurrentFrame = SPRITE_FRAMES.IDLE1;
    SPRITE_FRAMES CurrentAnimStartFrame = SPRITE_FRAMES.IDLE1;
    SPRITE_FRAMES CurrentAnimEndFrame = SPRITE_FRAMES.IDLE1;

    PLAYER_STATES CurrentState = PLAYER_STATES.IDLE;

    [Header("Movement")]
    public float MoveSpeed = 8f;
    public float AirControlPercentage = 0.2f;
    public float DiveControlPercentage = 0.05f;
    public float GlidingControlPercentage = 0.6f;
    public float AirFrictionPercentage = 0.99f;
    public float GroundFrictionPercentage = 0.9f;
    public float AirCurrentGravityFrictionPercentage = 0.8f;
    public float MaxGroundSpeed = 8f;
    public float MaxAirSpeed = 5f;
    public float JumpHeight = 2f;
    public float Gravity = -20f;
    public float MaxFallSpeed = -20f;
    public float MaxGlidingFallSpeed = -2f;
    public float DiveSpeed = 10f;
    public float DiveDuration = 0.4f;

    public float CoyoteTime = 0.15f;
    private float TimeSinceGrounded = 0f;

    private CharacterController Controller;
    private Vector3 Velocity;
    private Vector3 WindGlideForce = Vector3.zero;
    private Vector3 WindForce = Vector3.zero;
    private Vector3 LastMovementDirection = Vector3.forward;
    private bool CanDive = false;
    private float DiveZeroGravTimer = 0f;
    private Vector3 LastCheckpoint;

    List<WindCurrentScript> ActiveAirCurrents = new List<WindCurrentScript>();

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
        Controller = GetComponent<CharacterController>();
        LastCheckpoint = this.transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "AirCurrent")
        {
            ActiveAirCurrents.Add(other.gameObject.GetComponent<WindCurrentScript>());
        }
        else if(other.tag == "Checkpoint")
        {
            LastCheckpoint = other.transform.position;
        }
        else if (other.tag == "Hazard")
        {
            Die();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "AirCurrent")
        {
            ActiveAirCurrents.Remove(other.gameObject.GetComponent<WindCurrentScript>());
        }
    }

    void Update()
    {
        GetVelocity();

        HandleGrounded();
        HandleMovement();
        HandleGravity();
        HandleExternalForce();
        HandleJump();
        HandleDive();
        HandleGlide();
        HandleSprite();

        SetVelocity();
    }

    void GetVelocity()
    {
        Velocity.x = Controller.velocity.x;
        Velocity.z = Controller.velocity.z;
    }
    void SetVelocity()
    {
        Controller.Move(Velocity * Time.deltaTime);
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

    void HandleGrounded()
    {
        TimeSinceGrounded += Time.deltaTime;

        if(Controller.isGrounded)
            TimeSinceGrounded = 0f;
    }

    void HandleMovement()
    {
        // Apply friction
        Velocity.x *= IsOnGround() ? GroundFrictionPercentage : AirFrictionPercentage;
        Velocity.z *= IsOnGround() ? GroundFrictionPercentage : AirFrictionPercentage;
        if (WindGlideForce + WindGlideForce != Vector3.zero && CurrentState == PLAYER_STATES.GLIDING)
            Velocity.y *= AirCurrentGravityFrictionPercentage;


        if (Velocity.x < 0.01f && Velocity.x > -0.01f) Velocity.x = 0;
        if (Velocity.z < 0.01f && Velocity.z > -0.01f) Velocity.z = 0;

        Vector2 moveInput = MoveAction.ReadValue<Vector2>();
        Vector3 input = new Vector3(moveInput.x, 0, moveInput.y);

        if (input.magnitude > 0)
        {
            OrientToCameraAngle();

            // apply control debuffs for when movement control is reduced
            float control = 1f; 
            switch (CurrentState)
            {
                case PLAYER_STATES.IN_AIR:
                    control = AirControlPercentage;
                    break;
                case PLAYER_STATES.DIVING:
                    control = DiveControlPercentage;
                    break;
                case PLAYER_STATES.GLIDING:
                    control = GlidingControlPercentage;
                    break;
            }
            
            Vector3 move = transform.TransformDirection(input) * MoveSpeed * control;

            if (IsOnGround())
            {
                Velocity.x += move.x * Time.deltaTime;
                Velocity.z += move.z * Time.deltaTime;

                // cap move speed
                Vector3 vel = new Vector3(Velocity.x, 0, Velocity.z);

                if (vel.magnitude > MaxGroundSpeed)
                {
                    vel = vel.normalized * MaxGroundSpeed;
                }

                Velocity = new Vector3(vel.x, Velocity.y, vel.z);

                if (CurrentState != PLAYER_STATES.RUNNING)
                {
                    CurrentState = PLAYER_STATES.RUNNING;
                    SetAnim(SPRITE_FRAMES.RUN1, SPRITE_FRAMES.RUN_END);
                }
            }
            else
            {
                Velocity.x += move.x * Time.deltaTime;
                Velocity.z += move.z * Time.deltaTime;

                // cap move speed
                Vector3 vel = new Vector3(Velocity.x, 0, Velocity.z);

                if (vel.magnitude > MaxAirSpeed && CurrentState != PLAYER_STATES.DIVING)
                {
                    vel = vel.normalized * MaxAirSpeed;
                }

                Velocity = new Vector3(vel.x, Velocity.y, vel.z);
            }
        }
        else // if no input
        {
            if(IsOnGround())
            {
                if (CurrentState != PLAYER_STATES.IDLE)
                {
                    CurrentState = PLAYER_STATES.IDLE;
                    SetAnim(SPRITE_FRAMES.IDLE1, SPRITE_FRAMES.IDLE_END);
                }
            }
        }

        if (Velocity.x + Velocity.z != 0)
            LastMovementDirection = Velocity.normalized;
    }

    void HandleGravity()
    {
        if (DiveZeroGravTimer > 0)
            return;

        if (IsOnGround() && Velocity.y < 0)
            Velocity.y = -0.3f;


        // apply gravity except when gliding in an air current
        if (WindGlideForce + WindGlideForce != Vector3.zero && CurrentState == PLAYER_STATES.GLIDING)
        {
            
        }
        else if(!IsOnGround())
        {
            Velocity.y += Gravity * Time.deltaTime;
        }

        if (CurrentState == PLAYER_STATES.GLIDING)
        {
            if (Velocity.y < MaxGlidingFallSpeed)
                Velocity.y = MaxGlidingFallSpeed;
        }
        else
        {
            if (Velocity.y < MaxFallSpeed)
                Velocity.y = MaxFallSpeed;
        }
    }

    void HandleExternalForce()
    {
        WindGlideForce = Vector3.zero;
        WindForce = Vector3.zero;
        foreach (WindCurrentScript current in ActiveAirCurrents)
        {
            WindGlideForce += current.GliderWindDirection;
            WindForce += current.WindDirection;
        }

        if (CurrentState == PLAYER_STATES.GLIDING)
        {
            if (WindGlideForce + WindGlideForce == Vector3.zero)
                return;
            Velocity += WindGlideForce * Time.deltaTime;
            Velocity += WindForce * Time.deltaTime;
        }
        else
        {
            if (WindGlideForce == Vector3.zero)
                return;
            Velocity += WindForce * Time.deltaTime;
        }
    }

    public void AddForce(Vector3 force)
    {
        Velocity = force;
    }

    void HandleJump()
    {
        if (IsOnGround() && JumpAction.WasPressedThisFrame() && CurrentState != PLAYER_STATES.DIVING)
        {
            Velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }

        if ((CurrentState == PLAYER_STATES.IDLE || CurrentState == PLAYER_STATES.RUNNING) && !IsOnGround())
        {
            CurrentState = PLAYER_STATES.IN_AIR;
            SetAnim(SPRITE_FRAMES.INAIR, SPRITE_FRAMES.INAIR_END);
        }

    }
    void HandleDive()
    {
        // Reset dive when on the ground
        if (IsOnGround()) CanDive = true;

        // Check for and do the dive
        if (DiveAction.WasPressedThisFrame() && !IsOnGround() && CurrentState == PLAYER_STATES.IN_AIR && CanDive)
        {
            StartDive();
        }

        // Count down the timer for the dive
        if (CurrentState == PLAYER_STATES.DIVING)
        {
            DiveZeroGravTimer -= Time.deltaTime;

            if (IsOnGround())
                EndDive();
        }
    }

    void StartDive()
    {
        CurrentState = PLAYER_STATES.DIVING;
        SetAnim(SPRITE_FRAMES.DIVE1, SPRITE_FRAMES.DIVE_END);

        CanDive = false;
        DiveZeroGravTimer = DiveDuration;

        Vector3 dir = Velocity;
        dir.y = 0;
        dir.Normalize();

        // if the player isn't moving, dive away from the camera instead
        if (dir.x + dir.z == 0)
        {
            OrientToCameraAngle();
            dir = this.transform.forward;
        }
        Velocity = dir * DiveSpeed;
    }

    void EndDive()
    {
        CurrentState = PLAYER_STATES.IDLE;
        DiveZeroGravTimer = 0;
    }

    void HandleGlide()
    {
        if (JumpAction.WasPressedThisFrame() && !IsOnGround())
        {
            if (CurrentState == PLAYER_STATES.IN_AIR)
            {
                CurrentState = PLAYER_STATES.GLIDING;
                SetAnim(SPRITE_FRAMES.GLIDING1, SPRITE_FRAMES.GLIDING_END);
            }
            else if (CurrentState == PLAYER_STATES.GLIDING)
            {
                CurrentState = PLAYER_STATES.IN_AIR;
                SetAnim(SPRITE_FRAMES.INAIR, SPRITE_FRAMES.INAIR_END);
            }
        }

        if (CurrentState == PLAYER_STATES.GLIDING)
        {
            if (IsOnGround())
            {
                CurrentState = PLAYER_STATES.IN_AIR;
            }
        }
    }

    void HandleSprite()
    {
        // Increment animation
        if (TimeSinceFrameChanged > 0)
            TimeSinceFrameChanged -= Time.deltaTime;
        else
        {
            TimeSinceFrameChanged = AnimFrameRate;

            if (CurrentFrame >= CurrentAnimEndFrame)
                CurrentFrame = CurrentAnimStartFrame;
            else
                CurrentFrame++;
        }

        // Work out the direction the player is facing relative to the camera
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        float forwardDot = Vector3.Dot(LastMovementDirection.normalized, camForward);
        float rightDot = Vector3.Dot(LastMovementDirection.normalized, camRight);

        SPRITE_DIRECTION previousDisplayedDirection = CurrentlyDisplayedDirection;

        if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
        {
            if (forwardDot > 0)
                CurrentlyDisplayedDirection = SPRITE_DIRECTION.AWAY; // away
            else
                CurrentlyDisplayedDirection = SPRITE_DIRECTION.TOWARDS; //towards
        }
        else
        {
            if (rightDot > 0)
                CurrentlyDisplayedDirection = SPRITE_DIRECTION.RIGHT; // right
            else
                CurrentlyDisplayedDirection = SPRITE_DIRECTION.LEFT; // left
        }

        // Switch to the correct direction texture
        if (CurrentlyDisplayedDirection != previousDisplayedDirection)
            PlayerSpriteMat.SetTexture("_MainTex", SpriteSheets[(int)CurrentlyDisplayedDirection]);

        SetSpriteFrame((int)CurrentFrame);
    }

    void SetSpriteFrame(int frame)
    {
        int column = frame % SpriteSheetWidth;
        int row = frame / SpriteSheetWidth;
        
        float columnOffset = (1f / SpriteSheetWidth) * column;
        float rowOffset = 1 - ((1f / SpriteSheetHeight) * (row + 1));

        PlayerSpriteMat.SetTextureOffset("_MainTex", new Vector2(columnOffset, rowOffset));
    }

    void SetAnim(SPRITE_FRAMES startFrame, SPRITE_FRAMES endFrame)
    {
        CurrentAnimStartFrame = startFrame;
        CurrentAnimEndFrame = endFrame;
        CurrentFrame = startFrame;
        TimeSinceFrameChanged = AnimFrameRate + Time.deltaTime;
    }

    void Die()
    {
        Controller.enabled = false;
        this.transform.position = LastCheckpoint;
        Controller.enabled = true;
    }

    bool IsOnGround()
    {
        return TimeSinceGrounded < CoyoteTime;
    }
}
