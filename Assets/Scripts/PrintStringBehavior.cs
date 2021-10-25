using Pada1.BBCore;
using Pada1.BBCore.Framework;
using Pada1.BBCore.Tasks;
using UnityEngine;

[Action("MyActions/PrintStringBehavior")]
[Help("Prints string")]
public class PrintStringBehavior : BasePrimitiveAction
{
    [InParam("String")]
    public string _string;

    public override TaskStatus OnUpdate()
    {
        Debug.Log(_string);
        return TaskStatus.COMPLETED;
    }
}
