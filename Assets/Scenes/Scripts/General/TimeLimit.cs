using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TimeLimitation
{
    public int seconds = 0;
    public int minutes = 1;
    public int hours = 0;
}

public class TimeLimit : MonoBehaviour
{
    [Header("Time Limit")]
    public TimeLimitation timeLimitation;
    public GameObject gameOverScreen;

    [Header("Audio Clips")]
    public AudioClip mainTheme;
    [Space(10)]
    public AudioClip gameLost;
    public AudioClip gameWon;

    public Text txtTime;

    public RawImage black;

    public static bool GameOver = false;

    private float totalSeconds;

    // Start is called before the first frame update
    void Start()
    {
        GameOver = false;

        GetComponent<AudioSource>().Play();

        totalSeconds += timeLimitation.seconds;
        totalSeconds += timeLimitation.minutes * 60;
        totalSeconds += timeLimitation.hours * 60 * 60;

        //txtTime.text = "Time Limit: " + totalSeconds;

        //print();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    // Update is called once per frame
    private bool alreadyLost = false;
    private bool deathOverride = false;
    void Update()
    {
        if (GameOver)
            return;

        if (!(PlayerMovement.objectiveCount <= 0))
        {



            if (totalSeconds <= 0f)
            {
                txtTime.text = "0 seconds left!";
            }
            else
            {
                txtTime.text = ((int)totalSeconds).ToString() + " seconds left!";
            }

            if (GameObject.Find("Player").GetComponent<PlayerMovement>().Health <= 0)
            {
                deathOverride = true;
            }

            if (totalSeconds <= 0f || deathOverride)
            {
                if (!alreadyLost)
                {
                    alreadyLost = true;

                    GameOver = true;

                    StartCoroutine(Death());
                }
            }

            totalSeconds -= Time.deltaTime;
        }
        else
        {
            GameOver = true;

            StartCoroutine(Victory());
        }
    }

    private IEnumerator Victory()
    {
        GetComponent<AudioSource>().Stop();
        yield return new WaitForSeconds(2);
        GetComponent<AudioSource>().volume = 1f;
        GetComponent<AudioSource>().PlayOneShot(gameWon);

        // Game Over screen
        gameOverScreen.SetActive(true);

        StartCoroutine(Fadeout());

        gameOverScreen.transform.Find("Reason").GetComponent<Text>().text = "YOU WON!";
        

        // TODO: Click the button to go to main menu
    }

    private IEnumerator Death()
    {
        GetComponent<AudioSource>().Stop();
        yield return new WaitForSeconds(2);
        GetComponent<AudioSource>().volume = 1f;
        GetComponent<AudioSource>().PlayOneShot(gameLost);

        // Game Over screen
        gameOverScreen.SetActive(true);

        StartCoroutine(Fadeout());

        if (deathOverride)
        {
            gameOverScreen.transform.Find("Reason").GetComponent<Text>().text = "YOU DIED!";
        }
        else
        {
            gameOverScreen.transform.Find("Reason").GetComponent<Text>().text = "TIME RAN OUT!";
        }

        // TODO: Click the button to go to main menu
    }

    private IEnumerator Fadeout()
    {
        float alpha = 0f;
        float speed = 1f;

        while (true)
        {
            alpha = Mathf.Lerp(alpha, 1f, Time.deltaTime * speed) ;

            var color = black.color;
            color.a = alpha;
            black.color = color;

            yield return null;
        }

        
    }
}
