using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.ProBuilder;

[RequireComponent(typeof(Rigidbody))]
public class StaticObject : PlaceableObject
{
    [SerializeField]
    float snapDistance = -1;
    List<Color> objectDefaultColors;

    protected override void Awake()
    {
        base.Awake();
        objectDefaultColors = new List<Color>();
        foreach (Material objectMaterial in GetComponent<MeshRenderer>().materials)
        {
            if (objectMaterial.HasProperty("_Color"))
                objectDefaultColors.Add(objectMaterial.color);
        }
    }
    public virtual new void OnPlaced()
    {
        base.OnPlaced();
        GetComponent<Collider>().enabled = true;
        GetComponent<Rigidbody>().useGravity = true;
    }
    public override void SnapPosition()
    {
        float distance;
        Vector3 totalOffset = Vector3.zero;
        RaycastHit hit;

        foreach (Vector3 direction in new Vector3[] { Vector3.forward, Vector3.right })
        {
            List<Vector3> sub_offsets = new List<Vector3>();
            distance = Mathf.Abs(Vector3.Dot(HalfSizes, direction)) + (snapDistance == -1 ? Vector3.Dot(HalfSizes, direction) : snapDistance);
            if (Physics.Raycast(Center, direction, out hit, distance, LayerMask.GetMask("Walls", "Objects")))
            {
                sub_offsets.Add(hit.point - (Center + new Vector3(HalfSizes.x * direction.x, 0, HalfSizes.z * direction.z)));
            }
            if (Physics.Raycast(Center, -direction, out hit, distance, LayerMask.GetMask("Walls", "Objects")))
            {
                sub_offsets.Add(hit.point - (Center + new Vector3(HalfSizes.x * -direction.x, 0, HalfSizes.z * -direction.z)));
            }
            if (sub_offsets.Count > 0)
                totalOffset += sub_offsets.Aggregate((x, y) => x + y) / sub_offsets.Count;
        }
        totalOffset.y = 0f;
        transform.position += totalOffset;
    }
    public override bool CanBePlaced()
    {
        if (!floorDetected)
        {
            return false;
        }
        return true;
    }
    protected override void ChangeColor(Color newColor)
    {
        foreach (Material objectMaterial in GetComponent<MeshRenderer>().materials)
        {
            objectMaterial.SetColor("_BaseColor", newColor);
        }
    }
    protected override void ResetColor()
    {
        for (int i = 0; i < objectDefaultColors.Count; i++)
        {
            GetComponent<MeshRenderer>().materials[i].SetColor("_BaseColor", objectDefaultColors[i]);
        }
    }
    private void OnDrawGizmos()
    {
        //HalfSizes = GetComponent<Collider>().bounds.size / 2;
        //centerOffset = transform.position - GetComponent<Collider>().bounds.center;
    }
}
