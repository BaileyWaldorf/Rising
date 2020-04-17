using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadNextLevel : MonoBehaviour
{

    public GameObject playerOne, playerTwo;
    bool playerOneIn, playerTwoIn;
    public string nextScene;

    private void Start()
    {
        playerOneIn = false;
        playerTwoIn = false;
  
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject == playerOne)
            playerOneIn = true;
        else if (collision.gameObject == playerTwo)
            playerTwoIn = true;

        if (playerOneIn == true && playerTwoIn == true)
            SceneManager.LoadScene(nextScene);
            
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == playerOne)
            playerOneIn = false;
        else if (collision.gameObject == playerTwo)
            playerTwoIn = false;
    }

}
