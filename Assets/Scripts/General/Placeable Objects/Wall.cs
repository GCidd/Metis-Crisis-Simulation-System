using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class Wall : PlaceableObject
{
#if UNITY_EDITOR
    [CustomEditor(typeof(Wall))]
    public class customButton : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Wall w = (Wall)target;
            if (GUILayout.Button("Update all colliders"))
            {
                w.UpdateAllWallsColliders();
            }
        }

    }
#endif
    [SerializeField]
    private float snapDistance = 1.5f;

    private Color defaultMainColor = Color.white;
    private Color defaultSecondaryColor = Color.white;

    // default width, height and length stored here
    private float width;
    private float height;
    private float length;

    bool lowerWallDetected = false;

    [SerializeField]
    private float wallTouchMaxDistance = 0.2f;
    [SerializeField]
    [Tooltip("If true then it is available to be replaced by the exit during exit position randomization during training")]
    private bool exitReplaceable = false;
    public bool ExitReplaceable { get { return exitReplaceable; } }
    public new void OnPlaced()
    {
        base.OnPlaced();
        placed = true;
        GetComponent<Collider>().enabled = true;
    }

    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();
        height = GetComponent<MeshRenderer>().bounds.size.x;
        width = GetComponent<MeshRenderer>().bounds.size.y;
        length = GetComponent<MeshRenderer>().bounds.size.z;

        defaultMainColor = GetComponent<MeshRenderer>().materials[0].GetColor("_BaseColor");
        defaultSecondaryColor = GetComponent<MeshRenderer>().materials[1].GetColor("_BaseColor");
    }
    public Vector3 CheckWalling()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 totalSnapOffset = Vector3.zero;
        List<RaycastHit> rays = Physics.SphereCastAll(ray, snapDistance, Mathf.Infinity, LayerMask.GetMask("Walls")).ToList();
        List<RaycastHit> sameLevelWalls = rays.FindAll(c => c.transform.position.y == MouseModeManager.Instance.CurrentHeight);
        List<RaycastHit> lowerLevelWalls = rays.FindAll(c => c.transform.position.y <= MouseModeManager.Instance.CurrentHeight);
        if (sameLevelWalls.Count > 0)
        {
            // Transform objectHit = hit.transform;
            RaycastHit rayHit = sameLevelWalls.OrderBy(c => Vector3.Distance(c.transform.position, transform.position)).ToList()[0];
            return rayHit.transform.GetComponent<Wall>().ClosestEdge(rayHit.point);
        }
        else if (lowerLevelWalls.Count > 0)
        {
            lowerWallDetected = true;
            RaycastHit rayHit = lowerLevelWalls.OrderBy(c => Vector3.Distance(c.transform.position, transform.position)).ToList()[0];
            return rayHit.transform.GetComponent<Wall>().ClosestEdge(rayHit.point) + Vector3.up * MouseModeManager.Instance.LevelHeight;
        }
        lowerWallDetected = false;

        return -Vector3.one;
    }
    public Vector3 ClosestEdge(Vector3 point)
    {
        Vector3 leftEdge = transform.position + (transform.forward * width),
            rightEdge = transform.position;

        if (Vector3.Distance(leftEdge, point) <= Vector3.Distance(rightEdge, point))
        {
            return leftEdge;
        }
        else
        {
            return rightEdge;
        }
    }
    public List<GameObject> GetNeighbours()
    {
        Vector3 origin = transform.position + transform.forward * width / 2;
        // - transform.right because transform.right = (0, 0, -1)
        Vector3 halfExtents = GetComponent<BoxCollider>().bounds.size / 2 + (Vector3.forward + Vector3.right) * wallTouchMaxDistance;
        List<GameObject> neighbours = Physics.OverlapBox(origin, halfExtents, Quaternion.identity, LayerMask.GetMask("Walls")).Select(c => c.gameObject).ToList();

        return neighbours;
    }
    public List<Vector3> GetEdges()
    {
        return new List<Vector3> { transform.position + (transform.forward * width), transform.position };
    }
    public override void SnapPosition()
    {
        Vector3 wallSnapPos = CheckWalling();
        if (wallSnapPos != -Vector3.one)
        {
            transform.position = wallSnapPos;
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
        else if (!floorDetected && !lowerWallDetected)
        {
            return false;
        }
        return true;
    }
    protected override void ChangeColor(Color newColor)
    {
        GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", newColor);
        GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", newColor);
    }
    protected override void ResetColor()
    {
        GetComponent<MeshRenderer>().materials[0].SetColor("_BaseColor", defaultMainColor);
        GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", defaultSecondaryColor);
    }
    public void UpdateAllWallsColliders()
    {
        List<BoxCollider> colliders = GetComponents<BoxCollider>().ToList();

        foreach (Wall wall in FindObjectsOfType<Wall>())
        {
            if (wall.gameObject == gameObject)
                continue;
            GameObject.DestroyImmediate(wall.GetComponent<BoxCollider>());

            BoxCollider c1 = wall.gameObject.AddComponent<BoxCollider>();
            c1.center = colliders[0].center;
            c1.size = colliders[0].size;

            BoxCollider c2 = wall.gameObject.AddComponent<BoxCollider>();
            c2.center = colliders[1].center;
            c2.size = colliders[1].size;
        }
    }
}
