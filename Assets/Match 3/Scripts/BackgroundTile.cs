using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundTile : MonoBehaviour
{
    public int hitPoints;
    private SpriteRenderer sprite;
    private GoalManager goalManager;

    // Start is called before the first frame update
    void Start()
    {
        goalManager = FindObjectOfType<GoalManager>();
        sprite = GetComponent<SpriteRenderer>();
        sprite.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (hitPoints <= 0)
        {
            if (goalManager  != null)
            {
                goalManager.CompareGoal(this.gameObject.tag);
                goalManager.levelGoals[0].numberCollected++;
                goalManager.UpdateGoals();
            }
            Destroy(this.gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        hitPoints -= damage;
        MakeLighter();
    }

    void MakeLighter()
    {
        //take current color
        Color color = sprite.color;
        //get the current color's alpha value
        float newAlpha = color.a * .5f; 
        sprite.color = new Color(color.r, color.g, newAlpha);
    }

}