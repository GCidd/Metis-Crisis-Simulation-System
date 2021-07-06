using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PopupWindowManager : MonoBehaviour
{
    private static PopupWindowManager windowManager;
    private static List<GameObject> windowsStack = new List<GameObject>();
    private GameObject popUpDialogWindow;
    private GameObject popupMessageWindow;
    public static bool WindowVisible { get { return windowsStack.Count > 0; } }
    private void Awake()
    {
        popUpDialogWindow = GameObject.Find("Popup Dialog Window");

        popupMessageWindow = GameObject.Find("Popup Message Window");
        popupMessageWindow.GetComponent<PopupMessageWindow>().onCloseEvent.AddListener(PopWindow);
    }
    public static PopupWindowManager Instance
    {
        get
        {
            if (!windowManager)
            {
                windowManager = FindObjectOfType(typeof(PopupWindowManager)) as PopupWindowManager;

                if (!windowManager)
                {
                    Debug.LogError("There needs to be one active PopupWindowManager script on a GameObject in your scene.");
                }
                else
                {
                    windowManager.Init();
                }
            }

            return windowManager;
        }
    }

    void Init()
    {

    }
    private void PopWindow()
    {
        GameObject topWindow = windowsStack.Last();
        if (!topWindow.GetComponent<GenericWindow>().Closed)
            topWindow.GetComponent<GenericWindow>().CloseWindow();
        GameObject.Destroy(topWindow);
        windowsStack.Remove(topWindow);
    }
    public void ShowOKDialog(string windowName = "PopupMessage", string windowMessage = "Message here!", string okButtonText = "OK", UnityEvent onCloseEvent = null)
    {
        GameObject newDialog = GameObject.Instantiate(popUpDialogWindow);
        newDialog.transform.SetParent(popUpDialogWindow.transform.parent);
        windowsStack.Add(newDialog);
        if (onCloseEvent == null)
        {
            onCloseEvent = new UnityEvent();
        }
        onCloseEvent.AddListener(PopWindow);
        newDialog.GetComponent<PopupDialogWindow>().ShowOKDialog(windowName, windowMessage, okButtonText, onCloseEvent);
    }
    public void ShowYesNoDialog(string windowName, string windowMessage, string yesButtonText = "Yes", string noButtonText = "No",
        UnityEvent onYesEvent = null, UnityEvent onNoEvent = null, UnityEvent onCloseEvent = null, bool closeOnInput = true)
    {
        GameObject newDialog = GameObject.Instantiate(popUpDialogWindow);
        newDialog.transform.SetParent(popUpDialogWindow.transform.parent);
        windowsStack.Add(newDialog);
        if (onCloseEvent == null)
        {
            onCloseEvent = new UnityEvent();
        }
        onCloseEvent.AddListener(PopWindow);
        if (onNoEvent == null)
        {
            onNoEvent = new UnityEvent();
        }
        onNoEvent.AddListener(newDialog.GetComponent<GenericWindow>().CloseWindow);
        newDialog.GetComponent<PopupDialogWindow>().ShowYesNoDialog(windowName, windowMessage, yesButtonText, noButtonText, onYesEvent, onNoEvent, onCloseEvent, closeOnInput);
    }
    public void ShowYesNoCancelDialog(string windowName, string windowMessage, string yesButtonText = "Yes", string noButtonText = "No", string okButtonText = "Cancel",
        UnityEvent onYesEvent = null, UnityEvent onNoEvent = null, UnityEvent onCloseEvent = null, bool closeOnInput = true)
    {
        GameObject newDialog = GameObject.Instantiate(popUpDialogWindow);
        newDialog.transform.SetParent(popUpDialogWindow.transform.parent);
        windowsStack.Add(newDialog);
        if (onCloseEvent == null)
        {
            onCloseEvent = new UnityEvent();
        }
        onCloseEvent.AddListener(PopWindow);
        newDialog.GetComponent<PopupDialogWindow>().ShowYesNoCancelDialog(windowName, windowMessage, yesButtonText, noButtonText, okButtonText, onYesEvent, onNoEvent, onCloseEvent, closeOnInput);
    }

    public void ShowInputDialog(string windowName, string windowMessage, string okButtonText = "OK", string cancelButtonText = "Cancel", UnityEvent onOKEvent = null,
        UnityEvent onCancelEvent = null, UnityEvent onCloseEvent = null, UnityEvent<string> receiveInputCallback = null)
    {
        GameObject newDialog = GameObject.Instantiate(popUpDialogWindow);
        newDialog.transform.SetParent(popUpDialogWindow.transform.parent);
        windowsStack.Add(newDialog);
        if (onCloseEvent == null)
        {
            onCloseEvent = new UnityEvent();
        }
        onCloseEvent.AddListener(PopWindow);
        newDialog.GetComponent<PopupDialogWindow>().ShowInputDialog(windowName, windowMessage, okButtonText, cancelButtonText, onOKEvent, onCancelEvent, onCloseEvent, receiveInputCallback);
    }
    public void ShowMessage(
        string windowName = "Popup Message",
        string windowMessage = "Message here!")
    {
        GameObject newMessage = GameObject.Instantiate(popupMessageWindow);
        newMessage.transform.SetParent(popupMessageWindow.transform.parent);
        windowsStack.Add(newMessage);
        newMessage.GetComponent<PopupMessageWindow>().ShowMessage(windowName, windowMessage);
    }
}
