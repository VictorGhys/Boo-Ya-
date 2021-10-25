using System.Collections;
using System.Collections.Generic;
using BBUnity.Actions;
using Pada1.BBCore;             // Code attributes
using Pada1.BBCore.Tasks;       // TaskStatus
using UnityEngine;
using UnityEngine.AI;

[Action("MyActions/AttackBehavior")]
[Help("Attack the player")]
public class AttackBehavior : GOAction
{
    [InParam("player")]
    private GameObject _playerCharacter;

    [InParam("GhostHunter")]
    private GhostHunter _ghostHunter;

    private Transform _targetTransform;

    [InParam("nav agent")]
    private NavMeshAgent _navAgent;

    public override void OnStart()
    {
        if (_playerCharacter == null)
        {
            return;
        }
        _targetTransform = _playerCharacter.transform;
        _ghostHunter.LastSeenPosition = _targetTransform.position;

        _navAgent.SetDestination(_targetTransform.position);
        _navAgent.isStopped = false;
    }

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
    }
}