using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Floor : PlaceableObject
{
    [SerializeField]
    private float snapDistance = 1.5f;
    bool snappedToWall = false;
    bool snappedToFloor = false;
    // default width, height and length stored here
    private float width;
    List<Color> objectDefaultColors;
    protected override void Awake()
    {
        base.Awake();
        width = GetComponent<MeshRenderer>().bounds.size.x;
        objectDefaultColors = new List<Color>();
        foreach (Material objectMaterial in GetComponent<MeshRenderer>().materials)
        {
            objectDefaultColors.Add(objectMaterial.color);
        }
    }
    public override Vector3 CheckGround()
    {
        Vector3 groundCheckOrigin = new Vector3(Center.x, Mathf.Max(0.5f, Center.y), Center.z);

        RaycastHit groundHit;
        floorDetected = Physics.BoxCast(groundCheckOrigin, new Vector3(HalfSizes.x, 0.1f, HalfSizes.z), Vector3.down, out groundHit, Quaternion.identity, MouseModeManager.Instance.LevelHeight - 0.05f, LayerMask.GetMask("Ground"));

        if (floorDetected)
        {
            return new Vector3(0f, groundHit.point.y, 0f);
        }
        else
        {
            return -Vector3.one;
        }
    }
    public Vector3 CheckWalling()
    {
        //Vector3 origin = transform.position;
        //Vector3 size = GetComponent<BoxCollider>().bounds.size / 2 + Vector3.forward * snapDistance;

        //List<Collider> colls = Physics.OverlapBox(origin, size, Quaternion.identity, LayerMask.GetMask("Walls")).ToList();
        //size = GetComponent<BoxCollider>().bounds.size / 2 + Vector3.right * 1.5f;
        //colls.AddRange(Physics.OverlapBox(origin, size, Quaternion.identity, LayerMask.GetMask("Walls")).ToList());
        //colls = colls.OrderBy(c => Vector3.Distance(GetClosestCorner(c.transform.position), c.transform.position)).ToList();
        //if (colls.Count >= 1)
        //{
        //    Vector3 edge = GetClosestCorner(colls[0].transform.position);
        //    return colls[0].transform.position - edge;
        //}
        //return -Vector3.one;
        List<Vector3> corners = GetCornersPositions();
        foreach (Vector3 corner in corners)
        {
            Vector3 heading = corner - transform.position;
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            List<Collider> colls = Physics.OverlapSphere(corner, snapDistance, LayerMask.GetMask("Walls")).ToList();
            if (colls.Count > 0)
            {
                // Order by distance between floor and wall. distance is equal to the distance betwen both edges of the wall and the floor's position
                GameObject closestWall = colls.OrderBy(c => Vector3.Distance(transform.position, c.GetComponent<Wall>().GetEdges()[0]) + Vector3.Distance(transform.position, c.GetComponent<Wall>().GetEdges()[1])).ToList()[0].gameObject;
                Vector3 closestWallEdge = closestWall.GetComponent<Wall>().GetEdges().OrderBy(e => Vector3.Distance(e, corner)).ToList()[0];
                return closestWallEdge - corner;
            }
        }
        return -Vector3.one;
    }
    public Vector3 CheckFlooring()
    {
        List<Vector3> corners = GetCornersPositions();

        RaycastHit hit;
        foreach (Vector3 corner in corners)
        {
            Vector3 heading = corner - transform.position;
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            if (Physics.SphereCast(transform.position, snapDistance, direction, out hit, width, 1 << gameObject.layer))
            {
                Vector3 closestCornerOther = hit.transform.GetComponent<Floor>().GetClosestCorner(transform.position);
                Vector3 closestCorner = GetClosestCorner(closestCornerOther);
                return closestCornerOther - closestCorner;
            }
            //foreach (Vector3 direction in new ArrayList() { Vector3.forward, Vector3.back, Vector3.right, Vector3.left })
            //{
            //    if (Physics.Raycast(corner, direction, out hit, snapDistance, 1 << gameObject.layer))
            //    {
            //        Vector3 closestCornerOther = hit.transform.GetComponent<Floor>().GetClosestCorner(transform.position);
            //        Vector3 closestCorner = GetClosestCorner(closestCornerOther);
            //        return closestCornerOther - closestCorner;
            //    }
            //}
        }
        return -Vector3.one;
    }
    Vector3 GetClosestCorner(Vector3 target)
    {
        return GetCornersPositions().OrderBy(e => Vector3.Distance(e, target)).ToList()[0];
    }
    List<Vector3> GetCornersPositions()
    {
        Vector3 center = transform.position;
        List<Vector3> corners = new List<Vector3>();
        for (float angle = 45f; angle < 360f; angle += 90f)
        {
            float rad = Mathf.Deg2Rad * angle;
            float distance = Mathf.Sqrt(Mathf.Pow(width / 2, 2) * 2);
            corners.Add(center + new Vector3(Mathf.Cos(rad) * distance, 0, Mathf.Sin(rad) * distance));
        }
        return corners;
    }
    public override void OnPlaced()
    {
        base.OnPlaced();
        GetComponent<BoxCollider>().enabled = true;
    }
    public override void SnapPosition()
    {
        // wall snapping
        Vector3 wallSnapDiff = CheckWalling();
        snappedToWall = wallSnapDiff != -Vector3.one;
        if (snappedToWall)
        {
            transform.position += new Vector3(wallSnapDiff.x, 0, wallSnapDiff.z);
        }

        Vector3 floorSnapDiff = CheckFlooring();
        snappedToFloor = floorSnapDiff != -Vector3.one;
        if (snappedToFloor)
        {
            transform.position += new Vector3(floorSnapDiff.x, 0, floorSnapDiff.z);
        }
    }
    public override bool CanBePlaced()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Physics.OverlapBox(Center, HalfSizes, Quaternion.identity, LayerMask.GetMask("Walls", "Ground", "Floors")).Length == 0)
            {
                return false;
            }
        }
        else if (!(floorDetected || snappedToWall || snappedToFloor))
        {   // if at least one is true then it can be placed (so it results to false)
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
}
