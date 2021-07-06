using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using CsvHelper;
using System.Globalization;
using Unity.MLAgents.Actuators;
using System;
using UnityEngine.UI;

[Serializable]
public class LastAction
{
    public float horizontal;
    public float vertical;
    public float distance;
}
[Serializable]
public class LastObservation
{
    public Vector2 targetLocation;
    public Vector2 currentPosition;
    public float normalizedDistance;
    public Vector2 direction;
    public float distanceReward;
}

public class PositionRecord
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public PositionRecord(Vector3 pos)
    {
        x = pos.x;
        y = pos.y;
        z = pos.z;
    }
}

public enum EpisodeEndResult { None, Safe, Death }
// [RequireComponent(typeof(AgentMovementControl))]
public class MLCharacterControl : Agent
{
    protected float lifePercentageCoefficient = 0.25f;
    protected float existentialReward = -0.2f;
    protected float collisionReward = -0.2f;
    protected float damageReward = -0.8f;
    protected float deadReward = -1f;
    protected float safeReward = 3f;
    protected EpisodeEndResult lastEpisodeResult = EpisodeEndResult.None;

    protected List<GameObject> collidedObjects = new List<GameObject>();

    protected AgentMovementControl m_AgentMovementControl; // A reference to the ThirdPersonCharacter on the object
    protected Pedestrian pedestrian;
    [SerializeField] protected Building parentBuilding;

    [SerializeField]
    public LastAction lastAction;
    [SerializeField]
    public LastObservation lastObservation;

    [Header("Agent training options")]
    [SerializeField]
    public bool useVectorObs = false;

    StreamWriter csvStreamWriter;
    CsvWriter csvWriter;
    [SerializeField]
    private bool logPositions = false;
    [SerializeField]
    float positionLogFrequency = 5;

    protected Vector3 targetPosition;

    protected float triangularValue(int currentPosition, float period)
    {
        return currentPosition <= period ? currentPosition / period : 1 - Mathf.Abs(currentPosition / period - 1);
    }
    protected virtual void Awake()
    {
        parentBuilding = GetComponentInParent<Building>();
        if (GetComponent<RenderTextureSensorComponent>() != null)
        {
            // RenderTexture texture = GetComponent<RenderTextureSensorComponent>().RenderTexture;
            // RenderTexture agentTexture = GameObject.Instantiate(texture);
            // GetComponent<RenderTextureSensorComponent>().RenderTexture = agentTexture;

            var template = FindObjectOfType<AgentViewObject>().gameObject;
            var newRawImage = GameObject.Instantiate(template, Vector3.zero, Quaternion.identity, template.transform.parent);
            var newRenderTexture = GameObject.Instantiate(GetComponent<RenderTextureSensorComponent>().RenderTexture);
            GameObject.Destroy(newRawImage.GetComponent<AgentViewObject>());
            GetComponent<RenderTextureSensorComponent>().RenderTexture = newRenderTexture;
            newRawImage.name = name;
            newRawImage.GetComponent<RawImage>().texture = newRenderTexture;
            GetComponentInChildren<Camera>().targetTexture = GetComponent<RenderTextureSensorComponent>().RenderTexture;
        }
    }
    protected virtual void Start()
    {
        if (logPositions)
        {
            csvStreamWriter = new StreamWriter(string.Format(Paparazzi.destinationFolder + "{0}.csv", transform.parent.name));
            csvWriter = new CsvWriter(csvStreamWriter, CultureInfo.InvariantCulture);
            InvokeRepeating("LogPosition", 1f, 1f / positionLogFrequency);
        }
    }
    private void LogPosition()
    {
        csvWriter.WriteRecords(new List<PositionRecord> { new PositionRecord(transform.localPosition) });
    }
    public override void OnEpisodeBegin()
    {
        m_AgentMovementControl = GetComponent<AgentMovementControl>();
        pedestrian = GetComponent<Pedestrian>();
        parentBuilding = GetComponentInParent<Building>();
        lastAction = new LastAction();
        lastObservation = new LastObservation();

        int currentStep = Academy.Instance.StepCount;
        collidedObjects.Clear();
        // transform.position = pedestrian.PlacedPosition;
        pedestrian.ResetStatus();
        targetPosition = parentBuilding.GetClosestSafeArea(transform.localPosition);
    }
    protected Vector3 GetPointInFrontOfExit(GameObject closestExit)
    {
        Vector3 doorPosition = closestExit.transform.localPosition + Vector3.up * 0.5f;
        foreach (Vector3 doorDirection in new Vector3[] { closestExit.transform.right, -closestExit.transform.right })
        {
            Vector3 doorFront = doorPosition + doorDirection;
            // for each direction (left, right, forward, backward) cast a ray and check for any hits
            // after getting a list of bools (if ray hit something or not) find all the casts where the cast did not hit anything (!r)
            // if there is at least one element returned (count >= 1) then it is outside the position is outside, else check the other direction
            bool outsideBuilding = !Physics.Raycast(doorFront, Vector3.down, 5f, LayerMask.GetMask("Floors"));
            if (outsideBuilding)
            {
                return doorFront;
            }
        }
        return Vector3.zero;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        if (parentBuilding == null)
            return;

        // normalized target area, exit door and  agent positions
        if (useVectorObs && GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.HeuristicOnly)
        {
            // Vector2 doorPosition = parentBuilding.NormalizedPosition(closestExit.transform.localPosition);
            Vector2 normalizedPos = parentBuilding.NormalizedPosition(targetPosition);
            sensor.AddObservation(normalizedPos);
            //sensor.AddObservation(closestExit.transform.InverseTransformDirection(transform.eulerAngles));

            Vector2 position = parentBuilding.NormalizedPosition(transform.localPosition);
            sensor.AddObservation(position);

            //sensor.AddObservation(transform.InverseTransformPoint(closestExit.transform.position));
            float distance = Vector3.Distance(transform.position, targetPosition);
            sensor.AddObservation(parentBuilding.NormalizeDistance(distance));

            var heading = targetPosition - transform.localPosition;
            var distanceVec = heading.magnitude;
            var direction = heading / distance; // This is now the normalized direction.
            sensor.AddObservation(direction.x);
            sensor.AddObservation(direction.z);

            float currentDistance = Vector3.Distance(targetPosition, transform.position);
            float initialDistance = Vector3.Distance(targetPosition, pedestrian.PlacedPosition);
            float normDistance = parentBuilding.NormalizeDistance(initialDistance) - parentBuilding.NormalizeDistance(currentDistance);
            sensor.AddObservation(normDistance);

            //sensor.AddObservation(collidedObjects.Count > 0);
            //sensor.AddObservation(pedestrian.Stats.height);
            //sensor.AddObservation(pedestrian.Stats.collisionSize);
            //sensor.AddObservation(pedestrian.Stats.speedMultiplier);
            lastObservation.targetLocation = normalizedPos;
            lastObservation.currentPosition = position;
            lastObservation.normalizedDistance = normDistance;
            lastObservation.direction = new Vector2(direction.x, direction.z);
            lastObservation.distanceReward = CalculateDistanceReward();
        }
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (actionsOut.DiscreteActions.Array.Length == 0)
            return;
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) ? 1 : (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) ? -1 : 0;
        discreteActions[1] = (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) ? 1 : (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) ? -1 : 0;
    }
    protected virtual void CalculateReward()
    {
        // AddReward(existentialReward / MaxStep);

        if (collidedObjects.Count > 0)
        {
            // collision with objects penalty reward
            AddReward(collisionReward / MaxStep);
        }

        // AddReward(CalculateDistanceReward());
        if (Academy.Instance.StepCount == MaxStep)
        {
            EndEpisodeDeath();
        }
    }
    protected virtual float CalculateDistanceReward()
    {
        float currentDistance = Vector3.Distance(targetPosition, transform.position);

        float initialDistance = Vector3.Distance(targetPosition, pedestrian.PlacedPosition);
        // float distanceDirection = (distanceLastStep - currentDistance) * distanceCoefficient;
        float initialNormDistance = parentBuilding.NormalizeDistance(initialDistance);
        float currentNormDistance = parentBuilding.NormalizeDistance(currentDistance);
        float distanceReward = initialNormDistance - currentNormDistance;
        if (currentNormDistance > initialNormDistance)
            distanceReward *= (currentNormDistance / initialNormDistance);
        else
            distanceReward *= 1f - (currentNormDistance / initialNormDistance);
        // distanceReward = distanceReward > 0 ? 0.05f : -0.05f;

        return distanceReward / MaxStep;
    }
    private float DiscreteActionToFloat(float action)
    {
        switch (action)
        {
            case 0:
                return 0f;
            case 1:
                return 1f;
            case 2:
                return -1f;
            default:
                return 0f;
        }
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float h, v;
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.HeuristicOnly)
            CalculateReward();

        if (actionBuffers.DiscreteActions.Array.Length > 0)
        {
            if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly)
            {
                h = actionBuffers.DiscreteActions[0];
                v = actionBuffers.DiscreteActions[1];
            }
            else
            {
                h = DiscreteActionToFloat(actionBuffers.DiscreteActions[0]);
                v = DiscreteActionToFloat(actionBuffers.DiscreteActions[1]);
            }
        }
        else
        {
            h = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
            v = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
        }
        lastAction.horizontal = h;
        lastAction.vertical = v;
        lastAction.distance = Vector3.Distance(transform.position, targetPosition);
        m_AgentMovementControl.MLMove(h, v);

        if (StepCount >= MaxStep - 1)
        {
            lastEpisodeResult = EpisodeEndResult.None;
            EndEpisode();
        }
    }
    public virtual void DamageTaken()
    {
        // AddReward(damageReward * simpleRewardCurrentCoeff / MaxStep);
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
    public virtual void EndEpisodeDeath()
    {
        lastEpisodeResult = EpisodeEndResult.Death;
        // EventManager.TriggerEvent("PedestrianDied");
        gameObject.SetActive(false);
    }
    public virtual void EndEpisodeSafe()
    {
        lastEpisodeResult = EpisodeEndResult.Safe;
        pedestrian.Saved();
        // EventManager.TriggerEvent("PedestrianSafe");
        // gameObject.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Safe Area"))
        {
            EndEpisodeSafe();
        }
    }
    private bool TakeCollisionIntoAccount(Collision collision)
    {
        return !collision.gameObject.name.ToLower().Contains("door") &&
            !collision.gameObject.name.ToLower().Contains("floor") &&
            !collision.gameObject.CompareTag("Respawn");
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (TakeCollisionIntoAccount(collision))
        {
            if (collidedObjects.Find(c => c.gameObject == collision.gameObject) == null)
            {
                collidedObjects.Add(collision.gameObject);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (TakeCollisionIntoAccount(collision))
        {
            if (collidedObjects.Find(c => c.gameObject == collision.gameObject) != null)
            {
                collidedObjects.Remove(collision.gameObject);
            }
        }
    }
    private void OnApplicationQuit()
    {
        if (csvStreamWriter != null)
            csvStreamWriter.Close();
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
