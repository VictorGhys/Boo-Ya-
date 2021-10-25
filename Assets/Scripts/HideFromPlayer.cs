using System;
using System.Collections;
using System.Collections.Generic;
using BBUnity.Actions;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace BBUnity.Actions
{
    [Action("HideFromPlayer")]
    [Help("Generate possible hiding points and go there")]
    public class HideFromPlayer : GOAction
    {
        [InParam("player")] public GameObject _player;
        [InParam("npcBehavior")] public NPCBehavior _npcBehavior;
        [InParam("searchRadius")] public float _searchRadius;
        [InParam("searchIterations")] public int _searchIterations;
        private NavMeshAgent _navAgent;
        private System.Random _random;
        private Vector3 _hidingSpot;
        private bool _spotFound;
        public override void OnStart()
        {
            _navAgent = gameObject.GetComponent<NavMeshAgent>();
            _random = new System.Random();
            _spotFound = false;

            _navAgent.isStopped = false;
            _navAgent.ResetPath();

            // Find hiding spot
            bool spotFound = false;
            for (int count = 0; count < _searchIterations; ++count)
            {
                // Generate random point in the radios
                Vector3 randomSpot = gameObject.transform.position;
                Vector3 randomDirection = Random.insideUnitSphere * _searchRadius;
                randomSpot += randomDirection;

                RaycastHit hit;
                if (!Physics.Raycast(randomSpot, _player.transform.position, out hit))
                {
                    _spotFound = true;
                    _hidingSpot = hit.point;
                    _navAgent.SetDestination(_hidingSpot);
                    break;
                }
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (Physics.Raycast(_hidingSpot, _player.transform.position))
                return TaskStatus.COMPLETED;

            if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
                return TaskStatus.COMPLETED;
            else if (_navAgent.destination != _hidingSpot)
                _navAgent.SetDestination(_hidingSpot);
            return TaskStatus.RUNNING;
        }
    }
}

