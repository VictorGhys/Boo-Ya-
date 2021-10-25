using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinkBathBehavior : BaseInteractable
{
    private bool _isFlowing = false;
    public bool IsFlowing
    {
        get { return _isFlowing; }
    }

    // Audio
    [SerializeField] private AudioClip _waterAudioClip = null;

    private AudioSource _waterAudioSource = null;

    // Timer
    [SerializeField] private float _waterTime = 15.0f;

    private float _waterTimer = 0.0f;

    // Effects
    [SerializeField] private GameObject _waterEffect = null;

    private void Start()
    {
        // Create the audio source
        if (_waterAudioClip)
        {
            _waterAudioSource = gameObject.AddComponent<AudioSource>();
            _waterAudioSource.clip = _waterAudioClip;
            _waterAudioSource.loop = true;
            _waterAudioSource.spatialBlend = 1.0f;
        }
        else
            Debug.LogWarning($"{_waterAudioClip.name} not found");

        // Effects
        if (_waterEffect)
            _waterEffect.SetActive(false);

        // Delay
        _affectDelay = _waterTime;
    }

    // Update is called once per frame
    private void Update()
    {
        base.Update();

        if (_isFlowing)
            _waterTimer += Time.deltaTime;

        // Check if water needs to be stopped
        if (_waterTimer >= _waterTime)
        {
            StopFlowing();
            _waterTimer = 0.0f;
        }

        // Flow water if interaction takes place
        if (_isInteractedWith && !_isFlowing)
        {
            StartFlowing();
            _isInteractedWith = false;
            _interactingActor = null;
        }
        else if (_isInteractedWith && _isFlowing)
        {
            if (_interactingActor)
                if (_interactingActor.tag == _playerTag)
                {
                    _isInteractedWith = false;
                    return;
                }

                    StopFlowing();
            _isInteractedWith = false;
        }
    }

    private void StartFlowing()
    {
        _isFlowing = true;
        if (_waterAudioSource)
            _waterAudioSource.Play();

        _waterEffect.SetActive(true);

        ScareNPCs();
        AlertNPCs();
        AlertHunters();
    }

    private void StopFlowing()
    {
        _isFlowing = false;
        if (_waterAudioSource)
            _waterAudioSource.Stop();
        _waterEffect.SetActive(false);
    }
}
