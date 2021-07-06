using System;
using UnityEngine;
using UnityEngine.UI;

public class GrabModeBase : MouseMode
{
    protected enum ModeStep { ObjectSelection, ObjectPlacement };

    protected string placedContainerName = "";   // name of object holding all the placed objects inside a building
    [SerializeField] protected Button uiButton;
    protected ModeStep currentStep = ModeStep.ObjectSelection;
    protected private GameObject grabbedObject = null;
    protected private Vector3 initialObjectPosition;
    [SerializeField] private LayerMask objectsLayer;
    private float angleRotation = 22.5f;
    private Vector3 mOffset;

    public virtual void OnModeEnter(GameObject _object)
    {
        throw new NotImplementedException(string.Format("{0} has not implemented OnModeEnter(GameObject) method.", name));
    }
    public virtual void Update()
    {
        if (!CanUpdate())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit rayHit;
        if (currentStep == ModeStep.ObjectSelection)
        {
            // Call object hightlight event maybe
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(ray, out rayHit, Mathf.Infinity, objectsLayer))
                {
                    // Pick object
                    grabbedObject = rayHit.transform.gameObject;
                    PickUpObject(grabbedObject);
                }
            }
        }
        else if (currentStep == ModeStep.ObjectPlacement)
        {
            // Move selected object
            if (grabbedObject != null)
            {
                grabbedObject.transform.position = GetMouseWorldPos();
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    grabbedObject.transform.position = new Vector3((int)grabbedObject.transform.position.x, grabbedObject.transform.position.y, (int)grabbedObject.transform.position.z);
                }
                Vector3 ground = grabbedObject.GetComponent<PlaceableObject>().CheckGround();
                if (grabbedObject.GetComponent<PlaceableObject>().CanBePlaced())
                    grabbedObject.transform.position = new Vector3(grabbedObject.transform.position.x, ground.y, grabbedObject.transform.position.z);

                if (!Input.GetKey(KeyCode.LeftShift))
                    grabbedObject.GetComponent<PlaceableObject>().SnapPosition();

                if (grabbedObject.GetComponent<PlaceableObject>().CanBePlaced())
                {
                    grabbedObject.GetComponent<PlaceableObject>().UnHighlightError();
                    if (Input.GetMouseButtonDown(0))
                    {
                        PlaceObject();
                    }
                }
                else
                {
                    grabbedObject.GetComponent<PlaceableObject>().HighlightError();
                }

                if (Input.GetKey(KeyCode.LeftAlt) && Input.mouseScrollDelta.y < 0)
                {
                    grabbedObject.transform.Rotate(Vector3.up, angleRotation);
                }
                else if (Input.GetKey(KeyCode.LeftAlt) && Input.mouseScrollDelta.y > 0)
                {
                    grabbedObject.transform.Rotate(Vector3.up, -angleRotation);
                }
            }
        }

        if (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            MouseModeManager.Instance.CancelMode();
        }
    }
    public override void OnModeExit()
    {
        CancelChanges();
        currentStep = ModeStep.ObjectSelection;
        uiButton.GetComponent<ToggleButtonImage>().SetState(false);
        isActive = false;
    }
    protected virtual void ObjectPositioning()
    {

    }
    protected virtual void CancelChanges()
    {
        if (grabbedObject == null)
            return;

        grabbedObject.GetComponent<PlaceableObject>().UnHighlight();
        grabbedObject.GetComponent<PlaceableObject>().UnHighlightError();
        grabbedObject.transform.position = initialObjectPosition;
        if (grabbedObject.GetComponent<Rigidbody>())
            grabbedObject.GetComponent<Rigidbody>().useGravity = true;
        grabbedObject = null;
    }
    public virtual void PlaceObject()
    {
        currentStep = ModeStep.ObjectSelection;
        if (grabbedObject.GetComponent<Rigidbody>())
            grabbedObject.GetComponent<Rigidbody>().useGravity = true;
        grabbedObject = null;
    }
    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.WorldToScreenPoint(grabbedObject.transform.position).z;
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePoint);
        // zero out the y axis so that it doesn't change
        worldPoint.y = 0f;
        // add the mouse offset
        worldPoint += mOffset;
        worldPoint.y = MouseModeManager.Instance.CurrentHeight;
        return worldPoint;
    }

    protected void PickUpObject(GameObject pickedObject)
    {
        //Debug.Log("Picked object " + pickedObject.transform.name);
        initialObjectPosition = pickedObject.transform.position;
        currentStep = ModeStep.ObjectPlacement;
        mOffset = pickedObject.transform.position - GetMouseWorldPos();
        mOffset.y = pickedObject.transform.position.y;

        if (grabbedObject.GetComponent<Rigidbody>())
            grabbedObject.GetComponent<Rigidbody>().useGravity = false;

        //Debug.Log("Object width " + grabbedObject.GetComponent<MeshRenderer>().bounds.size.z);
    }

    protected Transform GetObjectBuilding(GameObject _object)
    {
        // for now just return the first building
        if (GameObject.Find("Placed Buildings").transform.childCount == 0)
            return null;
        return GameObject.Find("Placed Buildings").transform.GetChild(0);
        //Vector3 center = _object.GetComponent<StaticObject>().CenteredPosition() + Vector3.up;
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
