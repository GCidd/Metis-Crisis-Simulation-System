using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PointerMode : MouseModeSingleton<PointerMode>
{
    [SerializeField] private Button uiButton;
    private LayerMask objectsLayer;
    private bool ignoreUI = false;

    private List<GameObject> selectableObjects = new List<GameObject>();
    public void RegisterSelectableObject(GameObject _go) { selectableObjects.Add(_go); }
    public void UnRegisterSelectableObject(GameObject _go) { selectableObjects.Remove(_go); }
    private RectTransform selectionBox;
    private Canvas canvas;
    private Vector2 dragStartPosition;
    private bool isSelecting = false; public bool IgnoreUI { get { return ignoreUI; } }
    private List<GameObject> selectedObjects = new List<GameObject>();

    private Vector3 rightClickLocation;
    private RightContextMenu rightContextMenu;
    private bool rightclickDown = false;
    private bool rightClickDownHandled = false;
    public bool RightClickDownHandled { get { return rightClickDownHandled; } set { rightClickDownHandled = value; } }

    private struct ClipboardObjectInfo
    {
        public GameObject _object;
        public Vector3 relativePosition;   // relative position from the center if multiple objects were copied
        public Transform parent;
    }
    private static List<ClipboardObjectInfo> clipboardObjects;
    public void SetClipboard(List<GameObject> objects)
    {
        clipboardObjects.Clear();
        Ray ray = Camera.main.ScreenPointToRay(rightClickLocation);
        RaycastHit rayHit;
        bool hitSmthing = Physics.Raycast(ray, out rayHit, LayerMask.GetMask("Ground", "Floors"));

        if (!hitSmthing)
            return;

        foreach (GameObject _o in objects)
        {
            if (_o.name == "Ground") continue;
            ClipboardObjectInfo clipInfo = new ClipboardObjectInfo();
            clipInfo._object = _o;
            clipInfo.parent = _o.transform.parent;
            clipInfo.relativePosition = new Vector3(_o.transform.position.x - rayHit.point.x, 0, _o.transform.position.z - rayHit.point.z);
            clipboardObjects.Add(clipInfo);
        }
    }
    public void SetClipboard(GameObject _o)
    {
        Ray ray = Camera.main.ScreenPointToRay(rightClickLocation);
        RaycastHit rayHit;
        bool hitSmthing = Physics.Raycast(ray, out rayHit, LayerMask.GetMask("Ground", "Floors"));

        if (!hitSmthing)
            return;

        ClipboardObjectInfo clipInfo = new ClipboardObjectInfo();
        clipInfo._object = _o;
        clipInfo.relativePosition = new Vector3(_o.transform.position.x - rayHit.point.x, 0, _o.transform.position.z - rayHit.point.z);
        clipInfo.parent = _o.transform.parent;
        clipboardObjects.Add(clipInfo);
    }
    public void PasteClipboard()
    {
        Ray ray = Camera.main.ScreenPointToRay(rightClickLocation);
        RaycastHit rayHit;
        bool hitSmthing = Physics.Raycast(ray, out rayHit, LayerMask.GetMask("Ground", "Floors"));

        if (hitSmthing)
        {
            foreach (ClipboardObjectInfo clipInfo in clipboardObjects)
            {
                GameObject pastedObject = GameObject.Instantiate(clipInfo._object);
                pastedObject.transform.position = rayHit.point + clipInfo.relativePosition;
                pastedObject.transform.SetParent(clipInfo.parent);
                pastedObject.GetComponent<PlaceableObject>().OnPlaced();
            }
        }
        clipboardObjects.Clear();
    }

    private void Awake()
    {
        m_Instance = this;
        selectionBox = GameObject.Find("SelectionBox").GetComponent<RectTransform>();
        selectionBox.gameObject.SetActive(false);
        objectsLayer = LayerMask.GetMask("Objects");
        uiButton = GameObject.Find("Pointer Mode Button").GetComponent<Button>();
        uiButton.onClick.AddListener(delegate { MouseModeManager.Instance.EnableMode(Instance); });
        selectionBox.sizeDelta = Vector2.zero;
        canvas = GameObject.FindObjectOfType<Canvas>();
        rightContextMenu = GameObject.FindObjectOfType<RightContextMenu>();
        clipboardObjects = new List<ClipboardObjectInfo>();
    }
    void SetupDefaultEvents()
    {
        UnityEvent copyEvent = new UnityEvent();
        copyEvent.AddListener(CopyObject);
        rightContextMenu.AddContextButton("Copy", copyEvent, selectedObjects.Count > 0);

        UnityEvent pasteEvent = new UnityEvent();
        pasteEvent.AddListener(PasteClipboard);
        rightContextMenu.AddContextButton("Paste", pasteEvent, clipboardObjects.Count > 0);

        UnityEvent deleteEvent = new UnityEvent();
        deleteEvent.AddListener(DeleteSelectedObjects);
        rightContextMenu.AddContextButton("Delete", deleteEvent, selectedObjects.Count > 0);
    }
    void CopyObject()
    {
        if (selectedObjects.Count == 1)
        {
            SetClipboard(selectedObjects[0]);
        }
        else if (selectedObjects.Count > 1)
        {
            SetClipboard(selectedObjects);
        }
    }
    void DeleteSelectedObjects()
    {
        foreach (GameObject _o in selectedObjects)
        {
            _o.SetActive(false);
            GameObject.Destroy(_o);
        }
        selectedObjects.Clear();
    }
    public override void OnModeEnter()
    {
        isSelecting = false;
        uiButton.GetComponent<ToggleButtonImage>().SetState(true);
        isActive = true;
    }

    public override void OnModeExit()
    {
        isSelecting = false;
        uiButton.GetComponent<ToggleButtonImage>().SetState(false);
        isActive = false;
    }

    public void Update()
    {
        if (!CanUpdate())
            return;

        CheckRightClick();
        HandleSelection();
    }
    void CheckRightClick()
    {
        if (Input.GetMouseButtonDown(1))
            rightclickDown = true;

        if (rightClickDownHandled)
            rightclickDown = false;

        if (Input.GetMouseButtonUp(1) && rightclickDown && !rightClickDownHandled)
        {
            rightContextMenu.Clear();
            Ray mouseToWorldRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            //Shoots a ray into the 3D world starting at our mouseposition
            if (Physics.Raycast(mouseToWorldRay, out hitInfo, Mathf.Infinity, ~LayerMask.GetMask("Ground")))
            {   // one object hit with right click
                if (selectedObjects.Contains(hitInfo.collider.gameObject))
                {   // object hit is already selected
                    Dictionary<string, UnityEvent> commonEvents = new Dictionary<string, UnityEvent>();
                    List<string> commonEventNames = selectedObjects[0].GetComponent<PlaceableObject>().RightClickEvents.Keys.ToList();
                    selectedObjects.Skip(1).ToList().ForEach(o => commonEventNames = commonEventNames.Intersect(o.GetComponent<PlaceableObject>().RightClickEvents.Keys).ToList());

                    foreach (string eventName in commonEventNames)
                    {
                        UnityEvent callAllCommonEvents = new UnityEvent();
                        List<UnityEvent> allEvents = selectedObjects.Select(so => so.GetComponent<PlaceableObject>().RightClickEvents[eventName]).ToList();
                        callAllCommonEvents.AddListener(delegate { allEvents.ForEach(e => e.Invoke()); });
                        commonEvents.Add(eventName, callAllCommonEvents);
                    }
                    rightContextMenu.AddContextButtonsGroup(commonEvents);
                    rightContextMenu.AttachedObject = null;
                }
                else
                {   // object hit not already selected
                    ClearSelected();
                    UpdateSelection(hitInfo.collider.GetComponentInParent<PlaceableObject>(), true);
                    // Get all the common events from the selected objects
                    if (hitInfo.collider.GetComponent<PlaceableObject>())
                    {
                        Dictionary<string, UnityEvent> objectEvents = hitInfo.collider.GetComponent<PlaceableObject>().RightClickEvents;
                        foreach (string eventName in objectEvents.Keys)
                        {
                            if (!objectEvents.ContainsKey(eventName))
                                objectEvents.Add(eventName, objectEvents[eventName]);
                        }
                        rightContextMenu.AddContextButtonsGroup(objectEvents);
                        rightContextMenu.AttachedObject = hitInfo.collider.gameObject;
                    }
                }
            }
            else
            {   // no object hit with right click
                ClearSelected();
                rightContextMenu.AttachedObject = null;
            }
            SetupDefaultEvents();
            ShowRightClickMenu();
            rightclickDown = false;
        }
    }
    void HandleSelection()
    {
        Vector2 localMousePos = rightContextMenu.GetComponent<RectTransform>().InverseTransformPoint(Input.mousePosition);
        if (rightContextMenu.GetComponent<RectTransform>().rect.Contains(localMousePos))
        {
            return;
        }

        // https://wiki.unity3d.com/index.php/SelectionBox
        if (Input.GetMouseButtonDown(0))
        {
            rightContextMenu.gameObject.SetActive(false);

            Ray mouseToWorldRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            //Shoots a ray into the 3D world starting at our mouseposition
            if (Physics.Raycast(mouseToWorldRay, out hitInfo, Mathf.Infinity, ~LayerMask.GetMask("Ground")))
            {
                //We check if we clicked on an object with a Selectable component
                PlaceableObject s = hitInfo.collider.GetComponentInParent<PlaceableObject>();
                if (s != null)
                {
                    //While holding the copyKey, we can add and remove objects from our selection
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        //Toggle the selection
                        UpdateSelection(s, !s.IsSelected);
                    }
                    else
                    {
                        //If the copyKey was not held, we clear our current selection and select only this unit
                        ClearSelected();
                        UpdateSelection(s, true);
                    }

                    //If we clicked on a Selectable, we don't want to enable our SelectionBox
                    return;
                }
            }

            if (selectionBox == null)
                return;
            //Storing these variables for the selectionBox
            dragStartPosition = Input.mousePosition;
            isSelecting = true;
        }

        //If we never set the selectionBox variable in the inspector, we are simply not able to drag the selectionBox to easily select multiple objects. 'Regular' selection should still work
        if (selectionBox == null)
            return;

        //We finished our selection box when the key is released
        if (Input.GetMouseButtonUp(0))
        {
            isSelecting = false;
        }

        selectionBox.gameObject.SetActive(isSelecting);

        if (isSelecting)
        {
            Bounds b = new Bounds();
            //The center of the bounds is inbetween startpos and current pos
            b.center = Vector3.Lerp(dragStartPosition, Input.mousePosition, 0.5f);
            //We make the size absolute (negative bounds don't contain anything)
            b.size = new Vector3(Mathf.Abs(dragStartPosition.x - Input.mousePosition.x),
                Mathf.Abs(dragStartPosition.y - Input.mousePosition.y),
                0);

            //To display our selectionbox image in the same place as our bounds
            selectionBox.position = b.center;
            selectionBox.sizeDelta = canvas.transform.InverseTransformVector(b.size);

            //Looping through all the selectables in our world (automatically added/removed through the Selectable OnEnable/OnDisable)
            foreach (GameObject selectable in selectableObjects)
            {
                if (selectable.layer == LayerMask.GetMask("Ground"))
                    continue;
                //If the screenPosition of the worldobject is within our selection bounds, we can add it to our selection
                Vector3 screenPos = Camera.main.WorldToScreenPoint(selectable.transform.position);
                screenPos.z = 0;
                UpdateSelection(selectable.GetComponent<PlaceableObject>(), b.Contains(screenPos));
            }
        }
    }
    void ShowRightClickMenu()
    {
        rightContextMenu.gameObject.SetActive(true);
        Vector2 menuSize = rightContextMenu.GetComponent<RectTransform>().rect.size;
        float x = Mathf.Clamp(Input.mousePosition.x, 0, Screen.width - menuSize.x * canvas.scaleFactor);
        float y = Mathf.Clamp(Input.mousePosition.y, menuSize.y * canvas.scaleFactor, Screen.height);
        rightContextMenu.GetComponent<RectTransform>().position = new Vector3(x, y, 0);
        rightClickLocation = Input.mousePosition;
    }
    /// <summary>
    /// Add or remove a Selectable from our selection
    /// </summary>
    /// <param name="s"></param>
    /// <param name="value"></param>
    void UpdateSelection(PlaceableObject s, bool value)
    {
        if (s.IsSelected != value)
            s.IsSelected = value;

        if (value && !selectedObjects.Contains(s.gameObject))
            selectedObjects.Add(s.gameObject);
        else if (!value)
            selectedObjects.Remove(s.gameObject);
    }

    /// <summary>
    /// Returns all Selectable objects with isSelected set to true
    /// </summary>
    /// <returns></returns>
    List<GameObject> GetSelected()
    {
        return selectableObjects.Where(x => x.GetComponent<PlaceableObject>().IsSelected).ToList();
    }

    /// <summary>
    /// Clears the full selection
    /// </summary>
    void ClearSelected()
    {
        selectedObjects.ForEach(x => x.GetComponent<PlaceableObject>().Unselected());
        selectedObjects.Clear();
    }
}
