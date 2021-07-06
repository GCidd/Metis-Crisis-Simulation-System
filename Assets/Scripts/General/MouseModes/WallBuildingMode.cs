using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using JetBrains.Annotations;

public class WallBuildingMode : MouseModeSingleton<WallBuildingMode>
{
    enum ModeStep
    {
        WallBuilding, WallRotation, WallExtension
    };
    public Button uiButton;
    ModeStep currentStep;
    [SerializeField] private GameObject wallObject;
    private GameObject pivotHelperObject;
    private Transform initialWallParent;
    private float wallWidth = 1;
    private float wallHeight = 1;
    public float WallHeight { get { return wallHeight; } }
    private int lastExtension = 0;

    private Vector3 extentionDragStartPosition;

    private List<GameObject> wallParts = new List<GameObject>();
    private List<GameObject> placedWallParts = new List<GameObject>();
    private GameObject buildingsContainer;
    private List<GameObject> placedBuildings = new List<GameObject>();

    public List<GameObject> PlacedWallParts() { return placedWallParts; }

    private void Awake()
    {
        m_Instance = this;
        if (wallObject == null)
            wallObject = GameObject.Find("Tools").GetComponentInChildren<Wall>().gameObject;
        initialWallParent = wallObject.transform.parent;
        wallWidth = wallObject.GetComponent<MeshRenderer>().bounds.size.x;
        wallHeight = wallObject.GetComponent<MeshRenderer>().bounds.size.y;
        pivotHelperObject = GameObject.Find("Pivot Helper");
        if (uiButton == null)
            uiButton = GameObject.Find("Wall Mode Button").GetComponent<Button>();
        uiButton.onClick.AddListener(delegate { MouseModeManager.Instance.EnableMode(Instance); });
        buildingsContainer = GameObject.Find("Placed Buildings");
    }
    public override void OnModeEnter()
    {
        currentStep = ModeStep.WallBuilding;
        wallObject.SetActive(true);
        uiButton.GetComponent<ToggleButtonImage>().SetState(true);
        isActive = true;
    }
    public void Update()
    {
        if (!CanUpdate())
            return;

        if (currentStep == ModeStep.WallBuilding)
        {
            wallObject.transform.position = ObjectRelatedMouseWorldPos();
            if (Input.GetKey(KeyCode.LeftControl))
            {
                wallObject.transform.position = new Vector3((int)wallObject.transform.position.x, wallObject.transform.position.y, (int)wallObject.transform.position.z);
            }
            Vector3 ground = wallObject.GetComponent<Wall>().CheckGround();
            if (wallObject.GetComponent<Wall>().CanBePlaced())
                wallObject.transform.position = new Vector3(wallObject.transform.position.x, ground.y, wallObject.transform.position.z);

            if (!Input.GetKey(KeyCode.LeftShift))
                wallObject.GetComponent<Wall>().SnapPosition();

            if (wallObject.GetComponent<Wall>().CanBePlaced())
            {
                wallObject.GetComponent<Wall>().UnHighlightError();
                if (Input.GetMouseButtonDown(0))
                {
                    // Place wall start
                    currentStep = ModeStep.WallRotation;
                    pivotHelperObject.transform.position = wallObject.transform.position;
                    wallObject.transform.parent = pivotHelperObject.transform;
                    wallObject.transform.localPosition = Vector3.zero;
                    extentionDragStartPosition = MousePositionOnPlane();
                }
            }
            else
            {
                wallObject.GetComponent<Wall>().HighlightError();
            }
        }
        else if (currentStep == ModeStep.WallRotation)
        {
            if (Input.GetMouseButton(0))
            {
                Vector3 mousePos = ObjectRelatedMouseWorldPos();
                float diffX = pivotHelperObject.transform.position.x - mousePos.x, diffZ = pivotHelperObject.transform.position.z - mousePos.z;

                if (Mathf.Abs(diffX) > Mathf.Abs(diffZ))
                {
                    float angle = (diffX > 0 ? 180 : 0);
                    pivotHelperObject.transform.Rotate(0, angle - pivotHelperObject.transform.eulerAngles.y, 0);
                }
                else
                {
                    float angle = (diffZ > 0 ? 90 : 270);
                    pivotHelperObject.transform.Rotate(0, angle - pivotHelperObject.transform.eulerAngles.y, 0);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                wallObject.transform.parent = null;
                currentStep = ModeStep.WallExtension;
            }
        }
        else if (currentStep == ModeStep.WallExtension)
        {
            int distance = (int)Vector3.Distance(extentionDragStartPosition, MousePositionOnPlane()) / (int)wallWidth;

            if (distance <= 0)
            {   // Remove all wall parts
                foreach (GameObject wp in wallParts)
                {
                    GameObject.Destroy(wp);
                }
                wallParts.Clear();
            }
            else if (distance > lastExtension)
            {   // extend wall by (distance - lastExtension) wall parts
                ExtendWall(distance, wallObject.transform.forward, lastExtension + 1);
            }
            else if (distance < lastExtension)
            {
                int diff = (int)(lastExtension - distance);
                if (diff > 0)
                {   // shorten wall by diff wall parts
                    int start = wallParts.Count - 1;
                    int end = wallParts.Count - diff;
                    for (int i = start; i >= end; i--)
                    {
                        GameObject.Destroy(wallParts[i]);
                        wallParts.RemoveAt(i);
                    }
                }
            }
            lastExtension = distance;

            if (Input.GetMouseButtonDown(0))
            {
                // place wall parts, create an empty object as a parent of the parts, rename them, 
                // empty the wallparts list, add the parts to the placedWallParts list and add the 
                // wall to the placedWalls list
                GameObject newWallPartStart = GameObject.Instantiate(wallObject);
                newWallPartStart.name = "Wall";
                newWallPartStart.GetComponent<Wall>().OnPlaced();
                wallParts.Add(newWallPartStart);
                placedWallParts.Add(newWallPartStart);
                placedWallParts.AddRange(wallParts);

                if (placedBuildings.Count == 0)
                {
                    GameObject newBuilding = new GameObject();
                    newBuilding.AddComponent<Building>();
                    newBuilding.name = "Building";
                    newBuilding.transform.SetParent(buildingsContainer.transform);
                    newBuilding.GetComponent<Building>().OnPlaced();
                    newBuilding.GetComponent<Building>().FixCenter();
                    placedBuildings.Add(newBuilding);
                }
                foreach (GameObject wall in wallParts)
                {
                    wall.name = "Wall";
                    wall.GetComponent<Wall>().OnPlaced();
                    placedBuildings[0].GetComponent<Building>().AddWall(wall);
                }
                OnNewWallPlaced(null);
            }
        }
        if (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentStep == ModeStep.WallBuilding)
                MouseModeManager.Instance.CancelMode();
            else
                CancelRotationExtension();
        }
    }
    void CancelRotationExtension()
    {
        foreach (GameObject w in wallParts)
        {
            GameObject.Destroy(w);
        }
        wallParts.Clear();
        lastExtension = 0;
        currentStep = ModeStep.WallBuilding;
        ResetPivotAngle();
    }
    public override void OnModeExit()
    {
        CancelRotationExtension();
        wallObject.SetActive(false);
        uiButton.GetComponent<ToggleButtonImage>().SetState(false);
        isActive = false;
    }
    void OnNewWallPlaced(GameObject newWall)
    {
        wallParts.Clear();
        currentStep = ModeStep.WallBuilding;
        ResetPivotAngle();
        lastExtension = 0;
        //ReOrganizeWalls(newWall);
    }
    Vector3 ObjectRelatedMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.WorldToScreenPoint(wallObject.transform.position).z;
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePoint);
        worldPoint.y = MouseModeManager.Instance.CurrentHeight;
        return worldPoint;
    }
    void ResetPivotAngle()
    {
        wallObject.transform.parent = null;
        pivotHelperObject.transform.position = wallObject.transform.position + wallObject.transform.forward * wallWidth / 2;
        wallObject.transform.parent = pivotHelperObject.transform;
        wallObject.transform.localPosition = Vector3.zero;
        pivotHelperObject.transform.Rotate(0, -pivotHelperObject.transform.eulerAngles.y, 0);
        wallObject.transform.parent = initialWallParent;
    }
    void ExtendWall(int numberOfWalls, Vector3 direction, int start = 1)
    {
        for (int i = start; i <= numberOfWalls; i++)
        {
            Vector3 dist = direction * i * wallWidth;
            GameObject newWallPart = GameObject.Instantiate(wallObject);
            newWallPart.transform.position += dist;
            newWallPart.name = i.ToString();
            wallParts.Add(newWallPart);
        }
    }
    void ReOrganizeWalls(GameObject newWall)
    {
        GameObject newBuilding = new GameObject();
        placedBuildings.Add(newBuilding);
        newBuilding.AddComponent<Building>();
        newBuilding.GetComponent<Building>().OnPlaced();
        newBuilding.GetComponent<Building>().FixCenter();
        newBuilding.transform.SetParent(buildingsContainer.transform);
        newWall.transform.SetParent(newBuilding.transform);

        List<GameObject> touchingWalls = new List<GameObject>();
        foreach (Transform t in newWall.transform)
            touchingWalls.Add(t.gameObject);

        int initialLength = touchingWalls.Count;

        foreach (Transform t in newWall.transform)
        {
            GetAllNeighbours(t, ref touchingWalls);
        }

        if (initialLength == touchingWalls.Count)
        {   // no extra neighbours, no need to reorganize
            newWall.transform.SetParent(newBuilding.transform);
            return;
        }

        int i = 1;
        foreach (GameObject w in touchingWalls)
        {   // make all touching walls have the same parent
            w.transform.SetParent(newWall.transform);
            w.name = "WallPart " + i++;

            TransferEverythingToNewBuilding(w.transform.parent.parent, newBuilding.transform);
        }

        i = 1;
        foreach (Transform building in buildingsContainer.transform)
        {   // remove all parents that have no children
            Transform wallsChild = building.Find("Walls");
            if (wallsChild && wallsChild.childCount == 0)
            {
                GameObject.Destroy(building.gameObject);
                placedBuildings.Remove(building.gameObject);
                continue;
            }
            building.name = "Building " + i;
            i++;
        }
    }

    void TransferEverythingToNewBuilding(Transform oldBuilding, Transform newBuilding)
    {
        if (oldBuilding.childCount == 0)
            return;

        foreach (Transform oldChild in oldBuilding)
        {
            if (oldChild.name == "Walls")
                continue;

            Debug.Log(string.Format("Checking {0} of {1}", oldBuilding.name, oldChild.name));

            Transform newChild = newBuilding.Find(oldChild.name);
            if (newChild)
            {
                foreach (Transform t in oldChild)
                {
                    Debug.Log(string.Format("Moving {0} from {1}->{2}", t.name, oldChild.parent.name, newChild.parent.name));
                    t.SetParent(newChild);
                }
            }
            else
            {
                Debug.Log(string.Format("Moving {0} from {1}->{2}", oldChild.name, oldChild.parent.name, newChild.parent.name));
                oldChild.SetParent(newBuilding);
            }
        }
    }

    void GetAllNeighbours(Transform _wall, ref List<GameObject> all_neighbours)
    {
        List<GameObject> neighbours = _wall.GetComponent<Wall>().GetNeighbours();
        foreach (GameObject n in neighbours)
        {
            if (!all_neighbours.Contains(n))
            {
                all_neighbours.Add(n);
                GetAllNeighbours(n.transform, ref all_neighbours);
            }
        }
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
