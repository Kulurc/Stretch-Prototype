using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FrogController : MonoBehaviour
{
    [Header("Player Variables")]
    [SerializeField] int maxHealth = 3;
    [SerializeField] TextMeshProUGUI healthText;
    int currentHealth;

    [Header("Tongue Parameters")]
    [SerializeField] float tongueLength;
    [SerializeField] float tongueForceOffset;

    [Header("Movement Parameters")]
    [SerializeField] float walkSpeed = 12f;
    [SerializeField] float groundAcceleration = 5f;
    [SerializeField] float groundDeceleration = 20f;

    [Header("Jump Variables")]
    [SerializeField] float jumpHeight = 6.5f;
    [SerializeField] int numberOfJumpsAllowed = 1;

    #region Jump Stats
    float jumpHeightCompensationFactor = 1.854f;
    float TimeTillJumpApex = 0.35f;
    float GravityOnReleaseMultiplier = 2f;
    float maxFallSpeed = 15f;

    //Jump Cut
    float TimeForUpwardsCancel = 0.027f;

    //Jump Apex
    float ApexThreshold = 0.97f;
    float ApexHangTime = 0.075f;

    //Jump Buffer
    float jumpBufferTime = 0.125f;

    //Jump Coyote Time
    float jumpCoyoteTime = 0.1f;
    #endregion

    #region Jump Variables
    float VerticalVelocity;
    bool isJumping;
    bool isFastFalling;
    bool isFalling;
    float fastFallTime;
    float fastFallReleaseSpeed;
    int numberofJumpsUsed;

    //Apex Variables
    float apexPoint;
    float timePastApexThreshold;
    bool isPastApexThreshold;

    //Jump Buffer Variables
    float jumpBufferTimer;
    bool jumpReleasedDuringBuffer;

    //Coyote Time Variables
    float coyoteTimer;

    #endregion

    [Header("On Air Variables")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float airAcceleration = 5f;
    [SerializeField] float airDeceleration = 5f;
    float groundDetectionLength = 0.02f;
    float headDetectionLength = 0.02f;

    [Header("References")]
    [SerializeField] Collider2D feetColl;
    [SerializeField] Collider2D bodyColl;
    [SerializeField] GameObject head;
    [SerializeField] Transform playerPos;

    float Gravity;
    float InitialJumpVelocity;
    float AdjustedJumpHeight;

    Rigidbody2D rb;

    Vector2 moveVelocity;
    bool facingRight;
    Vector2 mousePosition;

    RaycastHit2D groundHit;
    RaycastHit2D headHit;
    bool isGrounded;
    bool bumpedHead;
    float headWidth;

    private void Awake()
    {
        facingRight = true;
        rb = GetComponent<Rigidbody2D>();

        AdjustedJumpHeight = jumpHeight * jumpHeightCompensationFactor;
        Gravity = -(2f * AdjustedJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2f);
        InitialJumpVelocity = Mathf.Abs(Gravity) * TimeTillJumpApex;

        currentHealth = maxHealth;
    }

    private void Update()
    {
        CountTimers();
        JumpChecks();
        TongueControls();

        if (Input.GetKey(KeyCode.R)) ResetPlayer();
        if (Input.GetKeyDown(KeyCode.Return)) TakeDamage();
    }

    private void FixedUpdate()
    {
        CollisionChecks();
        Jump();
        LookAtMouse();
        HandlePlayerHealth();

        if (isGrounded) Move(groundAcceleration, groundDeceleration, InputManager.Movement);
        else Move(airAcceleration, airDeceleration, InputManager.Movement);
    }

    #region Player Data

    void HandlePlayerHealth()
    {
        if (currentHealth <= 0) { 
            healthText.text = "DEAD";
            return;
        }

        healthText.text = currentHealth.ToString();
    }

    public void TakeDamage()
    {
        --currentHealth;

        if(currentHealth <= 0)
        {
            //Die logic
        }
    }

    void ResetPlayer()
    {
        transform.position = playerPos.position;
        currentHealth = maxHealth;
    }

    #endregion

    #region Mouse Controls

    void LookAtMouse()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePosition - (Vector2)head.transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float rotationOffset = -90f;

        angle += rotationOffset;

        head.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void TongueControls()
    {
        Vector2 headPos = (Vector2)head.transform.position;
        Vector2 direction = (mousePosition - headPos).normalized;

        if (InputManager.AttackWasPressed) print("Attack!");
        RaycastHit2D tongueHit = Physics2D.Raycast(headPos, direction, tongueLength);
        Vector2 endPoint = headPos + direction * tongueLength;

        Debug.DrawLine(headPos, endPoint, tongueHit ? Color.red : Color.green);

        if (tongueHit)
        {
            if(tongueHit.collider.gameObject.layer == 7)    //if tongue hits an object with layer 7 (Hook)
            {
                if (InputManager.AttackWasPressed)
                {
                    GrappleTowards(direction);
                }
            }
            else if(tongueHit.collider.gameObject.layer == 9)   //if tongue hits an enemy
            {
                if (InputManager.AttackWasPressed)
                {
                    EnemyAI hitEnemy = tongueHit.collider.GetComponent<EnemyAI>();
                    hitEnemy.TakeDamage(1);
                }
            }
            /*else if(tongueHit.collider.gameObject.layer == )
             * if hit enemy, deal damage to enemy
             */
        }
    }

    void GrappleTowards(Vector2 direction)
    {
        //[!] feels very bad need to refactor
        if (!isJumping) isJumping = true;

        jumpBufferTimer = 0f;
        numberofJumpsUsed += 1;

        VerticalVelocity = 100f * tongueForceOffset;
        rb.velocity = new Vector2(direction.x, rb.velocity.y);
    }

    #endregion

    #region Movement
    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if(moveInput != Vector2.zero)
        {
            TurnCheck(moveInput);
            Vector2 targetVelocity = Vector2.zero;

            targetVelocity = new Vector2(moveInput.x, 0f) * walkSpeed;
            moveVelocity = Vector2.Lerp(moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector2(moveVelocity.x, rb.velocity.y);
        }
        else
        {
            moveVelocity = Vector2.Lerp(moveVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector2(moveVelocity.x, rb.velocity.y);
        }
    }

    #region Turn Checks
    void TurnCheck(Vector2 moveInput)
    {
        if (facingRight && moveInput.x < 0) Turn(false);
        else if (!facingRight && moveInput.x > 0) Turn(true);
    }

    void Turn(bool turnRight)
    {
        if (turnRight)
        {
            turnRight = true;
            bodyColl.gameObject.transform.Rotate(0, 180f, 0);
        }
        else
        {
            turnRight = false;
            bodyColl.gameObject.transform.Rotate(0, -180f, 0);
        }
    }
    #endregion

    #endregion

    #region Jumps

    void JumpChecks()
    {
        //Jump button pressed
        if (InputManager.JumpWasPressed)
        {
            jumpBufferTimer = jumpBufferTime;
            jumpReleasedDuringBuffer = false;
        }

        //Jump button released
        if (InputManager.JumpWasReleased)
        {
            if(jumpBufferTimer > 0f)
            {
                jumpReleasedDuringBuffer = true;
            }

            if(isJumping && VerticalVelocity > 0f)
            {
                if (isPastApexThreshold)
                {
                    isPastApexThreshold = false;
                    isFastFalling = true;
                    fastFallTime = TimeForUpwardsCancel;
                    VerticalVelocity = 0f;
                }
                else
                {
                    isFastFalling = true;
                    fastFallReleaseSpeed = VerticalVelocity;
                }
            }
        }

        //Initiate jump with jump buffering and coyote time
        if(jumpBufferTimer > 0f && !isJumping && (isGrounded || coyoteTimer > 0f))
        {
            InitiateJump(1);

            if (jumpReleasedDuringBuffer)
            {
                isFastFalling = true;
                fastFallReleaseSpeed = VerticalVelocity;
            }
        }

        //Double Jump
        else if(jumpBufferTimer > 0f && isJumping && numberofJumpsUsed < numberOfJumpsAllowed)
        {
            isFastFalling = false;
            InitiateJump(1);
        }

        //Air Jump after coyote time lapsed
        else if(jumpBufferTimer > 0f && isFalling && numberofJumpsUsed < numberOfJumpsAllowed - 1)
        {
            InitiateJump(2);
            isFastFalling = false;
        }

        //Landed
        if((isJumping || isFalling) && isGrounded && VerticalVelocity <= 0f)
        {
            isJumping = false;
            isFalling = false;
            isFastFalling = false;
            fastFallTime = 0f;
            isPastApexThreshold = false;
            numberofJumpsUsed = 0;

            VerticalVelocity = Physics2D.gravity.y;
        }
    }

    void InitiateJump(int jumpsUsed)
    {
        if (!isJumping)
        {
            isJumping = true;
        }

        jumpBufferTimer = 0f;
        numberofJumpsUsed += jumpsUsed;
        VerticalVelocity = InitialJumpVelocity;
    }

    void Jump()
    {
        //Apply gravity while jumping
        if (isJumping)
        {
            //Check for Headbump
            if (bumpedHead) isFastFalling = true;

            //Gravity on ascending
            if (VerticalVelocity >= 0f)
            {
                //Apex controls
                apexPoint = Mathf.InverseLerp(InitialJumpVelocity, 0f, VerticalVelocity);
                if (apexPoint > ApexThreshold)
                {
                    if (!isPastApexThreshold)
                    {
                        isPastApexThreshold = true;
                        timePastApexThreshold = 0f;
                    }
                    if (isPastApexThreshold)
                    {
                        timePastApexThreshold += Time.fixedDeltaTime;
                        if (timePastApexThreshold < ApexHangTime)
                        {
                            VerticalVelocity = 0f;  //apply hang time
                        }
                        else
                        {
                            VerticalVelocity = -0.01f;
                        }
                    }
                }

                //Gravity on descending but not past apex threshold
                else
                {
                    VerticalVelocity += Gravity * Time.fixedDeltaTime;
                    if (isPastApexThreshold) isPastApexThreshold = false;
                }
            }

            //Gravity on descending
            else if (!isFastFalling)
            {
                VerticalVelocity += Gravity * GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (VerticalVelocity < 0f)
            {
                if (!isFalling) isFalling = true;
            }
        }

        //Jump Cut
        if (isFastFalling)
        {
            if(fastFallTime >= TimeForUpwardsCancel)
            {
                VerticalVelocity += Gravity * GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if(fastFallTime < TimeForUpwardsCancel)
            {
                VerticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0 , (fastFallTime / TimeForUpwardsCancel));
            }

            fastFallTime += Time.fixedDeltaTime;
        }

        //Normal gravity while falling
        if(!isGrounded && !isJumping)
        {
            if (!isFalling) isFalling = true;
            VerticalVelocity += Gravity * Time.fixedDeltaTime;
        }

        //Clamp fall speed
        VerticalVelocity = Mathf.Clamp(VerticalVelocity, -maxFallSpeed, 50f);
        rb.velocity = new Vector2(rb.velocity.x, VerticalVelocity);
    }

    #endregion

    #region Collision Checks
    void IsGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(feetColl.bounds.center.x, feetColl.bounds.min.y);
        Vector2 boxCastSize = new Vector2(feetColl.bounds.size.x, groundDetectionLength);

        groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, groundDetectionLength, groundLayer);

        if (groundHit.collider != null) isGrounded = true;
        else isGrounded = false;

        #region Debug Visual
        Color rayColor;
        if (isGrounded) rayColor = Color.green;
        else rayColor = Color.red;

        Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * groundDetectionLength, rayColor);
        Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * groundDetectionLength, rayColor);
        Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - groundDetectionLength), Vector2.right * boxCastSize.x, rayColor);
        #endregion
    }

    void BumpedHead()
    {
        Vector2 boxCastOrigin = new Vector2(feetColl.bounds.center.x, bodyColl.bounds.max.y);
        Vector2 boxCastSize = new Vector2(feetColl.bounds.size.x * headWidth, headDetectionLength);

        headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, headDetectionLength, groundLayer);

        if (headHit.collider != null) bumpedHead = true;
        else bumpedHead = false;

        #region Debug Visual
        Color rayColor;
        if (bumpedHead) rayColor = Color.green;
        else rayColor = Color.red;

        Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.up * headDetectionLength, rayColor);
        Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.up * headDetectionLength, rayColor);
        Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - headDetectionLength), Vector2.right * boxCastSize.x * headWidth, rayColor);
        #endregion
    }

    void CollisionChecks()
    {
        IsGrounded();
        BumpedHead();
    }

    #endregion

    #region Timers

    void CountTimers()
    {
        jumpBufferTimer -= Time.deltaTime;
        if (!isGrounded) coyoteTimer -= Time.deltaTime;
        else coyoteTimer = jumpCoyoteTime;
    }

    #endregion

    //Unused
    #region Debug Visuals
    private void OnDrawGizmos()
    {
        //DebugJumpArc(walkSpeed, Color.white);
    }

    void DebugJumpArc(float moveSpeed, Color gizmoColor)
    {
        float ArcResolution = 20;
        float VisualizationSteps = 90;

        Vector2 startPosition = new Vector2(feetColl.bounds.center.x, feetColl.bounds.min.y);
        Vector2 previousPosition = startPosition;
        float speed = 0f;
        speed = moveSpeed;

        Vector2 velocity = new Vector2(speed, InitialJumpVelocity);
        Gizmos.color = gizmoColor;

        float timeStep = 2 * TimeTillJumpApex / ArcResolution;

        for(int i = 0; i < VisualizationSteps; ++i)
        {
            float simulationTime = i * timeStep;
            Vector2 displacement;
            Vector2 drawPoint;

            if(simulationTime < TimeTillJumpApex)
            {
                displacement = velocity * simulationTime + 0.5f * new Vector2(0, Gravity) * simulationTime * simulationTime;
            } 
            else if(simulationTime < TimeTillJumpApex + ApexHangTime)
            {
                float apexTime = simulationTime - TimeTillJumpApex;
                displacement = velocity * TimeTillJumpApex + 0.5f * new Vector2(0, Gravity) * TimeTillJumpApex * TimeTillJumpApex;
                displacement += new Vector2(speed, 0) * apexTime;
            }
            else
            {
                float descendTime = simulationTime - (TimeTillJumpApex + ApexHangTime);
                displacement = velocity * TimeTillJumpApex + 0.5f * new Vector2(0, Gravity) * TimeTillJumpApex * TimeTillJumpApex;
                displacement += new Vector2(speed, 0) * ApexHangTime;
                displacement += new Vector2(speed, 0) * descendTime + 0.5f * new Vector2(0, Gravity) * descendTime * descendTime;
            }

            drawPoint = startPosition + displacement;

            RaycastHit2D hit = Physics2D.Raycast(previousPosition, drawPoint - previousPosition, Vector2.Distance(previousPosition, drawPoint), groundLayer);
            if(hit.collider != null)
            {
                Gizmos.DrawLine(previousPosition, hit.point);
                break;
            }

            Gizmos.DrawLine(previousPosition, drawPoint);
            previousPosition = drawPoint;
        }
    }
    #endregion
}
