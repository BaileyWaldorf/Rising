using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDelegates : MonoBehaviour {

    public delegate void CollisionEvent(GameObject go);
    public CollisionEvent triggerEnterEvent;
    //public CollisionEvent triggerStayEvent;

	// Use this for initialization
	void Start () {
        
	}
    

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log(other.name);
        if (other.gameObject != null)
        {
            triggerEnterEvent(other.gameObject);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        //Debug.Log(gameObject.name);
        //Debug.Log(other.name);
        if (other.gameObject != null) {
            //Debug.Log(other.gameObject);
            //triggerStayEvent(other.gameObject);
        }
    }

}
