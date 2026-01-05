using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(MRUKRoomExporter))]
public class ControllerInputExporter : MonoBehaviour
{
    public bool useRightController = true; // if false, use left
    public string exportButtonLabel = "Primary Button = Export";
    public string toggleServerLabel = "Secondary Button = Toggle View (AR/InRoom/DollHouse)";

    MRUKRoomExporter exporter;
    InputDevice controller;
    bool lastPrimary = false;
    bool lastSecondary = false;

    void Start()
    {
        exporter = GetComponent<MRUKRoomExporter>();
        FindController();
    }

    void FindController()
    {
        var devices = new List<InputDevice>();
        InputDeviceCharacteristics desiredChar = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller |
            (useRightController ? InputDeviceCharacteristics.Right : InputDeviceCharacteristics.Left);
        InputDevices.GetDevicesWithCharacteristics(desiredChar, devices);
        if (devices.Count > 0) controller = devices[0];
    }

    void Update()
    {
        if (!controller.isValid) FindController();
        if (!controller.isValid) return;

        bool primary = false;
        bool secondary = false;
        if (controller.TryGetFeatureValue(CommonUsages.primaryButton, out primary))
        {
            if (primary && !lastPrimary)
            {
                exporter.ExportAll();
                SendHaptic(0.1f, 0.2f);
            }
            lastPrimary = primary;
        }
        if (controller.TryGetFeatureValue(CommonUsages.secondaryButton, out secondary))
        {
            if (secondary && !lastSecondary)
            {
                var vm = GetComponent<ViewModeController>();
                if (vm != null) vm.ToggleMode();
                SendHaptic(0.05f, 0.15f);
            }
            lastSecondary = secondary;
        }
    }

    void SendHaptic(float amplitude, float duration)
    {
        if (!controller.isValid) return;
        HapticCapabilities caps;
        if (controller.TryGetHapticCapabilities(out caps) && caps.supportsImpulse)
        {
            uint channel = 0;
            controller.SendHapticImpulse(channel, amplitude, duration);
        }
    }

    void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Box(new Rect(10, 10, 450, 70), "Controller Exporter Controls");
        GUI.Label(new Rect(20, 30, 430, 20), exportButtonLabel);
        GUI.Label(new Rect(20, 50, 430, 20), toggleServerLabel);
    }
}
