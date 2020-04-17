using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quit : MonoBehaviour {

    // Use this for initialization
    public void quitGame()
    {
        Debug.Log("<i> Application Quit... </i>");
        Application.Quit();
    }
}
