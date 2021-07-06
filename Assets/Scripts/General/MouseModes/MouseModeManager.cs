using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.MLAgents;

public class MouseModeManager : MonoBehaviour
{
#if UNITY_EDITOR
    [CustomEditor(typeof(MouseModeManager))]
    public class customButton : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MouseModeManager mouseModeManager = (MouseModeManager)target;
            if (GUILayout.Button("Create mouse mode components"))
            {
                if (mouseModeManager.GetComponent<BuildingPlacementMode>() == null)
                    mouseModeManager.gameObject.AddComponent<BuildingPlacementMode>();
                if (mouseModeManager.GetComponent<DoubleDoorPlacementMode>() == null)
                    mouseModeManager.gameObject.AddComponent<DoubleDoorPlacementMode>();
                if (mouseModeManager.GetComponent<FireStarterMode>() == null)
                    mouseModeManager.gameObject.AddComponent<FireStarterMode>();
                if (mouseModeManager.GetComponent<FloorBuildingMode>() == null)
                    mouseModeManager.gameObject.AddComponent<FloorBuildingMode>();
                if (mouseModeManager.GetComponent<GrabMode>() == null)
                    mouseModeManager.gameObject.AddComponent<GrabMode>();
                if (mouseModeManager.GetComponent<NewObjectPlacementMode>() == null)
                    mouseModeManager.gameObject.AddComponent<NewObjectPlacementMode>();
                if (mouseModeManager.GetComponent<PedestrianPlacementMode>() == null)
                    mouseModeManager.gameObject.AddComponent<PedestrianPlacementMode>();
                if (mouseModeManager.GetComponent<PointerMode>() == null)
                    mouseModeManager.gameObject.AddComponent<PointerMode>();
                if (mouseModeManager.GetComponent<SafeAreaMode>() == null)
                    mouseModeManager.gameObject.AddComponent<SafeAreaMode>();
                if (mouseModeManager.GetComponent<StaircasePlacementMode>() == null)
                    mouseModeManager.gameObject.AddComponent<StaircasePlacementMode>();
                if (mouseModeManager.GetComponent<WallBuildingMode>() == null)
                    mouseModeManager.gameObject.AddComponent<WallBuildingMode>();
            }
        }

    }
#endif
    // Check to see if we're about to be destroyed.
    private static bool m_ShuttingDown = false;
    private static object m_Lock = new object();
    private static MouseModeManager m_Instance;

    public static MouseModeManager Instance
    {
        get
        {
            if (m_ShuttingDown)
            {
                Debug.LogWarning("[Singleton] Instance '" + typeof(MouseModeManager) +
                    "' already destroyed. Returning null.");
                return null;
            }

            lock (m_Lock)
            {
                if (m_Instance == null)
                {
                    // Search for existing instance.
                    m_Instance = (MouseModeManager)FindObjectOfType(typeof(MouseModeManager));
                    // Create new instance if one doesn't already exist.
                    if (m_Instance == null)
                    {
                        Debug.LogWarning("No object created with MouseModeManager component.");
                    }
                }

                return m_Instance;
            }
        }
    }

    private void OnApplicationQuit()
    {
        m_ShuttingDown = true;
    }


    private void OnDestroy()
    {
        m_ShuttingDown = true;
    }
    private MouseMode currentMouseMode;

    private int maxLevels = 3;
    private int currentLevel = 0;
    public int CurrentLevel { get { return currentLevel; } }
    public float LevelHeight { get { return WallBuildingMode.Instance.WallHeight; } }
    public float CurrentHeight { get { return currentLevel * LevelHeight; } }

    private void Start()
    {
        DontDestroyOnLoad(Instance);
        Initialize();
    }
    // Start is called before the first frame update
    void Initialize()
    {
        currentMouseMode = PointerMode.Instance;
        currentMouseMode.OnModeEnter();
    }
    public bool CanUpdateMode()
    {
        if (PopupWindowManager.WindowVisible)
        {
            return false;
        }
        else if (currentMouseMode == PointerMode.Instance)
        {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = Input.mousePosition;
            List<RaycastResult> raycastResultList = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, raycastResultList);
            // raycastResultList.FindAll(r => r.gameObject.GetComponent<MouseUIClickthrough>() != null);
            for (int i = 0; i < raycastResultList.Count; i++)
            {
                if (raycastResultList[i].gameObject.GetComponent<MouseUIClickthrough>())
                {
                    raycastResultList.RemoveAt(i);
                    i--;
                }
            }
            return raycastResultList.Count == 0;
        }
        else if (EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    // Update is called once per frame
    void Update()
    {
        UpdatePlacementLevel();
    }
    private void UpdatePlacementLevel()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.mouseScrollDelta.y < 0)
        {
            if (currentLevel < maxLevels)
            {
                currentLevel++;
            }
        }
        else if (Input.GetKey(KeyCode.LeftControl) && Input.mouseScrollDelta.y > 0)
        {
            if (currentLevel > 0)
                currentLevel--;
        }
    }
    public void EnableMode(MouseMode newMode)
    {
        currentMouseMode.OnModeExit();
        if (newMode == currentMouseMode && newMode != PointerMode.Instance)
        {
            currentMouseMode = PointerMode.Instance;
            currentMouseMode.OnModeEnter();
            return;
        }
        else
        {
            currentMouseMode = newMode;
            currentMouseMode.OnModeEnter();
        }
    }
    public void CancelMode()
    {
        currentMouseMode.OnModeExit();
        currentMouseMode = PointerMode.Instance;
        currentMouseMode.OnModeEnter();
    }
    public void PlaceNewObject(string category, GameObject newObject)
    {
        switch (category)
        {
            case "Objects":
                PlaceNewStaticObject(newObject);
                break;
            case "Staircase":
                PlaceNewStaircaseObject(newObject);
                break;
            case "Pedestrians":
                PlaceNewPedestrian(newObject);
                break;
            case "Buildings":
                PlaceNewBuilding(newObject);
                break;
        }
    }
    private void PlaceNewStaircaseObject(GameObject newObject)
    {
        if (currentMouseMode != StaircasePlacementMode.Instance)
        {
            currentMouseMode.OnModeExit();
            currentMouseMode = StaircasePlacementMode.Instance;
        }
        StaircasePlacementMode.Instance.OnModeEnter(newObject);
    }
    private void PlaceNewStaticObject(GameObject newObject)
    {
        if (currentMouseMode != NewObjectPlacementMode.Instance)
        {
            currentMouseMode.OnModeExit();
            currentMouseMode = NewObjectPlacementMode.Instance;
        }
        NewObjectPlacementMode.Instance.OnModeEnter(newObject);
    }
    private void PlaceNewPedestrian(GameObject newPedestrian)
    {
        if (currentMouseMode != PedestrianPlacementMode.Instance)
        {
            currentMouseMode.OnModeExit();
            currentMouseMode = PedestrianPlacementMode.Instance;
        }
        PedestrianPlacementMode.Instance.OnModeEnter(newPedestrian);
    }
    private void PlaceNewBuilding(GameObject newBuilding)
    {
        if (currentMouseMode != BuildingPlacementMode.Instance)
        {
            currentMouseMode.OnModeExit();
            currentMouseMode = BuildingPlacementMode.Instance;
        }
        BuildingPlacementMode.Instance.OnModeEnter(newBuilding);
    }
}
