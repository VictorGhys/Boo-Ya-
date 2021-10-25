using Pada1.BBCore;

namespace BBUnity.Conditions
{
    [Condition("MyConditions/HasSeenPlayer")]
    [Help("Returns if the ghost hunter has seen a player")]
    public class HasSeenPlayer : GOCondition
    {
        [InParam("GhostHunter")]
        private GhostHunter _ghostHunter;
        public override bool Check()
        {
            if (!_ghostHunter)
                return false;
            
            return _ghostHunter.VisionCone.IsActivated;
        }
    }
}