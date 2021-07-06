using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClearSearchFieldText : MonoBehaviour
{
    public void ClearText()
    {
        GetComponent<InputField>().text = "";
    }
}
