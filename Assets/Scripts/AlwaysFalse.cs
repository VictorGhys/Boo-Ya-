using System.Collections;
using System.Collections.Generic;
using Pada1.BBCore;
using Pada1.BBCore.Actions;
using Pada1.BBCore.Framework;
using UnityEngine;

[Condition("MyConditions/AlwaysFalse")]
[Help("Returns false")]
public class AlwaysFalse : ConditionBase
{
    public override bool Check()
    {
        return false;
    }
    
}
