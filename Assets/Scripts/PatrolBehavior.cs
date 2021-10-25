using BBUnity.Actions;
using Pada1.BBCore;             // Code attributes
using Pada1.BBCore.Tasks;       // TaskStatus
using UnityEngine;

[Action("MyActions/PatrolBehavior")]
[Help("Patrol using the patrol points")]
public class PatrolBehavior : GOAction
{
    [InParam("GhostHunter")]
    private GhostHunter _ghostHunter;
    public override TaskStatus OnUpdate()
    {
        if (_ghostHunter)
        {
            _ghostHunter.DoPatroling();
        }
        else
        {
            Debug.LogError("ghost hunter was null in patrol behavior!");
        }
        // The action is completed. We must inform the execution engine.
        return TaskStatus.COMPLETED;

    }
}