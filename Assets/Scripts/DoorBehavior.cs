using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class DoorBehavior : BaseInteractable
{
    [SerializeField] private GameObject _door = null;
    public bool IsOpen { get; set; } = false;
    private Animator _animator;
    private NavMeshObstacle _obstacle;
    [SerializeField] private bool _automaticlyClose = false;
    [SerializeField] private float _automaticCloseTime = 1.0f;
    [SerializeField] private float _enableObstacleTime = 1.0f;
    private bool _isTimeToRoll = true;
    private bool _rollOutcome;
    [SerializeField] private float _resetRollTime = 5f;

    // Audio
    [SerializeField] private AudioSource _openDoorAudioSource = null;

    [SerializeField] private AudioSource _closeDoorAudioSource = null;
    [SerializeField] private int _minPitch = 90;
    [SerializeField] private int _maxPitch = 125;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _obstacle = GetComponentInChildren<NavMeshObstacle>();
        _obstacle.enabled = false;
    }

    private void Update()
    {
        base.Update();

        if (!_door)
            return;

        if (_isInteractedWith)
        {
            if (IsOpen)
                CloseDoor();
            else
                OpenDoor();
        }
    }

    public void CloseDoor()
    {
        if (_door)
        {
            _animator.SetBool("open", false);
            _obstacle.enabled = false;
        }

        IsOpen = false;
        _isInteractedWith = false;
        Invoke("PlayCloseDoorSound", 0.5f);
    }

    private void PlayCloseDoorSound()
    {
        float newPitch = Random.Range(_minPitch, _maxPitch) / 100.0f;
        _closeDoorAudioSource.pitch = newPitch;
        _closeDoorAudioSource.Play();
    }

    public void OpenDoor()
    {
        if (_door)
        {
            _animator.SetBool("open", true);
            IsOpen = true;
            Invoke("EnableObstacle", _enableObstacleTime);
            if (_automaticlyClose)
            {
                Invoke("CloseDoor", _automaticCloseTime);
            }
        }
        _isInteractedWith = false;

        float newPitch = Random.Range(_minPitch, _maxPitch) / 100.0f;
        _openDoorAudioSource.pitch = newPitch;
        _openDoorAudioSource.Play();
    }

    private void EnableObstacle()
    {
        _obstacle.enabled = true;
    }

    public bool WillOpenClosetDoor(GhostHunter ghostHunter)
    {
        if (_isTimeToRoll)
        {
            _isTimeToRoll = false;
            // roll if the ghosthunter will open the closet door or if he will ignore it
            if (Random.Range(1, 100) < ghostHunter.OpenClosetDoorChance)
            {
                _rollOutcome = true;
                Invoke("ResetRoll", _resetRollTime);
                return true;
            }
            else
            {
                //ghostHunter.OpenedClosets.Add(ghostHunter.VisionCone.HitObject.transform.parent.gameObject);
                _rollOutcome = false;
                Invoke("ResetRoll", _resetRollTime);
                return false;
            }
        }
        return _rollOutcome;
    }

    private void ResetRoll()
    {
        _isTimeToRoll = true;
    }
}