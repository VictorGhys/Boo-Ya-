using System.Collections;
using System.Collections.Generic;
using BBUnity.Conditions;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace BBUnity.Actions
{
    [Action("Wander")]
    [Help("Wander around the starting point")]
    public class Wander : GOAction
    {
        [InParam("npcBehavior")] private NPCBehavior _npcBehavior;
        [InParam("wanderRadius")] private float _wanderRadius = 2.5f;

        [InParam("minWanderDelay")] private int _minWanderDelay;
        [InParam("maxWanderDelay")] private int _maxWanderDelay;

        private static float _wanderTimer = 0.0f;
        private static float _wanderTime = 0.0f;

        private NavMeshAgent _navAgent = null;
        private Vector3 _originalPos;

        System.Random _random = new System.Random();


        public override void OnStart()
        {
            if (_npcBehavior)
            {
                _navAgent = _npcBehavior.GetComponent<NavMeshAgent>();
                _originalPos = _npcBehavior.OriginalPos;
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (_wanderTimer >= _wanderTime)
            {
                // Generate a new target position
                Vector3 toTarget = (new Vector3(_random.Next(-1, 1) * _wanderRadius, 0, _random.Next(-1, 1) * _wanderRadius));
                Vector3 target;
                // Check if the no objects blocks our path
                if (!Physics.Raycast(gameObject.transform.position, toTarget.normalized,
                    Vector3.Distance(gameObject.transform.position, toTarget)))
                    target = _originalPos + toTarget;
                else
                    target = _originalPos - toTarget;

                _navAgent.SetDestination(target);
                _wanderTime = _random.Next(_minWanderDelay, _maxWanderDelay);
                _wanderTimer = 0.0f;
            }
            else
                _wanderTimer += Time.deltaTime;

            return TaskStatus.COMPLETED;
        }
    }
}

