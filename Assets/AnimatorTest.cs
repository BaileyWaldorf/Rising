using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorTest : MonoBehaviour
{

    public bool playerOne;

    [Header("Movement and Jump Values")]
    [SerializeField]
    private float jumpPow = 1f;
    [SerializeField]
    private float groundSpeed = .2f;
    [SerializeField]
    private float airSpeed = .1f;
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
    [SerializeField]
    private float groundFriction = .98f;

    [Header("Boost Values")]
    // Currently, this is the boost
    public float boostStrength;

    [Header("Hoist Values")]

    [Header("Self-References")]
    [SerializeField]
    private Transform flipable;
    [SerializeField]
    private Transform spriteObject;
    [SerializeField]
    private BoxCollider2D boostSpace;

    // Movement
    [HideInInspector]
    public Vector2 velocity = new Vector2(0, 0);
    [HideInInspector]
    public bool isJumping = true;
    [HideInInspector]
    public bool facingLeft;

    //  Boost
    [HideInInspector]
    public bool boostReady = false;
    [HideInInspector]
    public PlayerMovement boostTarget;

    // Movement privates
    // max fall speed achieved
    private bool isMaxFalling;
    private bool initJumpFlag;
    private float groundSpeedOnJump;
    private float jumpHoldTimer = 0;

    // Collision
    private int groundLayerMask = 1 << 8;
    private float groundDetectDistance = .5f;
    private float wallDetectDistance = .5f;
    private bool onWall;

    // for Gizmos
    private Vector2 groundHitPoint;
    private Vector2 upperWallDetectPoint;
    private Vector2 lowerWallDetectPoint;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private Animator animator;

    void Awake()
    {

    }

    // Use this for initialization
    void Start()
    {
        groundHitPoint = new Vector2(transform.position.x, groundDetectDistance);
        upperWallDetectPoint = new Vector2(transform.position.x + wallDetectDistance, transform.position.y + .9f * groundDetectDistance);
        lowerWallDetectPoint = new Vector2(transform.position.x + wallDetectDistance, transform.position.y - .9f * (groundDetectDistance));

    }

    // Update is called once per frame
    void Update()
    {

        if (((playerOne && (Input.GetKeyDown(KeyCode.W)) || Input.GetButtonDown("Jump")) || (!playerOne && Input.GetKeyDown(KeyCode.UpArrow))) && !boostReady && !isJumping)
        {
            //Debug.Log("Jumping");
            Jump();
            //Debug.Log(velocity);
        }


        //Debug.Log(velocity);

        //if((player))
    }

    private void FixedUpdate()
    {
        float x = playerOne ? Input.GetAxis("Horizontal") : Input.GetAxis("Horizontal2");
        float y = 0;
        //float y = Input.GetAxis("Vertical");

        if (x > 0)
        {
            flipable.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (x < 0)
        {
            flipable.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        //Debug.Log(isJumping);

        if (isJumping)
        {
            FallControl(x, y);
        }
        else
        {
            GroundControl(x, y);
        }

        //Debug.Log("Velocity: " + velocity);
        transform.position += new Vector3(velocity.x, velocity.y, 0);

    }

    private void FallControl(float x, float y)
    {
        if (!isMaxFalling)
        {
            if (jumpHoldTimer < jumpHoldEffectTime)
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
            else if (velocity.y >= 0 || onWall)
            {
                velocity -= new Vector2(0, gravity);
            }
            else
            {
                velocity -= new Vector2(0, gravity * endJumpGravityModifier);
            }


            if (velocity.y <= -maxFallSpeed)
            {
                isMaxFalling = true;
                velocity = new Vector2(velocity.x, -maxFallSpeed);
            }
        }
        groundSpeedOnJump = Mathf.Abs(groundSpeedOnJump * x);
        velocity = new Vector2((groundSpeedOnJump + airSpeed) * x, velocity.y);

        FloorCheck();

        CeilingCheck();

        WallCheck();
    }

    private void GroundControl(float x, float y)
    {
        if (x != 0)
        {
            velocity = new Vector2(groundSpeed * x, velocity.y);

            animator.SetFloat("Speed", velocity.x / groundSpeed * 0.5f);
            animator.SetBool("Walking", true);

        }
        else
        {
            animator.SetBool("Idle", true);
            if (Mathf.Abs(velocity.x) > 0)
            {
                //Debug.Log(velocity);
                velocity = new Vector2(velocity.x * groundFriction, velocity.y);
                if (Mathf.Abs(velocity.x) < .05f)
                {
                    velocity = new Vector2(0, velocity.y);
                }
            }
        }
        RaycastHit2D rch2D = Physics2D.Raycast(transform.position, -1 * transform.up, groundDetectDistance, groundLayerMask);
        if (!rch2D)
        {
            if (transform.parent != null)
            {
                transform.parent = null;
            }
            isJumping = true;
        }
        else
        {
            //Debug.Log("(Grounded) Hitting something: " + rch2D.collider.name);
        }

        /*
        if ((playerOne && (Input.GetKeyDown(KeyCode.W)) || Input.GetButtonDown("Jump")) || (!playerOne && Input.GetKeyDown(KeyCode.UpArrow)) && !boostReady)
        {
            Jump();
        }
        */

        WallCheck();
    }

    public void Jump()
    {
        groundSpeedOnJump = velocity.x;
        velocity = new Vector2(velocity.x, jumpPow);
        isJumping = true;
        if (transform.parent != null)
        {
            transform.parent = null;
        }
    }

    public void RecieveBoost(float strength, Vector2 direction)
    {
        groundSpeedOnJump = velocity.x;
        velocity = direction.normalized * strength;
        isJumping = true;
        if (transform.parent != null)
        {
            transform.parent = null;
        }
    }

    private void SetBoost()
    {
        // This will be replaced by an animation
        spriteObject.localScale = new Vector3(1, .5f, 1);
        boostSpace.enabled = true;

        boostReady = true;

    }

    private void DoBoost()
    {
        if (boostReady)
        {

            // Boost animation
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
    }


    #region Collision
    public void WallCheck()
    {
        if (velocity.x > 0)
        {
            RaycastHit2D rch2DD = Physics2D.Raycast(transform.position - new Vector3(0, groundDetectDistance * .9f, 0), transform.right, wallDetectDistance + velocity.x, groundLayerMask);
            RaycastHit2D rch2DU = Physics2D.Raycast(transform.position + new Vector3(0, groundDetectDistance * .9f, 0), transform.right, wallDetectDistance + velocity.x, groundLayerMask);
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
        else if (velocity.x < 0)
        {
            RaycastHit2D rch2DD = Physics2D.Raycast(transform.position - new Vector3(0, groundDetectDistance * .9f, 0), -1 * transform.right, wallDetectDistance + Mathf.Abs(velocity.x), groundLayerMask);
            RaycastHit2D rch2DU = Physics2D.Raycast(transform.position + new Vector3(0, groundDetectDistance * .9f, 0), -1 * transform.right, wallDetectDistance + Mathf.Abs(velocity.x), groundLayerMask);
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
            RaycastHit2D rch2DL = Physics2D.Raycast(transform.position - new Vector3(wallDetectDistance * .9f, 0, 0), transform.up, groundDetectDistance + Mathf.Abs(velocity.y), groundLayerMask);
            RaycastHit2D rch2DR = Physics2D.Raycast(transform.position + new Vector3(wallDetectDistance * .9f, 0, 0), transform.up, groundDetectDistance + Mathf.Abs(velocity.y), groundLayerMask);
            if (rch2DL || rch2DR)
            {
                Vector2 ceilingHitPoint;
                if (rch2DL)
                {
                    ceilingHitPoint = rch2DL.point;
                }
                else
                {
                    ceilingHitPoint = rch2DR.point;
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
            if (rch2DL || rch2DR)
            {
                //Debug.Log("Hitting something: " + rch2D.collider.name);
                if (rch2DM)
                {
                    groundHitPoint = rch2DM.point;
                    //transform.parent = rch2DM.transform;
                }
                else if (rch2DL)
                {
                    groundHitPoint = rch2DL.point;
                    //transform.parent = rch2DM.transform;
                }
                else
                {
                    groundHitPoint = rch2DR.point;
                    //transform.parent = rch2DM.transform;
                }

                Debug.Log("Flooring");
                isJumping = false;
                transform.position = new Vector2(transform.position.x, groundHitPoint.y + groundDetectDistance);
                velocity = new Vector2(velocity.x, 0);
                isMaxFalling = false;
                jumpHoldTimer = 0;

            }
        }
        else
        {

            groundHitPoint = new Vector2(transform.position.x, transform.position.y - groundDetectDistance);
        }
    }

    public void FloorParentCheck()
    {
        RaycastHit2D rch2DL = Physics2D.Raycast(transform.position - new Vector3(wallDetectDistance * .9f, 0, 0), -1 * transform.up, groundDetectDistance + Mathf.Abs(velocity.y), groundLayerMask);
        RaycastHit2D rch2DM = Physics2D.Raycast(transform.position, -1 * transform.up, groundDetectDistance + Mathf.Abs(velocity.y), groundLayerMask);
        RaycastHit2D rch2DR = Physics2D.Raycast(transform.position + new Vector3(wallDetectDistance * .9f, 0, 0), -1 * transform.up, groundDetectDistance + Mathf.Abs(velocity.y), groundLayerMask);
        if (rch2DL || rch2DR)
        {
            if (rch2DM)
            {
                groundHitPoint = rch2DM.point;
                transform.parent = rch2DM.transform;
            }
            else if (rch2DL)
            {
                groundHitPoint = rch2DL.point;
                transform.parent = rch2DM.transform;
            }
            else
            {
                groundHitPoint = rch2DR.point;
                transform.parent = rch2DM.transform;
            }


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
