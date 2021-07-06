using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ToggleButtonImage : MonoBehaviour
{
    public Sprite imageOn;
    public Sprite imageOff;
    public bool defaultState;

    private bool currentState;
    // Start is called before the first frame update
    void Start()
    {
        currentState = defaultState;
        ToggleButton();
    }

    public void ToggleButton()
    {
        if (currentState)
        {
            GetComponent<Button>().image.sprite = imageOn;
            currentState = false;
        }
        else
        {
            GetComponent<Button>().image.sprite = imageOff;
            currentState = true;
        }
    }
    public void SetState(bool state)
    {
        currentState = state;
        ToggleButton();
    }
    private void OnValidate()
    {
        if (defaultState)
        {
            GetComponent<Button>().image.sprite = imageOn;
        }
        else
        {
            GetComponent<Button>().image.sprite = imageOff;
        }
    }
}
