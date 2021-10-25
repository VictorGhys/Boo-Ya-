using BBUnity.Actions;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[Action("MyActions/InspectClosetBehavior")]
[Help("Inspect a closet")]
public class InspectClosetBehavior : GOAction
{
    [InParam("GhostHunter")]
    private GhostHunter _ghostHunter;

    private Vector3 _target;

    [InParam("nav agent")]
    private NavMeshAgent _navAgent;

    [InParam("OpenClosetDistance")]
    private float _openClosetDistance = 1.0f;

    public override void OnStart()
    {
        if (!_ghostHunter)
            return;
        GameObject closet = _ghostHunter.VisionCone.HitObject;
        if (closet)
        {
            // Target is right in front of the closet
            _target = closet.transform.parent.parent.position;

            // set on the same height as the ghosthunter
            _target = new Vector3(_target.x, _ghostHunter.transform.position.y, _target.z);

            _navAgent.SetDestination(_target);
            if (Vector3.Distance(_ghostHunter.transform.position, _target) < _openClosetDistance)
            {
                // Open the closet door
                GameObject parent = closet.transform.parent.gameObject;
                DoorBehavior closetDoor = parent.GetComponentInChildren<DoorBehavior>();
                if (!closetDoor)
                {
                    Debug.LogError("closet door was null in inspect closet behavior!");
                    return;
                }

                closetDoor.IsInteractedWith = true;
                _ghostHunter.OpenedClosets.Add(_ghostHunter.VisionCone.HitObject.transform.parent.gameObject);
                _ghostHunter.VisionCone.HitObject = null;
                //Have a look inside the closet
                _navAgent.SetDestination(closet.transform.position);
            }
        }
    }

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
    }
}