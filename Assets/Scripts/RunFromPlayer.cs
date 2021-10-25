using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Vector3 = UnityEngine.Vector3;

namespace BBUnity.Actions
{
    [Action("RunFromPlayer")]
    [Help("NPC runs from the player if in a certain dangerzone")]
    public class RunFromPlayer : GOAction
    {
        [InParam("player")] public GameObject _player;
        [InParam("safeRadius")] public float _safeRadius;
        [InParam("doorOpeningDistance")] public float _doorOpeningDistance = 12.5f;
        [InParam("npcBehavior")] private NPCBehavior _npcBehavior;
        private NavMeshAgent _navAgent = null;

        public override void OnStart()
        {
            _navAgent = gameObject.GetComponent<NavMeshAgent>();

            if (!_player || !_navAgent)
                return;
            
            _navAgent.isStopped = false;
            //_navAgent.ResetPath();

            Vector3 playerPos = _player.transform.position;
            Vector3 myPos = gameObject.transform.position;
            Vector3 toPlayer = playerPos - myPos;

            Vector3 newPos = myPos;
            if (Physics.Raycast(myPos, -toPlayer, 3.0f))
                newPos = myPos - toPlayer;
            else
                newPos = myPos + toPlayer;

            if (Vector3.Distance(myPos, playerPos) < _safeRadius)
                _navAgent.SetDestination(newPos);
        }

        public override TaskStatus OnUpdate()
        {
            if (!_player || !_navAgent)
                return TaskStatus.FAILED;

            if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
                return TaskStatus.COMPLETED;
   
            CalculateNewDestination();
            return TaskStatus.RUNNING;
        }

        private void CalculateNewDestination()
        {
            Vector3 playerPos = _player.transform.position;
            Vector3 myPos = gameObject.transform.position;
            Vector3 toPlayer = playerPos - myPos;

            Vector3 newPos = new Vector3();
            if (Physics.Raycast(gameObject.transform.position, gameObject.transform.forward, 0.5f))
                newPos = myPos + gameObject.transform.forward;
            else
                newPos = myPos - (toPlayer.normalized * _safeRadius);

            if (Vector3.Distance(myPos, playerPos) < _safeRadius)
                _navAgent.SetDestination(newPos);
        }

        private void CheckForDoors()
        {
            if (!_npcBehavior)
                return;

            // Check if the visioncone hit a door
            if (_npcBehavior.CurrentElement == NPCBehavior.ConeElement.Door)
            {
                GameObject door =
                     _npcBehavior.VisionCone.HitObject;

                if (door)
                {
                    // Check if we are close enough to the door
                    float distanceToDoor =
                        Vector3.Distance(gameObject.transform.position, door.transform.position);

                    if (distanceToDoor < _doorOpeningDistance)
                    {
                        // Open the door
                        DoorBehavior doorBehavior =
                            door.GetComponentInParent<DoorBehavior>();
                        doorBehavior.OpenDoor();
                    }
                }
            }
        }
    }
}