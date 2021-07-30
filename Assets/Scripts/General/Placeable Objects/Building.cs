using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System.Xml;
using Unity.MLAgents.Sensors;

public class Building : PlaceableObject
{
#if UNITY_EDITOR
    [CustomEditor(typeof(Building))]
    public class customButton : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Building building = (Building)target;
            if (GUILayout.Button("Save Building"))
            {
                building.SaveBuilding();
            }
            if (GUILayout.Button("Fix Center Pivot"))
            {
                building.FixCenter();
            }
            if (GUILayout.Button("Fix Materials"))
            {
                building.FixMaterials();
            }
        }

    }
#endif
    // to store a simple bbox for the initial testing if a point is inside the building
    Bounds boundingBox;

    static int buildingCount = 0;
    int buildingNumber = 0;
    // values used to normalize a position inside a building
    float distX = 0f, distZ = 0f;

    //private GameObject pedestriansContainer;
    private List<Pedestrian> pedestrians = new List<Pedestrian>();
    public List<Pedestrian> Pedestrians { get { return pedestrians; } }
    public void AddPedestrian(GameObject newPedestrian)
    {
        Pedestrian pedestrianComponent = newPedestrian.GetComponent<Pedestrian>();
        if (pedestrianComponent && !pedestrians.Contains(pedestrianComponent))
        {
            pedestrians.Add(pedestrianComponent);
            newPedestrian.transform.parent = transform;
        }
    }

    private GameObject floorsContainer;
    private List<Floor> floors = new List<Floor>();
    public List<Floor> Floors { get { return floors; } }
    public void AddFloor(GameObject newFloor)
    {
        Floor floorComponent = newFloor.GetComponent<Floor>();
        if (floorComponent && !floors.Contains(floorComponent))
        {
            floors.Add(floorComponent);
            newFloor.transform.parent = floorsContainer.transform;
        }
    }

    private GameObject wallsContainer;
    private List<Wall> walls = new List<Wall>();
    public List<Wall> Walls { get { return walls; } }
    public void AddWall(GameObject newWall)
    {
        Wall wallComponent = newWall.GetComponent<Wall>();
        if (wallComponent && !walls.Contains(wallComponent))
        {
            UpdateWalls(wallComponent);
        }
    }

    private GameObject doorsContainer;
    private List<Door> doors = new List<Door>();
    public List<Door> Doors { get { return doors; } }
    public void AddDoor(GameObject newDoor)
    {
        DoubleDoor doorComponent = newDoor.GetComponent<DoubleDoor>();
        if (doorComponent && !doors.Contains(doorComponent))
        {
            doors.Add(doorComponent);
            newDoor.transform.parent = doorsContainer.transform;
        }
    }

    private GameObject objectsContainer;
    private List<StaticObject> objects = new List<StaticObject>();
    public List<StaticObject> StaticObjects { get { return objects; } }
    public void AddStaticObject(GameObject newObject)
    {
        StaticObject objectComponent = newObject.GetComponent<StaticObject>();
        if (objectComponent && !objects.Contains(objectComponent))
        {
            objects.Add(objectComponent);
            newObject.transform.parent = objectsContainer.transform;
        }
    }

    private GameObject firesContainer;
    private List<FireSpreading> fires = new List<FireSpreading>();
    public List<FireSpreading> Fires { get { return fires; } }
    public void AddFire(GameObject newFire)
    {
        FireSpreading fireComponent = newFire.GetComponent<FireSpreading>();
        if (fireComponent && !fires.Contains(fireComponent))
        {
            fires.Add(fireComponent);
            newFire.transform.parent = firesContainer.transform;
            newFire.name = "Fire " + fires.Count().ToString();
        }
    }

    private GameObject safeAreasContainer;
    private List<SafeAreaPart> safeAreas = new List<SafeAreaPart>();
    public List<SafeAreaPart> SafeAreas { get { return safeAreas; } }
    public void AddSafeArea(GameObject newSafeArea)
    {
        SafeAreaPart safeAreaComponent = newSafeArea.GetComponent<SafeAreaPart>();
        if (safeAreaComponent && !safeAreas.Contains(safeAreaComponent))
        {
            safeAreas.Add(safeAreaComponent);
            newSafeArea.transform.parent = safeAreasContainer.transform;
            newSafeArea.name = "Safe Area " + safeAreas.Count().ToString();
        }
    }

    public new Vector3 Center { get { return CalculateCenter(); } }
    private Vector3 CalculateCenter()
    {
        if (walls.Count == 0)
            return transform.position;

        float minX = walls.Min(w => w.transform.position.x);
        float minZ = walls.Min(w => w.transform.position.z);
        float maxX = walls.Max(w => w.transform.position.x);
        float maxZ = walls.Max(w => w.transform.position.z);

        return new Vector3((maxX - minX) / 2, 0, (maxZ - minZ) / 2);
    }
    // Start is called before the first frame update
    protected override void Awake()
    {
        if (!initialized)
        {
            Initialize();
        }
        if (placed)
            OnPlaced();
    }
    protected override void Initialize()
    {
        GetComponentsInChildren<Wall>().ToList().ForEach(w => w.UnPlace());
        GetComponentsInChildren<Floor>().ToList().ForEach(f => f.UnPlace());
        GetComponentsInChildren<DoubleDoor>().ToList().ForEach(d => d.UnPlace());
        GetComponentsInChildren<StaticObject>().ToList().ForEach(o => o.UnPlace());
        GetComponentsInChildren<SafeAreaPart>().ToList().ForEach(s => s.UnPlace());
        initialized = true;
    }
    // Update is called once per frame
    void Update()
    {

    }
    public override void OnPlaced()
    {
        placed = true;
        if (transform.parent?.name != "Placeable Buildings")
        {
            buildingNumber = ++buildingCount;
            // name = "Building " + buildingNumber.ToString();

            CreateContainers();
            walls.AddRange(GetComponentsInChildren<Wall>().ToList());
            walls.ForEach(w => w.OnPlaced());

            doors.AddRange(GetComponentsInChildren<Door>().ToList());
            doors.ForEach(d => d.OnPlaced());

            floors.AddRange(GetComponentsInChildren<Floor>().ToList());
            floors.ForEach(f => f.OnPlaced());

            doors.AddRange(GetComponentsInChildren<DoubleDoor>().ToList());
            doors.ForEach(d => d.OnPlaced());

            objects.AddRange(GetComponentsInChildren<StaticObject>().ToList());
            objects.ForEach(o => o.OnPlaced());

            fires.AddRange(GetComponentsInChildren<FireSpreading>().ToList());

            safeAreas.AddRange(GetComponentsInChildren<SafeAreaPart>().ToList());
            safeAreas.ForEach(s => s.OnPlaced());

            pedestrians.AddRange(GetComponentsInChildren<Pedestrian>().ToList());
            pedestrians.ForEach(p => p.OnPlaced());

            List<Vector3> wallPositions = FindObjectsOfType<Wall>().ToList().Select(w => w.transform.localPosition).ToList();
            float left = wallPositions.OrderBy(p => p.x).ToList()[0].x,
                right = wallPositions.OrderByDescending(p => p.x).ToList()[0].x,
                top = wallPositions.OrderBy(p => p.z).ToList()[0].z,
                bottom = wallPositions.OrderByDescending(p => p.z).ToList()[0].z;

            distX = Mathf.Abs(right - left);
            distZ = Mathf.Abs(top - bottom);
        }
    }
    void CreateContainers()
    {
        //if (transform.Find("Pedestrians") == null)
        //{
        //    pedestriansContainer = new GameObject("Pedestrians");
        //    pedestriansContainer.transform.position = transform.position;
        //    pedestriansContainer.transform.parent = transform;
        //}
        //else
        //{
        //    firesContainer = transform.Find("Pedestrians").gameObject;
        //}

        if (transform.Find("Walls") == null)
        {
            wallsContainer = new GameObject("Walls");
            wallsContainer.transform.position = transform.position;
            wallsContainer.transform.parent = transform;
        }
        else
        {
            wallsContainer = transform.Find("Walls").gameObject;
        }

        if (transform.Find("Doors") == null)
        {
            doorsContainer = new GameObject("Doors");
            doorsContainer.transform.position = transform.position;
            doorsContainer.transform.parent = transform;
        }
        else
        {
            doorsContainer = transform.Find("Doors").gameObject;
        }

        if (transform.Find("Floors") == null)
        {
            floorsContainer = new GameObject("Floors");
            floorsContainer.transform.position = transform.position;
            floorsContainer.transform.parent = transform;
        }
        else
        {
            floorsContainer = transform.Find("Floors").gameObject;
        }

        if (transform.Find("Objects") == null)
        {
            objectsContainer = new GameObject("Objects");
            objectsContainer.transform.position = transform.position;
            objectsContainer.transform.parent = transform;
        }
        else
        {
            objectsContainer = transform.Find("Objects").gameObject;
        }

        if (transform.Find("Fires") == null)
        {
            firesContainer = new GameObject("Fires");
            firesContainer.transform.position = transform.position;
            firesContainer.transform.parent = transform;
        }
        else
        {
            firesContainer = transform.Find("Fires").gameObject;
        }

        if (transform.Find("Safe Areas") == null)
        {
            safeAreasContainer = new GameObject("Safe Areas");
            safeAreasContainer.transform.position = transform.position;
            safeAreasContainer.transform.parent = transform;
        }
        else
        {
            safeAreasContainer = transform.Find("Safe Areas").gameObject;
        }
    }
    void UpdateWalls(Wall newWall)
    {
        walls.Add(newWall);
        newWall.transform.parent = wallsContainer.transform;
        if (boundingBox == null && walls.Count > 1)
        {
            boundingBox = new Bounds();

            List<Vector3> walled = walls.Select(w => (w.transform.position + w.transform.forward * 3f)).ToList();
            Vector3 minX = walled.OrderBy(w => w.x).ToList()[0];
            Vector3 maxX = walled.OrderByDescending(w => w.x).ToList()[0];
            Vector3 minZ = walled.OrderBy(w => w.z).ToList()[0];
            Vector3 maxZ = walled.OrderByDescending(w => w.z).ToList()[0];

            boundingBox.center = new Vector3(Vector3.Lerp(minX, maxX, 0.5f).x, 0, Vector3.Lerp(minZ, maxZ, 0.5f).z);
            boundingBox.size = new Vector3(maxX.x - minX.x, 1.5f, maxZ.z - minZ.z);
        }

        List<Vector3> wallPositions = GetComponentsInChildren<Wall>().ToList().Select(w => w.transform.position).ToList();
        float left = wallPositions.OrderBy(p => p.x).ToList()[0].x,
            right = wallPositions.OrderByDescending(p => p.x).ToList()[0].x,
            top = wallPositions.OrderBy(p => p.z).ToList()[0].z,
            bottom = wallPositions.OrderByDescending(p => p.z).ToList()[0].z;

        distX = Mathf.Abs(right - left);
        distZ = Mathf.Abs(top - bottom);
    }
    public Vector2 NormalizedPosition(Vector3 position)
    {
        return new Vector2(position.x / distX, position.z / distZ);
    }
    public List<GameObject> GetExits()
    {
        return doors.FindAll(d => d.gameObject.CompareTag("Exit")).Select(d => d.gameObject).ToList();
    }
    public float NormalizeDistance(float distance)
    {
        return distance / Mathf.Sqrt(Mathf.Pow(distX, 2) + Mathf.Pow(distZ, 2));
    }
    public Vector3 GetClosestSafeArea(Vector3 localPosition)
    {
        return safeAreas.OrderByDescending(sa => Vector3.Distance(localPosition, sa.transform.localPosition)).ToList()[0].transform.localPosition;
    }
    protected override void SetUpRightClickOptions()
    {
        UnityEvent saveEvent = new UnityEvent();
        saveEvent.AddListener(SaveBuilding);
        rightClickEvents.Add("Save building", saveEvent);
    }
    protected override void ChangeColor(Color newColor)
    {
        foreach (Wall wall in walls)
        {
            wall.HighlightError();
        }
    }
    protected override void ResetColor()
    {
        foreach (Wall wall in walls)
        {
            wall.UnHighlightError();
        }
    }

    public void StartEvacuation()
    {
        foreach (FireSpreading fire in fires)
        {
            fire.enabled = true;
            fire.StartFire();
        }

        foreach (Pedestrian pedestrian in pedestrians)
        {
            pedestrian.StartEvacuation();
        }
    }
    public void StartSimulation()
    {
        foreach (Pedestrian pedestrian in pedestrians)
        {
            pedestrian.StartForSimulation();
        }
    }
    public void StopSimulation()
    {
        foreach (Pedestrian pedestrian in pedestrians)
        {
            pedestrian.StopEvacuation();
        }

        foreach (FireSpreading fire in fires)
        {
            fire.StopFire();
        }
    }
    public void StartLearning()
    {
        foreach (Pedestrian pedestrian in pedestrians)
        {
            pedestrian.StartLearning();
        }
    }
    public void FixCenter()
    {
        List<Vector3> allPositions = GetComponentsInChildren<PlaceableObject>().ToList().Select(o => o.transform.position).ToList();
        float minX, maxX, minZ, maxZ;
        minX = allPositions.OrderBy(o => o.x).ToList()[0].x;
        minZ = allPositions.OrderBy(o => o.z).ToList()[0].z;
        maxX = allPositions.OrderByDescending(o => o.x).ToList()[0].x;
        maxZ = allPositions.OrderByDescending(o => o.z).ToList()[0].z;
        Vector3 newCenter = new Vector3(minX + Mathf.Abs(maxX - minX) / 2f, 0f, minZ + Mathf.Abs(maxZ - minZ) / 2f);
        Vector3 offset = transform.position - newCenter;
        GetComponentsInChildren<PlaceableObject>().ToList().ForEach(o => o.transform.localPosition += offset);
        transform.position += offset;
    }
    public override Vector3 CheckGround()
    {
        Vector3 groundCheckOrigin = transform.position + Vector3.up * MouseModeManager.Instance.LevelHeight / 2f;

        RaycastHit groundHit;
        floorDetected = Physics.BoxCast(groundCheckOrigin, new Vector3(distX / 2f, 0.1f, distZ / 2f), Vector3.down, out groundHit, Quaternion.identity, MouseModeManager.Instance.LevelHeight - 0.05f, LayerMask.GetMask("Ground", "Floors"));

        if (floorDetected)
        {
            return new Vector3(0f, groundHit.point.y, 0f);
        }
        else
        {
            return -Vector3.one;
        }
    }
    public override XmlNode ToXmlNode(XmlDocument xmlDoc)
    {
        Debug.Log("Saving building " + name);
        XmlNode buildingNode = xmlDoc.CreateElement(name.Replace(' ', '_'));

        XmlNode wallsNode = xmlDoc.CreateElement("Walls");
        GetComponentsInChildren<Wall>().ToList().ForEach(w => wallsNode.AppendChild(w.ToXmlNode(xmlDoc)));
        buildingNode.AppendChild(wallsNode);

        XmlNode doorsNode = xmlDoc.CreateElement("DoubleDoors");
        GetComponentsInChildren<Door>().ToList().ForEach(d => doorsNode.AppendChild(d.ToXmlNode(xmlDoc)));
        buildingNode.AppendChild(doorsNode);

        XmlNode floorsNode = xmlDoc.CreateElement("Floors");
        GetComponentsInChildren<Floor>().ToList().ForEach(f => floorsNode.AppendChild(f.ToXmlNode(xmlDoc)));
        buildingNode.AppendChild(floorsNode);

        XmlNode objectsNode = xmlDoc.CreateElement("StaticObjects");
        GetComponentsInChildren<StaticObject>().ToList().ForEach(o => objectsNode.AppendChild(o.ToXmlNode(xmlDoc)));
        buildingNode.AppendChild(objectsNode);

        XmlNode firesNode = xmlDoc.CreateElement("Fires");
        GetComponentsInChildren<FireSpreading>().ToList().ForEach(f => firesNode.AppendChild(f.ToXmlNode(xmlDoc)));
        buildingNode.AppendChild(firesNode);

        XmlNode safeAreasNode = xmlDoc.CreateElement("SafeAreas");
        GetComponentsInChildren<SafeAreaPart>().ToList().ForEach(s => safeAreasNode.AppendChild(s.ToXmlNode(xmlDoc)));
        buildingNode.AppendChild(safeAreasNode);

        XmlNode pedestriansNode = xmlDoc.CreateElement("Pedestrians");
        GetComponentsInChildren<Pedestrian>().ToList().ForEach(p => pedestriansNode.AppendChild(p.ToXmlNode(xmlDoc)));
        buildingNode.AppendChild(pedestriansNode);

        buildingNode.AppendChild(base.ToXmlNode(xmlDoc));

        return buildingNode;
    }
    public override void LoadNodeFromXml(XmlNode buildingNode)
    {
        CreateContainers();

        LoadObjectsType(buildingNode["Walls"], "Wall", wallsContainer, true);
        LoadObjectsType(buildingNode["DoubleDoors"], "DoubleDoor", doorsContainer, true);
        LoadObjectsType(buildingNode["Floors"], "Floor", floorsContainer, true);
        LoadObjectsType(buildingNode["Fires"], "Fire", firesContainer, true);
        if (firesContainer.GetComponentsInChildren<FireSpreading>().ToList().Count > 0 && !SimulationManager.Playing)
            EventManager.TriggerEvent("StartEvacuation");
        LoadObjectsType(buildingNode["SafeAreas"], "SafeArea", safeAreasContainer, true);


        XmlNode objectsNode = buildingNode["StaticObjects"];
        Dictionary<string, int> counter = new Dictionary<string, int>();
        foreach (XmlNode objectNode in objectsNode.ChildNodes)
        {
            string objectName = objectNode.Name.Replace('_', ' ');

            if (!counter.ContainsKey(objectName))
            {
                counter.Add(objectName, 0);
            }
            counter[objectName] += 1;

            GameObject newObject = GameObject.Instantiate(PlaceableObject.Placeables[objectName], Vector3.zero, Quaternion.identity, objectsContainer.transform);
            newObject.SetActive(true);
            // newObject.name = string.Format("{0} {1}", objectName, counter[objectName]);
            newObject.name = objectName;
            newObject.GetComponent<StaticObject>().LoadNodeFromXml(objectNode);
        }
        counter.Clear();

        XmlNode pedestrianNodes = buildingNode["Pedestrians"];
        int pedCounter = 0;
        foreach (XmlNode pedestrianNode in pedestrianNodes.ChildNodes)
        {
            string pedestrianName = pedestrianNode.Name.Replace('_', ' ');

            if (!counter.ContainsKey(pedestrianName))
            {
                counter.Add(pedestrianName, 0);
            }
            counter[pedestrianName] += 1;

            GameObject newPedestrian = GameObject.Instantiate(PlaceableObject.Placeables[pedestrianName], transform);
            // newPedestrian.name = string.Format("{0} {1}", pedestrianName, counter[pedestrianName]);
            newPedestrian.name = string.Format("{0} {1}", pedestrianName, pedCounter.ToString());
            newPedestrian.GetComponent<RenderTextureSensorComponent>().RenderTexture.name = newPedestrian.name;
            newPedestrian.SetActive(true);
            pedCounter++;
            newPedestrian.GetComponent<Pedestrian>().LoadNodeFromXml(pedestrianNode);
        }

        FixCenter();
        name = buildingNode.Name.Replace('_', ' ');
        transform.position = String2Vector3(buildingNode["Building"].Attributes["position"].Value);
        transform.rotation = Quaternion.Euler(String2Vector3(buildingNode["Building"].Attributes["rotation"].Value));
        transform.SetParent(GameObject.Find("Placed Buildings").transform);
        OnPlaced();
    }
    private void LoadObjectsType(XmlNode xmlContainerNode, string templateObjectName, GameObject container, bool countPlaced = true)
    {
        container.transform.SetParent(transform);
        GameObject placeableObject = PlaceableObject.Placeables[templateObjectName];
        int counter = 1;
        placeableObject.SetActive(true);
        foreach (XmlNode objectNode in xmlContainerNode.ChildNodes)
        {
            GameObject newObject = GameObject.Instantiate(placeableObject, Vector3.zero, Quaternion.identity, container.transform);
            if (countPlaced)
            {
                // newObject.name = string.Format("{0} {1}", templateObjectName, counter.ToString());
                newObject.name = templateObjectName;
                counter++;
            }
            else
            {
                newObject.name = templateObjectName;
            }
            newObject.GetComponent<PlaceableObject>().LoadNodeFromXml(objectNode);
        }

        placeableObject.SetActive(false);
    }
    private void SaveBuilding()
    {
#if UNITY_EDITOR
        Debug.Log("Saving building");
        walls.ForEach(w => w.UnPlace());
        doors.ForEach(d => d.UnPlace());
        floors.ForEach(f => f.UnPlace());
        doors.ForEach(d => d.UnPlace());
        objects.ForEach(o => o.UnPlace());
        safeAreas.ForEach(s => s.UnPlace());
        FixCenter();
        PrefabUtility.SaveAsPrefabAsset(gameObject, string.Format("Assets/Resources/Buildings/{0}.prefab", name));
#endif
    }
    private void FixMaterials()
    {
#if UNITY_EDITOR
        Wall templateWall = GameObject.Find("Placeables").GetComponentInChildren<Wall>();
        foreach (Wall wall in GetComponentsInChildren<Wall>())
        {
            wall.GetComponent<Renderer>().sharedMaterials = templateWall.GetComponent<Renderer>().sharedMaterials;
        }

        Floor templateFloor = GameObject.Find("Placeables").GetComponentInChildren<Floor>();
        foreach (Floor floor in GetComponentsInChildren<Floor>())
        {
            floor.GetComponent<Renderer>().sharedMaterials = templateFloor.GetComponent<Renderer>().sharedMaterials;
        }

        DoubleDoor templateDoubleDoor = GameObject.Find("Placeables").GetComponentInChildren<DoubleDoor>();
        foreach (DoubleDoor doubledoor in GetComponentsInChildren<DoubleDoor>())
        {
            doubledoor.transform.GetChild(0).GetComponent<Renderer>().sharedMaterials = templateDoubleDoor.transform.GetChild(0).GetComponent<Renderer>().sharedMaterials;
            doubledoor.transform.GetChild(1).GetComponent<Renderer>().sharedMaterials = templateDoubleDoor.transform.GetChild(1).GetComponent<Renderer>().sharedMaterials;
        }

        List<Renderer> renderers = GameObject.Find("Placeables").transform.Find("Placeable Objects").GetComponentsInChildren<Renderer>().ToList();
        foreach (StaticObject _object in transform.Find("Objects").GetComponentsInChildren<StaticObject>())
        {
            // string[] objectNameParts = _object.name.Split(' ');
            // string[] objectNameWithoutIndex = _object.name.Split(' ').Take(Mathf.Min(objectNameParts.Length - 1, 1)).ToArray();
            // string objectName = string.Join(" ", objectNameWithoutIndex);
            string objectName = _object.name;
            print(_object.name + " " + objectName);
            _object.GetComponent<Renderer>().sharedMaterials = renderers.Find(o => o.name.Contains(objectName)).sharedMaterials;
        }
#endif
    }
}
