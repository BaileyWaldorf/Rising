using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeScript : MonoBehaviour {

    public CollisionDelegates top;
    public CollisionDelegates bottom;
    public CollisionDelegates body;
    public bool toLeft;
    public Vector3 startPoint;
    public PlayerMovement climber;

    public int numSegments;
    public GameObject ropeSegment;
    List<GameObject> segments = new List<GameObject>();

    public BoxCollider2D ropeCollider;
    public Transform groundDetector;
    
    // Animation stuff

	// Use this for initialization
	void Start () {
        bottom.triggerEnterEvent += StopLengthAnimation;
        top.triggerEnterEvent += PopPlayer;
        body.triggerEnterEvent += SetPlayer;
        //body.triggerStayEvent += SetPlayer;
        //DropRope();

        startPoint = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void StopLengthAnimation(GameObject ground)
    {
        if(ground.tag == "Walkable")
        {
            StopCoroutine(DoDropRope());
            segments[segments.Count - 1].GetComponent<Animator>().SetFloat("Speed", 0);
        }
    }

    void PopPlayer(GameObject other)
    {
        //Debug.Log(other.name);
        PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
        if(pm != null)
        {
            Debug.Log("Should Pop");
            //pm.transform.position = startPoint + new Vector3(0, pm.groundDetectDistance, 0);
            if (pm.isClimbing)
            {
                pm.PopUp();
            }
            climber = null;
        }
    }

    void DropPlayer()
    {
        if(climber != null)
        {
            climber.DropOff();
            //climber.transform.parent = null;
            climber = null;

        }
    }

    void SetPlayer(GameObject other)
    {
        //Debug.Log("SettingPlayer");
        PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
        if(pm!= null && climber == null)
        {
            if (pm.transform.position.y < startPoint.y)
            {
                climber = pm;
                //pm.transform.parent = transform;
                pm.SetClimb(this);
            }
        }
    }

    public void UnRope()
    {
        Debug.Log("DoingUnRope");
        StopCoroutine(DoDropRope());
        StopCoroutine(SlideCollider());
        // ResetRope
        for(int i = 0; i<segments.Count; i++)
        {
            Destroy(segments[i]);
        }
        segments.Clear();
        ropeCollider.transform.localPosition = Vector3.zero;
        ropeCollider.size = new Vector2(ropeCollider.size.x, .5f);
        groundDetector.localPosition = Vector3.zero;
    }

    private void OnDisable()
    {
        UnRope();
        DropPlayer();
    }

    public void DropRope()
    {
        StartCoroutine(DoDropRope());
    }

    IEnumerator DoDropRope()
    {
        for(int i = 0; i < numSegments; i++)
        {
            //Debug.Log("DroppingRope");
            StopCoroutine(SlideCollider());
            GameObject currentSegment = Instantiate(ropeSegment, transform);
            currentSegment.transform.localPosition = new Vector3(0, -1*i, 0);
            segments.Add(currentSegment);
            ropeCollider.transform.localPosition = new Vector3(0, -1*(float)(i+1) / 2, 0);
            ropeCollider.size = new Vector2(ropeCollider.size.x, i+1);
            StartCoroutine(SlideCollider());
            yield return new WaitForSeconds((8f/12f));
        }

    }

    IEnumerator SlideCollider()
    {
        for(int i =0; i<12; i++)
        {
            groundDetector.position -= new Vector3(0, (1f / 8f) * (8f / 12f), 0);
            yield return new WaitForSeconds((1f / 8f)*(8f/12f));
        }
    }
}
