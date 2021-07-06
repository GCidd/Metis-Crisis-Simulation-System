using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class ExitPlaceRandomizer : MonoBehaviour
{
#if UNITY_EDITOR
    [CustomEditor(typeof(ExitPlaceRandomizer))]
    public class customButton : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ExitPlaceRandomizer randomizer = (ExitPlaceRandomizer)target;

            if (GUILayout.Button("Next place"))
            {
                randomizer.ReplaceNextWall();
            }
            if (GUILayout.Button("Get Exits"))
            {
                randomizer.GetAllExits();
            }
            if (GUILayout.Button("Randomize Position"))
            {
                randomizer.RandomizeWallsPositions();
            }
            if (GUILayout.Button("Get all exit replacables"))
            {
                randomizer.GetExitReplaceable();
            }
        }

    }
#endif
    GameObject wallsContainer = null;
    [SerializeField]
    int currentWallIndex = 0;

    [SerializeField]
    public List<DoubleDoor> exits;

    [SerializeField]
    int exitIndexToTest = 0;

    [SerializeField]
    List<GameObject> availableWallsForExit = new List<GameObject>();
    private List<GameObject> lastReplacedWalls = new List<GameObject>();
    
    public void GetExitReplaceable()
    {
        wallsContainer = transform.Find("Walls").gameObject;
        availableWallsForExit = wallsContainer.GetComponentsInChildren<Wall>().ToList().FindAll(w => w.ExitReplaceable).Select(w => w.gameObject).ToList();
    }
    public void ReplaceNextWall()
    {
        if (availableWallsForExit.Count == 0)
        {
            wallsContainer = transform.Find("Walls").gameObject;
            availableWallsForExit = wallsContainer.GetComponentsInChildren<Wall>().ToList().FindAll(w => w.ExitReplaceable).Select(w => w.gameObject).ToList();
        }

        if (currentWallIndex >= availableWallsForExit.Count)
            currentWallIndex = 0;

        if (lastReplacedWalls[exitIndexToTest] != null)
        {
            lastReplacedWalls[exitIndexToTest].SetActive(true);
        }
        lastReplacedWalls[exitIndexToTest] = availableWallsForExit[currentWallIndex];
        lastReplacedWalls[exitIndexToTest].SetActive(false);

        exits[exitIndexToTest].transform.rotation = lastReplacedWalls[exitIndexToTest].transform.rotation;
        exits[exitIndexToTest].transform.position = lastReplacedWalls[exitIndexToTest].transform.position + exits[exitIndexToTest].transform.forward * 1.5f;

        currentWallIndex++;
    }

    public void RandomizeWallsPositions()
    {
        if (availableWallsForExit.Count == 0)
        {
            wallsContainer = transform.Find("Walls").gameObject;
            availableWallsForExit = wallsContainer.GetComponentsInChildren<Wall>().ToList().FindAll(w => w.ExitReplaceable).Select(w => w.gameObject).ToList();
        }
        
        int lastWallIndex = -1;

        for (int i = 0; i < exits.Count; i++)
        {
            int randomWallIndex = UnityEngine.Random.Range(0, availableWallsForExit.Count);
            while (randomWallIndex == lastWallIndex)
            {
                randomWallIndex = UnityEngine.Random.Range(0, availableWallsForExit.Count);
            }
            lastWallIndex = randomWallIndex;

            Transform wall = availableWallsForExit[randomWallIndex].transform;

            if (currentWallIndex >= availableWallsForExit.Count)
                currentWallIndex = 0;

            if (lastReplacedWalls[i] != null)
            {
                lastReplacedWalls[i].SetActive(true);
                lastReplacedWalls[i].GetComponent<Collider>().enabled = true;
            }
            lastReplacedWalls[i] = wall.gameObject;
            lastReplacedWalls[i].SetActive(false);
            
            exits[i].transform.rotation = lastReplacedWalls[i].transform.rotation;
            exits[i].transform.position = lastReplacedWalls[i].transform.position + exits[i].transform.forward * 1.5f;
            exits[i].GetComponent<DoubleDoor>().CloseDoors();
        }
    }
    public void GetAllExits()
    {
        exits = GetComponentsInChildren<DoubleDoor>().ToList().FindAll(d => d.IsExit).ToList();
        for (int i=0; i < exits.Count; i++)
        {
            lastReplacedWalls.Add(null);
        }
    }

    private void Awake()
    {
        GetAllExits();
    }
}
