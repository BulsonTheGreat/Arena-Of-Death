using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonControl : MonoBehaviour
{
    public void wyjscie()
    {
        Application.Quit();
    }

    public void resetgry()
    {
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }

    public void startgry()
    {
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }
}
