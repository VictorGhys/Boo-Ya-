using Pada1.BBCore;

namespace BBUnity.Conditions
{
    [Condition("HearedNPCScream")]
    [Help("Returns if the ghost hunter has heared a NPC scream")]
    public class HearedNPCScream : GOCondition
    {
        [InParam("GhostHunter")]
        private GhostHunter _ghostHunter;

        public override bool Check()
        {
            if (!_ghostHunter)
                return false;

            return _ghostHunter.HasHearedScream;
        }
    }
}