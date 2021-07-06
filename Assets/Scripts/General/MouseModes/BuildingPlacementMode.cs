using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildingPlacementMode : GrabModeSingleton<BuildingPlacementMode>
{
    public string currentObjectName;
    [SerializeField] private GameObject placeableBuildingsContainer;
    private List<GameObject> placeableBuildings = new List<GameObject>();
    private List<GameObject> placedBuildings = new List<GameObject>();

    private void Awake()
    {
        m_Instance = this;
        if (placeableBuildingsContainer == null)
            placeableBuildingsContainer = GameObject.Find("Placeable Buildings");
        UnpackPlaceableObjects();
    }
    void UnpackPlaceableObjects()
    {
        foreach (Transform child in placeableBuildingsContainer.transform)
        {
            placeableBuildings.Add(child.gameObject);
        }
    }

    public override void OnModeEnter(GameObject _object)
    {
        if (grabbedObject != null)
            grabbedObject.SetActive(false);
        grabbedObject = _object;
        grabbedObject.SetActive(true);
        initialObjectPosition = Vector3.zero;
        currentStep = ModeStep.ObjectPlacement;
        isActive = true;
    }
    public override void OnModeExit()
    {
        grabbedObject.SetActive(false);
        initialObjectPosition = grabbedObject.transform.position;
        currentObjectName = "";
        base.CancelChanges();
    }

    public override void PlaceObject()
    {
        if (GameObject.Find("Placed Buildings").transform.childCount > 0)
        {
            string windowMessage = "System currently supports only one placed building. The previous one will be destroyed.";
            UnityEvent destroyPreviousBuildingEvent = new UnityEvent();
            destroyPreviousBuildingEvent.AddListener(delegate { GameObject.Destroy(GameObject.Find("Placed Buildings").transform.GetChild(0).gameObject); Debug.Log("Destroying"); });
            PopupWindowManager.Instance.ShowOKDialog("Another building already placed", windowMessage, onCloseEvent: destroyPreviousBuildingEvent);
        }

        GameObject newBuilding = GameObject.Instantiate(grabbedObject);
        newBuilding.transform.position = grabbedObject.transform.position;
        newBuilding.transform.parent = GameObject.Find("Placed Buildings").transform;
        newBuilding.GetComponent<Building>().OnPlaced();
        placedBuildings.Add(newBuilding);
    }
}
