using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;
using Random = System.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerCharacter : BasicCharacter
{
    private PlayerControls _controls;
    private Vector2 _move;

    public bool _isInvisible;

    [SerializeField]
    private float _invisibleDuration = 5;

    private Material _originalMaterial;

    [SerializeField]
    private Material _invisibleMaterial;

    [SerializeField]
    private MeshRenderer _body;

    [SerializeField]
    private float _invisibiltyCooldown = 5;

    private float _currentInvisibilityCooldown;
    private bool _isInteracting = false;
    private bool _isScreaming = false;

    private float _booCooldown = 5;
    private float _currentBooCooldown;
    [SerializeField] private Transform _pfVisionCone;

    private VisionCone _booCone;

    [SerializeField] private const float _npcScareIncrease = 25.0f;
    [SerializeField] private const int _npcScoreIncrease = 250;
    [SerializeField] private const int _hunterScoreIncrease = 500;
    [SerializeField] private CameraBehavior _camera;

    private List<GameObject> _interactables = new List<GameObject>();

    [SerializeField]
    private Transform _pfScorePopUp;

    // Audio
    private AudioSource _booAudioSource = null;

    [SerializeField] private AudioClip _booAudioClip = null;
    private AudioSource _invivibleAudioSource = null;
    [SerializeField] private AudioClip _invisibleAudioClip = null;
    private AudioSource _visibleAudioSource = null;
    [SerializeField] private AudioClip _visibleAudioClip = null;

    private const int _minBooPitch = 90;
    private const int _maxBooPitch = 125;

    // HUD
    [SerializeField] private HUD _hud;

    [SerializeField] private bool _enableDebugRendering;

    // Size increase animation
    [SerializeField] private float _maxSizeAnimationTime = 2f;

    [SerializeField] private float _sizeAnimationSpeed = 1f;
    [SerializeField] private float _sizeScaleAmount = 1f;
    private float _sizeAnimationTime;

    // RFX
    [SerializeField] private ParticleSystem _smokePuff = null;

    [SerializeField] private ParticleSystem _invisibilityParticles = null;

    [SerializeField] private GameObject _booWaveObject = null;
    private Material _booWavMaterial = null;
    private float _booWavePercentage = 0.0f;
    private bool _booWaveFired = false;
    private const string _percentageVariable = "_Percent";

    // Rumble
    [SerializeField] private float _invisibleRumbleTime = 0.1f;

    [SerializeField] private float _invisibleLowLeftRumble = 0.3f;
    [SerializeField] private float _invisibleHighRightRumble = 0.3f;

    [SerializeField] private float _booRumbleTime = 0.1f;
    [SerializeField] private float _booLowLeftRumble = 0.3f;
    [SerializeField] private float _booHighRightRumble = 0.3f;

    [SerializeField] private float _interactRumbleTime = 0.1f;
    [SerializeField] private float _interactLowLeftRumble = 0.3f;
    [SerializeField] private float _interactHighRightRumble = 0.3f;

    [SerializeField] private float _growSpeed = 1f;

    // NPC push
    [SerializeField] private const float _npcPushForce = 10.0f;

    protected override void Awake()
    {
        base.Awake();

        // Controls
        _controls = new PlayerControls();
        _controls.GamePlay.Interact.performed += ctx => Interact();
        //_controls.GamePlay.Interact.canceled += ctx => Interact();
        _controls.GamePlay.Boo.performed += ctx => _isScreaming = true;
        _controls.GamePlay.Boo.canceled += ctx => _isScreaming = false;
        _controls.GamePlay.Invisible.performed += ctx => Invisible();
        _controls.GamePlay.Movement.performed += ctx => _move = ctx.ReadValue<Vector2>();
        _controls.GamePlay.Movement.canceled += ctx => _move = Vector2.zero;

        // Invisibility
        _originalMaterial = _body.material;
        _isInvisible = false;
        _currentInvisibilityCooldown = _invisibiltyCooldown;
        _currentBooCooldown = _booCooldown;

        // Vision cone
        _booCone = Instantiate(_pfVisionCone, null).GetComponent<VisionCone>();
        _booCone.HitDelegate = ScreamBoo;
        _booCone._layerMask = LayerMask.GetMask("AI", "Wall", "Default");
        _booCone.GetComponent<MeshRenderer>().enabled = _enableDebugRendering;

        // Audio
        CreateSoundEffects();

        //HUD
        _hud.SetMaxInvisibleCoolDown(_invisibiltyCooldown);
        _hud.SetMaxBooCoolDown(_booCooldown);

        Time.timeScale = 1f;

        // RFX
        if (_booWaveObject)
        {
            _booWavMaterial = _booWaveObject.GetComponent<MeshRenderer>().material;
            _booWavMaterial.SetFloat(_percentageVariable, 0.0f);

            _booWaveObject.gameObject.SetActive(false);
        }

        if (_smokePuff)
            _smokePuff.Stop();
        if (_invisibilityParticles)
            _invisibilityParticles.Stop();
    }

    private void Update()
    {
        HandleMovementInput();
        _currentInvisibilityCooldown += Time.deltaTime;
        _currentBooCooldown += Time.deltaTime;
        _hud.SetInvisibleCoolDown(_currentInvisibilityCooldown);
        _hud.SetBooCoolDown(_currentBooCooldown);

        _booCone.SetAimDirection(transform.forward);
        _booCone.SetOrigin(transform.position);

        UpdateShockWave();

        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, _growSpeed * Time.deltaTime);
        }

        if (_sizeAnimationTime >= 0)
        {
            if (_sizeAnimationTime > _maxSizeAnimationTime * 0.5f)
            {
                // First half of the animation
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * _sizeScaleAmount, _sizeAnimationSpeed * Time.deltaTime);
            }
            else
            {
                // Second half of the animation
                transform.localScale = Vector3.Lerp(Vector3.one, transform.localScale, _sizeAnimationSpeed * Time.deltaTime);
            }
            _sizeAnimationTime -= Time.deltaTime;
        }
    }

    private void HandleMovementInput()
    {
        Vector3 forward = new Vector3(_camera.transform.forward.x, 0, _camera.transform.forward.z).normalized;
        Vector3 right = new Vector3(_camera.transform.right.x, 0, _camera.transform.right.z).normalized;
        Vector3 movement = _move.x * right + _move.y * forward;

        _movementBehaviour.DesiredMovementDirection = movement;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interact"))
        {
            _interactables.Add(other.gameObject);
            other.gameObject.GetComponent<BaseInteractable>().PlayerSeesObject = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interact"))
        {
            _interactables.Remove(other.gameObject);
            other.gameObject.GetComponent<BaseInteractable>().PlayerSeesObject = false;
        }
    }

    private void Interact()
    {
        BaseInteractable closestInteractable = null;
        float closestDistance = Single.MaxValue;
        foreach (var interactable in _interactables)
        {
            if (!interactable)
                continue;
            BaseInteractable baseInteractable = interactable.gameObject.GetComponent<BaseInteractable>();
            if (!baseInteractable)
                continue;
            if (Vector3.Distance(gameObject.transform.position, baseInteractable.transform.position) < closestDistance)
            {
                closestInteractable = baseInteractable;
            }
        }

        if (closestInteractable != null)
        {
            closestInteractable.IsInteractedWith = true;
            closestInteractable.InteractingActor = gameObject;
            ControllerShaker.PlayRumble(_interactLowLeftRumble, _interactHighRightRumble);
            Invoke("StopRumble", _invisibleRumbleTime);
        }
    }

    private void Invisible()
    {
        // Don't go invisible if you are on cooldown.
        if (_currentInvisibilityCooldown >= _invisibiltyCooldown)
        {
            if (_isInvisible)
            {
                MakeVisible();
            }
            else
            {
                _isInvisible = true;
                Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Wall"), LayerMask.NameToLayer("Player"), true);
                _body.material = _invisibleMaterial;

                // Make visible again after invisible duration.
                Invoke("MakeVisible", _invisibleDuration);
                _hud.UpdatePlayerIcon(true);
                // Play sound and particles
                _invivibleAudioSource.Play();
                _smokePuff.Play();
                _invisibilityParticles.Play();
                ControllerShaker.PlayRumble(_invisibleLowLeftRumble, _invisibleHighRightRumble);
                Invoke("StopRumble", _invisibleRumbleTime);
            }
        }
    }

    private void MakeVisible()
    {
        if (_isInvisible)
        {
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Wall"), LayerMask.NameToLayer("Player"), false);
            _body.material = _originalMaterial;

            _isInvisible = false;
            _currentInvisibilityCooldown = 0;

            // Play sound and particles
            _visibleAudioSource.Play();
            _smokePuff.Play();
            _invisibilityParticles.Stop();
            _hud.UpdatePlayerIcon(false);
        }
    }

    public void Kill()
    {
        if (gameObject)
        {
            // Notify LevelManager
            LevelManager.Instance.OnPlayerDeath();

            // Destroy objects
            _booCone.GetComponent<MeshRenderer>().enabled = false;
            Destroy(_booCone);
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EnableControls();
    }

    public void EnableControls()
    {
        _controls.GamePlay.Enable();
    }

    private void OnDisable()
    {
        DisableControls();
        StopRumble();
    }

    public void DisableControls()
    {
        _controls.GamePlay.Disable();
    }

    private bool ScreamBoo(RaycastHit raycastHit, Vector3 origin, ref bool isActivated, ref bool hasOneHit, ref GameObject hitObject, bool isLastRay)
    {
        // Don't scream boo if you are on cooldown.
        if (_currentBooCooldown < _booCooldown)
        {
            //Debug.Log($"can't scream boo, because of cooldown, wait {_booCooldown - _currentBooCooldown} more seconds");
            _isScreaming = false;
            return true;
        }

        if (raycastHit.collider.CompareTag("NPC"))
        {
            isActivated = true;
            hasOneHit = true;
            if (_isScreaming)
            {
                if (_isInvisible)
                {
                    return true;
                }

                // Play the boo sound and wave
                PlayBooSound();
                PlayBooWave();

                // Alert the hunters
                LevelManager.Instance.AlertClosestGhostHunter(transform.position);

                // Make the npc scared
                NPCBehavior npcBehavior = raycastHit.collider.GetComponent<NPCBehavior>();
                npcBehavior.IncreaseScareMeter(_npcScareIncrease);
                _isScreaming = false;
                _currentBooCooldown = 0;

                // Play the scream sound
                npcBehavior.PlayScream();

                // Alert close hunters
                LevelManager.Instance.AlertClosestGhostHunter(transform.position);

                // Increase the score
                int increase = (int)(npcBehavior.ScareMeter / 100.0f * _npcScoreIncrease);
                int multipliedScore = GameManager.Instance.IncreaseScore(increase);

                // Display score popup
                ScorePopUp.Create(_pfScorePopUp, npcBehavior.transform.position + Vector3.up, multipliedScore);

                // Push back NPC
                Vector3 pushForce = new Vector3(transform.forward.x * _npcPushForce,
                    transform.forward.y * _npcPushForce, transform.forward.z * _npcPushForce);
                npcBehavior.gameObject.GetComponent<Rigidbody>().
                    AddForce(pushForce.x, pushForce.y, pushForce.z, ForceMode.Impulse);

                // Fall over
                npcBehavior.FallOver();

                // Do camera zoom
                _camera.IsZooming = true;

                // Play rumble
                ControllerShaker.PlayRumble(_booLowLeftRumble, _booHighRightRumble);
                Invoke("StopRumble", _booRumbleTime);
                // Play size increase animation
                _sizeAnimationTime = _maxSizeAnimationTime;
                transform.localScale = Vector3.one;
                return true;
            }
            return true;
        }
        if (raycastHit.collider.CompareTag("GhostHunter"))
        {
            isActivated = true;
            hasOneHit = true;

            // Check if we are behind the hunter
            GameObject ghostHunter = raycastHit.collider.gameObject;
            Vector3 playerForward = transform.forward;
            Vector3 hunterForward = ghostHunter.transform.forward;

            if (_isScreaming &&
                Vector3.Dot(hunterForward, playerForward) > 0)
            {
                PlayBooSound();
                PlayBooWave();
                _camera.GetComponent<CameraBehavior>().IsZooming = true;
                LevelManager.Instance.AlertClosestGhostHunter(transform.position);
                int multipliedScore = GameManager.Instance.IncreaseScore(_hunterScoreIncrease);
                ScorePopUp.Create(_pfScorePopUp, ghostHunter.transform.position + Vector3.up, multipliedScore);
                ghostHunter.GetComponent<GhostHunter>().PlayJumpAnimation();
                ghostHunter.GetComponent<GhostHunter>().PlayScaredSound();
                _isScreaming = false;
                _currentBooCooldown = 0;
                return true;
            }
        }
        return true;
    }

    private void UpdateShockWave()
    {
        if (_booWaveFired)
        {
            _booWavePercentage += Time.deltaTime;
            _booWavMaterial.SetFloat(_percentageVariable, _booWavePercentage);
            if (_booWavePercentage >= 1.0f)
            {
                _booWavePercentage = 0.0f;
                _booWaveFired = false;
                _booWaveObject.gameObject.SetActive(false);
            }
        }
    }

    private void PlayBooWave()
    {
        _booWaveFired = true;
        _booWaveObject.gameObject.SetActive(true);
    }

    private void CreateSoundEffects()
    {
        // Boo sound
        _booAudioSource = gameObject.AddComponent<AudioSource>();
        _booAudioSource.spatialBlend = 0.75f;
        if (!_booAudioClip)
        {
            Debug.LogWarning($"{_booAudioClip.name} not found");
            return;
        }
        _booAudioSource.clip = _booAudioClip;

        // Sounds for becoming invisible
        _invivibleAudioSource = gameObject.AddComponent<AudioSource>();
        _invivibleAudioSource.spatialBlend = 0.75f;
        if (!_invisibleAudioClip)
        {
            Debug.LogWarning($"{_invisibleAudioClip.name} not found");
            return;
        }
        _invivibleAudioSource.clip = _invisibleAudioClip;

        // Sound for becoming visible again
        _visibleAudioSource = gameObject.AddComponent<AudioSource>();
        _visibleAudioSource.spatialBlend = 0.75f;
        if (!_visibleAudioClip)
        {
            Debug.LogWarning($"{_visibleAudioClip.name} not found");
            return;
        }
        _visibleAudioSource.clip = _visibleAudioClip;
    }

    private void PlayBooSound()
    {
        Random r = new Random();
        float newPitch = r.Next(_minBooPitch, _maxBooPitch) / 100.0f;
        _booAudioSource.pitch = newPitch;
        _booAudioSource.Play();
    }

    private void StopRumble()
    {
        ControllerShaker.StopRumble();
    }
}