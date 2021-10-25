using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using MilkShake;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering.Universal;
using Vector3 = UnityEngine.Vector3;

public class CameraBehavior : MonoBehaviour
{
    [SerializeField] private GameObject _followTarget = null;
    private Camera _camera = null;

    public bool _smoothing = true;
    public float _smoothingSpeed = 0.25f;
    public Vector3 _offset;
    public Vector3 _rotation;

    // Zooming
    private float _defaultFov = 60;

    [SerializeField] private float _booFov = 50;
    public bool IsZooming { get; set; } = false;
    [SerializeField] private float _zoomSpeed = 10f;
    [SerializeField] private float _recoverZoomSpeed = 5f;

    // Shaking
    [SerializeField] private Shaker _shaker;

    [SerializeField] private ShakePreset _shakePreset;

    private ShakeInstance _shakeInstance;

    // Snap camera to the player at the start
    private void Start()
    {
        transform.position = _followTarget.transform.position + _offset;
        //GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        _camera = GetComponentInChildren<Camera>();
        _defaultFov = _camera.fieldOfView;
    }

    private void Update()
    {
        if (IsZooming)
        {
            if (_camera.fieldOfView > _booFov)
            {
                _camera.fieldOfView -= _zoomSpeed * Time.deltaTime;
            }
            else
            {
                IsZooming = false;
            }
        }
        else
        {
            if (_camera.fieldOfView < _defaultFov)
            {
                _camera.fieldOfView += _recoverZoomSpeed * Time.deltaTime;
            }
        }
    }

    // Update camera position after player update
    private void LateUpdate()
    {
        if (_followTarget == null)
        {
            return;
        }
        // Handle camera translation
        Vector3 desiredPosition = _followTarget.transform.position + _offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothingSpeed * Time.deltaTime);
        if (_smoothing)
            transform.position = smoothedPosition;
        else
            transform.position = desiredPosition;

        // Handle camera rotation
        transform.eulerAngles = _rotation;
    }

    public void Shake()
    {
        // Shake if you are not already
        if (_shakeInstance == null || !(_shakeInstance.State == ShakeState.FadingIn || _shakeInstance.State == ShakeState.Sustained))
        {
            _shakeInstance = _shaker.Shake(_shakePreset);
        }
    }

    public void StopShake()
    {
        _shakeInstance?.Stop(1f, true);
    }
}