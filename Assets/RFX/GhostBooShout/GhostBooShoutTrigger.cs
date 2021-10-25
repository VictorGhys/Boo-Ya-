using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostBooShoutTrigger : MonoBehaviour
{
    private Material _Mat;
    private float _Percentage;
    public float Duration = 1;
    private bool _Fired = false;

    // Start is called before the first frame update
    void Start()
    {
        if (TryGetComponent(out MeshRenderer renderer))
            _Mat = renderer.material;
        else
        {
            Debug.LogError(message: "cant get meshrenderer from booshout mesh");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_Fired)
        {
            _Percentage += Time.deltaTime / Duration;
            _Mat.SetFloat(name: "Percent", _Percentage);
            if(_Percentage > 1)
            {
                _Percentage = 0;
                _Fired = false;
            }
        }
        _Mat.SetFloat(name: "_Percent", _Percentage);

        if (Input.GetKeyDown(name:"space"))
        {
            _Percentage = 0;
            _Fired = true;
        }
    }
}
