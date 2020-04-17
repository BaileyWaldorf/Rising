using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    public bool playerOne;

    [Header("Movement and Jump Values")]
    public float jumpPow = 1f;
    [SerializeField]
    private float groundSpeed = .2f;
    [SerializeField]
    private float airAcceleration = .02f;
    [SerializeField]
    private float baseMaxAirSpeed = .1f;
    [SerializeField]
    private float gravity = .1f;
    // For variable jump height
    [SerializeField]
    private float jumpHoldGravityModifier = .5f;
    // Modifier for end of jump (should generally increase gravity)
    // Good for nice curved jumps
    [SerializeField]
    private float endJumpGravityModifier = 1.2f;
    [SerializeField]
    private float jumpHoldEffectTime = .2f;
    [SerializeField]
    private float maxFallSpeed = 10f;
    [Tooltip("Lower value means MORE friction")]
    [SerializeField]
    private float groundFriction = .9f;

    [Header("Boost Values")]
    // Currently, this is the boost
    public float boostStrength;

    [Header("Hoist Values")]
    [SerializeField]
    private float climbSpeed;
    [SerializeField]
    private float ropeLength;

    [Header("Self-References")]
    [SerializeField]
    private Transform flipable;
    [SerializeField]
    private Transform spriteObject;
    [SerializeField]
    private BoxCollider2D boostSpace;
    [SerializeField]
    private BoxCollider2D boostTop;
    [SerializeField]
    private CollisionDelegates boostZoneDelegate;
    [SerializeField]
    private RopeScript rope;

    // Movement
    [HideInInspector]
    public Vector2 velocity = new Vector2(0,0);
    //[HideInInspector]
    public bool isJumping = true;
    [HideInInspector]
    public bool facingLeft;

    //  Boost
    [HideInInspector]
    public bool boostReady = false;
    [HideInInspector]
    public PlayerMovement boostTarget;

    // Hoist
    [HideInInspector]
    public bool isClimbing;
    [HideInInspector]
    public bool isHoisting;
    [HideInInspector]
    public RopeScript targetRope;

    // Movement privates
    // max fall speed achieved
    private bool isMaxFalling;
    private bool initJumpFlag;
    private float currentMaxAirSpeed;
    private float currentMinAirSpeed;
    private float groundSpeedOnJump;
    private float jumpHoldTimer = 0;

    // Collision
    private int groundLayerMask = 3 << 8;
    private int wallLayerMask = 1 << 8;
    private int ceilingLayerMask = 1 << 8;
    [HideInInspector]
    public float groundDetectDistance = .5f;
    private float wallDetectDistance = .5f;
    private bool onWall;

    // Hoist

    // for Gizmos
    private Vector2 groundHitPoint;
    private Vector2 upperWallDetectPoint;
    private Vector2 lowerWallDetectPoint;

    // Animation
    [SerializeField]
    private Animator animator;

    // Use this for initialization
    void Start () {
        groundHitPoint = new Vector2(transform.position.x, groundDetectDistance);
        upperWallDetectPoint = new Vector2(transform.position.x + wallDetectDistance, transform.position.y + .9f*groundDetectDistance);
        lowerWallDetectPoint = new Vector2(transform.position.x + wallDetectDistance, transform.position.y - .9f*(groundDetectDistance));

        boostTop.enabled = false;
        boostSpace.enabled = false;

        rope.gameObject.SetActive(false);

        boostZoneDelegate.triggerEnterEvent += OnEnterBoostZone;
    }

    #region Update Functions
    // Update is called once per frame
    void Update () {
        
        if (((playerOne && (Input.GetKeyDown(KeyCode.W) || Input.GetButtonDown("Jump"))) || 
            (!playerOne && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetButtonDown("Jump2")))) 
            && !boostReady && !isJumping && !isClimbing && !isHoisting)
        {
//            Debug.Log(Input.GetAxis("Jump") + "      " + Input.GetAxis("Jump2"));
            Jump();
        }

        if ((playerOne && (Input.GetButtonDown("Set Boost 1")) || (!playerOne && Input.GetButtonDown("Set Boost 2"))) && !isJumping && !isClimbing && !isHoisting)
        {
            SetBoost();
        }

        if ((playerOne && (Input.GetButtonUp("Set Boost 1")) || (!playerOne && Input.GetButtonUp("Set Boost 2"))) && boostReady)
        {
            DoBoost(true);
        }

        if ((playerOne && (Input.GetAxis("Hoist 1") != 0 || Input.GetKeyDown(KeyCode.Q)) || (!playerOne && (Input.GetAxis("Hoist 2") != 0 || Input.GetKeyDown(KeyCode.Return))) && !boostReady && !isJumping && !isClimbing && !isHoisting))
        {
            if(!isHoisting)
                ToggleHoist();
        }

    }

    private void FixedUpdate()
    {
        float x = playerOne ? Input.GetAxis("Horizontal") : Input.GetAxis("Horizontal2");
        //float y = Input.GetAxis("Vertical");
        if(isHoisting && x != 0)
        {
            if (rope.climber)
            {
                rope.climber.transform.parent = null;
            }
            ToggleHoist();
        }
        if (!isHoisting && !boostReady)
        {
            

            //Debug.Log(isJumping);
            if (!isClimbing)
            {
                if (isJumping)
                {
                    FallControl(x);
                }
                else
                {
                    GroundControl(x);
                }
            }
            else
            {
                Climb(x);
            }

            if (x > 0)
            {
                facingLeft = false;
                flipable.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else if (x < 0)
            {
                facingLeft = true;
                flipable.transform.rotation = Quaternion.Euler(0, 180, 0);
            }

            //Debug.Log("Velocity: " + velocity);
            transform.position += new Vector3(velocity.x, velocity.y, 0);
        }
    }

    #endregion

    #region Movement Functions

    private void FallControl(float x)
    {
        if (!isMaxFalling)
        {
            if (jumpHoldTimer < jumpHoldEffectTime )
            {
                if ((playerOne && (Input.GetKey(KeyCode.W)) || Input.GetButton("Jump")) || (!playerOne && Input.GetKey(KeyCode.UpArrow)))
                {
                    velocity -= new Vector2(0, gravity * jumpHoldGravityModifier);
                    jumpHoldTimer += Time.deltaTime;
                }
                else
                {
                    jumpHoldTimer = jumpHoldEffectTime;
                }
            }
            else if(velocity.y >=0 || onWall)
            {
                velocity -= new Vector2(0, gravity);
            }
            else
            {
                velocity -= new Vector2(0, gravity*endJumpGravityModifier);
            }

            
            if (velocity.y <= -maxFallSpeed)
            {
                isMaxFalling = true;
                velocity = new Vector2(velocity.x, -maxFallSpeed);
            }
        }
        
        //groundSpeedOnJump = Mathf.Abs(groundSpeedOnJump * x);
        /*if(Mathf.Abs(velocity.x) < airSpeed)
        {
            groundSpeedOnJump = airSpeed;
        }*/
        //float currentAirSpeed = groundSpeedOnJump + airSpeed * x;
        //Debug.Log("Current Air Speed: " + currentAirSpeed);
        velocity += new Vector2(airAcceleration*x, 0);
        
        if(velocity.x < currentMinAirSpeed)
        {
            velocity = new Vector2(currentMinAirSpeed, velocity.y);
        }else if (velocity.x > currentMaxAirSpeed)
        {
            velocity = new Vector2(currentMaxAirSpeed, velocity.y);
        }
        

        FloorCheck();
        
        CeilingCheck();

        WallCheck();
    }

    private void GroundControl(float x)
    {
        if (x != 0)
        {
            velocity = new Vector2(groundSpeed * x, velocity.y);
            animator.SetBool("Walking", true);
            animator.SetBool("Idle", false);
        }
        else
        {
            animator.SetBool("Walking", false);
            animator.SetBool("Idle", true);
            if (Mathf.Abs(velocity.x) > 0)
            {
                //Debug.Log(velocity);
                velocity = new Vector2(velocity.x * groundFriction, velocity.y);
                //Debug.Log(velocity.x + " friction: " + groundFriction);
                if(Mathf.Abs(velocity.x) < .05f)
                {
                    velocity = new Vector2(0, velocity.y);
                }
            }
        }
        

        FloorParentCheck();

        WallCheck();
    }

    #region climbing
    void Climb(float x)
    {

        //Debug.Log(Input.GetAxis("Vertical2"));
        float y = playerOne ? Input.GetAxis("Vertical") : Input.GetAxis("Vertical2");

        if (facingLeft)
        {
            //Debug.Log("Facing left x: " + x);
            if(x > 0f)
            {
                PopOff();
            }
        }
        else
        {
            if(x < 0f)
            {
                PopOff();
            }
        }

        if(y != 0)
        {
            velocity = new Vector2(0, climbSpeed * y);
            animator.speed = 1;
        }
        else
        {
            animator.speed = 0f;
            velocity = Vector2.zero;
        }
        FloorCheck();
    }

    public void SetClimb(RopeScript rs)
    {
        animator.SetBool("Climb Rope", true);
        animator.SetBool("Walking", false);
        targetRope = rs;
        transform.parent = rs.transform;
        if (!facingLeft)
        {
            transform.localPosition = new Vector3(.3f, transform.localPosition.y, 0);
        }
        else
        {
            transform.localPosition = new Vector3(-.3f, transform.localPosition.y, 0);
        }
        isClimbing = true;
        velocity = new Vector2(0, 0);
        //Debug.Log("IsClimbing: " + isClimbing);
    }

    public void PopOff()
    {
        if (facingLeft)
        {
            RecieveBoost(jumpPow, new Vector2(.5f, 0.86602540378f));
        }
        else
        {
            RecieveBoost(jumpPow, new Vector2(-.5f, 0.86602540378f));
        }
        DropOff();
    }

    public void PopUp()
    {
        if (facingLeft)
        {
            RecieveBoost(jumpPow, new Vector2(-.5f, 0.86602540378f));
        }
        else
        {
            RecieveBoost(jumpPow, new Vector2(.5f, 0.86602540378f));
        }

        DropOff();
    }

    public void DropOff()
    {
        isClimbing = false;
        targetRope.climber = null;
        targetRope = null;
        
        animator.SetBool("Idle", true);

        //animator.SetFloat("Speed", 1);
        animator.SetBool("Climb Rope", false);
        //Debug.Log("Animator climb  " + animator.GetBool("Climb Rope"));
    }
    #endregion

    #region jumping
    public void Jump()
    {
        animator.SetBool("Jumping", true);
        animator.SetBool("Walking", false);

        float y = playerOne ? Input.GetAxis("Jump") : Input.GetAxis("Jump2");

        SetMaxAirSpeed();
        velocity = new Vector2(velocity.x, jumpPow);
        isJumping = true;
        isClimbing = false;
        if (transform.parent != null)
        {
            transform.parent = null;
        }

        isMaxFalling = false;
        jumpHoldTimer = 0;
    }

    public void SetMaxAirSpeed()
    {
        groundSpeedOnJump = velocity.x;
        if (velocity.x > 0)
        {
            currentMaxAirSpeed = velocity.x + baseMaxAirSpeed;
            currentMinAirSpeed = -1 * baseMaxAirSpeed;
        }
        else if(velocity.x < 0)
        {
            currentMaxAirSpeed = baseMaxAirSpeed;
            currentMinAirSpeed = -1 * baseMaxAirSpeed + velocity.x;
        }
        else
        {
            currentMaxAirSpeed = baseMaxAirSpeed;
            currentMinAirSpeed = -1 * baseMaxAirSpeed;
        }
    }
    #endregion

    #endregion

    #region Boost Functions

    public void RecieveBoost(float strength, Vector2 direction)
    {
        
        velocity = direction*strength;
        //Debug.Log(velocity.magnitude);
        
        SetMaxAirSpeed();
        
        isJumping = true;
        if (transform.parent != null)
        {
            transform.parent = null;
        }

        isMaxFalling = false;
        jumpHoldTimer = 0;
    }

    private void SetBoost()
    {
        animator.SetBool("Walking", false);
        animator.SetBool("BoostQeueue", true);
        //Debug.Log("Enabling boost collider");
        boostSpace.enabled = true;
        boostTop.enabled = true;
        boostReady = true;


    }

    private void OnEnterBoostZone(GameObject other)
    {
        if (other.tag == "Player")
        {
            boostTarget = other.GetComponentInParent<PlayerMovement>();
            if (boostTarget != null)
            {
                DoBoost(false);
            }
        }
    }

    private void DoBoost(bool top)
    {
        if (boostReady)
        {
            if(boostTarget != null)
            {
                animator.SetBool("DoBoost", true);
                if (top)
                {
                    boostTarget.RecieveBoost(boostStrength, Vector2.up);
                }
                else
                {
                    if (facingLeft)
                    {
                        Debug.Log("Should send left");
                        // 60 degrees
                        boostTarget.RecieveBoost(boostStrength, new Vector2(-.5f, 0.86602540378f));
                    }
                    else
                    {
                        boostTarget.RecieveBoost(boostStrength, new Vector2(.5f, 0.86602540378f));
                    }
                }
            }
            // Boost animation
            UnSetBoost(true);
        }

    }

    private void UnSetBoost(bool fromSet)
    {
        // Stand up animation (maybe)
        if (fromSet)
        {
            // Do Standing to idle animation
        }
        else
        {
            // assuming end of boost will loop to idle
        }
        animator.SetBool("BoostQeueue", false);
        animator.SetBool("DoBoost", false);
        spriteObject.localScale = new Vector3(1, 1, 1);
        boostSpace.enabled = false;
        boostTop.enabled = false;
        boostReady = false;
        boostTarget = null;
    }

    #endregion

    #region Hoist Functions
    
    void ToggleHoist()
    {
        if (!isHoisting)
        {
            if (DropRope())
            {
                animator.SetBool("Walking", false);
                animator.SetBool("Lower Rope", true);
                isHoisting = true;
            }
        }
        else
        {
            UndoRope();
            //rope.
            animator.SetBool("Lower Rope", false);
        }
    }

    bool DropRope()
    {
        // play ducking/squatting animation
        if (facingLeft)
        {
            RaycastHit2D rchRopeDropCheck = Physics2D.Raycast(transform.position - new Vector3(wallDetectDistance * 1.2f, groundDetectDistance, 0), -1*transform.up, .1f, wallLayerMask);
            if (!rchRopeDropCheck)
            {
                RaycastHit2D edgeCheck = Physics2D.Raycast(transform.position - new Vector3(wallDetectDistance * 1.2f, groundDetectDistance*1.2f, 0), transform.right, wallDetectDistance*2.2f, wallLayerMask);
                if (edgeCheck)
                {
                    transform.position = new Vector2(edgeCheck.point.x + wallDetectDistance*.2f, transform.position.y);
                }
                rope.gameObject.SetActive(true);
                rope.DropRope();
                return true;
            }
            else
            {
                // undo ducking animation (maybe see if you can extend this out)

            }
        }
        else
        {
            RaycastHit2D rchRopeDropCheck = Physics2D.Raycast(transform.position + new Vector3(wallDetectDistance * 1.2f, -groundDetectDistance, 0), -1 * transform.up, .1f, wallLayerMask);
            if (!rchRopeDropCheck)
            {
                RaycastHit2D edgeCheck = Physics2D.Raycast(transform.position + new Vector3(wallDetectDistance * 1.2f, -groundDetectDistance*1.2f, 0), -1 * transform.right, wallDetectDistance*2.2f, wallLayerMask);
                if (edgeCheck)
                {
                    transform.position = new Vector2(edgeCheck.point.x - wallDetectDistance*.2f, transform.position.y);
                }

                rope.gameObject.SetActive(true);
                rope.DropRope();
                return true;
            }
            else
            {
                // undo ducking animation (maybe see if you can extend this out)

            }
        }
        return false;
    }

    void UndoRope()
    {
        
        rope.gameObject.SetActive(false);
        //transform.parent = null;
        isHoisting = false;
    }

    

    

    #endregion

    #region Collision
    public void WallCheck()
    {
        if (velocity.x > 0 /*|| (!facingLeft && transform.parent != null)*/)
        {
            RaycastHit2D rch2DD = Physics2D.Raycast(transform.position - new Vector3(0, groundDetectDistance * .9f, 0), transform.right, wallDetectDistance + velocity.x, wallLayerMask);
            RaycastHit2D rch2DU = Physics2D.Raycast(transform.position + new Vector3(0, groundDetectDistance * .9f, 0), transform.right, wallDetectDistance + velocity.x, wallLayerMask);
            if (rch2DD || rch2DU)
            {
                Vector2 wallHitPoint;
                if (rch2DD)
                {
                    wallHitPoint = rch2DD.point;
                    lowerWallDetectPoint = wallHitPoint;
                }
                else
                {
                    wallHitPoint = rch2DU.point;
                    upperWallDetectPoint = wallHitPoint;
                }
                transform.position = new Vector2(wallHitPoint.x - wallDetectDistance, transform.position.y);
                velocity = new Vector2(0, velocity.y);
                onWall = true;
            }
            else
            {
                upperWallDetectPoint = new Vector2(transform.position.x + wallDetectDistance + velocity.x, transform.position.y + .9f * groundDetectDistance);
                lowerWallDetectPoint = new Vector2(transform.position.x + wallDetectDistance + velocity.x, transform.position.y - .9f * groundDetectDistance);
            }
            onWall = false;
        }
        else if (velocity.x < 0 /*|| (facingLeft && transform.parent != null)*/)
        {
            RaycastHit2D rch2DD = Physics2D.Raycast(transform.position - new Vector3(0, groundDetectDistance * .9f, 0), -1 * transform.right, wallDetectDistance + Mathf.Abs(velocity.x), wallLayerMask);
            RaycastHit2D rch2DU = Physics2D.Raycast(transform.position + new Vector3(0, groundDetectDistance * .9f, 0), -1 * transform.right, wallDetectDistance + Mathf.Abs(velocity.x), wallLayerMask);
            if (rch2DD || rch2DU)
            {
                Vector2 wallHitPoint;
                if (rch2DD)
                {
                    wallHitPoint = rch2DD.point;
                }
                else
                {
                    wallHitPoint = rch2DU.point;
                }
                transform.position = new Vector2(wallHitPoint.x + wallDetectDistance, transform.position.y);
                velocity = new Vector2(0, velocity.y);
                onWall = true;
            }
            else
            {
                upperWallDetectPoint = new Vector2(transform.position.x - wallDetectDistance, transform.position.y + .9f * groundDetectDistance);
                lowerWallDetectPoint = new Vector2(transform.position.x - wallDetectDistance, transform.position.y - .9f * (groundDetectDistance));
            }
            onWall = false;
        }
        else
        {
            upperWallDetectPoint = new Vector2(transform.position.x + wallDetectDistance, transform.position.y + .9f * groundDetectDistance);
            lowerWallDetectPoint = new Vector2(transform.position.x + wallDetectDistance, transform.position.y - .9f * (groundDetectDistance));
            onWall = false;
        }
    }

    public void CeilingCheck()
    {
        if (velocity.y > 0)
        {
            RaycastHit2D rch2DL = Physics2D.Raycast(transform.position - new Vector3(wallDetectDistance*.9f, -groundDetectDistance*.9f, 0), transform.up, groundDetectDistance*.1f + Mathf.Abs(velocity.y), ceilingLayerMask);
            RaycastHit2D rch2DR = Physics2D.Raycast(transform.position + new Vector3(wallDetectDistance*.9f, groundDetectDistance*.9f, 0), transform.up, groundDetectDistance*.1f + Mathf.Abs(velocity.y), ceilingLayerMask);
            if (rch2DL || rch2DR)
            {
                Vector2 ceilingHitPoint;
                if (rch2DL && !(isClimbing && facingLeft))
                {
                    ceilingHitPoint = rch2DL.point;
                }
                else if(!(isClimbing && !facingLeft))
                {
                    Debug.Log("Bool: " + !(isClimbing && !facingLeft));
                    ceilingHitPoint = rch2DR.point;
                }
                else
                {
                    return;
                }
                transform.position = new Vector2(transform.position.x, ceilingHitPoint.y - groundDetectDistance);
                velocity = new Vector2(velocity.x, 0);
            }
        }
    }

    public void FloorCheck()
    {
        if (velocity.y <= 0)
        {
            RaycastHit2D rch2DL = Physics2D.Raycast(transform.position - new Vector3(wallDetectDistance * .9f, 0, 0), -1 * transform.up, groundDetectDistance + Mathf.Abs(velocity.y), groundLayerMask);
            RaycastHit2D rch2DM = Physics2D.Raycast(transform.position, -1 * transform.up, groundDetectDistance + Mathf.Abs(velocity.y), groundLayerMask);
            RaycastHit2D rch2DR = Physics2D.Raycast(transform.position + new Vector3(wallDetectDistance * .9f, 0, 0), -1 * transform.up, groundDetectDistance + Mathf.Abs(velocity.y), groundLayerMask);
            if (rch2DL || rch2DR || rch2DM)
            {
                //Debug.Log("Hitting something: " + rch2D.collider.name);
                string tag;
                if (rch2DM)
                {
                    groundHitPoint = rch2DM.point;

                    if (!isClimbing)
                        transform.parent = rch2DM.transform;

                    tag = rch2DM.transform.gameObject.name;
                }
                else if (rch2DL)
                {
                    groundHitPoint = rch2DL.point;

                    if(!isClimbing)
                        transform.parent = rch2DL.transform;

                    tag = rch2DL.transform.gameObject.tag;
                }
                else
                {
                    groundHitPoint = rch2DR.point;

                    if(!isClimbing)
                        transform.parent = rch2DR.transform;

                    tag = rch2DR.transform.gameObject.tag;
                }
                //Debug.Log(tag);
                //Debug.Log("Flooring");

                animator.SetBool("Jumping", false);
                isJumping = false;
                transform.position = new Vector2(transform.position.x, groundHitPoint.y + groundDetectDistance);
                velocity = new Vector2(velocity.x, 0);
                isMaxFalling = false;
                jumpHoldTimer = 0;
                if(tag == "Player")
                {
                    transform.parent.gameObject.GetComponentInParent<PlayerMovement>().boostTarget = this;
                }

            }
        }
        else
        {

            groundHitPoint = new Vector2(transform.position.x, transform.position.y - groundDetectDistance);
        }
    }

    public void FloorParentCheck()
    {
        
        RaycastHit2D rch2DL = Physics2D.Raycast(transform.position - new Vector3(wallDetectDistance * .9f, 0, 0), -1 * transform.up, groundDetectDistance*1.1f, groundLayerMask);
        RaycastHit2D rch2DM = Physics2D.Raycast(transform.position, -1 * transform.up, groundDetectDistance*1.1f, groundLayerMask);
        RaycastHit2D rch2DR = Physics2D.Raycast(transform.position + new Vector3(wallDetectDistance * .9f, 0, 0), -1 * transform.up, groundDetectDistance*1.1f, groundLayerMask);
        
        if (rch2DM)
        {
            groundHitPoint = rch2DM.point;
            transform.parent = rch2DM.transform;
            if (rch2DM.transform.tag == "Player")
            {
                transform.parent.gameObject.GetComponentInParent<PlayerMovement>().boostTarget = this;
            }
        }
        else if (rch2DL)
        {
            groundHitPoint = rch2DL.point;
            transform.parent = rch2DL.transform;
            if (rch2DL.transform.tag == "Player")
            {
                transform.parent.gameObject.GetComponentInParent<PlayerMovement>().boostTarget = this;
            }

        }
        else if(rch2DR)
        {
            groundHitPoint = rch2DR.point;
            transform.parent = rch2DR.transform;
            if (rch2DR.transform.tag == "Player")
            {
                transform.parent.gameObject.GetComponentInParent<PlayerMovement>().boostTarget = this;
            }

        }
        else
        {
            
            if (transform.parent != null)
            {
                transform.parent = null;
            }
            SetMaxAirSpeed();
            isJumping = true;
        }
    }
    #endregion

    // Drawing ground detect for testing
    private void OnDrawGizmos()
    {
        if (groundHitPoint != null)
        {
            Gizmos.DrawLine(transform.position, groundHitPoint);
            Gizmos.DrawLine(transform.position - new Vector3(0, groundDetectDistance * .9f, 0), lowerWallDetectPoint);
            Gizmos.DrawLine(transform.position + new Vector3(0, groundDetectDistance * .9f, 0), upperWallDetectPoint);
            Gizmos.color = Color.blue;
        }
    }
}
