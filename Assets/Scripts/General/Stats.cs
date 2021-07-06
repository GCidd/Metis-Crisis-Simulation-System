using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class Stats
{
    public float maxLifePoints = 150f;
    [NonSerialized]
    public bool dead = false;
    public float lifePoints;
    public float LifePercentage { get { return lifePoints / maxLifePoints; } }


    public virtual void InitializeStats()
    {
        lifePoints = maxLifePoints;
        dead = false;
    }
    public virtual void TakeDamage(float damage)
    {
        if (dead)
            return;

        lifePoints -= damage;
        if (lifePoints <= 0)
        {
            OnDead();
        }
    }

    public virtual void OnDead()
    {
        dead = true;
    }
    public virtual void Revive()
    {
        dead = false;
    }
}
