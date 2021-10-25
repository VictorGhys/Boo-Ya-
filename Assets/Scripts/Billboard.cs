using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform _camera;

    private void Awake()
    {
        _camera = GameObject.Find("Camera_PF").transform;
    }

    private void LateUpdate()
    {
        transform.LookAt(transform.position + _camera.forward);
    }
}