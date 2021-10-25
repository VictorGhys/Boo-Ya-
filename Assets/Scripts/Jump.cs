using System.Collections;
using System.Collections.Generic;
using BBUnity.Actions;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;


namespace BBUnity.Actions
{
    [Action("Jump")]
    [Help("NPC jumps in the air")]
    public class Jump : GOAction
    {
        private Rigidbody _rigidbody = null;
        private float _jumpVelocity = 5.0f;
        private float _jumpHeight = 2.0f;
        private float _distanceToGround = 0.0f;

        private NavMeshAgent _navAgent = null;

        public override void OnStart()
        {
            _rigidbody = gameObject.GetComponent<Rigidbody>();

            if (!_rigidbody)
                Debug.LogWarning("No rigidbody found on NPC");

            _navAgent = 
                gameObject.GetComponent<NavMeshAgent>();

            if (!_navAgent)
                return;

            _navAgent.enabled = false;

            _distanceToGround = gameObject.transform.position.y;

            //_navAgent.isStopped = true;
            //_navAgent.updatePosition = false;
            //_navAgent.updateRotation = false;
        }

        public override TaskStatus OnUpdate()
        {
            if (!_rigidbody)
                return TaskStatus.ABORTED;

            if (Physics.Raycast(gameObject.transform.position, -Vector3.up, _jumpHeight))
            {
                //gameObject.transform.position += (Vector3.up * _jumpVelocity * Time.deltaTime);
                _rigidbody.AddForce(new Vector3(0, 1, 0), ForceMode.Impulse);
                return TaskStatus.RUNNING;
            }

            if (!Physics.Raycast(gameObject.transform.position, -Vector3.up, _jumpHeight / 2.0f))
            {
                _navAgent.enabled = true;
                return TaskStatus.COMPLETED;
            }

            return TaskStatus.RUNNING;
        }
    }
}

