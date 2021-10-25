using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowableBehavior : BaseInteractable
{
    // Throwing
    private Rigidbody _rigidbody = null;

    [SerializeField] private GameObject _player = null;
    [SerializeField] private float _impulseStrengthForward = 3;
    [SerializeField] private float _impulseStrengthUp = 1;
    [SerializeField] private bool _breakable = false;
    private bool _isGrounded = false;
    [SerializeField] private float _breakingStrength = 1.0f;

    // Audio
    [SerializeField] private AudioSource _impactAudioSource = null;

    [SerializeField] private AudioSource _breakingAudioSource = null;
    [SerializeField] private AudioSource _throwAudioSource = null;

    // Particles
    [SerializeField] private ParticleSystem _smokePuff = null;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag(_playerTag);
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        base.Update();

        bool impulseCondition =
        _player && _rigidbody && _isInteractedWith;
        if (impulseCondition)
        {
            ScareNPCs();

            Vector3 impulseDirection = _player.transform.forward * _impulseStrengthForward;
            Vector3 impulse = impulseDirection + Vector3.up * _impulseStrengthUp;

            _rigidbody.AddForce(impulse, ForceMode.Impulse);

            if (_throwAudioSource)
                _throwAudioSource.Play();

            _isGrounded = false;
            _isInteractedWith = false;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!_interactingActor)
            return;

        if (!_isGrounded && _interactingActor.tag == _playerTag)
        {
            // Is this object breakable and was it thrown hard enough
            if (_breakable && _rigidbody.velocity.magnitude > _breakingStrength)
            {
                // Alert the hunters
                AlertHunters();

                // Play particles and sound
                if (_smokePuff)
                    Instantiate(_smokePuff, gameObject.transform.position, gameObject.transform.rotation);

                // Play audio
                if (_breakingAudioSource)
                    _breakingAudioSource.Play();

                // Destroy this object
                Invoke("Destroy", 0.33f);
            }
            else
            {
                if (_impactAudioSource)
                    _impactAudioSource.Play();
            }

            _isGrounded = true;
            _isInteractedWith = false;

            _interactingActor = null;
        }
    }

    private void Destroy()
    {
        gameObject.SetActive(false);
    }

    private void Break()
    {
        Destroy(gameObject);
    }

    private void CreateSoundEffects()
    {
    }
}