using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneLoader : MonoBehaviour
{
    [SerializeField]
    private string sceneName;

    public void FadeMe()
    {
        StartCoroutine(DoFade());
    }

    IEnumerator DoFade()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= (Time.deltaTime / 1) * 2;
            yield return null;
        }
        canvasGroup.interactable = false;
        loadScene();
        yield return null;
    }

    public void loadScene()
    {
        StartCoroutine(DelayedLoad());
        
    }

    IEnumerator DelayedLoad()
    {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(sceneName);
    }
}
