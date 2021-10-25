using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR.Haptics;

public class ControllerShaker : MonoBehaviour
{
    static public void PlayRumble(float lowLeft, float highRight)
    {
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(lowLeft, highRight);
    }

    static public void StopRumble()
    {
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0, 0);
    }
}