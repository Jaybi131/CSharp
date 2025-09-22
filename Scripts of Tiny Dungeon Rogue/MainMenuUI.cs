using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = 1;
    }

    // Update is called once per frame
    public void StartGameButton()
    {
        SceneManager.LoadScene(1);
    }
    public void ExitGameButton()
    {
        Application.Quit();
    }
}
