using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack_Controller : MonoBehaviour
{
    // Call components and variables for setup, so we can use them later
    // (Many of our Public variables are being set up in the Unity Editor with predetermined variables/prefabs)
    public Stats_Controller user;
    
    public int damage;
    public float attackTimer;
    

    // Update is called once per frame
    void FixedUpdate()
    {
        // If our attack's timer is still going, continue to decrement
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
        // If not, set "is_attack_present" to false for the attack's user and destroy
        // this gameObject
        else if (attackTimer <= 0f)
        {
            user.GetComponent<Player_FSM_Controller>().is_attack_present = false;
            Destroy(this.gameObject);
        }
    }

    // Detects when the attack collides with an actor
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If the attack hitbox collides with an actor...
        if (collision.gameObject.tag == "Actor")
        {
            // Deal damage to our target (and ignore the user-actor as a target)
            Stats_Controller targetStats = collision.gameObject.GetComponent<Stats_Controller>();
           
            if (targetStats != user)
            {
                targetStats.currentHealth = targetStats.ModifyStat(targetStats.currentHealth, damage, targetStats.maxHealth);
                if (user.GetComponent<Player_FSM_Controller>().canPogo)
                {
                    user.GetComponent<Player_FSM_Controller>().isPogoing = true;
                    user.GetComponent<Player_FSM_Controller>().canPogo = false;
                }
                
               
            }
        }
    }
}
