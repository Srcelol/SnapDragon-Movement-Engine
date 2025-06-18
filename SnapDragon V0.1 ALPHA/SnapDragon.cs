using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SnapDragon : MonoBehaviour
{
//==========================================================================
// SERIALIZED FIELDS
//==========================================================================

csharp
Copy
Edit
[Header("Movement")]
[SerializeField] private float walkSpeed = 6f;
[SerializeField] private float sprintSpeed = 10f;
[SerializeField] private float airControl = 0.5f;
[SerializeField] private float acceleration = 12f;

[Header("Jumping")]
[SerializeField] private float jumpForce = 7f;
[SerializeField] private int maxJumps = 2;
[SerializeField] private float coyoteTime = 0.2f;

[Header("Sliding")]
[SerializeField] private float slideSpeed = 14f;
[SerializeField] private float slideDuration = 1f;
[SerializeField] private float slideFriction = 0.5f;

[Header("Wall Running")]
[SerializeField] private float wallRunForce = 10f;
[SerializeField] private float wallRunGravity = 1f;
[SerializeField] private float wallRunDuration = 1.5f;

[Header("Camera Tilt")]
[SerializeField] private Transform cameraTransform;
[SerializeField] private float wallrunTiltAmount = 15f;
[SerializeField] private float wallrunTiltSpeed = 5f;

[Header("Camera Effects")]
[SerializeField] private Camera playerCamera;
[SerializeField] private float normalFOV = 110f;
[SerializeField] private float sprintFOV = 120f;
[SerializeField] private float wallrunFOV = 120f;
[SerializeField] private float fovLerpSpeed = 8f;
[SerializeField] private float landingDipAngle = 15f;
[SerializeField] private float dipSpeed = 10f;

[Header("Physics")]
[SerializeField] private float gravityMultiplier = 2f;
[SerializeField] private LayerMask groundLayer;
[SerializeField] private LayerMask wallLayer;

//==========================================================================
//                                FIELDS
//==========================================================================

private Rigidbody rb;
private Vector3 inputDirection;
private Vector3 wallNormal = Vector3.zero;

private int jumpCount = 0;
private float lastGroundedTime;
private float slideTimer = 0f;
private float wallRunTimer = 0f;
private float currentTilt = 0f;

private bool isSliding = false;
private bool isWallRunning = false;
private bool isLanding = false;

private Coroutine landingDipRoutine;

private bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);

//==========================================================================
//                                UNITY EVENTS
//==========================================================================

void Start()
{
    rb = GetComponent<Rigidbody>();
    rb.useGravity = false;

    if (cameraTransform == null && Camera.main != null)
        cameraTransform = Camera.main.transform;

    if (playerCamera == null && Camera.main != null)
        playerCamera = Camera.main;
}

void Update()
{
    bool wasGroundedLastFrame = IsGrounded;

    HandleInput();

    if (IsGrounded)
    {
        lastGroundedTime = Time.time;
        jumpCount = 0;

        if (Input.GetKey(KeyCode.LeftControl) && inputDirection.magnitude > 0.1f)
            StartSlide();
    }
    else if (!isWallRunning && CheckWall(out wallNormal) && rb.velocity.y <= 0)
    {
        Vector3 toWall = wallNormal;
        float towardWallDot = Vector3.Dot(inputDirection, -toWall);

        if (towardWallDot > 0.5f && rb.velocity.magnitude > 1f)
            StartWallRun();
    }

    if (Input.GetKeyDown(KeyCode.Space))
    {
        if (Time.time - lastGroundedTime <= coyoteTime || jumpCount < maxJumps)
            Jump();
    }

    if (isSliding)
    {
        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0 || !IsGrounded)
            StopSlide();
    }

    if (isWallRunning)
    {
        wallRunTimer -= Time.deltaTime;
        if (wallRunTimer <= 0)
            StopWallRun();
    }

    if (!wasGroundedLastFrame && IsGrounded)
        TriggerLandingDip();

    UpdateCameraTilt();
    UpdateCameraFOV();
}

void FixedUpdate()
{
    ApplyMovement();
    ApplyGravity();
}

//==========================================================================
//                                INPUT & MOVEMENT
//==========================================================================

private void HandleInput()
{
    float h = 0f;
    float v = 0f;

    if (Input.GetKey(KeyCode.W)) v += 1f;
    if (Input.GetKey(KeyCode.S)) v -= 1f;
    if (Input.GetKey(KeyCode.D)) h += 1f;
    if (Input.GetKey(KeyCode.A)) h -= 1f;

    Vector3 moveInput = transform.right * h + transform.forward * v;
    inputDirection = moveInput.normalized;
}

private void ApplyMovement()
{
    float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
    if (!IsGrounded && !isWallRunning)
        targetSpeed *= airControl;

    Vector3 targetVelocity = inputDirection * targetSpeed;
    Vector3 velocityChange = targetVelocity - rb.velocity;
    velocityChange.y = 0;

    rb.AddForce(velocityChange * acceleration, ForceMode.Acceleration);

    if (isSliding)
    {
        rb.AddForce(inputDirection * slideSpeed, ForceMode.Acceleration);
        rb.velocity = new Vector3(rb.velocity.x * slideFriction, rb.velocity.y, rb.velocity.z * slideFriction);
    }

    if (isWallRunning)
    {
        Vector3 alongWall = Vector3.Cross(wallNormal, Vector3.up).normalized;
        rb.AddForce(alongWall * wallRunForce, ForceMode.Acceleration);
    }
}

private void ApplyGravity()
{
    if (!IsGrounded && !isWallRunning)
    {
        rb.AddForce(Vector3.down * 9.81f * gravityMultiplier, ForceMode.Acceleration);
    }
    else if (isWallRunning)
    {
        rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Acceleration);
    }
}

private void Jump()
{
    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    jumpCount++;
}

//==========================================================================
//                                SLIDING
//==========================================================================

private void StartSlide()
{
    if (isSliding) return;

    isSliding = true;
    slideTimer = slideDuration;
}

private void StopSlide()
{
    isSliding = false;
}

//==========================================================================
//                                WALL RUNNING
//==========================================================================

private void StartWallRun()
{
    if (isWallRunning) return;

    isWallRunning = true;
    wallRunTimer = wallRunDuration;
    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
}

private void StopWallRun()
{
    isWallRunning = false;
}

private bool CheckWall(out Vector3 normal)
{
    RaycastHit hit;
    if (Physics.Raycast(transform.position, transform.right, out hit, 1.2f, wallLayer))
    {
        normal = hit.normal;
        return true;
    }
    else if (Physics.Raycast(transform.position, -transform.right, out hit, 1.2f, wallLayer))
    {
        normal = hit.normal;
        return true;
    }

    normal = Vector3.zero;
    return false;
}

//==========================================================================
//                                CAMERA EFFECTS
//==========================================================================

private void UpdateCameraTilt()
{
    if (cameraTransform == null || isLanding) return;

    float targetTilt = 0f;

    if (isWallRunning)
    {
        float side = Vector3.Dot(wallNormal, transform.right) > 0 ? -1f : 1f;
        targetTilt = wallrunTiltAmount * side;
    }

    currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * wallrunTiltSpeed);
    cameraTransform.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
}

private void UpdateCameraFOV()
{
    if (playerCamera == null) return;

    float targetFOV = normalFOV;

    if (isWallRunning)
        targetFOV = wallrunFOV;
    else if (Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0.1f && IsGrounded)
        targetFOV = sprintFOV;

    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
}

private void TriggerLandingDip()
{
    if (cameraTransform == null) return;

    if (landingDipRoutine != null)
        StopCoroutine(landingDipRoutine);

    landingDipRoutine = StartCoroutine(LandingDipRoutine());
}

private IEnumerator LandingDipRoutine()
{
    isLanding = true;

    float elapsed = 0f;
    float duration = 0.15f;
    float angle = landingDipAngle;

    Quaternion originalRot = cameraTransform.localRotation;
    Quaternion dipRot = Quaternion.Euler(angle, 0f, currentTilt);

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime * dipSpeed;
        cameraTransform.localRotation = Quaternion.Slerp(originalRot, dipRot, elapsed / duration);
        yield return null;
    }

    elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime * dipSpeed;
        cameraTransform.localRotation = Quaternion.Slerp(dipRot, Quaternion.Euler(0f, 0f, currentTilt), elapsed / duration);
        yield return null;
    }

    cameraTransform.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
    isLanding = false;
}

//==========================================================================
//                           UNITY SETUP MANUAL
//==========================================================================

/*
 * UNITY SETUP INSTRUCTIONS:
 * 
 * 1. Create a new GameObject and name it "Player".
 * 2. Attach a Rigidbody component to the Player and enable Interpolate.
 * 3. Create a child GameObject of Player and name it "CameraHolder". Attach your Main Camera to this.
 * 4. Attach this SnapDragon script to the Player.
 * 5. Assign the CameraHolder Transform to the "cameraTransform" field.
 * 6. Assign the Main Camera to the "playerCamera" field.
 * 7. Set the LayerMask fields "groundLayer" and "wallLayer" appropriately in the inspector.
 * 8. Tweak movement values (speed, jumpForce, etc.) to your liking.
 * 
 * CONTROLS:
 * - W/A/S/D: Move
 * - Left Shift: Sprint
 * - Space: Jump
 * - Wallrunning is automatic when jumping toward a wall
 * - Left Control: Slide
 * 
 * This script simulates Titanfall-style momentum-based movement.
 */