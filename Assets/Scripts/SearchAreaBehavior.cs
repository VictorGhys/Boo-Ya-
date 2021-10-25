using BBUnity.Actions;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

[Action("MyActions/SearchAreaBehavior")]
[Help("Search the area")]
public class SearchAreaBehavior : GOAction
{
    private Vector3 _target;

    [InParam("GhostHunter")]
    private GhostHunter _ghostHunter;

    [InParam("player")]
    private GameObject _playerCharacter;

    [InParam("nav agent")]
    private NavMeshAgent _navAgent;

    public override void OnStart()
    {
        if (!_ghostHunter)
            return;
        if (_ghostHunter.LastSeenPosition == Vector3.zero || _navAgent.remainingDistance <= _navAgent.stoppingDistance)
        {
            _target = _ghostHunter.LastSeenPosition;
            var inCircle = Random.insideUnitCircle;
            _target += new Vector3(inCircle.x, 0, inCircle.y) * _ghostHunter.SearchRadius;
            // set on the same height as the ghosthunter
            _target = new Vector3(_target.x, _ghostHunter.transform.position.y, _target.z);

            _navAgent.SetDestination(_target);
        }
    }

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
    }
}