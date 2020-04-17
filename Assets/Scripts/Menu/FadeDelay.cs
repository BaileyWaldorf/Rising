using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeDelay : MonoBehaviour
{

    public void DelayedFade()
    {
        StartCoroutine(DoDelayedFade());
    }

    IEnumerator DoDelayedFade()
    {

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

        yield return new WaitForSeconds(3);

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= (Time.deltaTime / 1) * 2;
            yield return null;
        }
        canvasGroup.interactable = false;
        yield return null;
    }
}