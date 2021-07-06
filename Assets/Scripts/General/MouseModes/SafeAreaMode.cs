using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class SafeAreaMode : MouseModeSingleton<SafeAreaMode>
{
    enum ModeStep { AreaStartPick, AreaExtention };
    public Button uiButton;
    ModeStep currentStep;
    public GameObject areaObject;
    private List<GameObject> placedAreas = new List<GameObject>();
    private Vector3 defaultScale;

    private Vector3 areaClickPosition;
    private Vector3 extentionDragStartPosition;
    private Vector3 extentionDragEndPosition;

    float wallHeight;
    private float snapDistance;
    private void Awake()
    {
        m_Instance = this;
        areaObject = GameObject.Find("Tools").GetComponentInChildren<SafeAreaPart>().gameObject;
        snapDistance = 1.5f;
        uiButton = GameObject.Find("SafeArea Mode Button").GetComponent<Button>();
        uiButton.onClick.AddListener(delegate { MouseModeManager.Instance.EnableMode(Instance); });
        defaultScale = areaObject.transform.localScale;
    }
    public override void OnModeEnter()
    {
        currentStep = ModeStep.AreaStartPick;
        areaObject.SetActive(true);
        uiButton.GetComponent<ToggleButtonImage>().SetState(true);
        isActive = true;
    }
    public void Update()
    {
        if (!CanUpdate())
            return;

        if (currentStep == ModeStep.AreaStartPick)
        {
            Vector3 offset = new Vector3(0, areaObject.transform.localScale.y, 0);
            areaObject.transform.position = ObjectRelatedMouseWorldPos(areaObject, areaObject.transform.position.y, offset);

            if (Input.GetKey(KeyCode.LeftControl))
            {
                // snap to "grid" (rounds the coordinates of the wall part to be placed)
                areaObject.transform.position = new Vector3((int)areaObject.transform.position.x, (int)areaObject.transform.position.y, (int)areaObject.transform.position.z);
            }
            else if (Input.GetKey(KeyCode.LeftAlt))
            {
                Vector3 areaSnapDiff = areaObject.GetComponent<SafeAreaPart>().CheckCloseAreas(snapDistance);
                if (areaSnapDiff.ToString() != Vector3.positiveInfinity.ToString())
                {
                    areaObject.transform.position += areaSnapDiff;
                }
            }

            // Call object hightlight event maybe
            if (Input.GetMouseButtonDown(0))
            {
                // Place wall start
                currentStep = ModeStep.AreaExtention;
                areaClickPosition = areaObject.transform.position;
                extentionDragStartPosition = MousePositionOnPlane();
            }
            else if (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                MouseModeManager.Instance.CancelMode();
            }
        }
        else if (currentStep == ModeStep.AreaExtention)
        {
            Vector3 newPosition = MousePositionOnPlane();

            if (newPosition != extentionDragEndPosition)
            {
                extentionDragEndPosition = newPosition;
                Vector3 difference = extentionDragEndPosition - extentionDragStartPosition;
                ResizeArea(difference.x, -difference.z);
            }

            if (Input.GetMouseButtonDown(0))
            {
                GameObject newArea = GameObject.Instantiate(areaObject);
                newArea.name = "Safe Area";
                newArea.transform.position = areaObject.transform.position;
                newArea.GetComponent<SafeAreaPart>().OnPlaced();
                placedAreas.Add(newArea);
                Transform parentBuilding = GetAreaBuilding(newArea);
                parentBuilding.GetComponent<Building>().AddSafeArea(newArea);
                areaObject.transform.localScale = defaultScale;
                currentStep = ModeStep.AreaStartPick;
            }
            else if (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                MouseModeManager.Instance.CancelMode();
            }
        }
    }
    public override void OnModeExit()
    {
        CancelAreaStartPick();
        uiButton.GetComponent<ToggleButtonImage>().SetState(false);
        currentStep = ModeStep.AreaStartPick;
        areaObject.SetActive(false);
        isActive = false;
    }

    Transform GetAreaBuilding(GameObject area)
    {
        if (GameObject.Find("Placed Buildings").transform.childCount == 0)
            return null;
        return GameObject.Find("Placed Buildings").transform.GetChild(0);

        //Vector3 center = new Vector3(area.transform.position.x, 1.5f, area.transform.position.z) + Vector3.right * 500f;
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

    void CancelAreaStartPick()
    {
        areaObject.SetActive(false);
        areaObject.transform.localScale = defaultScale;
    }
    Vector3 ObjectRelatedMouseWorldPos(GameObject _object, float initialY, Vector3 offset)
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.WorldToScreenPoint(_object.transform.position - offset).z;
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePoint);
        worldPoint.y = initialY;
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
    void ResizeArea(float x, float z)
    {
        areaObject.transform.localScale = new Vector3(1f, 0.1f, 1f);
        areaObject.transform.position = areaClickPosition;

        GameObject dump = new GameObject();
        dump.transform.position = areaClickPosition;
        dump.AddComponent<SphereCollider>();

        Vector3 dumpOffset = new Vector3(0, 0, 0);
        if (x < 0)
            dumpOffset.x += 0.5f;
        else
            dumpOffset.x -= 0.5f;

        if (z < 0)
            dumpOffset.z -= 0.5f;
        else
            dumpOffset.z += 0.5f;
        dump.transform.position += dumpOffset;

        areaObject.transform.SetParent(dump.transform);
        dump.transform.localScale = new Vector3(Mathf.Abs(x), 0.1f, Mathf.Abs(z));

        areaObject.transform.SetParent(null);
        GameObject.Destroy(dump);
    }

}
