using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupDialogWindow : GenericWindow
{
    private Text windowNameText;
    private Text messageText;
    private InputField inputField;
    private Button yesButton;
    private Button noButton;
    private Button okButton;
    private GameObject yesNoContainer;
    private GameObject okContainer;

    private UnityEvent onYesEvent;
    private UnityEvent onNoEvent;
    private UnityEvent onCloseEvent;
    private UnityEvent<string> inputResultEvent;
    private bool closeOnInput = false;

    private static bool visible = false;
    public static bool Visible { get { return visible; } }
    protected override void Awake()
    {
        base.Awake();
        windowNameText = transform.Find("Header").GetComponentInChildren<Text>();
        messageText = transform.Find("Body").GetComponentInChildren<Text>();

        inputField = transform.Find("Body").GetComponentInChildren<InputField>();

        yesButton = GetComponentsInChildren<Button>().ToList().Find(b => b.name.ToLower().Contains("yes"));
        noButton = GetComponentsInChildren<Button>().ToList().Find(b => b.name.ToLower().Contains("no"));
        okButton = GetComponentsInChildren<Button>().ToList().Find(b => b.name.ToLower().Contains("ok"));

        yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(OnNoClicked);
        okButton.onClick.AddListener(OnOkClicked);

        yesNoContainer = transform.Find("Body").Find("YesNo Container").gameObject;
        okContainer = transform.Find("Body").Find("OK Container").gameObject;
    }
    private void Prepare()
    {
        closeOnInput = false;
        onYesEvent = null;
        onNoEvent = null;
        onCloseEvent = null;
        inputResultEvent = null;
    }
    public void ShowOKDialog(string windowName, string windowMessage, string okButtonText = "OK", UnityEvent onCloseEvent = null)
    {
        Prepare();

        windowNameText.text = windowName;
        messageText.text = windowMessage;

        this.onCloseEvent = onCloseEvent;
        onYesEvent = null;
        onNoEvent = null;
        okButton.GetComponentInChildren<Text>().text = okButtonText;
        yesNoContainer.SetActive(false);
        inputField.gameObject.SetActive(false);
        okContainer.SetActive(true);

        Open();
    }
    public void ShowYesNoDialog(string windowName, string windowMessage, string yesButtonText = "Yes", string noButtonText = "No", 
        UnityEvent onYesEvent = null, UnityEvent onNoEvent = null, UnityEvent onCloseEvent = null, bool closeOnInput = true)
    {
        Prepare();
        windowNameText.text = windowName;
        messageText.text = windowMessage;
        this.onCloseEvent = onCloseEvent;
        this.onYesEvent = onYesEvent;
        this.onNoEvent = onNoEvent;
        this.closeOnInput = closeOnInput;

        yesButton.GetComponentInChildren<Text>().text = yesButtonText;
        noButton.GetComponentInChildren<Text>().text = noButtonText;
        yesNoContainer.SetActive(true);
        okContainer.SetActive(false);
        inputField.gameObject.SetActive(false);

        Open();
    }
    public void ShowYesNoCancelDialog(string windowName, string windowMessage, string yesButtonText = "Yes", string noButtonText = "No", string okButtonText = "Cancel", 
        UnityEvent onYesEvent = null, UnityEvent onNoEvent = null, UnityEvent onCloseEvent = null, bool closeOnInput = true)
    {
        Prepare();
        windowNameText.text = windowName;
        messageText.text = windowMessage;
        this.onCloseEvent = onCloseEvent;
        this.onYesEvent = onYesEvent;
        this.onNoEvent = onNoEvent;
        this.closeOnInput = closeOnInput;
        okButton.onClick.AddListener(CloseWindow);

        yesButton.GetComponentInChildren<Text>().text = yesButtonText;
        noButton.GetComponentInChildren<Text>().text = noButtonText;
        okButton.GetComponentInChildren<Text>().text = okButtonText;

        yesNoContainer.SetActive(true);
        okContainer.SetActive(true);
        inputField.gameObject.SetActive(false);

        Open();
    }
    public void ShowInputDialog(string windowName, string windowMessage, string okButtonText = "OK", string cancelButtonText = "Cancel", UnityEvent onOKEvent = null,
        UnityEvent onCancelEvent = null, UnityEvent onCloseEvent = null, UnityEvent<string> receiveInputCallback = null)
    {
        Prepare();
        windowNameText.text = windowName;
        messageText.text = windowMessage;
        this.onCloseEvent = onCloseEvent;
        closeOnInput = true;
        
        inputResultEvent = receiveInputCallback;
        onYesEvent = onOKEvent;
        onNoEvent = onCancelEvent;

        yesButton.GetComponentInChildren<Text>().text = okButtonText;
        noButton.GetComponentInChildren<Text>().text = cancelButtonText;
        yesNoContainer.SetActive(true);
        okContainer.SetActive(false);
        inputField.gameObject.SetActive(true);
        
        Open();
    }
    protected override void OnClose()
    {
        base.OnClose();
        if (onCloseEvent != null)
        {
            onCloseEvent.Invoke();
            onCloseEvent = null;
        }
        inputField.text = "";
        Time.timeScale = 1f;
    }
    private void OnYesClicked()
    {
        if (inputResultEvent != null)
        {
            inputResultEvent.Invoke(inputField.text);
        }

        if (onYesEvent != null)
        {
            onYesEvent.Invoke();
        }

        if (closeOnInput)
            CloseWindow();
    }
    private void OnNoClicked()
    {
        if (onNoEvent != null)
        {
            onNoEvent.Invoke();
        }

        if (closeOnInput)
            CloseWindow();
    }
    private void OnOkClicked()
    {
        CloseWindow();
    }
}
