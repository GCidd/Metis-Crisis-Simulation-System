using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

public class PedestrianPlacementMode : GrabModeSingleton<PedestrianPlacementMode>
{
    public string currentObjectName;
    private List<GameObject> pedestrians = new List<GameObject>();
    private List<GameObject> placedPedestrians = new List<GameObject>();
    private void Awake()
    {
        m_Instance = this;
        placedContainerName = "Pedestrians";
    }
    public override void OnModeEnter(GameObject _object)
    {
        grabbedObject = _object;
        grabbedObject.SetActive(true);
        initialObjectPosition = Vector3.zero;
        grabbedObject.GetComponent<Rigidbody>().useGravity = false;
        currentStep = ModeStep.ObjectPlacement;
        isActive = true;
    }

    public override void OnModeExit()
    {
        grabbedObject.SetActive(false);
        currentObjectName = "";
        base.CancelChanges();
        isActive = false;
    }

    public override void PlaceObject()
    {
        GameObject newPedestrian = GameObject.Instantiate(grabbedObject);
        Transform parentBuilding = GetObjectBuilding(grabbedObject);
        if (parentBuilding == null)
        {
            PopupWindowManager.Instance.ShowOKDialog("No building placed/built", "Please create a building or place a pre-built one before placing pedestrians!");
            return;
        }
        parentBuilding.GetComponent<Building>().AddPedestrian(newPedestrian);
        newPedestrian.name = grabbedObject.name;
        newPedestrian.transform.position = grabbedObject.transform.position;
        newPedestrian.GetComponent<Pedestrian>().OnPlaced();
        placedPedestrians.Add(newPedestrian);
    }
    protected new Transform GetObjectBuilding(GameObject _object)
    {
        // for now just return the first building
        if (GameObject.Find("Placed Buildings").transform.childCount == 0)
            return null;
        return GameObject.Find("Placed Buildings").transform.GetChild(0);

        //Vector3 center = _object.transform.position + Vector3.up;
        //List<GameObject> colls = new List<GameObject>();

        //colls.AddRange(Physics.RaycastAll(center, Vector3.right, 500f, LayerMask.GetMask("Walls")).Select(h => h.transform.parent.parent.gameObject));

        //if (colls.Count % 2 == 0)
        //    return null;

        //List<GameObject> uniqBuildings = colls.Distinct().ToList();

        //if (uniqBuildings.Count == 1)
        //    return uniqBuildings[0].transform;
        //else
        //{
        //    List<GameObject> buildings = uniqBuildings.FindAll(b => colls.FindAll(c => c == b).Count % 2 == 1).ToList();
        //    if (buildings.Count > 0)
        //        return buildings[0].transform;
        //}

        //return null;
    }


}
