using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ToggleObjectActive : MonoBehaviour
{
    private bool defaultActiveState = false;
    private GameObject objectToToggle;
    public GameObject ToggleObject { set { objectToToggle = value; } get { return objectToToggle; } }
    public bool DefaultActiveState { set { defaultActiveState = value; currentState = defaultActiveState; objectToToggle.SetActive(currentState); } }

    private bool currentState;
    public bool State { get { return currentState; } }
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(delegate { ToggleButton(); });
    }
    public void ToggleButton()
    {
        if (currentState)
        {
            objectToToggle.SetActive(false);
            currentState = false;
        }
        else
        {
            objectToToggle.SetActive(true);
            currentState = true;
        }
    }
    public void SetState(bool state)
    {
        currentState = state;
        ToggleButton();
    }
}
