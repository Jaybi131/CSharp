using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Health : MonoBehaviour
{
    [SerializeField] int health = 100;

    private int MAX_HEALTH = 100;

    public static Action OnPlayerDeath;
    public static Action OnEnemyDeath;
   // public int mana = 50;
    //[SerializeField] private int experience = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   

    // Update is called once per frame
    void Update()
    {
       if(Input.GetKeyDown(KeyCode.L))
        {
            Damage(10);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            Heal(10);
            
        }
    }

    public void SetHealth(int maxHealth, int health)
    {
        this.MAX_HEALTH = maxHealth;
        this.health = health;   
    }

    private IEnumerator VisualIndicator(Color color)
    {
        GetComponent<SpriteRenderer>().color = color;
        yield return new WaitForSeconds(0.15f);
        GetComponent<SpriteRenderer>().color = Color.white;

    }

    public void Damage(int amount)
    {
        if(amount < 0)
        {
            throw new System.ArgumentOutOfRangeException("Cannot have negative damage");
        }

        this.health -= amount;

        StartCoroutine(VisualIndicator(Color.red));

        if(health <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount < 0)
        {
            throw new System.ArgumentOutOfRangeException("Cannot have negative healing");
        }

        bool wouldbeOverMAxHealth = health + amount > MAX_HEALTH;

        if (wouldbeOverMAxHealth)
        {
            this.health = MAX_HEALTH;
        }
        else
        {
            this.health += amount;
            StartCoroutine(VisualIndicator(Color.green));
        }
         
    }

    private void Die()
    {
        Debug.Log("Dead...");
        Destroy(gameObject);

        if (this.CompareTag("Player"))
        {
            Time.timeScale = 0;
            OnPlayerDeath?.Invoke();
        }

        else
        {
            OnEnemyDeath?.Invoke();
        }
    }
}
