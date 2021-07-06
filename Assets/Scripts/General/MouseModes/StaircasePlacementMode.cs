using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class StaircasePlacementMode : GrabModeSingleton<StaircasePlacementMode>
{
    [SerializeField] private GameObject staircaseObject;
    private List<GameObject> placedObjects = new List<GameObject>();
    // private float snapDistance = 1.5f;

    private void Awake()
    {
        m_Instance = this;
        placedContainerName = "Staircases";
        if (uiButton == null)
            uiButton = GameObject.Find("Staircase Mode Button").GetComponent<Button>();
        if (staircaseObject == null)
            staircaseObject = GameObject.Find("Tools").transform.Find("Staircase").gameObject;
        uiButton.onClick.AddListener(delegate { MouseModeManager.Instance.PlaceNewObject("Staircase", staircaseObject); });
    }
    public override void OnModeEnter(GameObject _object)
    {
        uiButton.GetComponent<ToggleButtonImage>().SetState(true);
        grabbedObject = _object;
        grabbedObject.SetActive(true);
        initialObjectPosition = Vector3.zero;
        currentStep = ModeStep.ObjectPlacement;
        isActive = true;
    }
    public override void OnModeExit()
    {
        CancelChanges();
        uiButton.GetComponent<ToggleButtonImage>().SetState(false);
        isActive = false;
    }
    protected new void CancelChanges()
    {
        if (grabbedObject == null)
            return;
        grabbedObject.SetActive(false);
        base.CancelChanges();
    }
    public override void PlaceObject()
    {
        GameObject newObject = GameObject.Instantiate(grabbedObject);
        Transform parentBuilding = GetObjectBuilding(grabbedObject);
        newObject.name = grabbedObject.name;
        parentBuilding.GetComponent<Building>().AddStaticObject(grabbedObject);
        newObject.transform.position = grabbedObject.transform.position;
        newObject.GetComponent<StaticObject>().OnPlaced();
    }
}
