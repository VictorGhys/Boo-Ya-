using System.Collections;
using System.Collections.Generic;
using BBUnity.Conditions;
using Pada1.BBCore;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace BBUnity.Conditions
{
    [Condition("IsLightSwitchInSight")]
    [Help("Returns true if a lightswitch is inside the visioncone")]
    public class IsLightSwitchInSight : GOCondition
    {
        [InParam("npcBehavior")] private NPCBehavior _npcBehavior;
        [OutParam("lightSwitchBehavior")] public LightSwitchBehavior _lightSwitchBehavior;
        [OutParam("lightSwitch")] public GameObject _lightSwitch;

        public override bool Check()
        {
            if (_npcBehavior.CurrentElement == NPCBehavior.ConeElement.Interactable)
            {
                _lightSwitch = _npcBehavior.VisionCone.HitObject;
                _lightSwitchBehavior = _lightSwitch.GetComponent<LightSwitchBehavior>();
                _npcBehavior.CurrentElement = NPCBehavior.ConeElement.None;
                return true;
            }

            return false;
        }
    }
}

