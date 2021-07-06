using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PedestrianStats : Stats
{
    public float collisionSize = 0.3f;
    public float height = 1f;
    public Color color = Color.black;
    public float speedMultiplier = 1f;

    public new void InitializeStats()
    {
        base.InitializeStats();
    }
}
