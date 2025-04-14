using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats_Controller : MonoBehaviour
{
    // Call components and variables for setup, so we can use them later
    public int maxHealth;
    public int currentHealth;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void FixedUpdate()
    {

    }

    public int ModifyStat(int minStat, int changeStat, int maxStat)
    {
        minStat = minStat + changeStat;

        if (minStat <= 0)
        {
            minStat = 0;
        }
        else if (minStat > maxStat)
        {
            minStat = maxStat;
        }
        
        return minStat;
    }
}
