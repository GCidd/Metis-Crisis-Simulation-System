using Unity.MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEditor;

[System.Serializable]
public class DialogInputCallback : UnityEvent<string>
{

}

public class SimulationManager : MonoBehaviour
{
    private SimulationOptions simulationOptions;
    private LearningOptions learningOptions;

    Text headerText;

    Button simulationModeButton;
    GameObject simulationOptionsWindow;
    Button learningModeButton;
    GameObject learningOptionsWindow;
    Image academyConnectionStatus;
    GameObject loadEnvironmentWindow;

    GameObject buildingsContainerObject;
    GameObject controlPanel;
    Button pauseButton;
    Button playButton;
    Button saveLoadEnvironmentButton;
    Button optionsButton;

    private static bool simulationPlaying;
    private static bool evacuating;
    private static bool simulationMode = true;
    private bool shouldEvacuateOnPlay = false;
    public static bool Playing { get { return simulationPlaying; } }
    public static bool Evacuating { get { return evacuating; } }
    public static bool SimulationMode { get { return simulationMode; } }

    private static bool learning;
    public static bool Learning { get { return learning; } }

    private int totalPedestrians = 0;
    private int deadCount = 0;
    private int safeCount = 0;
    // Start is called before the first frame update
    void Awake()
    {
        EventManager.StartListening("StartEvacuation", StartEvacuation);
        EventManager.StartListening("PedestrianDied", PedestrianDied);
        EventManager.StartListening("PedestrianSafe", PedestrianSafe);

        simulationOptions = new SimulationOptions();
        simulationOptions.Initialize(FindObjectsOfType<OptionsWindow>().ToList().Find(w => w.name.Contains("Simulation")).transform);
        learningOptions = new LearningOptions();
        learningOptions.Initialize(FindObjectsOfType<OptionsWindow>().ToList().Find(w => w.name.Contains("Learning")).transform);

        loadEnvironmentWindow = GameObject.Find("Load Environment Window");
        loadEnvironmentWindow.SetActive(false);

        headerText = GameObject.Find("Control Panel").transform.Find("Header").GetComponentInChildren<Text>();
        academyConnectionStatus = GameObject.Find("Control Panel").transform.Find("Header").GetComponentInChildren<Image>();
        if (Academy.Instance.IsCommunicatorOn)
            academyConnectionStatus.color = new Color(76f / 255f, 175f / 255f, 80f / 255f, 21f);
        else
            academyConnectionStatus.color = new Color(244f / 255f, 67f / 255f, 54f / 255f, 1f);
        academyConnectionStatus.gameObject.SetActive(false);

        simulationModeButton = GameObject.Find("Top Panel").transform.Find("Simulation Mode Button").GetComponent<Button>();
        simulationModeButton.onClick.AddListener(SetSimulationMode);
        simulationOptionsWindow = GameObject.Find("Simulation Options Window");
        simulationOptionsWindow.SetActive(false);

        learningModeButton = GameObject.Find("Top Panel").transform.Find("Learning Mode Button").GetComponent<Button>();
        learningModeButton.onClick.AddListener(SetLearningMode);
        learningOptionsWindow = GameObject.Find("Learning Options Window");
        learningOptionsWindow.SetActive(false);

        buildingsContainerObject = GameObject.Find("Placed Buildings");
        controlPanel = GameObject.Find("Control Panel");
        // pauseButton = controlPanel.transform.Find("Buttons").transform.Find("Pause Button").GetComponent<Button>();
        playButton = controlPanel.transform.Find("Buttons").transform.Find("Play Button").GetComponent<Button>();
        optionsButton = controlPanel.transform.Find("Buttons").transform.Find("Options Button").GetComponent<Button>();

        saveLoadEnvironmentButton = controlPanel.transform.Find("Buttons").transform.Find("SaveLoad Button").GetComponent<Button>();
        saveLoadEnvironmentButton.onClick.AddListener(AskSaveLoad);

        UpdateButtonEvents();
    }
    private void UpdateButtonEvents()
    {
        playButton.onClick.RemoveAllListeners();
        optionsButton.onClick.RemoveAllListeners();
        if (simulationMode)
        {
            playButton.onClick.AddListener(StartSimulation);
            // by default opens up the simulation options window
            optionsButton.onClick.AddListener(delegate { simulationOptionsWindow.SetActive(true); });
        }
        else
        {
            playButton.onClick.AddListener(StartLearning);
            // by default opens up the simulation options window
            optionsButton.onClick.AddListener(delegate { learningOptionsWindow.SetActive(true); });
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (buildingsContainerObject.transform.childCount > 0 && !evacuating)
        {
            int pedestriansCount = buildingsContainerObject.transform.GetChild(0).GetComponentsInChildren<Pedestrian>().Length;
            totalPedestrians = pedestriansCount;
            simulationOptions.UpdatePedestriansSlidersMaxValues(pedestriansCount);
        }
    }
    public void PedestrianDied()
    {
        deadCount += 1;
        CheckEndConditions();
    }
    public void PedestrianSafe()
    {
        safeCount += 1;
        CheckEndConditions();
    }
    public void CheckEndConditions()
    {
        if (!evacuating)
            return;

        if (simulationOptions.AllDeadOrSafe)
        {
            if (deadCount + safeCount == totalPedestrians)
                StopEvacuation();
        }

        if (simulationOptions.MinDeadToEnd > 0)
        {
            if (deadCount >= simulationOptions.MinDeadToEnd)
                StopEvacuation();
        }

        if (simulationOptions.MinSafeToEnd > 0)
        {
            if (safeCount >= simulationOptions.MinSafeToEnd)
                StopEvacuation();
        }
    }
    private void SetSimulationMode()
    {
        simulationMode = true;
        headerText.text = "Simulation Mode";
        EventManager.TriggerEvent("InferenceAgentBehaviorType");
        simulationModeButton.GetComponent<ToggleButtonImage>().SetState(true);
        learningModeButton.GetComponent<ToggleButtonImage>().SetState(false);
        academyConnectionStatus.gameObject.SetActive(false);
        UpdateButtonEvents();
    }
    private void SetLearningMode()
    {
        simulationMode = false;
        headerText.text = "Learning Mode";
        EventManager.TriggerEvent("DefaultAgentBehaviorType");
        simulationModeButton.GetComponent<ToggleButtonImage>().SetState(false);
        learningModeButton.GetComponent<ToggleButtonImage>().SetState(true);
        academyConnectionStatus.gameObject.SetActive(true);
        UpdateButtonEvents();
    }
    public void StartSimulation()
    {
        if (simulationPlaying)
        {   // executed when the play button is pressed
            StopEvacuation();
            return;
        }

        if (buildingsContainerObject.GetComponentInChildren<SafeAreaPart>() == null)
        {
            PopupWindowManager.Instance.ShowOKDialog("No safe areas placed", "No safe areas have been placed. Please place at least one small safe area to start the simulation.");
            return;
        }

        simulationPlaying = true;
        playButton.GetComponent<ToggleButtonImage>().SetState(true);
        Debug.Log("Started simulation!");

        if (shouldEvacuateOnPlay)
        {
            StartEvacuation();
        }
        else
        {
            foreach (Building building in buildingsContainerObject.GetComponentsInChildren<Building>())
            {
                if (building.GetComponentsInChildren<FireSpreading>().Length > 0)
                {
                    building.StartSimulation();
                }
            }
        }
    }
    public void StartEvacuation()
    {
        if (!simulationPlaying)
        {
            Debug.Log("Simulation not started yet.");
            shouldEvacuateOnPlay = true;
            return;
        }
        if (evacuating)
        {
            Debug.Log("Already evacuating.");
            return;
        }

        evacuating = true;
        foreach (Building building in buildingsContainerObject.GetComponentsInChildren<Building>())
        {
            building.StartEvacuation();
        }

        if (simulationOptions.MaxDuration > 0)
        {
            Invoke("StopEvacuation", simulationOptions.MaxDuration);
        }
        Debug.Log("Started evacuation!");
    }
    public void StopEvacuation()
    {
        Debug.Log("Stopped evacuation1");
        CancelInvoke("StopEvacuation");
        playButton.GetComponent<ToggleButtonImage>().SetState(false);

        // int pedestriansCount = buildingsContainerObject.transform.GetChild(0).GetComponentsInChildren<Pedestrian>().Length;
        // deadCount += pedestriansCount;

        foreach (Building building in buildingsContainerObject.GetComponentsInChildren<Building>())
        {
            building.StopSimulation();
        }

        string windowMessage = string.Format("Simulation has ended.\nTotal pedestrians survived {0}.\nTotal pedestrians died {1}.", safeCount, deadCount);
        PopupWindowManager.Instance.ShowOKDialog("Simulation ended", windowMessage);
    }

    public void StartLearning()
    {
        if (learning)
        {
            StopLearning();
            return;
        }

        learning = true;
        playButton.GetComponent<ToggleButtonImage>().SetState(true);
        Debug.Log("Started learning!");

        StartCoroutine("WaitForAcademyConnection");
    }
    private void WaitForAcademyConnection()
    {
        // kept for later when the academy reconnection finally works
        //PopupMessageOptions.windowName = "Connecting to ML-Agents service";
        //PopupMessageOptions.windowMessage = "Please start the ml-agents environment training service.";
        //EventManager.TriggerEvent(PopupMessageOptions.showWindowEventName);
        //Academy.Instance.Dispose();
        //yield return new WaitForSecondsRealtime(2f);

        if (!Academy.Instance.IsCommunicatorOn)
        {
            string windowMessage = "MLAgents academy could not connect to the python trainer upon startup. Please ensure that python trainer is running before starting the Metis system.";
            PopupWindowManager.Instance.ShowOKDialog("Trainer not connected", windowMessage);
            return;
        }
        else
        {
            foreach (Building building in buildingsContainerObject.GetComponentsInChildren<Building>())
            {
                building.StartLearning();
            }
        }
    }
    public void StopLearning()
    {
        learning = false;
        playButton.GetComponent<ToggleButtonImage>().SetState(false);
        Debug.Log("Stopped learning!");
    }
    private void AskSaveLoad()
    {
        UnityEvent yesEvent = new UnityEvent();
        yesEvent.AddListener(PromptSaveEnvironment);
        UnityEvent noEvent = new UnityEvent();
        noEvent.AddListener(delegate { loadEnvironmentWindow.GetComponent<GenericWindow>().Open(); });
        PopupWindowManager.Instance.ShowYesNoCancelDialog("Save/Load Environment", "What would you like to do?", "Save", "Load", "Close", yesEvent, noEvent, closeOnInput: false);
    }
    private void PromptSaveEnvironment()
    {
        if (buildingsContainerObject.transform.childCount == 0)
        {
            PopupWindowManager.Instance.ShowOKDialog("No Buildings Placed", "You need to place at least one building to save the environment.");
            return;
        }
        DialogInputCallback receiveInputEvent = new DialogInputCallback();
        receiveInputEvent.AddListener(SaveEnvironment);
        PopupWindowManager.Instance.ShowInputDialog("Save Environment to File", "Please enter the name of the environment and then press the Save button to save it.", "Save", "Cancel", receiveInputCallback: receiveInputEvent);
    }
    private void SaveEnvironment(string environmentName)
    {
        Debug.Log("Saving environment with name " + environmentName);
        XmlDocument xmlEnvironment = new XmlDocument();
        //XmlAttribute fileVersion = xmlEnvironment.CreateAttribute("version");
        //fileVersion.Value = "1.0.0";
        //xmlEnvironment.Attributes.Append(fileVersion);
        foreach (Building building in buildingsContainerObject.GetComponentsInChildren<Building>())
        {
            xmlEnvironment.AppendChild(building.ToXmlNode(xmlEnvironment));
        }
        xmlEnvironment.Save("Assets/Resources/SavedEnvironments/" + environmentName + ".xml");
    }
}
