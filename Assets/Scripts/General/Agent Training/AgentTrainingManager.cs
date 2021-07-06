using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.MLAgents;
using UnityEngine.UI;

// [RequireComponent(typeof(AgentMovementControl))]
public class AgentTrainingManager : MLCharacterControl
{
    [Header("Difficulty setting")]
    [SerializeField] private GameObject buildingForDifficultyStage;
    [SerializeField] private int startingStep = 0;
    private int currentDifficultyStage = 1;
    public bool differentStartingDifficulty = false;
    public int startDifficultyStage = 1;
    List<GameObject> possibleSpawnAreas = new List<GameObject>();
    // [SerializeField] private int fireGrowOnLastStage = 3;

    ExitPlaceRandomizer exitDoorRandomizer;
    static GameObject staticFire;
    GameObject buildingFire;
    // private bool startPlacingFires = false;
    int areaToSpawn = 0;

    private GameObject spawnAreasParent;
    public bool scaleDifficulty = false;

    float difficultyChangeStepCount = 2000000f;
    float totalDistanceReward = 0f;
    private void OnValidate()
    {
        if (buildingForDifficultyStage == null)
            return;
        if (!differentStartingDifficulty)
            return;
        spawnAreasParent = buildingForDifficultyStage.transform.Find("Spawn Areas").gameObject;
        possibleSpawnAreas.Clear();
        foreach (Transform _child in spawnAreasParent.transform)
        {   // get spawn areas of the building
            possibleSpawnAreas.Add(_child.gameObject);
        }

        if (startDifficultyStage < 1)
        {
            startDifficultyStage = 1;
        }
        else if (startDifficultyStage > possibleSpawnAreas.Count + 1)
        {
            startDifficultyStage = possibleSpawnAreas.Count;
        }
    }
    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();

        if (transform.parent.parent?.name != "Testing Grounds")
            return;

        exitDoorRandomizer = GetComponentInParent<ExitPlaceRandomizer>();

        if (staticFire == null)
        {
            staticFire = GameObject.Find("Fire");
            staticFire.name = "Static Fire";
            staticFire.SetActive(false);
        }

        spawnAreasParent = transform.parent.Find("Spawn Areas").gameObject;
        foreach (Transform _child in spawnAreasParent.transform)
        {   // get spawn areas of the building
            possibleSpawnAreas.Add(_child.gameObject);
        }

        if (!scaleDifficulty)
        {   // if we are not scaling difficulty then all spawn areas are unlocked
            currentDifficultyStage = possibleSpawnAreas.Count;
        }
        exitDoorRandomizer.RandomizeWallsPositions();

        // startingStep = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("step_start", 0.0f) * 2;
    }
    public override void OnEpisodeBegin()
    {
        if (parentBuilding == null) return;

        base.OnEpisodeBegin();

        GetComponent<Pedestrian>().ResetStatus();
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly)
            currentDifficultyStage = possibleSpawnAreas.Count;
        else
        {
            if (scaleDifficulty)
            {
                currentDifficultyStage = Mathf.Min((Academy.Instance.TotalStepCount + startingStep) / (int)difficultyChangeStepCount + 1, possibleSpawnAreas.Count);
                if (differentStartingDifficulty)
                {
                    currentDifficultyStage = Mathf.Min(startDifficultyStage + currentDifficultyStage, possibleSpawnAreas.Count);
                }
            }
            else
            {
                currentDifficultyStage = possibleSpawnAreas.Count;
            }
        }

#if UNITY_EDITOR
        if (Selection.Contains(gameObject.GetInstanceID()))
            Debug.Log(string.Format("[{0}] Current Difficulty Stage: {1}/{3}, Total Step Count: {2}", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), currentDifficultyStage, (Academy.Instance.TotalStepCount + startingStep), possibleSpawnAreas.Count));
#endif

        exitDoorRandomizer.RandomizeWallsPositions();

        // order the spawn areas by the sum of the distance between all the exits
        possibleSpawnAreas = possibleSpawnAreas.OrderBy(s =>
            parentBuilding.GetExits().Sum(e => Vector3.Distance(e.transform.position, s.transform.position))
        ).ToList();

        if (possibleSpawnAreas.Count > 0)
        {
            float minDifficulty = Mathf.Max(0f, currentDifficultyStage - 3f);
            areaToSpawn = (int)(CompletedEpisodes % (currentDifficultyStage - minDifficulty)) + (int)minDifficulty;
            Vector3 size = possibleSpawnAreas[areaToSpawn].GetComponent<MeshCollider>().bounds.size;
            Vector3 pos = possibleSpawnAreas[areaToSpawn].transform.position;
            float minX = pos.x - size.x / 2, maxX = pos.x + size.x / 2;
            float minY = pos.y - size.y / 2, maxY = pos.y + size.y / 2;
            float minZ = pos.z - size.z / 2, maxZ = pos.z + size.z / 2;
            transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), Random.Range(minZ, maxZ));
            pedestrian.OnPlaced();
        }

        GameObject closestExit = parentBuilding.GetExits().OrderBy(e => Vector3.Distance(e.transform.position, transform.position)).ToList()[0];
        targetPosition = GetPointInFrontOfExit(closestExit);

        foreach (GameObject exitDoor in parentBuilding.GetExits())
        {
            float direction = Vector3.Dot(transform.position - exitDoor.transform.position, transform.right);
            exitDoor.GetComponent<DoubleDoor>().OpenDoors(direction);
        }
        PlaceFireRandomly(areaToSpawn);
        totalDistanceReward = 0f;
    }
    public void PlaceFireRandomly(int pedestrianArea)
    {
        if (buildingFire == null)
        {
            buildingFire = GameObject.Instantiate(staticFire, transform.parent.Find("Fires"));
            buildingFire.name = transform.parent.name;
            buildingFire.SetActive(true);
        }
        buildingFire.GetComponent<FireSpreading>().CleanFire();
        // make a copy for the fire
        List<GameObject> possibleAreaToSpawnFire = new List<GameObject>();
        possibleAreaToSpawnFire.AddRange(possibleSpawnAreas);
        // calculate the maximum steps required to reach the point were the pedestrian has unlocked all the possible spawn areas
        float maxAreasSteps = possibleSpawnAreas.Count * difficultyChangeStepCount;
        int areaToSpawn;
        // chance to spawn in the same area as the pedestrian depends on the current step, the higher the step the higher the chance
        if (Random.value > triangularValue((int)maxAreasSteps, maxAreasSteps))
        {
            areaToSpawn = pedestrianArea;
        }
        else
        {
            // remove the area the pedestrian has spawn so as to avoid spawning in the same
            possibleAreaToSpawnFire.RemoveAt(pedestrianArea);
            areaToSpawn = Random.Range(0, possibleSpawnAreas.Count - 1);
        }
        Vector3 size = possibleSpawnAreas[areaToSpawn].GetComponent<MeshCollider>().bounds.size;
        Vector3 pos = possibleSpawnAreas[areaToSpawn].transform.position;
        float minX = pos.x - size.x / 2, maxX = pos.x + size.x / 2;
        float minY = pos.y - size.y / 2, maxY = pos.y + size.y / 2;
        float minZ = pos.z - size.z / 2, maxZ = pos.z + size.z / 2;
        buildingFire.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), Random.Range(minZ, maxZ));

        if (Academy.Instance.TotalStepCount / 2 > 30000000)
            buildingFire.GetComponent<FireSpreading>().StartFire();
        // int difficultyOffset = differentStartingDifficulty ? startDifficultyStage : 0;
        // int difficultyLevel = Academy.Instance.TotalStepCount / (int)difficultyChangeStepCount;
        // if (difficultyLevel + difficultyOffset > possibleSpawnAreas.Count)
        // {
        //     if (Selection.Contains(gameObject.GetInstanceID()) && currentDifficultyStage + difficultyOffset == possibleSpawnAreas.Count + 1)
        //         Debug.Log("New difficulty stage.");
        //     buildingFire.GetComponent<FireSpreading>().BurstGrow(fireGrowOnLastStage);
        // }
    }
    protected override void CalculateReward()
    {
        if (transform.position.y < 0)
            EndEpisodeSafe();
        base.CalculateReward();
        UpdateTexts();
    }
    private void UpdateTexts()
    {
        GetComponentInChildren<TextMesh>().text = GetCumulativeReward().ToString("n4");
        GetComponentInChildren<TextMesh>().transform.rotation = Quaternion.Euler(0, -Camera.main.transform.eulerAngles.y, -Camera.main.transform.eulerAngles.z);
#if UNITY_EDITOR
        if (Selection.Contains(gameObject.GetInstanceID()))
        {
            Text agentNameText = FindObjectsOfType<Text>().ToList().Find(t => t.name == "Agent Name Text").GetComponent<Text>();
            if (agentNameText.text != parentBuilding.name)
            {
                agentNameText.text = parentBuilding.name;
            }
            float distanceReward = CalculateDistanceReward();
            totalDistanceReward += distanceReward;
            Text totalDistanceText = FindObjectsOfType<Text>().ToList().Find(t => t.name == "Total Distance Reward").GetComponent<Text>();
            totalDistanceText.text = totalDistanceReward.ToString("n4");

            FindObjectOfType<RawImage>().texture = GetComponent<RenderTextureSensorComponent>().RenderTexture;

            Text distanceText = FindObjectsOfType<Text>().ToList().Find(t => t.name == "Distance Reward").GetComponent<Text>();
            distanceText.text = distanceReward.ToString("n4");
        }
#endif
    }

    public new void EndEpisode()
    {
        switch (lastEpisodeResult)
        {
            case EpisodeEndResult.Death:
                SetReward(deadReward);
                break;
            case EpisodeEndResult.Safe:
                float finalSafeReward = pedestrian.Stats.LifePercentage * lifePercentageCoefficient;
                // AddReward(finalSafeReward * safeRewardCurrentCoeff);
                // AddReward(1f);
                SetReward(1f - StepCount / MaxStep);
                break;
            case EpisodeEndResult.None:
            default:
                SetReward(deadReward);
                break;
        }
        base.EndEpisode();
    }
    public override void EndEpisodeDeath()
    {
        lastEpisodeResult = EpisodeEndResult.Death;
        EndEpisode();
    }
    public override void EndEpisodeSafe()
    {
        lastEpisodeResult = EpisodeEndResult.Safe;
        EndEpisode();
    }
    public override void DamageTaken()
    {
        base.DamageTaken();
    }
    private void OnDrawGizmos()
    {
        if (parentBuilding == null)
            return;
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(targetPosition, 0.2f);
        // for each direction (left, right, forward, backward) cast a ray and check for any hits
        // after getting a list of bools (if ray hit something or not) find all the casts where the cast did not hit anything (!r)
        // if there is at least one element returned (count >= 1) then it is outside the position is outside, else check the other direction
        bool outsideBuilding = !Physics.Raycast(targetPosition, Vector3.down, 5f, LayerMask.GetMask("Floors"));
        if (outsideBuilding)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetPosition, 0.2f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetPosition + Vector3.up * 0.3f);
            Gizmos.DrawSphere(targetPosition + Vector3.up * 0.3f, 0.2f);
        }
    }
}
