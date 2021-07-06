using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using System.Xml;
using Unity.Mathematics;
using UnityEditor;
using System.Data.Common;

public class DoubleDoor : Door
{
    [SerializeField]
    private float doorRotationAngle = 145f;
    //[SerializeField]
    //private float autoCloseSeconds = 30f;

    private Color leftDefaultMainColor = Color.white;
    private Color leftDefaultSecondaryColor = Color.white;
    private Color rightDefaultMainColor = Color.white;
    private Color rightDefaultSecondaryColor = Color.white;
    private Color exitColor = Color.red;

    [SerializeField]
    private bool isExit = false;
    public bool IsExit { get { return isExit; } }
    private float width;
    private float height;

    public float Height { get { return height; } }
    public float Width { get { return width; } }

    public new Vector3 HalfSizes { get { return leftDoor.GetComponent<Renderer>().bounds.size; } }
    public new Vector3 Center { get { return (leftDoor.GetComponent<Renderer>().bounds.center + rightDoor.GetComponent<Renderer>().bounds.center) / 2; } }

    private GameObject leftDoor;
    public GameObject LeftDoor { get { return leftDoor; } }
    private GameObject rightDoor;
    public GameObject RightDoor { get { return rightDoor; } }
    Quaternion closedAnglesLeft = new Quaternion(0, -90f, 0, 0);
    Quaternion closedAnglesRight = new Quaternion(0, 90f, 0, 0);
    private bool doorsOpen = false;

    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();

        leftDoor = transform.GetChild(0).gameObject;
        rightDoor = transform.GetChild(1).gameObject;

        height = leftDoor.GetComponent<MeshRenderer>().bounds.size.y;
        width = leftDoor.GetComponent<MeshRenderer>().bounds.size.x * 2;
        closedAnglesLeft = leftDoor.transform.rotation;
        closedAnglesRight = rightDoor.transform.rotation;

        leftDefaultMainColor = leftDoor.GetComponent<MeshRenderer>().materials[0].color;
        leftDefaultSecondaryColor = leftDoor.GetComponent<MeshRenderer>().materials[1].color;

        rightDefaultMainColor = rightDoor.GetComponent<MeshRenderer>().materials[0].color;
        rightDefaultSecondaryColor = rightDoor.GetComponent<MeshRenderer>().materials[1].color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pedestrian") && !doorsOpen)
            OpenDoors(Vector3.Dot(transform.position - other.transform.position, transform.right));
    }
    private void OnTriggerExit(Collider other)
    {
        // Invoke("OpenDoors", autoCloseSeconds);
    }
    public override void OnPlaced()
    {
        base.OnPlaced();
        GetComponent<Collider>().enabled = true;
        //leftDoor.GetComponent<BoxCollider>().enabled = true;
        //rightDoor.GetComponent<BoxCollider>().enabled = true;
    }
    public void OpenDoors(float direction)
    {
        if (doorsOpen || !placed)
            return;

        doorsOpen = true;
        if (direction >= 0)
        {
            leftDoor.transform.localRotation = Quaternion.Euler(0, doorRotationAngle, 0);
            rightDoor.transform.localRotation = Quaternion.Euler(0, -doorRotationAngle, 0);
        }
        else
        {
            leftDoor.transform.localRotation = Quaternion.Euler(0, 45f, 0);
            rightDoor.transform.localRotation = Quaternion.Euler(0, -45f, 0);
        }
        leftDoor.GetComponent<BoxCollider>().enabled = false;
        rightDoor.GetComponent<BoxCollider>().enabled = false;
        GetComponent<Collider>().enabled = true;
    }
    public void CloseDoors()
    {
        if (!doorsOpen || !placed)
            return;

        doorsOpen = false;
        leftDoor.transform.rotation = closedAnglesLeft;
        rightDoor.transform.rotation = closedAnglesRight;
        leftDoor.GetComponent<BoxCollider>().enabled = true;
        rightDoor.GetComponent<BoxCollider>().enabled = true;
        GetComponent<Collider>().enabled = true;
        // GetComponent<Collider>().enabled = true;
    }
    public Transform GetWallToSnap()
    {
        Vector3 origin = transform.position + Vector3.up * height / 2;
        // a very small amount smaller so that it doesn't overlap with multiple walls or walls on lower levels
        Collider[] colliders = Physics.OverlapBox(origin, HalfSizes - Vector3.one * 0.05f, Quaternion.identity, LayerMask.GetMask("Walls"));
        if (colliders.Length >= 1)
        {
            colliders = colliders.ToList().OrderBy(c => Vector3.Distance(c.transform.position + c.transform.right * width, transform.position)).ToArray();

            return colliders[0].transform;
        }

        return null;
    }
    public void SetExit(bool isExitNew)
    {
        if (!isExitNew)
        {
            tag = "Door";

            leftDoor.GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", leftDefaultMainColor);
            leftDoor.GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", leftDefaultSecondaryColor);

            rightDoor.GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", rightDefaultMainColor);
            rightDoor.GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", rightDefaultSecondaryColor);
            isExit = false;
        }
        else
        {
            tag = "Exit";

            leftDoor.GetComponent<MeshRenderer>().materials[0].SetColor("_Color", exitColor);
            leftDoor.GetComponent<MeshRenderer>().materials[1].SetColor("_Color", exitColor);

            rightDoor.GetComponent<MeshRenderer>().materials[0].SetColor("_Color", exitColor);
            rightDoor.GetComponent<MeshRenderer>().materials[1].SetColor("_Color", exitColor);
            isExit = true;
        }
    }
    public void ToggleExit()
    {
        SetExit(!isExit);
    }
    protected override void SetUpRightClickOptions()
    {
        UnityEvent setAsExitEvent = new UnityEvent();
        setAsExitEvent.AddListener(ToggleExit);
        rightClickEvents.Add("Toggle exit", setAsExitEvent);
    }
    private void OnDrawGizmos()
    {


    }
    protected override void ChangeColor(Color newColor)
    {
        leftDoor.GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", newColor);
        leftDoor.GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", newColor);

        rightDoor.GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", newColor);
        rightDoor.GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", newColor);
    }
    protected override void ResetColor()
    {
        leftDoor.GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", isExit ? exitColor : leftDefaultMainColor);
        leftDoor.GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", isExit ? exitColor : leftDefaultSecondaryColor);

        rightDoor.GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", isExit ? exitColor : rightDefaultMainColor);
        rightDoor.GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", isExit ? exitColor : rightDefaultSecondaryColor);
    }
    public override XmlNode ToXmlNode(XmlDocument xmlDoc)
    {
        XmlNode doorNode = base.ToXmlNode(xmlDoc);
        XmlAttribute isExitAttr = xmlDoc.CreateAttribute("Exit");
        isExitAttr.Value = isExit ? "Yes" : "No";
        doorNode.Attributes.Append(isExitAttr);
        return doorNode;
    }
    public override void LoadNodeFromXml(XmlNode xmlNode)
    {
        base.LoadNodeFromXml(xmlNode);
        if (xmlNode.Attributes["Exit"].Value == "Yes")
        {
            SetExit(true);
        }
    }
}
