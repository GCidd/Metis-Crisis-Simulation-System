using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GenericWindow : MonoBehaviour
{
    private bool visible = false;
    public bool IsVisible { get { return visible; } }
    public bool Closed { get { return transform.localScale == Vector3.zero; } }
    // Start is called before the first frame update
    protected virtual void Awake()
    {
        transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
        transform.localScale = Vector3.zero;
    }

    public virtual void Open()
    {
        iTween.ScaleTo(gameObject, iTween.Hash("scale", Vector3.one, "time", 0.5f, "ignoretimescale", true));
        Time.timeScale = 0f;
        visible = true;
        transform.SetAsLastSibling();
    }
    public virtual void CloseWindow()
    {
        iTween.ScaleTo(gameObject, iTween.Hash("scale", Vector3.zero, "time", 0.3f, "ignoretimescale", true, "oncomplete", "OnClose"));
    }
    protected virtual void OnClose()
    {
        visible = false;
        Time.timeScale = 1f;
    }
}
