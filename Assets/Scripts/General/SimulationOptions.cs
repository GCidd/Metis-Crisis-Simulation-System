using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.IO;
using Unity.MLAgents.Policies;
using UnityEngine.PlayerLoop;
using Unity.Barracuda;

public class SimulationOptions
{
    private string modelsDirectoryStr = "Assets/Resources/NNModels";

    Transform optionsWindow;

    Toggle allDeadOrSafeToggle;
    private bool allDeadOrSafe = true;
    public bool AllDeadOrSafe { get { return allDeadOrSafe; } }

    InputField durationInput;
    private float maxDuration = 0f;
    public float MaxDuration { get { return maxDuration; } }

    Slider minDeadSlider;
    private int minDeadToEnd = 0;
    public int MinDeadToEnd { get { return minDeadToEnd; } }

    Slider minSafeSlider;
    private int minSafeToEnd = 0;
    public int MinSafeToEnd { get { return minDeadToEnd; } }

    Transform optionsContainer;
    Transform agentModelOptionSampleContainer;
    List<FileInfo> modelsPaths;
    List<GameObject> agents = new List<GameObject>();

    public void Initialize(Transform optionsWindow)
    {
        this.optionsWindow = optionsWindow;
        optionsContainer = optionsWindow.Find("Options").GetChild(0).GetChild(0);

        allDeadOrSafeToggle = optionsContainer.Find("AllDeadOrSafe Option").GetComponent<Toggle>();
        allDeadOrSafeToggle.onValueChanged.AddListener(delegate { allDeadOrSafe = allDeadOrSafeToggle.isOn; });
        allDeadOrSafe = allDeadOrSafeToggle.enabled;

        durationInput = optionsContainer.Find("Duration Option").GetComponentInChildren<InputField>();
        durationInput.onValueChanged.AddListener(delegate { float.TryParse(durationInput.text, out maxDuration); });

        minDeadSlider = optionsContainer.Find("MinDeadToEnd Option").GetComponentInChildren<Slider>();
        minDeadSlider.value = 0f;
        minDeadSlider.onValueChanged.AddListener(delegate { minDeadToEnd = (int)minDeadSlider.value; });
        minDeadToEnd = (int)minDeadSlider.value;

        minSafeSlider = optionsContainer.Find("MinSafeToEnd Option").GetComponentInChildren<Slider>();
        minSafeSlider.value = 0f;
        minSafeSlider.onValueChanged.AddListener(delegate { minSafeToEnd = (int)minSafeSlider.value; });
        minSafeToEnd = (int)minSafeSlider.value;

        UpdatePedestriansSlidersMaxValues(0);
        SetupModelsOptions();
    }
    public void UpdatePedestriansSlidersMaxValues(int maxVal)
    {
        minDeadSlider.maxValue = maxVal;
        minSafeSlider.maxValue = maxVal;
    }
    private List<FileInfo> GetAllAvailableAgentMdels()
    {
        DirectoryInfo modelsDirectory = new DirectoryInfo(modelsDirectoryStr);
        List<FileInfo> files = modelsDirectory.GetFiles().ToList().FindAll(f => f.Extension != ".meta").ToList();
        return files;
    }
    private void SetupModelsOptions()
    {
        modelsPaths = GetAllAvailableAgentMdels();
        Dropdown.OptionDataList dropdownDataList = new Dropdown.OptionDataList();
        foreach (FileInfo modelPath in modelsPaths)
        {
            dropdownDataList.options.Add(new Dropdown.OptionData(modelPath.Name));
        }

        agentModelOptionSampleContainer = optionsContainer.Find("Agent Models Container").Find("Model Option");
        Transform agentModelsParent = agentModelOptionSampleContainer.parent;

        agents = GameObject.FindObjectOfType<CategoriesManager>().GetObjectsOfCategory("Pedestrians");
        foreach (GameObject agent in agents)
        {
            Transform newContainer = GameObject.Instantiate(agentModelOptionSampleContainer);
            newContainer.name = agent.name + "'s Model Option";
            newContainer.GetChild(0).GetComponent<Text>().text = agent.name + "'s Model:";
            Dropdown dropdownMenu = newContainer.GetChild(1).GetComponent<Dropdown>();
            dropdownMenu.AddOptions(dropdownDataList.options);

            FileInfo modelFileInfo = modelsPaths.Find(f => f.Name.Split('.')[0] == agent.name);
            if (modelFileInfo != null)
            {
                int index = modelsPaths.IndexOf(modelFileInfo);
                dropdownMenu.value = index;
            }
            else
            {
                dropdownMenu.value = 0;
            }
            dropdownMenu.onValueChanged.AddListener(delegate { UpdateAgentModel(agent, dropdownMenu); });
            newContainer.SetParent(agentModelsParent);
            newContainer.GetComponent<RectTransform>().localScale = Vector3.one;
        }
        agentModelOptionSampleContainer.gameObject.SetActive(false);
    }
    private void UpdateAgentModel(GameObject agent, Dropdown change)
    {
        BehaviorParameters behaviorParams = agent.GetComponent<BehaviorParameters>();
        NNModel newModel = Resources.Load<NNModel>("Models/" + change.options[change.value].text.Split('.')[0]);
        agent.GetComponent<Pedestrian>().UpdateDefaultNNModel(newModel);

    }
}
