using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DoubleDoorPlacementMode : MouseModeSingleton<DoubleDoorPlacementMode>
{

    enum ModeStep { Placement };
    ModeStep currentStep;
    public Button uiDoubleDoorButton;


    public DoubleDoor doubleDoor;
    private GameObject lastReplacedWallPart;
    private Vector3 lastReplacedWallPartPosition = Vector3.zero;

    public void Awake()
    {
        m_Instance = this;
        doubleDoor = GameObject.Find("Tools").GetComponentInChildren<DoubleDoor>();
        uiDoubleDoorButton = GameObject.Find("DoubleDoor Mode Button").GetComponent<Button>();
        uiDoubleDoorButton.onClick.AddListener(delegate { MouseModeManager.Instance.EnableMode(Instance); });
    }

    public override void OnModeEnter()
    {
        currentStep = ModeStep.Placement;
        uiDoubleDoorButton.GetComponent<ToggleButtonImage>().SetState(true);
        doubleDoor.gameObject.SetActive(true);
        isActive = true;
    }
    public void Update()
    {
        if (!CanUpdate())
            return;

        if (currentStep == ModeStep.Placement)
        {
            // snap to other wall that is near the cursor
            doubleDoor.transform.position = ObjectRelatedMouseWorldPos();
            Transform closestRelative = doubleDoor.GetComponent<DoubleDoor>().GetWallToSnap();
            if (closestRelative != null)
            {
                //Debug.Log(closestRelative.position + " " + doubleDoor.Width / 2);
                doubleDoor.transform.position = closestRelative.position + doubleDoor.transform.forward * doubleDoor.Width / 2;
                doubleDoor.transform.rotation = closestRelative.rotation;
                if (lastReplacedWallPart != closestRelative)
                {
                    if (lastReplacedWallPart != null)
                        lastReplacedWallPart.GetComponent<Renderer>().enabled = true;   // re-activate previous wall part
                    lastReplacedWallPart = closestRelative.gameObject; // get new wall part that will be de-activated
                    lastReplacedWallPart.GetComponent<Renderer>().enabled = false;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    // place double door
                    GameObject newDoor = GameObject.Instantiate(doubleDoor.gameObject);
                    newDoor.transform.position = doubleDoor.transform.position;
                    newDoor.name = doubleDoor.name;
                    newDoor.transform.SetSiblingIndex(lastReplacedWallPart.transform.GetSiblingIndex());
                    lastReplacedWallPart.transform.parent.parent.GetComponent<Building>().AddDoor(newDoor);
                    newDoor.GetComponent<DoubleDoor>().OnPlaced();
                    // remove replaced wall from list and destroy it
                    GameObject.Destroy(lastReplacedWallPart);
                    lastReplacedWallPart = null;
                }
            }
            else
            {
                if (lastReplacedWallPart != null)
                {
                    lastReplacedWallPart.GetComponent<Renderer>().enabled = true;
                    lastReplacedWallPart = null;
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                MouseModeManager.Instance.CancelMode();
            }
        }
    }

    public override void OnModeExit()
    {
        if (lastReplacedWallPart != null)
        {
            lastReplacedWallPart.GetComponent<Renderer>().enabled = true;
            lastReplacedWallPart.transform.localScale = Vector3.zero;
        }
        uiDoubleDoorButton.GetComponent<ToggleButtonImage>().SetState(false);
        doubleDoor.gameObject.SetActive(false);
        isActive = false;
    }

    private bool CanBePlaced(Vector3 groundSnap, Vector3 wallSnap)
    {
        if (groundSnap == -Vector3.one && wallSnap == -Vector3.one)
        {
            return false;
        }
        return true;
    }
    Vector3 ObjectRelatedMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.WorldToScreenPoint(doubleDoor.transform.position).z;
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePoint);
        worldPoint.y = MouseModeManager.Instance.CurrentHeight;
        return worldPoint;
    }
}
