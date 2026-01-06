using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public class ControllerWorldPointer : MonoBehaviour
{
    public bool useRight = true;
    public WorldSpacePanel panel;
    InputDevice controller;
    bool lastPrimary = false;

    void Start()
    {
        #if UNITY_EDITOR
        // Disable in Editor - requires VR controllers
        gameObject.SetActive(false);
        #else
        FindController();
        #endif
    }
    void FindController()
    {
        var devices = new List<InputDevice>();
        InputDeviceCharacteristics desired = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | (useRight ? InputDeviceCharacteristics.Right : InputDeviceCharacteristics.Left);
        InputDevices.GetDevicesWithCharacteristics(desired, devices);
        if (devices.Count > 0) controller = devices[0];
    }

    void Update()
    {
        if (!controller.isValid) FindController();
        if (!controller.isValid) return;

        bool primary = false;
        controller.TryGetFeatureValue(CommonUsages.primaryButton, out primary);

        // get controller pose
        Vector3 pos; Quaternion rot;
        if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out pos) && controller.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))
        {
            var origin = pos;
            var dir = rot * Vector3.forward;
            if (panel != null) panel.HandlePointer(origin, dir, primary && !lastPrimary);
        }

        lastPrimary = primary;
    }
}
