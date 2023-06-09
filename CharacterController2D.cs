using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController2D : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 faceDirection;
    private bool isDashAttackTimeout;
    private Vector2 moveInput;
    private bool jumpInput;
    private bool dashInput;
    private bool strikeInput;
    private bool hasJumped;
    private bool hasDashed;
    private bool isStriking;
    private PlayerInput inputActions;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        SetGravityScale(gravityScale);

        inputActions = new PlayerInput();
        inputActions.Player.Move.performed += ctx => { moveInput = ctx.ReadValue<Vector2>(); CheckFaceDirection(moveInput); };
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.started += ctx => { jumpInputTimer = jumpInputWindow; jumpInput = true; };
        inputActions.Player.Jump.canceled += ctx => jumpInput = false;
        inputActions.Player.Dash.started += ctx => dashInput = true;
        inputActions.Player.Dash.canceled += ctx => dashInput = false;
        inputActions.Player.Strike.performed += ctx => strikeInput = true;
        inputActions.Player.Strike.canceled += ctx => strikeInput = false;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        HandleJump();
        HandleWalking(1);
        HandleGravity();
        HandleDash();
        HandleStrike();
    }

    [Header("Walking")]
    [SerializeField] private float walkSpeed = 15;
    [SerializeField] private float acceleration = 10;
    [SerializeField] private float deceleration = 20;
    [SerializeField] private float airMultiplier = .5f;
    private void HandleMovement()
    {
        float targetSpeed = moveInput.magnitude * walkSpeed;
        float speedDifference = targetSpeed - rb.velocity.magnitude;
        float appliedMovement = speedDifference * acceleration;
        rb.AddForce(appliedMovement * moveInput, ForceMode.Force);
    }

    private void HandleWalking(float lerpAmount)
    {
        //Calculate the direction we want to move in and our desired velocity
        Vector2 targetSpeed = moveInput * walkSpeed;
        //We can reduce our control using Lerp() this smooths changes to our direction and speed
        //targetSpeed = Mathf.Lerp(rb.velocity.magnitude, targetSpeed, lerpAmount);

        //Gets an acceleration value based on if we are accelerating (includes turning) 
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        Vector2 accelRate;
        if (groundedTimer > 0)
        {
            accelRate.x = (Mathf.Abs(targetSpeed.x) > 0.01f) ? acceleration : deceleration;
            accelRate.y = (Mathf.Abs(targetSpeed.y) > 0.01f) ? acceleration : deceleration;
        }
        else
        {
            accelRate.x = (Mathf.Abs(targetSpeed.x) > 0.01f) ? acceleration * airMultiplier : deceleration * airMultiplier;
            accelRate.y = (Mathf.Abs(targetSpeed.y) > 0.01f) ? acceleration * airMultiplier : deceleration * airMultiplier;
        }
        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        //    if (Mathf.Abs(rb.velocity.magnitude) > Mathf.Abs(targetSpeed) && Mathf.Sign(rb.velocity.magnitude) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && groundedTimer < 0)
        //{
        //Prevent any deceleration from happening, or in other words conserve are current momentum
        //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
        //    accelRate = 0;
        //}

        //Calculate difference between current velocity and desired velocity
        Vector2 speedDif = targetSpeed - new Vector2(rb.velocity.x,rb.velocity.z);
        //Calculate force along x-axis to apply to thr player
        Vector2 movement = speedDif * accelRate;
        //Convert this to a vector and apply to rigidbody
        rb.AddForce(movement.x * Vector3.right * Time.deltaTime * 1000, ForceMode.Force);
        rb.AddForce(movement.y * Vector3.forward * Time.deltaTime * 1000, ForceMode.Force);
        //rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    [Header("Gravity")]
    [SerializeField] private float gravityScale = 5;
    [SerializeField] private float jumpVelocityFalloff = 2;
    [SerializeField] private float fallMultiplier = 15;
    [SerializeField] private float maxFallSpeed = 30;
    void HandleGravity()
    {
        //if (isDashAttackTimeout) SetGravityScale(0);
        if (rb.velocity.y < jumpVelocityFalloff || rb.velocity.y > 0 && jumpInput == false) {rb.velocity += Vector3.down * gravityScale * fallMultiplier * Time.deltaTime; rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed), rb.velocity.z); }
        else SetGravityScale(gravityScale);
    }

    private void SetGravityScale(float scale)
    {
        rb.mass = scale;
    }

    [Header("Grounded")]
    [SerializeField] private float coyoteTime = .1f;
    private float groundedTimer;
    private void CheckGrounded()
    {
        groundedTimer -= Time.deltaTime;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);
        if (isGrounded) groundedTimer = coyoteTime;
        if (isGrounded) hasJumped = false;
    }

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 20;
    [SerializeField] private float jumpInputWindow = .1f;
    private float jumpInputTimer;
    private void HandleJump()
    {
        jumpInputTimer -= Time.deltaTime;
        if (jumpInputTimer >0 && !hasJumped && groundedTimer>0)
        {
            hasJumped = true;
            float force = jumpForce;
            if (rb.velocity.y < 0) force -= rb.velocity.y;
            rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        }
    }

    [Header("Dashing")]
    [SerializeField] private float dashForce = 50;
    [SerializeField] private float dashFreezeTime = 0.05f;
    [SerializeField] private float dashAttackTimeout = .1f;
    [SerializeField] private float dashEndTimeout = .4f;
    private void HandleDash()
    {
        if(dashInput && !hasDashed)
        {
            hasDashed = true;
            Sleep(dashFreezeTime);
            StartCoroutine(nameof(StartDash), faceDirection);
        }
    }
    private IEnumerator StartDash(Vector3 direction)
    {
        isDashAttackTimeout = true;
        float startTime = Time.time;
        //We keep the player's velocity at the dash speed during the "attack" phase (in celeste the first 0.15s)
        while (Time.time - startTime <= dashAttackTimeout)
        {
            rb.velocity = direction * dashForce;
            //Pauses the loop until the next frame, creating something of a Update loop. 
            //This is a cleaner implementation opposed to multiple timers and this coroutine approach is actually what is used in Celeste :D
            yield return null;
        }
        isDashAttackTimeout = false;
        startTime = Time.time;
        //Begins the "end" of our dash where we return some control to the player but still limit run acceleration (see Update() and Run())
        rb.velocity = walkSpeed * direction;
        while (Time.time - startTime <= dashEndTimeout)
        {
            yield return null;
        }
        //Dash over
        hasDashed = false;
    }

    [Header("Striking")]
    [SerializeField] private GameObject striker;
    [SerializeField] private GameObject strikeParticle;
    [SerializeField] private GameObject strikeImpactParticle;
    [SerializeField] private GameObject preStrikeParticle;
    [SerializeField] private float stepTimeout;
    [SerializeField] private float stepForce;
    [SerializeField] private Vector3 strikeDisplacement = new Vector3(2,.5f,2);
    [SerializeField] private int burstCount = 0;
    [SerializeField] private float strikeRange = 5;
    [SerializeField] private float strikeStartDelay = .1f;
    [SerializeField] private float strikeRate = .025f;
    [SerializeField] private float strikeEndDelay = .2f;
    [SerializeField] private float strikeFreezeTime = .05f;
    [SerializeField] private float strikeCooldownTime = 1f;
    [SerializeField] private LayerMask strikeLayer;
    private float strikeTimer;
    private void HandleStrike()
    {
        strikeTimer -= Time.deltaTime;
        if (strikeInput && !isStriking && !hasDashed && strikeTimer < 0)
        {
            isStriking = true;
            Sleep(strikeFreezeTime);
            StartCoroutine(nameof(StartStrike), faceDirection);
        }
    }
    private IEnumerator StartBarrage(Vector2 direction)
    {

        yield return new WaitForSeconds(strikeStartDelay);
        var count = burstCount;
        while (strikeInput && count>0)
        {
            float strikeAngle = Quaternion.Euler(direction).y;
            Quaternion strikeRotation = Quaternion.Euler(0, 0, strikeAngle);
            var instance = Instantiate(strikeParticle, transform.position, strikeRotation);
            Destroy(instance, 0.5f);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, strikeRange, strikeLayer);
            if (hit) { var particle = Instantiate(strikeImpactParticle, hit.point, strikeParticle.transform.rotation); Destroy(particle, 0.5f); }
            count--;
            yield return new WaitForSeconds(strikeRate);
        }
        yield return new WaitForSeconds(strikeEndDelay);
        strikeTimer = strikeCooldownTime;
        isStriking = false;
    }
    private IEnumerator StartStrike(Vector3 direction)
    {
        // Spawn pre strike special effect and step
        var instance = Instantiate(preStrikeParticle, transform.position, transform.rotation, transform);
        Destroy(instance, 0.5f);
        yield return new WaitForSeconds(strikeStartDelay);
        StartCoroutine(nameof(StrikeStep),direction);

        // Calculate the strike angle based on the direction
        Quaternion strikeRotation = Quaternion.LookRotation(direction, Vector3.up) * strikeParticle.transform.rotation;
        Vector3 sctikeLocation = new Vector3(transform.position.x + direction.x * strikeDisplacement.x, transform.position.y + strikeDisplacement.y, transform.position.z + direction.z * strikeDisplacement.z);
        instance = Instantiate(strikeParticle, sctikeLocation, strikeRotation);
        Destroy(instance, 0.5f);
        StartCoroutine(nameof(StrikeScan), direction);
        yield return new WaitForSeconds(strikeEndDelay);
        isStriking = false;
    }
    private IEnumerator StrikeStep(Vector3 direction) {
        float startTime = Time.time;
        while (Time.time - startTime <= stepTimeout)
        {
            rb.velocity = direction * stepForce;
            yield return null;
        }
        startTime = Time.time;
        rb.velocity = walkSpeed * direction;
    }
    private IEnumerator StrikeScan(Vector3 direction) {
        int count = 7;
        float degreesToRotate = -45.0f;
        Quaternion rotation = Quaternion.Euler(0, degreesToRotate, 0);
        direction = rotation * direction;
        degreesToRotate = 5.0f;
        rotation = Quaternion.Euler(0, degreesToRotate, 0);
        while (count > 0) {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, strikeRange, strikeLayer))
            {
                var particle = Instantiate(strikeImpactParticle, hit.point, strikeImpactParticle.transform.rotation);
                Destroy(particle, 0.5f);
                hit.collider.SendMessage("Strike", SendMessageOptions.DontRequireReceiver);
            }
            direction = rotation * direction;
            count--;
        }
        yield return null;
    }



    private void Sleep(float duration)
    {
        //Method used so we don't need to call StartCoroutine everywhere
        //nameof() notation means we don't need to input a string directly.
        //Removes chance of spelling mistakes and will improve error messages if any
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private IEnumerator PerformSleep(float duration)
    {
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(duration); //Must be Realtime since timeScale with be 0 
        Time.timeScale = 1;
    }

    void CheckFaceDirection(Vector2 direction) {
        if(direction.magnitude > 0.1f) {
            faceDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        }
    }
}
