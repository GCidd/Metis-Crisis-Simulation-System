using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using System.Linq;
using System.Xml;

public class Populator : MonoBehaviour
{
    [SerializeField]
    GameObject agentToTrain;
    [SerializeField]
    [Tooltip("Number of times to spawn each type of building")]
    int numberOfBuildingsToSpawn = 4;

    List<Agent> templateAgents;
    List<Building> templateBuildings;

    List<GameObject> newAgents;
    List<GameObject> newBuildings;

    GameObject newEnvironmentsContainer;

    private void Awake()
    {
        templateAgents = GameObject.Find("Available Agents").GetComponentsInChildren<Agent>().ToList();

        if (System.IO.File.Exists("populator_options.xml"))
        {
            XmlDocument optionsXmlDoc = new XmlDocument();
            optionsXmlDoc.Load("populator_options.xml");
            XmlNode xmlNode = optionsXmlDoc.ChildNodes[0];

            Debug.Log("Loading populator options " + xmlNode["agent"].Attributes["name"].Value + " " + xmlNode["buildings_to_spawn"].Attributes["number"].Value);

            agentToTrain = templateAgents.Find(a => a.name == xmlNode["agent"].Attributes["name"].Value).gameObject;

            int.TryParse(xmlNode["buildings_to_spawn"].Attributes["number"].Value, out numberOfBuildingsToSpawn);
        }


        templateBuildings = GameObject.Find("Available Buildings").GetComponentsInChildren<Building>().ToList();
        newEnvironmentsContainer = GameObject.Find("Testing Grounds");
        newAgents = new List<GameObject>();
        newBuildings = new List<GameObject>();
    }

    private void Start()
    {
        Transform testingGrounds = GameObject.Find("Testing Grounds").transform;
        for (int j = 0; j < numberOfBuildingsToSpawn; j++)
        {
            int buildingLevel = 0;
            foreach (Building building in templateBuildings)
            {
                GameObject newBuilding = GameObject.Instantiate(building.gameObject, testingGrounds);
                newBuilding.name = string.Format("{0}_{1}_{2}", agentToTrain.name, building.name, j);
                newBuilding.transform.position = new Vector3(300 * j, 0, 300 * buildingLevel);

                GameObject newAgent = GameObject.Instantiate(agentToTrain, newBuilding.transform);
                newAgent.transform.localPosition = Vector3.zero;
                newBuilding.GetComponent<Building>().AddPedestrian(newAgent);
                newBuilding.GetComponent<Building>().OnPlaced();
                if (agentToTrain.GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly)
                {
                    newAgent.GetComponent<BehaviorParameters>().Model = agentToTrain.GetComponent<BehaviorParameters>().Model;
                    newAgent.GetComponent<BehaviorParameters>().BehaviorType = agentToTrain.GetComponent<BehaviorParameters>().BehaviorType;
                    newBuilding.GetComponent<Building>().StartEvacuation();
                }
                else
                {
                    newBuilding.GetComponent<Building>().StartLearning();
                }

                newBuildings.Add(newBuilding);
                newAgents.Add(newAgent);

                buildingLevel++;
            }
        }
        foreach (Building building in templateBuildings)
        {
            building.gameObject.SetActive(false);
        }
        foreach (Agent agent in templateAgents)
        {
            agent.gameObject.SetActive(false);
        }

        GetComponent<Paparazzi>().PrepareAndStart();
    }
}
