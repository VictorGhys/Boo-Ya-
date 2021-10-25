using Pada1.BBCore;
using UnityEngine;

namespace BBUnity.Conditions
{
    [Condition("MyConditions/HasSeenCloset")]
    [Help("Returns if the ghost hunter has seen a closet")]
    public class HasSeenCloset : GOCondition
    {
        [InParam("GhostHunter")]
        private GhostHunter _ghostHunter;

        public override bool Check()
        {
            if (!_ghostHunter)
                return false;
            if (!_ghostHunter.VisionCone.HitObject)
            {
                return false;
            }

            GameObject hitCloset = _ghostHunter.VisionCone.HitObject;
            GameObject parent = _ghostHunter.VisionCone.HitObject.transform.parent.gameObject;
            if (hitCloset.CompareTag("Closet") && !_ghostHunter.OpenedClosets.Contains(parent))
            {
                DoorBehavior closetDoor = parent.GetComponentInChildren<DoorBehavior>();
                if (!closetDoor.IsOpen)
                {
                    if (closetDoor.WillOpenClosetDoor(_ghostHunter))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}