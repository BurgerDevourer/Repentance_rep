using UnityEngine;

public class ParalaxEffect : MonoBehaviour
{
    public Camera cam;
    public Transform followTarget;

    float startingZ;

    Vector2 startingPosition;

    float zDistanceFromTarget => transform.position.z - followTarget.transform.position.z;

    float parallaxFactor => Mathf.Abs(zDistanceFromTarget) / clippingPlane;

    float clippingPlane => cam.transform.position.z + (zDistanceFromTarget > 0 ? cam.farClipPlane : cam.nearClipPlane);

    Vector2 camMoveSinceStart => (Vector2)cam.transform.position - startingPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startingPosition = transform.position;
        startingZ = transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 newPosition = startingPosition + camMoveSinceStart * parallaxFactor;
        transform.position = new Vector3(newPosition.x, newPosition.y, startingZ);
    }
}
