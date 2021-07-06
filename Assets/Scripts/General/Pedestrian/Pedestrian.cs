using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine.PlayerLoop;
using Unity.Barracuda;

//[RequireComponent(typeof(CapsuleCollider))]
//[RequireComponent(typeof(MLCharacterControl))]
public class Pedestrian : PlaceableObject
{
    [SerializeField]
    private PedestrianStats stats = new PedestrianStats();
    public PedestrianStats Stats { get { return stats; } }
    public bool IsDead { get { return stats.dead; } }
    public bool IsAlive { get { return !stats.dead; } }
    private bool isSafe = false;
    public bool IsSafe { get { return isSafe; } }
    private AgentMovementControl movementControl;
    private Rigidbody rigidBody;
    private MLCharacterControl mlCharacterControl;
    private Collider agentCollider;
    private DecisionRequester decisionRequester;
    private BehaviorParameters behaviorParameters;
    private NNModel defaultNNModel;
    public void UpdateDefaultNNModel(NNModel newModel) { defaultNNModel = newModel; behaviorParameters.Model = newModel; }
    public override Vector3 HalfSizes { get { return transform.GetChild(0).GetComponent<Renderer>().bounds.size / 2; } }
    public override Vector3 Center { get { return transform.GetChild(0).GetComponent<Renderer>().bounds.center; } }

    private void OnValidate()
    {
        if (!GetComponent<CapsuleCollider>())
            return;
        GetComponent<CapsuleCollider>().radius = stats.collisionSize;

        float actualScale = stats.height / GetComponent<CapsuleCollider>().height;

        transform.localScale = new Vector3(actualScale, actualScale, 1);

        if (GetComponent<AgentMovementControl>())
            GetComponent<AgentMovementControl>().MovementSpeedMultiplier = stats.speedMultiplier;

        if (transform.GetChild(0) && transform.GetChild(0).GetComponent<Renderer>())
        {
            if (transform.GetChild(0).GetComponent<Renderer>().sharedMaterial != null)
            {
                transform.GetChild(0).GetComponent<Renderer>().sharedMaterial.SetColor("MaterialColor", stats.color);
                transform.GetChild(1).GetComponent<Renderer>().sharedMaterial.SetColor("MaterialColor", stats.color);
            }
        }

        if (placed)
        {
            OnPlaced();
        }
        else
        {
            UnPlace();
        }
    }

    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Initialize()
    {
        base.Initialize();
        stats.InitializeStats();
        movementControl = GetComponent<AgentMovementControl>();
        rigidBody = GetComponent<Rigidbody>();
        mlCharacterControl = GetComponent<MLCharacterControl>();
        agentCollider = GetComponent<Collider>();
        decisionRequester = GetComponent<DecisionRequester>();
        behaviorParameters = GetComponent<BehaviorParameters>();
        defaultNNModel = behaviorParameters.Model;

        EventManager.StartListening("DecisionPeriodChanged", delegate { decisionRequester.DecisionPeriod = LearningOptions.DecisionPeriod; });
        EventManager.StartListening("TakeActionsBetweenDecisionsChanged", delegate { decisionRequester.TakeActionsBetweenDecisions = LearningOptions.TakeActionsBetweenDecisions; });
        EventManager.StartListening("InferenceDeviceChanged", delegate { behaviorParameters.InferenceDevice = LearningOptions.Device; });
        EventManager.StartListening("MaxStepsChanged", delegate { mlCharacterControl.MaxStep = LearningOptions.MaxSteps; });
        EventManager.StartListening("InferenceAgentBehaviorType", delegate { behaviorParameters.BehaviorType = BehaviorType.InferenceOnly; });
        EventManager.StartListening("DefaultAgentBehaviorType", delegate { behaviorParameters.BehaviorType = BehaviorType.Default; });

        if (behaviorParameters.BehaviorType == BehaviorType.HeuristicOnly)
        {
            Prepare();
            mlCharacterControl.enabled = true;
        }
        else
        {
            movementControl.enabled = false;
            //rigidBody.isKinematic = true;
            //rigidBody.useGravity = true;
            mlCharacterControl.enabled = false;
        }
    }
    public void ResetStatus()
    {
        stats.InitializeStats();
    }
    public override void OnPlaced()
    {
        base.OnPlaced();
    }
    // Update is called once per frame
    void Update()
    {
        if (IsDead)
        {
            return;
        }
    }
    private void Prepare()
    {
        movementControl.enabled = true;
        movementControl.Setup();
        agentCollider.enabled = true;
        rigidBody.isKinematic = false;
    }
    public void StartForSimulation()
    {
        Prepare();
    }
    public void StartEvacuation()
    {
        StartForSimulation();
        Debug.Log(name + " evacuating");
        mlCharacterControl.enabled = true;
    }
    public void StopEvacuation()
    {
        movementControl.enabled = false;
        mlCharacterControl.enabled = false;
        rigidBody.isKinematic = true;
    }
    public void StartLearning()
    {
        Prepare();
        behaviorParameters.Model = null;
        mlCharacterControl.enabled = true;
    }
    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        mlCharacterControl.DamageTaken();

        stats.TakeDamage(damage);
        if (stats.dead)
        {
            OnDead();
        }
    }

    private void OnDead()
    {
        // Debug.Log(name + " has died.");
        if (behaviorParameters.BehaviorType == BehaviorType.InferenceOnly)
        {
            stats.lifePoints = 0;
            stats.dead = true;
            mlCharacterControl.EndEpisodeDeath();

            // transform.GetChild(0).GetComponent<Renderer>().material.SetColor("MaterialColor", Color.red);
            // transform.GetChild(1).GetComponent<Renderer>().material.SetColor("MaterialColor", Color.red);

            EventManager.TriggerEvent("PedestrianDied");
            gameObject.SetActive(false);

            // agentCollider.enabled = false;
            // movementControl.enabled = false;
            // GetComponent<Animator>().SetBool("Crouch", true);

            // mlCharacterControl.enabled = false;
        }
        else
        {
            mlCharacterControl.EndEpisodeDeath();
        }
    }
    public void OnSafe()
    {
        if (behaviorParameters.BehaviorType == BehaviorType.InferenceOnly)
        {
            EventManager.TriggerEvent("PedestrianSafe");
            isSafe = true;
            //GetComponent<CapsuleCollider>().enabled = false;
            //GetComponent<ThirdPersonCharacter>().enabled = false;
            //GetComponent<Animator>().SetBool("Crouch", true);
            //mlCharacterControl.enabled = false;
            gameObject.SetActive(false);
        }
        else
        {
            mlCharacterControl.EndEpisodeSafe();
        }
    }
    public void Saved()
    {
        if (placed && SimulationManager.Evacuating)
            OnSafe();
    }
    private void Revive()
    {
        stats.dead = false;
    }
    public override bool CanBePlaced()
    {
        return floorDetected;
    }
    protected override void ChangeColor(Color newColor)
    {
        transform.GetChild(0).GetComponent<Renderer>().material.SetColor("EmissionColor", newColor);
        transform.GetChild(1).GetComponent<Renderer>().material.SetColor("EmissionColor", newColor);

        transform.GetChild(0).GetComponent<Renderer>().material.SetFloat("EmissionIntensity", 0.5f);
        transform.GetChild(1).GetComponent<Renderer>().material.SetFloat("EmissionIntensity", 0.5f);
    }
    protected override void ResetColor()
    {
        transform.GetChild(0).GetComponent<Renderer>().material.SetColor("EmissionColor", stats.color);
        transform.GetChild(1).GetComponent<Renderer>().material.SetColor("EmissionColor", stats.color);

        transform.GetChild(0).GetComponent<Renderer>().material.SetFloat("EmissionIntensity", 0f);
        transform.GetChild(1).GetComponent<Renderer>().material.SetFloat("EmissionIntensity", 0f);
    }
}
