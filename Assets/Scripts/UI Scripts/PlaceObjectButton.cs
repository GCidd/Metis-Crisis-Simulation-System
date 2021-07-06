using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class PlaceObjectButton : MonoBehaviour
{
    private GameObject objectToPlace;
    private string buttonText;
    // Start is called before the first frame update
    public void PrepareButton(string text, GameObject _object, UnityEvent _event)
    {
        objectToPlace = _object;
        buttonText = text;
        GetComponent<Button>().onClick.AddListener(delegate { _event.Invoke(); });
    }
}
