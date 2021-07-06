using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSearch : MonoBehaviour
{
    private void OnEnable()
    {
        transform.localScale = new Vector3(1f, 0f, 1f);
        iTween.ScaleTo(gameObject, iTween.Hash("scale", Vector3.one, "time", 0.5f, "ignoretimescale", true));
    }
}
