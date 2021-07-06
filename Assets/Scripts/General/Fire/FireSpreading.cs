using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FireSpreading : PlaceableObject
{
    [SerializeField]
    private float growDelay = 3;
    [SerializeField]
    private float growRate = 1;
    [SerializeField]
    private float startingRadius = 1;
    [SerializeField]
    private GameObject fireObject;
    public int burstSize = 100;
    public bool burstOnGrow = true;

    private float currentRadius;
    private List<GameObject> firesInArea = new List<GameObject>();

    protected override void Initialize()
    {
        currentRadius = startingRadius;
        rightClickEvents = new Dictionary<string, UnityEvent>();
        SetUpRightClickOptions();
        initialized = true;
    }
    public void StartFire()
    {
        InvokeRepeating("Grow", growDelay, growDelay);
        GetComponentsInChildren<Fire>().ToList().ForEach(f => f.StartDamage());
    }
    public void StopFire()
    {
        CancelInvoke("Grow");
        GetComponentsInChildren<Fire>().ToList().ForEach(f => f.StopDamage());
    }
    public void CleanFire()
    {
        StopFire();
        currentRadius = startingRadius;
        firesInArea.ForEach(f => GameObject.Destroy(f));
        firesInArea.Clear();
        CancelInvoke("Grow");
    }
    void AddNewFire()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject newFire = Instantiate(fireObject, transform);
            firesInArea.Add(newFire);
            int randomAngle = UnityEngine.Random.Range(0, 360);
            float newX = currentRadius * Mathf.Cos(randomAngle);
            float newZ = currentRadius * Mathf.Sin(randomAngle);
            newFire.transform.localPosition = new Vector3(newX, 0, newZ);
            newFire.SetActive(true);
            newFire.GetComponent<Fire>().StartDamage();
        }
    }
    void Grow()
    {
        currentRadius += growRate;
        AddNewFire();
        if (burstOnGrow)
            firesInArea.ForEach(f => f.GetComponent<ParticleSystem>().Emit(burstSize));
    }
    public void BurstGrow(int times)
    {
        for (int i = 0; i < times; i++)
            Grow();
    }
    protected override void ChangeColor(Color newColor)
    {

    }
    protected override void ResetColor()
    {

    }

}
