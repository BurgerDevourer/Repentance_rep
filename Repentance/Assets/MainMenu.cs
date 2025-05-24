using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;


public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Load the game scene
        SceneManager.LoadSceneAsync("GameplayScene");
    }
}
