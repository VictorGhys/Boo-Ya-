using BBUnity.Actions;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;
using UnityEngine.AI;

[Action("Navigation/GoToRandomPosition")]
[Help("Gets a random position from a given area and moves the game object to that point by using a NavMeshAgent")]
public class GoToRandomPos : GOAction
{
    [InParam("GhostHunter")]
    private GhostHunter _ghostHunter;

    [InParam("nav agent")]
    private NavMeshAgent _navAgent;

    [InParam("random pos")]
    private Vector3 _randomPos;

    [OutParam("random pos")]
    private Vector3 _outRandomPos;

    [InParam("area")]
    [Help("game object that must have a BoxCollider or SphereColider, which will determine the area from which the position is extracted")]
    private GameObject area;

    // Start is called before the first frame update
    public override void OnStart()
    {
        if (_randomPos == Vector3.zero)
        {
            _randomPos = GetRandomPos();
            _navAgent.SetDestination(_randomPos);
        }
        if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
        {
            //_ghostHunter.GoToRandomPos = false;
            _ghostHunter.ResetGoToRandomPosGhostHunter();
            _randomPos = Vector3.zero;
        }
        _outRandomPos = _randomPos;
    }

    private Vector3 GetRandomPos()
    {
        BoxCollider boxCollider = area != null ? area.GetComponent<BoxCollider>() : null;
        if (boxCollider != null)
        {
            return new Vector3(Random.Range(area.transform.position.x - area.transform.localScale.x * boxCollider.size.x * 0.5f,
                    area.transform.position.x + area.transform.localScale.x * boxCollider.size.x * 0.5f),
                area.transform.position.y,
                Random.Range(area.transform.position.z - area.transform.localScale.z * boxCollider.size.z * 0.5f,
                    area.transform.position.z + area.transform.localScale.z * boxCollider.size.z * 0.5f));
        }
        return Vector3.zero;
    }

    // Update is called once per frame
    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
    }
}