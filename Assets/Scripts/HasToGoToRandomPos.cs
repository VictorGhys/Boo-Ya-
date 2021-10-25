using BBUnity.Conditions;
using Pada1.BBCore;
using UnityEngine;

[Condition("MyConditions/HasToGoToRandomPos")]
[Help("Returns if the ghost hunter goes to a random pos")]
public class HasToGoToRandomPos : GOCondition
{
    [InParam("GhostHunter")]
    private GhostHunter _ghostHunter;

    [OutParam("random pos")]
    private Vector3 _outRandomPos;

    public override bool Check()
    {
        if (!_ghostHunter)
            return false;
        if (!_ghostHunter.GoToRandomPos)
        {
            _outRandomPos = Vector3.zero;
        }
        return _ghostHunter.GoToRandomPos;
    }
}