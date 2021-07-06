using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;
using System.ComponentModel;

public class NewObjectPlacementMode : GrabModeSingleton<NewObjectPlacementMode>
{
    [SerializeField] private GameObject placeableObjectsParent;
    private List<GameObject> placeableObjects = new List<GameObject>();
    private void Awake()
    {
        m_Instance = this;
        if (placeableObjectsParent == null)
            placeableObjectsParent = GameObject.Find("Placeable Objects");
        UnpackPlaceableObjects();
    }
    void UnpackPlaceableObjects()
    {
        foreach (Transform child in placeableObjectsParent.transform)
        {
            placeableObjects.Add(child.gameObject);
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
        CancelChanges();
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
        parentBuilding.GetComponent<Building>().AddStaticObject(newObject);
        newObject.transform.position = grabbedObject.transform.position;
        newObject.GetComponent<StaticObject>().OnPlaced();
    }
}
