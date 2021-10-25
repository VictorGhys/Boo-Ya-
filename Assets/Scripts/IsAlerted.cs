using Pada1.BBCore;


namespace BBUnity.Conditions
{
    [Condition("IsAlerted")]
    [Help("Returns if the ghost hunter is alerted")]
    public class IsAlerted : GOCondition
    {
        [InParam("GhostHunter")]
        private GhostHunter _ghostHunter;
        public override bool Check()
        {
            if (!_ghostHunter)
                return false;

            return _ghostHunter.IsAlerted;
        }
    }
}