using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate bool HitDelegate(RaycastHit raycastHit, Vector3 origin, ref bool isActivated, ref bool hasOneHit, ref GameObject hitObject, bool isLastRay);

public class VisionCone : MonoBehaviour
{
    public LayerMask _layerMask;
    private Mesh _mesh;
    [SerializeField] private float _fov = 90f;
    private Vector3 _origin;
    private float _startingAngle = 0;
    [SerializeField] public float _viewDistance = 10f;
    [SerializeField] private float _coneHeight = 0.3f;
    [SerializeField] private int _rayCount = 15;

    [SerializeField] private Material _activatedMaterial;

    private Material _originalMaterial;

    private GameObject _hitObject;

    public GameObject HitObject
    {
        get
        {
            return _hitObject;
        }
        set
        {
            _hitObject = value;
        }
    }

    private bool _isActivated = false;

    public bool IsActivated
    {
        get
        {
            return _isActivated;
        }
        set
        {
            _isActivated = value;
        }
    }

    public HitDelegate HitDelegate { get; set; }

    // Start is called before the first frame update
    private void Start()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;

        _originalMaterial = GetComponentInChildren<MeshRenderer>().material;
    }

    private void OnDrawGizmos()
    {
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        _mesh.Clear();

        // Cone mesh.
        float angle = _startingAngle;
        float angleIncrease = _fov / _rayCount;

        Vector3[] vertices = new Vector3[_rayCount + 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[_rayCount * 3];

        vertices[0] = _origin;

        int vertexIndex = 1;
        int triangleIndex = 0;
        bool hasOneHit = false;
        for (int i = 0; i < _rayCount; i++)
        {
            Vector3 vertex;
            RaycastHit raycastHit;

            if (Physics.Raycast(_origin, GetVectorFromAngle(angle) * _viewDistance, out raycastHit, _viewDistance, _layerMask))
            {
                // The ray hit an object.
                // Call the delegate function, it returns true if the hit was a valid hit.
                if (HitDelegate.Invoke(raycastHit, _origin, ref _isActivated, ref hasOneHit, ref _hitObject, i == _rayCount - 1))
                {
                    // Valid hit. The vision cone changes to the hit position
                    vertex = raycastHit.point - transform.position;

                    if (IsActivated)
                    {
                        //HitObject = raycastHit.collider.gameObject;
                        GetComponentInChildren<MeshRenderer>().material = _activatedMaterial;
                    }
                }
                else
                {
                    // Not a valid hit. The vision cone doesn't change
                    vertex = _origin + GetVectorFromAngle(angle) * _viewDistance;
                }
            }
            else
            {
                // No hit.
                vertex = _origin + GetVectorFromAngle(angle) * _viewDistance;
            }

            vertices[vertexIndex] = vertex;

            if (i > 0)
            {
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;

                triangleIndex += 3;
            }

            vertexIndex++;
            angle -= angleIncrease;
        }
        // Deactivate the vision cone if he didn't hit any thing important
        if (!hasOneHit && IsActivated)
        {
            IsActivated = false;
            GetComponentInChildren<MeshRenderer>().material = _originalMaterial;
        }

        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
        _mesh.bounds = new Bounds(_origin, Vector3.one * 1000f);
        _mesh.RecalculateNormals();
    }

    public void SetAimDirection(Vector3 aimDirection)
    {
        _startingAngle = GetAngleFromVectorFloat(aimDirection) + _fov / 2f;
    }

    public void SetOrigin(Vector3 position)
    {
        _origin = position + new Vector3(0, _coneHeight, 0);
    }

    private Vector3 GetVectorFromAngle(float angle)
    {
        // Angle = 0 -> 360.
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad));
    }

    public float GetAngleFromVectorFloat(Vector3 dir)
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        if (n < 0)
        {
            n += 360;
        }
        return n;
    }

    public void SetFOV(int fov)
    {
        _fov = fov;
    }
}