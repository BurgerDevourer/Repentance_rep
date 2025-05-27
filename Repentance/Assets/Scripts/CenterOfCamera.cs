using UnityEngine;

public class CenterOfCamera : MonoBehaviour
{
    public Camera mainCamera;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public float distanceFromCamera = 5f;

    void Update()
    {
        if (mainCamera != null)
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, distanceFromCamera);
        Vector3 worldCenter = mainCamera.ScreenToWorldPoint(screenCenter);
        transform.position = worldCenter;
        }
        else
        {
            Debug.LogWarning("Main camera not assigned or found.");
        }
    }
}