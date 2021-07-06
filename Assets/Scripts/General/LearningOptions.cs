using Unity.MLAgents.Policies;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LearningOptions
{
    Transform optionsWindow;

    Slider decisionPeriodSlider;
    private static float decisionPeriod = 20;
    public static int DecisionPeriod { get { return (int)decisionPeriod; } }

    Toggle takeActionsBetweenDecisionsToggle;
    private static bool takeActionsBetweenDecisions;
    public static bool TakeActionsBetweenDecisions { get { return takeActionsBetweenDecisions; } }

    Dropdown deviceDropdown;
    private static InferenceDevice device;
    public static InferenceDevice Device { get { return device; } }

    InputField maxStepsInput;
    private static float maxSteps = 10000f;
    public static int MaxSteps { get { return (int)maxSteps; } }


    public void Initialize(Transform optionsWindow)
    {
        this.optionsWindow = optionsWindow;
        Transform optionsContainer = optionsWindow.Find("Options").GetChild(0).GetChild(0);

        decisionPeriodSlider = optionsContainer.Find("Decision Period Option").GetComponentInChildren<Slider>();
        decisionPeriodSlider.onValueChanged.AddListener(delegate { decisionPeriod = decisionPeriodSlider.value; EventManager.TriggerEvent("DecisionPeriodChanged"); });

        takeActionsBetweenDecisionsToggle = optionsContainer.Find("Take Actions Between Decision Option").GetComponentInChildren<Toggle>();
        takeActionsBetweenDecisionsToggle.onValueChanged.AddListener(delegate { takeActionsBetweenDecisions = takeActionsBetweenDecisionsToggle.isOn; EventManager.TriggerEvent("TakeActionsBetweenDecisionsChanged"); });

        deviceDropdown = optionsContainer.Find("Inference Device Option").GetComponentInChildren<Dropdown>();
        deviceDropdown.onValueChanged.AddListener(delegate { device = (InferenceDevice)deviceDropdown.value; EventManager.TriggerEvent("InferenceDeviceChanged"); });

        maxStepsInput = optionsContainer.Find("Max Steps Option").GetComponentInChildren<InputField>();
        maxStepsInput.onValueChanged.AddListener(delegate { float.TryParse(maxStepsInput.text, out maxSteps); EventManager.TriggerEvent("MaxStepsChanged"); });
    }

}
