using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupMessageWindow : GenericWindow
{
    private Text windowNameText;
    private Text messageText;
    public UnityEvent onCloseEvent;
    protected override void Awake()
    {
        base.Awake();
        windowNameText = transform.Find("Header").GetComponentInChildren<Text>();
        messageText = transform.Find("Body").GetComponentInChildren<Text>();
    }
    public void ShowMessage(string windowName, string windowMessage)
    {
        windowNameText.text = windowName;
        messageText.text = windowMessage;
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }
}
