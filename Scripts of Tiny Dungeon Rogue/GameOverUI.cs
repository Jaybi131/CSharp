using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
public class GameOverUI : MonoBehaviour
{

    [SerializeField] private TMP_Text scoreValueText;

    private int score = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Health.OnPlayerDeath += ActivateGameObject;
        Health.OnEnemyDeath += CountScore;
        this.gameObject.SetActive(false);
    }

    // Update is called once per frame
    private void OnDestroy()
    {
        Health.OnPlayerDeath -= ActivateGameObject;
        Health.OnEnemyDeath -= CountScore;
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene(0);
    }
    private void CountScore()
    {
        score++;
    }

    private void ActivateGameObject()
    {
        this.gameObject.SetActive(true);
        scoreValueText.text = score.ToString();
    }
}