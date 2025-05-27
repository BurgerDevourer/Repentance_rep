using System;
using UnityEngine;
using UnityEngine.SceneManagement;


public class StartGame : MonoBehaviour
{   
    // changing scene - starts game
    public void ChangeScene()
    {
        // Load the game scene
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameplayScene");
        Time.timeScale = 1f; // Ensure the game is running at normal speed
    }

    // Quitting game
    public void QuitGame()
    {
        Application.Quit();
        
        // If running in the editor, stop playing
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
