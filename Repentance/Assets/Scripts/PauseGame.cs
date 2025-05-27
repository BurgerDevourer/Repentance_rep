using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PauseGame : MonoBehaviour
{
    public GameObject pauseMenu; // Assign in the inspector

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
            ShowPauseMenu();
        }
    }

//FREEZE TIME
    void TogglePause()
    {
        if (Time.timeScale == 1f)
        {
            Time.timeScale = 0f; // Pausa
        }
        else
        {
            Time.timeScale = 1f; // Resume
        }
    }


// PAUSE MENU when pause
    void ShowPauseMenu()
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf);
        }
    }


//MAINMENY BUTTON
    public void SceneMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu");
    }


//QUITGAME BUTTON
    public void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

//RESPWN PLAYER BUTTON
    public void RespawnPlayer()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}