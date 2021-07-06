using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class Fire : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Time per second damage is inflicted.")]
    private float damageRate = 1f;
    [SerializeField]
    private float damage = 100f;
    private float damageCooldown;
    [SerializeField]
    private bool canDamage = false;
    public void StartDamage()
    {
        GetComponent<Collider>().enabled = true;
        canDamage = true;
    }
    public void StopDamage()
    {
        canDamage = false;
    }

    private Dictionary<GameObject, float> objectDamageCooldown = new Dictionary<GameObject, float>();
    // Start is called before the first frame update
    void Start()
    {
        damageCooldown = 1f / damageRate;
    }

    private void Update()
    {
        if (!canDamage)
            return;
        // keys may change during loop
        List<GameObject> keys = objectDamageCooldown.Keys.ToList();
        foreach (GameObject _object in keys)
        {
            if (_object.GetComponent<Pedestrian>() != null && _object.GetComponent<Pedestrian>().IsDead)
                continue;

            if (Time.time - objectDamageCooldown[_object] >= damageCooldown)
            {
                // Debug.Log(_object.name + " takes " + damage + " damage!");
                _object.GetComponent<Pedestrian>().TakeDamage(damage);
                objectDamageCooldown[_object] = Time.time;
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!objectDamageCooldown.ContainsKey(other.gameObject))
        {
            Pedestrian stats = other.gameObject.GetComponent<Pedestrian>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
                // Debug.Log(other.gameObject.name + " takes " + damage + " damage!");
                objectDamageCooldown.Add(other.gameObject, Time.time);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (objectDamageCooldown.ContainsKey(other.gameObject))
        {
            objectDamageCooldown.Remove(other.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        //Vector3 center = transform.position;
        //List<GameObject> colls = new List<GameObject>();
        //Gizmos.DrawRay(center, Vector3.right * 500);
        //colls.AddRange(Physics.RaycastAll(center, transform.right, 500f, LayerMask.GetMask("Walls")).Select(h => h.transform.parent.parent.gameObject));

        //if (colls.Count % 2 == 0)
        //{
        //    Gizmos.color = Color.green;
        //    Gizmos.DrawRay(center, Vector3.left);
        //    return;
        //}

        //List<GameObject> uniqBuildings = colls.Distinct().ToList();

        //if (uniqBuildings.Count == 1)
        //{
        //    Gizmos.color = Color.blue;
        //    Gizmos.DrawRay(center, Vector3.left);
        //    return;
        //}
        //else
        //{
        //    List<GameObject> buildings = uniqBuildings.FindAll(b => colls.FindAll(c => c == b).Count % 2 == 1).ToList();
        //    if (buildings.Count > 0)
        //    {
        //        Gizmos.color = Color.red;
        //        Gizmos.DrawRay(center, Vector3.left);
        //        return;
        //    }
        //}
    }
}

