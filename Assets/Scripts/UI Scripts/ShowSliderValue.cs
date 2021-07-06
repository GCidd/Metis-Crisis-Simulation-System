using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ShowSliderValue : MonoBehaviour
{
    [SerializeField]
    private string prefixText = "";
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private string postfixTest = "";

    private Text textElement;
    // Start is called before the first frame update
    void Start()
    {
        textElement = GetComponent<Text>();
    }

    public void UpdateText()
    {
        textElement.text = string.Format("{0} {1} {2}", prefixText, slider.value.ToString(), postfixTest);
    }
    private void OnValidate()
    {
        textElement = GetComponent<Text>();
        textElement.text = string.Format("{0} {1} {2}", prefixText, slider.value.ToString(), postfixTest);
    }
}
