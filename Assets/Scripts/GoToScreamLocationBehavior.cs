using BBUnity.Actions;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;
using UnityEngine.AI;

[Action("MyActions/GoToScreamLocationBehavior")]
[Help("Go To Scream Location")]
public class GoToScreamLocationBehavior : GOAction
{
    private Vector3 _target;

    [InParam("GhostHunter")]
    private GhostHunter _ghostHunter;

    [InParam("nav agent")]
    private NavMeshAgent _navAgent;

    public override void OnStart()
    {
        if (!_ghostHunter)
            return;

        _navAgent.speed = _ghostHunter.HuntingSpeed;
        _navAgent.SetDestination(_ghostHunter.HearedScreamLocation);
        if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
        {
            _ghostHunter.HasHearedScream = false;
        }
    }

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
    }
}