using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BaseInteractable : MonoBehaviour
{
    // Interaction
    protected bool _isInteractedWith = false;

    public bool IsInteractedWith
    {
        get { return _isInteractedWith; }
        set
        {
            if (_cooldownTimer > _cooldownTime)
            {
                _isInteractedWith = value;
                _cooldownTimer = 0.0f;
            }
        }
    }

    // Room
    private int _roomId = -1;

    public int RoomId
    {
        get { return _roomId; }
        set { _roomId = value; }
    }

    // Cooldown (interacting)
    private float _cooldownTimer = 0.0f;

    [SerializeField] private float _cooldownTime = 0.25f;
    [SerializeField] private float _scareIncrease = 25.0f;
    [SerializeField] private float _scareDistance = float.MaxValue;
    [SerializeField] private int _scoreIncrease = 100;

    // Cooldown (scaring and alerting)
    private bool _canScareNPC = false;

    private bool _canAlertNPC = false;

    private float _affectTimer = 0.0f;
    [SerializeField] protected float _affectDelay = 2.5f;

    // Alerting hunter
    [SerializeField] private const float _detectionDistance = 15.0f;

    [SerializeField] private float _alertRadius = 10.0f;

    // Interacting actor
    protected GameObject _interactingActor = null;

    // Tags
    protected const string _playerTag = "Player";

    // Popup
    [SerializeField]
    private Transform _pfScorePopUp;

    // Rumble
    [SerializeField] private float _RumbleTime = 0.3f;

    [SerializeField] private float _LowLeftRumble = 0.3f;
    [SerializeField] private float _HighRightRumble = 0.3f;

    // _outline
    private Outline _outline = null;

    public bool PlayerSeesObject = false;
    private float _outlineTimer = 0.0f;
    [SerializeField] private const float _outlineTime = 0.0f;

    // Interacting actor
    public GameObject InteractingActor
    {
        get { return _interactingActor; }
        set { _interactingActor = value; }
    }

    protected void Awake()
    {
        _outline = GetComponentInChildren<Outline>();
        if (!_outline)
            _outline = GetComponentInParent<Outline>();

        if (_outline)
            _outline.enabled = false;
    }

    protected void Update()
    {
        _cooldownTimer += Time.deltaTime;
        UpdateOutline();
        UpdateDelay();
    }

    private void UpdateOutline()
    {
        if (!_outline)
            return;

        if (PlayerSeesObject)
        {
            _outlineTimer += Time.deltaTime;
            _outline.enabled = true;
        }
        else
            _outline.enabled = false;

        //if (_outlineTimer >= _outlineTime)
        //{
        //    PlayerSeesObject = false;
        //    _outlineTimer = 0.0f;
        //}
    }

    private void UpdateDelay()
    {
        _affectTimer += Time.deltaTime;

        if (_affectTimer >= _affectDelay)
        {
            _canScareNPC = true;
            _canAlertNPC = true;
        }
    }

    protected void ScareNPCs()
    {
        // Check if we can scare the NPC
        if (!_canScareNPC)
            return;

        _canScareNPC = false;
        _affectTimer = 0.0f;

        // Check if the player is interacting
        // with this object
        if (_interactingActor)
        {
            if (_interactingActor.tag != _playerTag)
                return;
        }

        // Get the NPCs in this room
        List<GameObject> npcList =
            LevelManager.Instance.GetAllNPCsInRoom(_roomId);

        // Check the distances to the NPCs
        foreach (var npc in npcList)
        {
            if (Vector3.Distance(this.transform.position, npc.transform.position) < _scareDistance)
            {
                NPCBehavior npcBehavior = npc.GetComponent<NPCBehavior>();

                // Scare the NPC
                npcBehavior.IncreaseScareMeter(_scareIncrease);

                // Increase the score
                int increase = (int)((npcBehavior.ScareMeter / 100.0f) * _scoreIncrease);
                int multipliedScore = GameManager.Instance.IncreaseScore(increase);
                // Display score popup
                ScorePopUp.Create(_pfScorePopUp, npcBehavior.transform.position + Vector3.up, multipliedScore);
                // Play rumble
                ControllerShaker.PlayRumble(_LowLeftRumble, _HighRightRumble);
                Invoke("StopRumble", _RumbleTime);
            }
        }
    }

    protected void AlertNPCs()
    {
        // Check if we can alert the NPC
        if (!_canAlertNPC)
            return;

        _canAlertNPC = false;
        _affectTimer = 0.0f;

        // Check if the player is interacting
        // with this object
        if (_interactingActor)
        {
            if (_interactingActor.tag != _playerTag)
                return;
        }

        // Get the NPCs in this room
        List<GameObject> npcList =
            LevelManager.Instance.GetAllNPCsInRoom(_roomId);

        // Check the distances to the NPCs
        foreach (var npc in npcList)
        {
            if (Vector3.Distance(this.transform.position, npc.transform.position) < _scareDistance)
            {
                NPCBehavior npcBehavior = npc.GetComponent<NPCBehavior>();
                if (npcBehavior.CurrentState != NPCBehavior.State.Scared &&
                    npcBehavior.CurrentState != NPCBehavior.State.Terrified)
                {
                    npcBehavior.CurrentState = NPCBehavior.State.Alert;
                    npcBehavior.AlertingObject = gameObject;

                    npcBehavior.PlayGasp();
                }
            }
        }
    }

    protected void AlertHunters()
    {
        // Check if a hunter is close enough
        GameObject hunter =
            LevelManager.Instance.GetClosestGhostHunter(transform.position);

        if (!hunter)
            return;

        if (Vector3.Distance(transform.position, hunter.transform.position) > _detectionDistance)
            return;

        // Generate a position close to this object
        Vector3 alertPos = transform.position;
        alertPos += (Random.insideUnitSphere * _alertRadius);

        // Alert the hunters
        LevelManager.Instance.AlertClosestGhostHunter(alertPos);
    }

    private void StopRumble()
    {
        ControllerShaker.StopRumble();
    }
}