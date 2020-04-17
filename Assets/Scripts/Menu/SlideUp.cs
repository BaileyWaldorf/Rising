using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideUp : MonoBehaviour{

    RectTransform background;
    public float speed;

    public void Slide()
    {
        StartCoroutine(doSlide(speed));
    }

    IEnumerator doSlide(float speed)
    {
        background = gameObject.GetComponent<RectTransform>();

        while (background.transform.position.y < 3770)
        {
            background.transform.position += new Vector3(0, speed * Time.deltaTime / 1, 0);
            yield return null;
        }

        yield return null;
    }
}