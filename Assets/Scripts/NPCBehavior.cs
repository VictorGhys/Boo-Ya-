using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.Animations;
using Random = UnityEngine.Random;

public class NPCBehavior : MonoBehaviour
{
    // Room-id
    private int _roomId = -1;

    public int RoomId
    {
        get { return _roomId; }
        set { _roomId = value; }
    }

    // Tags
    private const string _playerTag = "Player";

    private const string _interactTag = "Interact";

    // Scaremeter
    private float _scareMeter = 0.0f;

    public float ScareMeter
    {
        get { return _scareMeter; }
    }

    private const float _meterLimit = 100.0f;
    private const float _scaredLimit = 5.0f;
    private const float _terrifiedLimit = 70.0f;
    private const float _playerSeenReduction = 2.5f;

    private Slider _meterSlider = null;

    // NPC-state
    public enum State
    {
        Default,
        Scared,
        Terrified,
        Alert,
        Fallen
    }

    private State _state = State.Default;

    public State CurrentState
    {
        get { return _state; }
        set { _state = value; }
    }

    // Audio
    [SerializeField] private List<AudioClip> _screamAudioClips = null;

    private List<AudioSource> _screamAudioSources = null;

    [SerializeField] private List<AudioClip> _gaspAudioClips = null;
    private List<AudioSource> _gaspAudioSources = null;

    [SerializeField] private AudioClip _footstepsAudioClip = null;
    private AudioSource _footstepsAudioSource = null;

    // Vision cone
    private VisionCone _visionCone = null;

    public VisionCone VisionCone
    {
        get { return _visionCone; }
    }

    [SerializeField] private Transform _conePrefab = null;

    public enum ConeElement
    {
        None,
        Player,
        Interactable,
        Door,
        LightSwitch
    }

    private ConeElement _currentElement = ConeElement.None;

    public ConeElement CurrentElement
    {
        get { return _currentElement; }
        set { _currentElement = value; }
    }

    // Alerting object
    private GameObject _alertingObject = null;

    public GameObject AlertingObject
    {
        get { return _alertingObject; }
        set { _alertingObject = value; }
    }

    [SerializeField] private bool _enableDebugRendering;

    // RFX
    [SerializeField] private ParticleSystem _scaredTears = null;

    [SerializeField] private ParticleSystem _alertTears = null;

    [SerializeField] private GameObject _scaredIcon = null;
    [SerializeField] private GameObject _alertIcon = null;

    // Original (starting) position
    private Vector3 _originalPos;

    public Vector3 OriginalPos
    {
        get { return _originalPos; }
    }

    // Navigation
    private NavMeshAgent _navAgent = null;

    [SerializeField] private float _speed = 2.8f;
    [SerializeField] private float _fallingTime = 1.75f;

    // Animation
    private Animator _animator = null;

    // Falling
    [SerializeField] private float _fallingChance = 0.5f;

    //[SerializeField] private float _dancingChance = 0.1f;

    private void Start()
    {
        // Audio
        _screamAudioSources = new List<AudioSource>();
        _gaspAudioSources = new List<AudioSource>();

        // Init the meter
        _meterSlider = gameObject.GetComponentInChildren<Slider>();

        // Init the visioncone
        if (!_conePrefab)
            Debug.LogError("No cone prefab found");
        else
        {
            _visionCone = Instantiate(_conePrefab, null).GetComponent<VisionCone>();
            if (_visionCone)
            {
                _visionCone._layerMask = LayerMask.GetMask("Player", "Wall", "Default");
                _visionCone.HitDelegate = CheckHit;
            }
            _visionCone.GetComponent<MeshRenderer>().enabled = _enableDebugRendering;
        }

        // Save the original position
        _originalPos = transform.position;

        // Create sound effects
        CreateSoundEffects();

        // RFX
        _alertTears.gameObject.SetActive(false);
        _scaredTears.gameObject.SetActive(false);

        // Navigation
        _navAgent = GetComponent<NavMeshAgent>();

        // Animation
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // Update the scaremeter
        if (_scareMeter >= 0.0f)
            DecreaseScareMeter(Time.deltaTime);
        else
            _scareMeter = 0.0f;

        if (_meterSlider)
            _meterSlider.value = (_scareMeter / _meterLimit);

        // Update the NPC-state
        if (_state != State.Alert)
        {
            if (_scareMeter >= _terrifiedLimit)
                _state = State.Terrified;
            else if (_scareMeter >= _scaredLimit)
                _state = State.Scared;
            else
                _state = State.Default;
        }

        // If we are scared, lower the meter over time
        if (_state == State.Scared)
            _scareMeter -= Time.deltaTime;

        // Update the visioncone
        _visionCone.SetAimDirection(transform.forward);
        _visionCone.SetOrigin(transform.position);

        // Play RFX
        PlayRFX();

        // Play animations
        PlayAnimations();
    }

    public void IncreaseScareMeter(float amount)
    {
        if (_currentElement == ConeElement.Player)
            amount /= _playerSeenReduction;

        _scareMeter += amount;
        if (_scareMeter > _meterLimit)
            _scareMeter = _meterLimit;
    }

    private void DecreaseScareMeter(float amount)
    {
        _scareMeter -= amount;
    }

    private bool CheckHit(RaycastHit raycastHit, Vector3 origin, ref bool isActivated, ref bool hasOneHit, ref GameObject hitObject, bool isLastRay)
    {
        if (raycastHit.collider.gameObject == null)
        {
            _currentElement = ConeElement.None;
            _visionCone.HitObject = null;
            isActivated = false;
            hasOneHit = false;
            return false;
        }

        if (raycastHit.collider.tag == _playerTag)
        {
            _currentElement = ConeElement.Player;
            isActivated = true;
            hasOneHit = true;
            return true;
        }
        if (raycastHit.collider.tag == _interactTag)
        {
            // Check for doors to open to open
            // if we are scared or alerted
            DoorBehavior doorBehavior =
                raycastHit.collider.GetComponentInParent<DoorBehavior>();
            if (doorBehavior && (_state == State.Alert || _state == State.Scared))
            {
                if (!doorBehavior.IsOpen)
                    doorBehavior.OpenDoor();
            }
            else if (doorBehavior && _state == State.Default)
            {
                if (Vector3.Distance(doorBehavior.gameObject.transform.position, transform.position) < 1.5f)
                        if (!doorBehavior.IsOpen)
                            doorBehavior.OpenDoor();
            }

            // Change cone-element to interactable
            BaseInteractable baseInteractable =
                raycastHit.collider.gameObject.GetComponentInParent<BaseInteractable>();

            if (baseInteractable)
            {
                _currentElement = ConeElement.Interactable;
                _visionCone.HitObject = raycastHit.collider.gameObject;
                isActivated = true;
                hasOneHit = true;
                return true;
            }
        }

        return false;
    }

    private void CreateSoundEffects()
    {
        for (int index = 0; index < _screamAudioClips.Count; ++index)
        {
            _screamAudioSources.Add(gameObject.AddComponent<AudioSource>());
            _screamAudioSources[index].spatialBlend = 0.95f;
            _screamAudioSources[index].clip = _screamAudioClips[index];
        }

        for (int index = 0; index < _gaspAudioClips.Count; ++index)
        {
            _gaspAudioSources.Add(gameObject.AddComponent<AudioSource>());
            _gaspAudioSources[index].spatialBlend = 1.0f;
            _gaspAudioSources[index].clip = _gaspAudioClips[index];
        }

        _footstepsAudioSource = gameObject.AddComponent<AudioSource>();
        _footstepsAudioSource.spatialBlend = 1.0f;
        _footstepsAudioSource.volume = 0.25f;
        _footstepsAudioSource.maxDistance = 0.1f;
        _footstepsAudioSource.clip = _footstepsAudioClip;
    }

    public void PlayScream()
    {
        int randomIndex =
            Random.Range(0, _screamAudioSources.Count);

        _screamAudioSources[randomIndex].pitch = Random.Range(0.8f, 1.2f);
        _screamAudioSources[randomIndex].Play();
    }

    public void PlayGasp()
    {
        int randomIndex =
            Random.Range(0, _screamAudioSources.Count);

        _gaspAudioSources[randomIndex].pitch = Random.Range(1.5f, 2.0f);
        _gaspAudioSources[randomIndex].Play();
    }

    private void PlayFootSteps()
    {
        float velMag = _navAgent.velocity.magnitude;
        //Debug.Log(velMag);
        if (velMag != 0)
        {
            if (!_footstepsAudioSource.isPlaying)
            {
                float pitch = Mathf.Max(velMag, 1.5f);
                pitch += Random.Range(-0.1f, 0.1f);
                _footstepsAudioSource.pitch = pitch;
                _footstepsAudioSource.PlayOneShot(_footstepsAudioClip);
            }
        }
    }

    private void PlayRFX()
    {
        if (!_alertTears || !_scaredTears)
            return;

        switch (_state)
        {
            case State.Alert:
                _alertTears.gameObject.SetActive(true);
                _alertIcon.SetActive(true);
                _scaredTears.gameObject.SetActive(false);
                _scaredIcon.SetActive(false);
                break;

            case State.Scared:
                _scaredTears.gameObject.SetActive(true);
                _scaredIcon.SetActive(true);
                _alertTears.gameObject.SetActive(false);
                _alertIcon.SetActive(false);
                break;

            case State.Terrified:
                _scaredTears.gameObject.SetActive(true);
                _scaredIcon.SetActive(true);
                _alertTears.gameObject.SetActive(false);
                _alertIcon.SetActive(false);
                break;

            case State.Default:
                _scaredTears.gameObject.SetActive(false);
                _scaredIcon.SetActive(false);
                _alertTears.gameObject.SetActive(false);
                _alertIcon.SetActive(false);
                break;
        };
    }

    private void PlayAnimations()
    {
        if (_animator.GetBool("isDancing"))
            return;

        float velMag = _navAgent.velocity.magnitude;
        switch (_state)
        {
            case State.Default:
                _animator.SetBool("isRunning", false);
                _animator.SetBool("isTerrified", false);
                _animator.SetBool("isWalking", velMag != 0);
                break;

            case State.Alert:
                _animator.SetBool("isRunning", false);
                _animator.SetBool("isTerrified", false);
                _animator.SetBool("isWalking", velMag != 0);
                break;

            case State.Scared:
                _animator.SetBool("isDancing", false);
                if (velMag != 0)
                {
                    _animator.SetBool("isRunning", true);
                    _animator.SetBool("isTerrified", false);
                }
                else
                {
                    _animator.SetBool("isRunning", false);
                    _animator.SetBool("isTerrified", true);
                }
                break;

            case State.Terrified:
                _animator.SetBool("isDancing", false);
                if (velMag != 0)
                {
                    _animator.SetBool("isRunning", true);
                    _animator.SetBool("isTerrified", false);
                }
                else
                {
                    _animator.SetBool("isRunning", false);
                    _animator.SetBool("isTerrified", true);
                }
                break;
        }
    }

    public void PlayJumpAnimation()
    {
        _animator.SetTrigger("jump");
    }

    public void StartDancing()
    {
        _scareMeter = 0;
        _state = State.Default;
        _animator.SetBool("isRunning", false);
        _animator.SetBool("isWalking", false);
        _animator.SetBool("isDancing", true);
        _navAgent.isStopped = true;
    }

    public void FallOver()
    {
        if (_currentElement == ConeElement.Player)
            return;
        else if (Random.Range(0.0f, 1.0f) < _fallingChance)
        {
            _state = State.Fallen;
            _navAgent.speed = 0.0f;
            Invoke("GetBackUp", _fallingTime);
            _animator.SetTrigger("fallForward");
        }
    }

    private void GetBackUp()
    {
        _state = State.Default;
        _navAgent.speed = _speed;
    }

    public void InteruptSearch()
    {
        if (_state == State.Alert)
        {
            _state = State.Default;
            AlertingObject = null;
        }
    }
}