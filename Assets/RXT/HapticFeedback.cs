using UnityEngine;
using UnityEngine.XR;

public class HapticsManager : MonoBehaviour
{
    private static InputDevice GetController(bool isRightHanded = true)
    {
        return InputDevices.GetDeviceAtXRNode(
            isRightHanded ? XRNode.RightHand : XRNode.LeftHand
        );
    }

    public static void CustomTriggerHaptic(float amplitude, float duration)
    {
        InputDevice Rightcontroller = GetController(true);
        InputDevice Leftcontroller = GetController(false);
        if (Rightcontroller.isValid && Rightcontroller.TryGetHapticCapabilities(out HapticCapabilities capabilities))
        {
            if (capabilities.supportsImpulse)
                Rightcontroller.SendHapticImpulse(0, amplitude, duration);
        }
        if (Leftcontroller.isValid && Leftcontroller.TryGetHapticCapabilities(out HapticCapabilities Capabilities))
        {
            if (Capabilities.supportsImpulse)
                Leftcontroller.SendHapticImpulse(0, amplitude, duration);
        }
    }

    public static void TriggerHaptic()
    {
        CustomTriggerHaptic(1f, 1f);
    }
}