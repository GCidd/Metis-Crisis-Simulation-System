using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

public class FireStarterMode : MouseModeSingleton<FireStarterMode>
{
    enum ModeStep { FireStarting };

    public Button uiButton;
    ModeStep currentStep = ModeStep.FireStarting;
    public GameObject fireObject;
    private List<GameObject> firesPlaced = new List<GameObject>();
    private Vector3 zoomOffset = Vector3.zero;
    private bool warned = false;

    // Start is called before the first frame update
    private void Awake()
    {
        m_Instance = this;
        fireObject = GameObject.Find("Tools").GetComponentInChildren<FireSpreading>().gameObject;
        uiButton = GameObject.Find("Fire Mode Button").GetComponent<Button>();
        uiButton.onClick.AddListener(delegate { MouseModeManager.Instance.EnableMode(Instance); });
    }
    public override void OnModeEnter()
    {
        uiButton.GetComponent<ToggleButtonImage>().SetState(true);
        fireObject.SetActive(true);
        isActive = true;
    }

    public void Update()
    {
        if (!CanUpdate())
            return;

        if (currentStep == ModeStep.FireStarting)
        {
            fireObject.transform.position = ObjectRelatedMouseWorldPos(fireObject);

            if (Input.GetMouseButtonDown(0))
            {
                if (!SimulationManager.Playing && !warned)
                {
                    UnityEvent placeFireEvent = new UnityEvent();
                    placeFireEvent.AddListener(PlaceFire);
                    PopupWindowManager.Instance.ShowOKDialog("Simulation not running", "Simulation has not started yet!\nFires will start when simulation starts.", onCloseEvent: placeFireEvent);
                    warned = true;
                    return;
                }
                else if (SimulationManager.Playing && !SimulationManager.Evacuating)
                {
                    EventManager.TriggerEvent("StartEvacuation");
                    PlaceFire();
                }
                else
                {
                    PlaceFire();
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                MouseModeManager.Instance.CancelMode();
            }
        }
    }
    void PlaceFire()
    {
        zoomOffset = Vector3.zero;
        GameObject newFire = GameObject.Instantiate(fireObject);
        Building parentBuilding = GetFireBuilding(newFire);
        parentBuilding.AddFire(newFire);
        newFire.transform.position = fireObject.transform.position;
        newFire.GetComponent<FireSpreading>().enabled = true;
        newFire.GetComponent<FireSpreading>().OnPlaced();
        EventManager.TriggerEvent("StartEvacuation");
    }
    public override void OnModeExit()
    {
        fireObject.SetActive(false);
        uiButton.GetComponent<ToggleButtonImage>().SetState(false);
        isActive = false;
    }
    Vector3 ObjectRelatedMouseWorldPos(GameObject _object)
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.WorldToScreenPoint(_object.transform.position).z;
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePoint);
        worldPoint.y = MouseModeManager.Instance.CurrentHeight;
        return worldPoint;
    }
    Building GetFireBuilding(GameObject _object)
    {
        GameObject buildingsContainer = GameObject.Find("Placed Buildings");
        if (buildingsContainer.transform.childCount == 0)
        {
            return null;
        }
        else
        {
            return buildingsContainer.transform.GetChild(0).GetComponent<Building>();
        }
        //Vector3 center = _object.transform.position;
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
