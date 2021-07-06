using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class Staircase : StaticObject
{
    bool overlapsObject = false;
    protected override void Awake()
    {
        base.Awake();
        foreach (Transform child in transform)
        {
            child.GetComponent<Collider>().enabled = false;
        }
    }
    public override void OnPlaced()
    {
        base.OnPlaced();
        GetComponent<Collider>().enabled = true;
        foreach (Transform child in transform)
        {
            child.GetComponent<Collider>().enabled = true;
        }
    }
    public override Vector3 CheckGround()
    {
        Vector3 groundCheckOrigin = transform.position - transform.right * HalfSizes.x + new Vector3(0f, HalfSizes.y, 0f);

        RaycastHit groundHit;
        floorDetected = Physics.BoxCast(groundCheckOrigin, new Vector3(HalfSizes.x, 0.1f, HalfSizes.z), Vector3.down, out groundHit, transform.rotation, MouseModeManager.Instance.LevelHeight - 0.05f, LayerMask.GetMask("Ground", "Floors"));

        overlapsObject = false;
        if (floorDetected)
        {
            return new Vector3(0f, groundHit.point.y + 3f, 0f);
        }
        else
        {
            return -Vector3.one;
        }
    }
    public override bool CanBePlaced()
    {
        if (!floorDetected)
        {
            return false;
        }
        // else
        // {
        //     if (overlapsObject)
        //         return false;
        // }
        return true;
    }
    public override void SnapPosition()
    {
        List<Collider> collidersToSnap = Physics.OverlapBox(transform.position, HalfSizes, Quaternion.identity, LayerMask.GetMask("Floors")).ToList();
        if (collidersToSnap.Count > 0)
        {
            Collider colToSnap = collidersToSnap.OrderBy(c => Vector3.Distance(transform.position, c.transform.position)).ToList()[0];

            Vector3 mousePoint = Input.mousePosition;
            mousePoint.z = Camera.main.WorldToScreenPoint(colToSnap.transform.position).z;
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePoint);
            worldPoint.y = colToSnap.transform.position.y;

            List<Vector3[]> places = new List<Vector3[]>();
            foreach (Vector3 side in new List<Vector3> { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                Vector3[] place = new Vector3[3];
                place[0] = side;
                place[1] = colToSnap.transform.position + side * 1.5f;
                if (side.z != 0)
                {
                    place[2] = new Vector3(0, 90 * (side.z < 0 ? -1 : 1), 0);
                }
                else
                {
                    place[2] = new Vector3(0, side.x < 0 ? 0 : 180, 0);
                }
                places.Add(place);
            }

            places = places.OrderBy(p => Vector3.Distance(worldPoint, p[1])).ToList();
            overlapsObject = true;
            foreach (Vector3[] place in places)
            {
                List<Collider> collisions = Physics.OverlapBox(place[1] + place[0] * HalfSizes.x, HalfSizes - Vector3.one * 0.25f, Quaternion.Euler(place[2]), LayerMask.GetMask("Floors", "Objects")).ToList();
                if (collisions.Count == 0)
                {
                    transform.position = place[1];
                    transform.eulerAngles = place[2];
                    overlapsObject = false;
                    break;
                }
            }
        }
        else
        {
            overlapsObject = true;
        }
    }
    private void OnDrawGizmos()
    {
        //Color prevColor = Gizmos.color;
        //Matrix4x4 prevMatrix = Gizmos.matrix;

        //List<Collider> collisions = Physics.OverlapBox(transform.position - transform.right * 2.25f, HalfSizes - Vector3.one * 0.25f, Quaternion.Euler(transform.eulerAngles), LayerMask.GetMask("Floors", "Objects")).ToList();
        //foreach(Collider col in collisions)
        //{
        //    Gizmos.DrawSphere(col.transform.position, 0.5f);
        //}
        //if (collisions.Count == 0)
        //    Gizmos.color = Color.white;
        //else
        //    Gizmos.color = Color.red;
        //Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        //Gizmos.matrix = rotationMatrix;
        //Vector3 pos = transform.InverseTransformPoint(transform.position - transform.right * 2.25f);
        //Gizmos.DrawWireCube(pos, HalfSizes * 2f - Vector3.one * 0.5f);


        //// restore previous Gizmos settings
        //Gizmos.color = prevColor;
        //Gizmos.matrix = prevMatrix;
    }
}
