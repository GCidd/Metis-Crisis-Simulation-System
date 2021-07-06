using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadEnvironmentWindow : GenericWindow
{
    List<string> environmentsFiles = new List<string>();
    Button environmentTemplateButton;
    Scrollbar scrollbar;
    List<Button> buttons = new List<Button>();
    protected override void Awake()
    {
        base.Awake();

        environmentTemplateButton = transform.Find("Body").GetComponentInChildren<Button>();
        transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
        transform.Find("Header").GetComponentInChildren<Button>().onClick.AddListener(CloseWindow);
        scrollbar = GetComponentInChildren<Scrollbar>();
    }
    public override void Open()
    {
        CreateButtons();
        base.Open();
        gameObject.SetActive(true);
    }
    private void CreateButtons()
    {
        ClearButtons();

        List<string> environments = Directory.GetFiles("Assets/Resources/SavedEnvironments/").ToList().FindAll(f => f.EndsWith(".xml"));
        foreach (string environment in environments)
        {
            environmentsFiles.Add(environment);
            Button newButton = GameObject.Instantiate(environmentTemplateButton);
            newButton.transform.SetParent(environmentTemplateButton.transform.parent);
            string environmentName = environment.Split('/').Last().Split('.')[0];
            newButton.name = environmentName;
            newButton.GetComponentInChildren<Text>().text = environmentName;
            newButton.transform.localScale = Vector3.one;
            newButton.gameObject.SetActive(true);
            newButton.onClick.AddListener(delegate { LoadEnvironment(environment); });
            buttons.Add(newButton);
        }
        environmentTemplateButton.gameObject.SetActive(false);
        scrollbar.value = 1.0f;
    }
    private void LoadEnvironment(string environmentXmlFilePath)
    {
        foreach (Transform building in GameObject.Find("Placed Buildings").transform)
        {
            GameObject.Destroy(building.gameObject);
        }

        Debug.Log("Loading environment " + environmentXmlFilePath);
        XmlDocument environmentXmlDoc = new XmlDocument();
        environmentXmlDoc.Load(environmentXmlFilePath);

        foreach (XmlNode buildingNode in environmentXmlDoc.ChildNodes)
        {
            GameObject newBuilding = new GameObject();
            newBuilding.name = "Building";
            newBuilding.AddComponent<Building>();
            newBuilding.GetComponent<Building>().LoadNodeFromXml(buildingNode);
        }
    }
    private void ClearButtons()
    {
        foreach (Button button in buttons)
        {
            GameObject.Destroy(button.gameObject);
        }
        buttons.Clear();
    }
}
