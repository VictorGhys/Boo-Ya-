using System.Collections;
using System.Collections.Generic;
using Pada1.BBCore;
using UnityEngine;

namespace BBUnity.Conditions
{
    [Condition("IsScared")]
    [Help("Check if this NPC is scared")]
    public class IsScared : GOCondition
    {
        public override bool Check()
        {
            NPCBehavior npcBehavior = gameObject.GetComponent<NPCBehavior>();
            if (npcBehavior)
                return (npcBehavior.CurrentState == NPCBehavior.State.Scared);
            return false;
        }
    }
}

