using System;
using System.Collections;
using System.Collections.Generic;
using BBUnity.Actions;
using MilkShake;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class GhostHunter : MonoBehaviour
{
    public VisionCone VisionCone { get; set; }
    public bool GoToRandomPos { get; set; }
    public bool IsAlerted { get; set; }
    public Vector3 LastSeenPosition { get; set; } = Vector3.zero;
    public bool HasHearedScream { get; set; }
    public Vector3 HearedScreamLocation { get; set; } = Vector3.zero;
    public List<GameObject> OpenedClosets { get; set; } = new List<GameObject>();
    [SerializeField] public float SearchRadius { get; set; } = 15.0f;

    [SerializeField] public float PatrolSpeed = 3.5f;
    [SerializeField] public float HuntingSpeed = 5.0f;

    [SerializeField] private Transform _pfVisionCone;
    [SerializeField] private Transform[] _patrolPoints;
    private int _currentPatrolPointIdx = 0;
    private NavMeshAgent _navMeshAgent;
    [SerializeField] private float _dieDistance = 1.5f;

    [SerializeField] private float _pullForceIncrease = 30;
    [SerializeField] private float _pullForceStart = 200;
    [SerializeField] private float _pullForceMax = 300;
    private float _pullForce;

    [SerializeField] private float _minGoToRandomPosTime = 15.0f;
    [SerializeField] private float _maxGoToRandomPosTime = 30.0f;
    [SerializeField] private float _alertedTime = 5.0f;
    private float _distanceToOpenDoor = 2.0f;
    [SerializeField] private GameObject _rfx;
    [SerializeField] private GameObject _alertIcon;
    [SerializeField] private bool _enableDebugRendering;
    [SerializeField] private float _graceTimeMax = 0.5f;
    private float _graceTime = 0.0f;

    [SerializeField] private float _openClosetChance = 20f;

    public float OpenClosetDoorChance
    {
        get { return _openClosetChance; }
        set { _openClosetChance = value; }
    }

    private PlayerCharacter _player;

    private CameraBehavior _camera;

    // Audio
    [SerializeField] private AudioSource _vacuumStartAudioSource = null;

    [SerializeField] private AudioSource _vacuumStopAudioSource = null;
    [SerializeField] private AudioSource _hunAudioSource = null;
    [SerializeField] private AudioSource _evilLaughAudioSource = null;
    [SerializeField] private AudioSource _footstepsAudioSource = null;
    [SerializeField] private float _footstepLouderDistance = 30;
    [SerializeField] private float _footstepLoudVolume = 1;
    [SerializeField] private float _footstepSoftVolume = 0.5f;
    [SerializeField] private AudioSource _screamAudioSource = null;

    // Animation
    private Animator _animator = null;

    private void Start()
    {
        VisionCone = Instantiate(_pfVisionCone, null).GetComponent<VisionCone>();
        VisionCone.SetFOV(120);
        _navMeshAgent = GetComponent<NavMeshAgent>();

        VisionCone.HitDelegate = SuckUpPlayer;
        VisionCone._layerMask = LayerMask.GetMask("Player", "Wall", "Default");
        Invoke("GoToRandomPosGhostHunter", Random.Range(_minGoToRandomPosTime, _maxGoToRandomPosTime));
        _rfx.SetActive(false);
        VisionCone.GetComponent<MeshRenderer>().enabled = _enableDebugRendering;
        _pullForce = _pullForceStart;
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerCharacter>();
        _camera = GameObject.FindObjectOfType<CameraBehavior>();
        // Animation
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        VisionCone.SetAimDirection(transform.forward);
        VisionCone.SetOrigin(transform.position);
        UpdateIcon();
        PlayFootSteps();
        // Play animations
    }

    private void GoToRandomPosGhostHunter()
    {
        GoToRandomPos = true;
    }

    public void ResetGoToRandomPosGhostHunter()
    {
        GoToRandomPos = false;

        _navMeshAgent.ResetPath();
        Invoke("GoToRandomPosGhostHunter", Random.Range(_minGoToRandomPosTime, _maxGoToRandomPosTime));
    }

    public void DoPatroling()
    {
        if (_navMeshAgent.enabled)
        {
            _navMeshAgent.speed = PatrolSpeed;
            if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
            {
                MoveToNextPatrolPoint();
            }
        }
    }

    private void MoveToNextPatrolPoint()
    {
        if (_patrolPoints.Length > 0)
        {
            _navMeshAgent.isStopped = false;
            _navMeshAgent.destination = _patrolPoints[_currentPatrolPointIdx].position;
            ++_currentPatrolPointIdx;
            _currentPatrolPointIdx %= _patrolPoints.Length;
        }
    }

    private bool SuckUpPlayer(RaycastHit raycastHit, Vector3 origin, ref bool isActivated, ref bool hasOneHit, ref GameObject hitObject, bool isLastRay)
    {
        _rfx.SetActive(isActivated);
        if (raycastHit.collider.CompareTag("Player"))
        {
            if (raycastHit.collider.GetComponent<PlayerCharacter>()._isInvisible)
            {
                _pullForce = _pullForceStart;
                if (_vacuumStartAudioSource.isPlaying)
                {
                    _vacuumStartAudioSource.Stop();
                    _vacuumStopAudioSource.Play();
                }
                ControllerShaker.StopRumble();
                _camera.StopShake();
                return false;
            }
            // Pull the player in with a force.
            Vector3 toOrigin = (origin - raycastHit.collider.transform.position).normalized;
            raycastHit.collider.GetComponent<Rigidbody>().AddForce(toOrigin * _pullForce * Time.deltaTime);
            if (_pullForce < _pullForceMax)
            {
                _pullForce += _pullForceIncrease * Time.deltaTime;
            }

            // Do a small suck up animation by scaling the player
            GameObject player = raycastHit.collider.gameObject;
            float distanceToPlayer = (origin - raycastHit.collider.transform.position).magnitude;
            if (distanceToPlayer <= 0)
            {
                distanceToPlayer = 0.0001f;
            }
            float scale = (distanceToPlayer + 5) / VisionCone._viewDistance;
            scale += 0.1f;
            if (scale > 1f)
            {
                scale = 1;
            }
            player.GetComponentInChildren<Transform>().localScale = new Vector3(scale, 1, scale);

            // Rumble harder when the player gets closer to dying
            float rumbleIntensity = (distanceToPlayer) / VisionCone._viewDistance;
            ControllerShaker.PlayRumble(1 - rumbleIntensity, 1 - rumbleIntensity);

            // Shake the camera
            _camera.Shake();

            // Kill the player if he's too close.
            if (Vector3.Distance(raycastHit.collider.transform.position, origin) < _dieDistance)
            {
                _graceTime += Time.deltaTime;
                if (_graceTime >= _graceTimeMax)
                {
                    raycastHit.collider.GetComponent<PlayerCharacter>().Kill();
                    _vacuumStartAudioSource.Stop();
                    _vacuumStopAudioSource.Stop();
                    _evilLaughAudioSource.Play();
                    ControllerShaker.StopRumble();
                    _camera.StopShake();
                }
                ControllerShaker.StopRumble();
                _camera.StopShake();
                return true;
            }
            else
            {
                _graceTime = 0;
            }
            // Play start sound
            if (!_vacuumStartAudioSource.isPlaying)
            {
                _vacuumStartAudioSource.Play();
            }
            isActivated = true;
            hasOneHit = true;
            if (IsAlerted == false)
            {
                //Debug.Log("start searching area");
                IsAlerted = true;
                _pullForce = _pullForceStart;
                Invoke("ResetAlerted", _alertedTime);
                _hunAudioSource.Play();
            }
            return true;
        }
        else
        {
            if (isLastRay && isActivated && !hasOneHit)
            {
                ControllerShaker.StopRumble();
                _camera.StopShake();
                _vacuumStartAudioSource.Stop();
                _vacuumStopAudioSource.Play();
            }
        }
        if (raycastHit.collider.CompareTag("Closet"))
        {
            hitObject = raycastHit.collider.gameObject;
        }
        if (raycastHit.collider.CompareTag("Interact"))
        {
            DoorBehavior door = raycastHit.collider.gameObject.GetComponentInParent<DoorBehavior>();
            if (door && !door.IsOpen && Vector3.Distance(raycastHit.collider.transform.position, transform.position) < _distanceToOpenDoor)
            {
                door.OpenDoor();
            }
        }
        return true;
    }

    private void ResetAlerted()
    {
        IsAlerted = false;
    }

    private void UpdateIcon()
    {
        if (IsAlerted || HasHearedScream)
            _alertIcon.SetActive(true);
        else
            _alertIcon.SetActive(false);
    }

    private void PlayFootSteps()
    {
        float velMag = _navMeshAgent.velocity.magnitude;
        //Debug.Log(velMag);
        if (velMag != 0)
        {
            if (!_footstepsAudioSource.isPlaying)
            {
                if (_player && Vector3.Distance(_player.transform.position, transform.position) < _footstepLouderDistance)
                {
                    _footstepsAudioSource.volume = _footstepLoudVolume;
                }
                else
                {
                    _footstepsAudioSource.volume = _footstepSoftVolume;
                }
                _footstepsAudioSource.PlayOneShot(_footstepsAudioSource.clip);
            }
        }
    }

    public void PlayJumpAnimation()
    {
        _animator.SetTrigger("jump");
    }

    public void StartDancing()
    {
        _animator.SetBool("isDancing", true);
        _navMeshAgent.isStopped = true;
        GetComponent<NavMeshObstacle>().carving = false;
    }

    public void PlayScaredSound()
    {
        _screamAudioSource.Play();
    }
}