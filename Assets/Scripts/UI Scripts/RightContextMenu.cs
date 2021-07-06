using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RightContextMenu : MonoBehaviour
{
    private List<GameObject> buttons;
    private List<GameObject> separators;
    private Transform sampleButton;
    private RectTransform contextRect;
    private GameObject separator;
    private GameObject attachedObject;
    public GameObject AttachedObject { set { attachedObject = value; } }
    private void Awake()
    {
        sampleButton = GetComponentsInChildren<LayoutElement>().ToList().Find(c => c.name.Contains("Button")).transform;
        separator = GetComponentsInChildren<LayoutElement>().ToList().Find(c => c.name == "Separator").gameObject;
        buttons = new List<GameObject>();
        separators = new List<GameObject>();
    }
    private void Start()
    {
        separator.SetActive(false);
        sampleButton.gameObject.SetActive(false);
        contextRect = GetComponent<RectTransform>();
    }
    void Update()
    {
        if (attachedObject != null)
        {
            transform.position = Camera.main.WorldToScreenPoint(attachedObject.transform.position);
        }
    }
    public void AddContextButtonsGroup(Dictionary<string, UnityEvent> events)
    {
        foreach (string eventName in events.Keys)
        {
            AddContextButton(eventName, events[eventName]);
        }
        AddSeparator();
    }
    public void AddContextButton(string buttonText, UnityEvent buttonEvent, bool enabled = true)
    {
        GameObject menuButton = buttons.Find(k => k.transform.GetComponentInChildren<Text>().text == buttonText);
        if (menuButton == null)
        {
            GameObject newButton = Instantiate(sampleButton.gameObject);
            newButton.transform.SetParent(sampleButton.parent);
            newButton.GetComponent<Button>().name = buttonText + " Button";
            newButton.GetComponent<Button>().interactable = enabled;
            newButton.GetComponent<Button>().onClick.AddListener(delegate { buttonEvent.Invoke(); this.gameObject.SetActive(false); });
            newButton.GetComponentInChildren<Text>().text = buttonText;
            Color defaultTextColor = newButton.GetComponentInChildren<Text>().color;
            newButton.GetComponentInChildren<Text>().color = new Color(defaultTextColor.r, defaultTextColor.g, defaultTextColor.b, (enabled ? defaultTextColor.a : 0.5f));
            newButton.transform.localScale = Vector3.one;
            newButton.SetActive(true);
            buttons.Add(newButton);
        }
        else
        {
            menuButton.GetComponent<Button>().enabled = enabled;
            menuButton.GetComponent<Button>().onClick.RemoveAllListeners();
            menuButton.GetComponent<Button>().onClick.AddListener(delegate { buttonEvent.Invoke(); this.gameObject.SetActive(false); });
        }
    }
    void AddSeparator()
    {
        GameObject newSeparator = Instantiate(separator);
        separators.Add(newSeparator);
        newSeparator.SetActive(true);
        newSeparator.transform.SetParent(separator.transform.parent);
    }
    public void Clear()
    {
        foreach (GameObject _button in buttons)
        {
            Destroy(_button);
        }
        foreach (GameObject _separator in separators)
        {
            Destroy(_separator);
        }
        separators.Clear();
        buttons.Clear();
    }
    private void OnDisable()
    {
        foreach (GameObject b in buttons)
        {
            Destroy(b);
        }
        buttons.Clear();
    }
}
