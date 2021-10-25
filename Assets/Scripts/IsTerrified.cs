using System.Collections;
using System.Collections.Generic;
using BBUnity.Conditions;
using Pada1.BBCore;
using UnityEngine;
using UnityEngine.AI;

namespace BBUnity.Conditions
{
    [Condition("IsTerrified")]
    [Help("Check if this NPC is terrified")]
    public class IsTerrified : GOCondition
    {
        public override bool Check()
        {
            NPCBehavior npcBehavior = gameObject.GetComponent<NPCBehavior>();
            if (npcBehavior)
                if (npcBehavior.CurrentState == NPCBehavior.State.Terrified)
                {
                    npcBehavior.GetComponent<NavMeshAgent>().isStopped = false;
                    return true;
                }

                return false;
        }
    }
}
