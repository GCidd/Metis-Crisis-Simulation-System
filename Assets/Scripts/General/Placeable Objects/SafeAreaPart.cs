using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;

public class SafeAreaPart : PlaceableObject
{
    // default width, height and length stored here
    private Color defaultColor;
    protected override void Awake()
    {
        base.Awake();
        defaultColor = GetComponent<MeshRenderer>().materials[0].GetColor("_BaseColor");
    }
    // Start is called before the first frame update
    void Start()
    {

    }
    public Vector3 CheckCloseAreas(float snapDistance)
    {
        List<Vector3> corners = GetCornersPositions();

        RaycastHit hit;
        foreach (Vector3 corner in corners)
        {
            foreach (Vector3 direction in new ArrayList() { Vector3.forward, Vector3.back, Vector3.right, Vector3.left })
            {
                if (Physics.Raycast(corner, direction, out hit, snapDistance, 1 << gameObject.layer))
                {
                    Vector3 closestCornerOther = hit.transform.GetComponent<SafeAreaPart>().GetClosestCorner(transform.position);
                    Vector3 closestCorner = GetClosestCorner(closestCornerOther);

                    Vector3 cornerMove = closestCornerOther - closestCorner;
                    Vector3 hitMove = hit.point - closestCorner;

                    if (Vector3.Distance(closestCornerOther, closestCorner) > snapDistance)
                    {
                        return hitMove;
                    }

                    if (Vector3.Distance(closestCornerOther, closestCorner) < Vector3.Distance(hit.point, closestCorner))
                    {
                        return cornerMove;
                    }
                    else
                    {
                        return hitMove;
                    }
                }
            }
        }
        return Vector3.positiveInfinity;
    }
    Vector3 GetClosestCorner(Vector3 target)
    {
        return GetCornersPositions().OrderBy(e => Vector3.Distance(e, target)).ToList()[0];
    }
    List<Vector3> GetCornersPositions()
    {
        Vector3 center = transform.position;
        List<Vector3> corners = new List<Vector3>();
        float x = transform.localScale.x / 2, z = transform.localScale.z / 2;
        corners.Add(center + new Vector3(x, 0f, z));
        corners.Add(center + new Vector3(x, 0f, -z));
        corners.Add(center + new Vector3(-x, 0f, z));
        corners.Add(center + new Vector3(-x, 0f, -z));
        return corners;
    }
    public override void OnPlaced()
    {
        base.OnPlaced();
        placedPosition = transform.position;
        GetComponent<BoxCollider>().enabled = true;
    }
    protected override void ChangeColor(Color newColor)
    {
        GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", newColor);
    }
    protected override void ResetColor()
    {
        GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", defaultColor);
    }
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.GetComponent<Pedestrian>())
    //    {
    //        other.GetComponent<Pedestrian>().Saved();
    //    }
    //}
    public override XmlNode ToXmlNode(XmlDocument xmlDoc)
    {
        XmlNode objectNode = base.ToXmlNode(xmlDoc);

        XmlAttribute scaleAttribute = xmlDoc.CreateAttribute("scale");
        scaleAttribute.Value = transform.localScale.ToString();
        objectNode.Attributes.Append(scaleAttribute);
        return objectNode;
    }
    public override void LoadNodeFromXml(XmlNode xmlNode)
    {
        base.LoadNodeFromXml(xmlNode);
        transform.localScale = String2Vector3(xmlNode.Attributes["scale"].Value);
    }
}
