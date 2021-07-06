using Unity.Barracuda;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Xml;
using UnityEngine.Rendering.UI;

public class PlaceableObject : MonoBehaviour
{
    protected bool initialized = false;
    private static Dictionary<string, GameObject> allPlaceables = new Dictionary<string, GameObject>();
    public static Dictionary<string, GameObject> Placeables { get { return allPlaceables; } }

    protected bool canBeHighlighted = true;
    protected bool highlightedSelection = false;
    protected Color highlightedSelectionColor = Color.green;
    protected bool highlightedError = false;
    protected Color highlightedErrorColor = Color.red;

    public virtual Vector3 HalfSizes { get { return GetComponent<Renderer>().bounds.size / 2; } }
    public virtual Vector3 Center { get { return GetComponent<Renderer>().bounds.center; } }

    protected bool floorDetected = false;

    [SerializeField]
    protected bool placed = false;
    public bool IsPlaced { get { return placed; } }
    protected Vector3 placedPosition;
    public Vector3 PlacedPosition { get { return placedPosition; } }

    protected bool selected = false;
    public bool IsSelected { get { return selected; } set { if (value) Selected(); else Unselected(); } }
    protected bool mouseOver = false;
    protected Dictionary<string, UnityEvent> rightClickEvents;
    public Dictionary<string, UnityEvent> RightClickEvents { get { return rightClickEvents; } }

    // Start is called before the first frame update
    protected virtual void Awake()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (placed)
            OnPlaced();

        if (transform.parent?.parent?.name == "Placeables" && !allPlaceables.ContainsKey(name))
        {
            allPlaceables.Add(name, gameObject);
        }
    }
    protected virtual void Initialize()
    {
        foreach (Collider collider in GetComponents<Collider>())
            collider.enabled = false;
        rightClickEvents = new Dictionary<string, UnityEvent>();
        SetUpRightClickOptions();
        initialized = true;
    }
    // Update is called once per frame
    void Update()
    {

    }
    private void OnEnable()
    {
        if (PointerMode.Instance != null)   // only null when training agent (No mouse mode manager is placed)
            PointerMode.Instance.RegisterSelectableObject(gameObject);
    }
    private void OnDisable()
    {
        if (PointerMode.Instance != null)   // only null when training agent (No mouse mode manager is placed)
            PointerMode.Instance.UnRegisterSelectableObject(gameObject);
    }
    protected virtual void SetUpRightClickOptions()
    {
        // Debug.Log("No right click events for object " + name);
    }
    public virtual void Selected()
    {
        selected = true;
        Highlight();
    }
    public virtual void Unselected()
    {
        selected = false;
        UnHighlight();
    }
    private bool CanBeHighlighted()
    {
        if (PointerMode.Instance != null)
            return canBeHighlighted && PointerMode.Instance.IsActive && placed;
        return false;
    }
    public void HighlightError()
    {
        highlightedError = true;
        DetermineHighlightColor();
    }
    public void UnHighlightError()
    {
        highlightedError = false;
        DetermineHighlightColor();
    }
    public void Highlight()
    {
        if (!CanBeHighlighted())
            return;
        highlightedSelection = true;
        DetermineHighlightColor();
    }
    public void UnHighlight()
    {
        highlightedSelection = false;
        DetermineHighlightColor();
    }
    private void DetermineHighlightColor()
    {
        if (highlightedError)
        {
            ChangeColor(highlightedErrorColor);
        }
        else if (highlightedSelection)
        {
            ChangeColor(highlightedSelectionColor);
        }
        else
        {
            ResetColor();
        }
    }
    protected virtual void ChangeColor(Color newColor)
    {
        throw new NotImplementedException();
    }
    protected virtual void ResetColor()
    {
        throw new NotImplementedException();
    }

    public virtual void OnPlaced()
    {
        placed = true;
        placedPosition = transform.position;
        foreach (Collider collider in GetComponents<Collider>())
            collider.enabled = true;
    }
    public virtual void UnPlace()
    {
        if (placed)
        {
            Initialize();
            placed = false;
            foreach (Collider collider in GetComponents<Collider>())
                collider.enabled = false;
        }
    }
    //protected virtual void OnLeftClick()
    //{
    //    selected = true;
    //    Highlight();
    //}

    private void OnMouseOver()
    {
        mouseOver = true;
        Highlight();
    }
    private void OnMouseExit()
    {
        mouseOver = false;
        if (!selected)
            UnHighlight();
    }
    public virtual void SnapPosition()
    {

    }
    public virtual bool CanBePlaced()
    {
        return true;
    }
    public virtual Vector3 CheckGround()
    {
        Vector3 groundCheckOrigin = new Vector3(Center.x, Mathf.Max(0.5f, Center.y), Center.z);

        RaycastHit groundHit;
        floorDetected = Physics.BoxCast(groundCheckOrigin, new Vector3(HalfSizes.x, 0.1f, HalfSizes.z), Vector3.down, out groundHit, Quaternion.identity, MouseModeManager.Instance.LevelHeight - 0.05f, LayerMask.GetMask("Ground", "Floors"));

        if (floorDetected)
        {
            return new Vector3(0f, groundHit.point.y, 0f);
        }
        else
        {
            return -Vector3.one;
        }
    }
    public virtual XmlNode ToXmlNode(XmlDocument xmlDoc)
    {
        // split name by space, take all parts except the last one (which is its index)
        // string[] objectNameWithoutIndex = name.Split(' ').Take(name.Split(' ').Length - 1).ToArray();
        // join the parts by space and then replace space with '_'
        // string objectName = String.Join(" ", objectNameWithoutIndex).Replace(' ', '_');
        string objectName = name.Replace(' ', '_');
        XmlNode objectNode = xmlDoc.CreateElement(objectName);
        XmlAttribute positionAttribute = xmlDoc.CreateAttribute("position");
        positionAttribute.Value = transform.localPosition.ToString("R");
        XmlAttribute rotationAttrubte = xmlDoc.CreateAttribute("rotation");
        rotationAttrubte.Value = transform.rotation.eulerAngles.ToString("R");
        objectNode.Attributes.Append(positionAttribute);
        objectNode.Attributes.Append(rotationAttrubte);
        return objectNode;
    }
    protected Vector3 String2Vector3(string stringValue)
    {
        string[] positionAttr = stringValue.Trim(new char[] { '(', ')' }).ToString().Split(',');
        Vector3 outputVec = new Vector3();
        float attrVal;
        if (float.TryParse(positionAttr[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attrVal))
            outputVec.x = attrVal;
        if (float.TryParse(positionAttr[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attrVal))
            outputVec.y = attrVal;
        if (float.TryParse(positionAttr[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attrVal))
            outputVec.z = attrVal;

        return outputVec;
    }
    protected Quaternion String2Quaternion(string stringValue)
    {
        string[] positionAttr = stringValue.Trim(new char[] { '(', ')' }).ToString().Split(',');
        Quaternion outputVec = new Quaternion();
        float attrVal;
        if (float.TryParse(positionAttr[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attrVal))
            outputVec.x = attrVal;
        if (float.TryParse(positionAttr[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attrVal))
            outputVec.y = attrVal;
        if (float.TryParse(positionAttr[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attrVal))
            outputVec.z = attrVal;
        if (float.TryParse(positionAttr[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attrVal))
            outputVec.w = attrVal;
        return outputVec;
    }
    public virtual void LoadNodeFromXml(XmlNode xmlNode)
    {
        transform.localPosition = String2Vector3(xmlNode.Attributes["position"].Value);
        transform.localRotation = Quaternion.Euler(String2Vector3(xmlNode.Attributes["rotation"].Value));
    }
}
