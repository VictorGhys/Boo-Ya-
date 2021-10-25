using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToiletBehavior : BaseInteractable
{
    private bool _isFlushing = false;

    public bool IsFlushing
    {
        get { return _isFlushing; }
    }

    // Audio
    [SerializeField] private AudioClip _flushingAudioClip = null;

    private AudioSource _flushAudioSource = null;

    // Timer
    [SerializeField] private float _flushTime = 15.0f;

    private float _flushTimer = 0.0f;

    // Effects
    private Animator _animator = null;

    [SerializeField] private List<ParticleSystem> _waterParticles = new List<ParticleSystem>();
    [SerializeField] private GameObject _splashEffect = null;

    // Puddle
    [SerializeField] private MeshRenderer _puddleRenderer;

    [SerializeField] private float _puddleAppearSpeed = 1f;

    // Player
    [SerializeField] private Rigidbody _playerRB;

    [SerializeField] private float _pushBackForce = 1f;

    // Shaking
    [SerializeField] private GameObject _gameObjectToShake;

    [SerializeField] private float _shakeSpeed = 1f;

    [SerializeField] private float _shakeAmount = 1f;

    private void Start()
    {
        // Create the audio source
        if (_flushingAudioClip)
        {
            _flushAudioSource = gameObject.AddComponent<AudioSource>();
            _flushAudioSource.clip = _flushingAudioClip;
            _flushAudioSource.loop = true;
            _flushAudioSource.spatialBlend = 1.0f;
        }
        else
            Debug.LogWarning($"{_flushingAudioClip.name} not found");

        // Effects
        if (_waterParticles.Count != 0)
        {
            foreach (ParticleSystem system in _waterParticles)
            {
                system.Stop();
            }
        }
        if (_splashEffect)
            DisableEffect();
        // Make puddle transparent
        if (_puddleRenderer)
        {
            _puddleRenderer.material.color = new Color(_puddleRenderer.material.color.r, _puddleRenderer.material.color.b, _puddleRenderer.material.color.b, 0);
        }

        // Animation
        _animator =
            GetComponentInChildren<Animator>();
        if (_animator)
            _animator.SetBool("isFlushing", false);

        // Delay
        _affectDelay = _flushTime;
    }

    // Update is called once per frame
    private void Update()
    {
        base.Update();

        if (_isFlushing)
        {
            _flushTimer += Time.deltaTime;
            if (_gameObjectToShake)
            {
                _gameObjectToShake.transform.Rotate(Vector3.up, Mathf.Sin(Time.time * _shakeSpeed) * _shakeAmount);
                _gameObjectToShake.transform.Rotate(Vector3.left, Mathf.Sin(Time.time * _shakeSpeed) * _shakeAmount);
            }
            float newAlpha = Mathf.Lerp(_puddleRenderer.material.color.a, 1f, Time.deltaTime * _puddleAppearSpeed);
            _puddleRenderer.material.color = new Color(_puddleRenderer.material.color.r,
                _puddleRenderer.material.color.b, _puddleRenderer.material.color.b, newAlpha);
        }

        // Check if flushing needs to be stopped
        if (_flushTimer >= _flushTime)
        {
            StopFlushing();
            _flushTimer = 0.0f;
        }

        // Flush the toilet if interaction takes place
        if (_isInteractedWith && !_isFlushing)
        {
            FlushToilet();
            _isInteractedWith = false;
            _interactingActor = null;
        }
        else if (_isInteractedWith && _isFlushing)
        {
            if (_interactingActor)
                if (_interactingActor.tag == _playerTag)
                {
                    _isInteractedWith = false;
                    return;
                }

            StopFlushing();
            _isInteractedWith = false;
        }
    }

    private void FlushToilet()
    {
        _isFlushing = true;
        if (_flushAudioSource)
            _flushAudioSource.Play();

        ScareNPCs();
        AlertNPCs();
        AlertHunters();

        // Play effects
        if (_animator)
            _animator.SetBool("isFlushing", _isFlushing);
        if (_splashEffect)
            _splashEffect.SetActive(true);
        if (_waterParticles.Count != 0)
        {
            foreach (ParticleSystem system in _waterParticles)
            {
                system.Play();
            }
        }

        // Push back player
        Vector3 force = (_playerRB.transform.position - transform.position).normalized * _pushBackForce;
        _playerRB.AddForce(force, ForceMode.Impulse);
    }

    private void StopFlushing()
    {
        _isFlushing = false;
        if (_flushAudioSource)
            _flushAudioSource.Stop();

        // Play effects
        if (_animator)
            _animator.SetBool("isFlushing", _isFlushing);
        if (_splashEffect)
            Invoke("DisableEffect", 0.4f);
        foreach (ParticleSystem system in _waterParticles)
        {
            system.Stop();
        }
    }

    private void DisableEffect()
    {
        _splashEffect.SetActive(false);
    }
}