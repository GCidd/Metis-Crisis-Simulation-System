using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class FloorBuildingMode : MouseModeSingleton<FloorBuildingMode>
{
    enum ModeStep { FloorBuilding, FloorExtention };
    [SerializeField] private Button uiButton;
    ModeStep currentStep;
    public GameObject floorObject;
    private float floorWidth = 1;
    private List<GameObject> floorParts = new List<GameObject>();
    private List<GameObject> placedFloorParts = new List<GameObject>();

    private Vector3 extentionDragStartPosition;
    private Vector3 extentionDragEndPosition;

    private void Awake()
    {
        m_Instance = this;
        floorObject = GameObject.Find("Floor");
        floorObject.SetActive(false);
        uiButton.onClick.AddListener(delegate { MouseModeManager.Instance.EnableMode(Instance); });
        floorWidth = floorObject.GetComponent<MeshRenderer>().bounds.size.x;
    }
    public override void OnModeEnter()
    {
        currentStep = ModeStep.FloorBuilding;
        floorObject.SetActive(true);
        uiButton.GetComponent<ToggleButtonImage>().SetState(true);
        isActive = true;
    }
    public void Update()
    {
        if (!CanUpdate())
            return;

        if (currentStep == ModeStep.FloorBuilding)
        {
            floorObject.transform.position = ObjectRelatedMouseWorldPos();
            if (Input.GetKey(KeyCode.LeftControl))
            {
                // snap to "grid" (rounds the coordinates of the wall part to be placed)
                floorObject.transform.position = new Vector3((int)floorObject.transform.position.x, floorObject.transform.position.y, (int)floorObject.transform.position.z);
            }
            Vector3 ground = floorObject.GetComponent<Floor>().CheckGround();
            if (floorObject.GetComponent<Floor>().CanBePlaced() && MouseModeManager.Instance.CurrentLevel == 0)
                floorObject.transform.position = new Vector3(floorObject.transform.position.x, ground.y, floorObject.transform.position.z);

            if (!Input.GetKey(KeyCode.LeftShift))
                floorObject.GetComponent<Floor>().SnapPosition();

            if (floorObject.GetComponent<Floor>().CanBePlaced())
            {
                floorObject.GetComponent<Floor>().UnHighlightError();
                if (Input.GetMouseButtonDown(0))
                {
                    // Place wall start
                    currentStep = ModeStep.FloorExtention;
                    extentionDragStartPosition = MousePositionOnPlane();
                }
            }
            else
            {
                floorObject.GetComponent<Floor>().HighlightError();
            }
        }
        else if (currentStep == ModeStep.FloorExtention)
        {
            Vector3 newPosition = MousePositionOnPlane();
            if (newPosition != extentionDragEndPosition)
            {
                foreach (GameObject fc in floorParts)
                {
                    GameObject.Destroy(fc);
                }
                floorParts.Clear();

                extentionDragEndPosition = newPosition;
                float diffX = extentionDragEndPosition.x - extentionDragStartPosition.x, diffZ = extentionDragEndPosition.z - extentionDragStartPosition.z;
                Vector3 xDirection = diffX > 0 ? Vector3.right : Vector3.left;
                Vector3 zDirection = diffZ > 0 ? Vector3.forward : Vector3.back;
                for (int x = 0; x < Mathf.Abs(diffX) / floorWidth; x++)
                {
                    for (int z = 0; z < Mathf.Abs(diffZ) / floorWidth; z++)
                    {
                        GameObject newFloor = GameObject.Instantiate(floorObject);
                        newFloor.transform.position = floorObject.transform.position + zDirection * z * floorWidth + xDirection * x * floorWidth;
                        floorParts.Add(newFloor);
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                placedFloorParts.AddRange(floorParts);
                Transform parentBuilding = GetFloorBuilding(floorParts);
                for (int i = 0; i < floorParts.Count; i++)
                {
                    floorParts[i].GetComponent<Floor>().OnPlaced();
                    floorParts[i].name = floorObject.name;
                    parentBuilding.GetComponent<Building>().AddFloor(floorParts[i]);
                }
                floorParts.Clear();
                currentStep = ModeStep.FloorBuilding;
            }
        }
        if (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentStep == ModeStep.FloorBuilding)
                MouseModeManager.Instance.CancelMode();
            else
                CancelExtension();
        }
    }
    public override void OnModeExit()
    {
        CancelExtension();
        floorObject.SetActive(false);
        uiButton.GetComponent<ToggleButtonImage>().SetState(false);
        isActive = false;
    }
    void CancelExtension()
    {
        foreach (GameObject f in floorParts)
        {
            GameObject.Destroy(f);
        }
        floorParts.Clear();
        currentStep = ModeStep.FloorBuilding;
    }
    Transform GetFloorBuilding(List<GameObject> floors)
    {
        return GameObject.FindObjectOfType<Building>().transform;

        //float x = floors.Select(f => f.transform.position.x).Sum() / floors.Count;
        //float z = floors.Select(f => f.transform.position.z).Sum() / floors.Count;

        //Vector3 center = new Vector3(x, 1.5f, z) + Vector3.right * 500f;
        //List<GameObject> colls = new List<GameObject>();

        //colls.AddRange(Physics.RaycastAll(center, Vector3.left, 500f, LayerMask.GetMask("Walls")).Select(h => h.transform.parent.parent.gameObject));

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
    void AddFloorsToBuilding(List<GameObject> floors, Transform building)
    {
        if (building != null)
        {
            Transform floorsContainer = building.Find("Floors");
            if (floorsContainer == null)
            {
                floorsContainer = new GameObject("Floors").transform;
                floorsContainer.SetParent(building);
            }
            foreach (GameObject floor in floors)
            {
                floor.transform.SetParent(floorsContainer);
            }
        }
    }
    Vector3 ObjectRelatedMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.WorldToScreenPoint(floorObject.transform.position).z;
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePoint);
        worldPoint.y = MouseModeManager.Instance.CurrentHeight;
        return worldPoint;
    }
    Vector3 MousePositionOnPlane()
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float entry;

        if (plane.Raycast(ray, out entry))
        {
            return ray.GetPoint(entry);
        }
        return Vector3.negativeInfinity;
    }
}
