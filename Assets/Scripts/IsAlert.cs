using System.Collections;
using System.Collections.Generic;
using Pada1.BBCore;
using UnityEngine;

namespace BBUnity.Conditions
{
    [Condition("IsAlert")]
    [Help("Check if the NPC is alert")]
    public class IsAlert : GOCondition
    {
        [InParam("npcBehavior")] public NPCBehavior _npcBehavior;
        [OutParam("alteringObject")] public GameObject _alertingObject;

        public override bool Check()
        {
            if (_npcBehavior.CurrentState == NPCBehavior.State.Alert
                && _npcBehavior.AlertingObject != null)
            {
                _alertingObject = _npcBehavior.AlertingObject;
                return true;
            }
            return false;
        }
    }
}