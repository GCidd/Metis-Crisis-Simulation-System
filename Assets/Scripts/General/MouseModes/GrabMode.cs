using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrabMode : GrabModeSingleton<GrabMode>
{
    private void Awake()
    {
        m_Instance = this;
        if (uiButton == null)
            this.uiButton = GameObject.Find("Grab Mode Button").GetComponent<Button>();

        this.uiButton.onClick.AddListener(delegate { MouseModeManager.Instance.EnableMode(Instance); });
    }
    public override void OnModeEnter()
    {
        currentStep = ModeStep.ObjectSelection;
        uiButton.GetComponent<ToggleButtonImage>().SetState(true);
        isActive = true;
    }

    public override void OnModeEnter(GameObject _object)
    {
        currentStep = ModeStep.ObjectSelection;
        uiButton.GetComponent<ToggleButtonImage>().SetState(true);
        PickUpObject(_object);
        isActive = true;
    }
}
