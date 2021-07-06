using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class OptionsWindow : MonoBehaviour
{
    private void Start()
    {
        transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
        FindObjectsOfType<Button>().ToList().Find(b => b.name.Contains("CloseWindow")).onClick.AddListener(OnClose);
    }
    private void OnEnable()
    {
        transform.localScale = Vector3.zero;
        GetComponentInChildren<Scrollbar>().value = 1.0f;
        iTween.ScaleTo(gameObject, iTween.Hash("scale", Vector3.one, "time", 0.25f, "ignoretimescale", true, "oncomplete", "ScrollToTop"));
    }
    private void ScrollToTop()
    {
        GetComponentInChildren<Scrollbar>().value = 1.0f;
    }
    public void OnClose()
    {
        Debug.Log("Closing");
        iTween.ScaleTo(gameObject, iTween.Hash("scale", Vector3.zero, "time", 0.25f, "ignoretimescale", true, "oncomplete", "DisableMe"));
    }
    private void DisableMe()
    {
        gameObject.SetActive(false);
    }
}
