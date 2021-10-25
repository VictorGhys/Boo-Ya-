using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class NavigationDebugger : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent _agentToDebug;
    private LineRenderer _lineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_agentToDebug.hasPath)
        {
            _lineRenderer.positionCount = _agentToDebug.path.corners.Length;
            _lineRenderer.SetPositions(_agentToDebug.path.corners);
            _lineRenderer.enabled = true;
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }
}
