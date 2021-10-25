using System.Collections;
using System.Collections.Generic;
using BBUnity.Actions;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;

namespace BBUnity.Actions
{   
    [Action("PerformInteraction")]
    [Help("Perform interaction with the alerting object")]
    public class PerformInteraction : GOAction
    {
        [InParam("npcBehavior")] public NPCBehavior _npcBehavior;
        private Animator _animator = null;

        public override void OnStart()
        {
            base.OnStart();
            _animator = gameObject.GetComponentInChildren<Animator>();
        }

        public override TaskStatus OnUpdate()
        {

            BaseInteractable interactable = 
                    _npcBehavior.AlertingObject.GetComponent<BaseInteractable>();

            if (!interactable)
                return TaskStatus.ABORTED;

            ToiletBehavior toiletBehavior =
                _npcBehavior.AlertingObject.GetComponent<ToiletBehavior>();

            SinkBathBehavior sinkBathBehavior =
                _npcBehavior.AlertingObject.GetComponent<SinkBathBehavior>();

            LightSwitchBehavior lightSwitchBehavior =
                _npcBehavior.AlertingObject.GetComponent<LightSwitchBehavior>();

            if (toiletBehavior || sinkBathBehavior || lightSwitchBehavior)
            {
                if (toiletBehavior)
                    if (!toiletBehavior.IsFlushing)
                    {
                        _npcBehavior.CurrentState = NPCBehavior.State.Default;
                        _npcBehavior.AlertingObject = null;
                        return TaskStatus.ABORTED;
                    }
                if (sinkBathBehavior)
                    if (!sinkBathBehavior.IsFlowing)
                    {
                        _npcBehavior.CurrentState = NPCBehavior.State.Default;
                        _npcBehavior.AlertingObject = null;
                        return TaskStatus.ABORTED;
                    }

                interactable.IsInteractedWith = true;
                _npcBehavior.CurrentState = NPCBehavior.State.Default;
                _npcBehavior.AlertingObject = null;

                if (_animator) _animator.SetTrigger("flickLightswitch");

                return TaskStatus.COMPLETED;
            }

            return TaskStatus.NONE;
        }
    }
}

