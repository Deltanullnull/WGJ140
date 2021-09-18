using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuBehavior : MonoBehaviour
{
    public AudioClip btnHover;
    public AudioClip btnSelect;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartButton()
    {
        StartCoroutine(StartGame());
    }

    public void MenuButton()
    {
        StartCoroutine(BackToMenu());
    }

    private IEnumerator StartGame()
    {
        // TODO play sound
        this.GetComponent<AudioSource>().clip = btnSelect;
        this.GetComponent<AudioSource>().Play();

        float length = btnSelect.length;

        yield return new WaitForSeconds(length);

        SceneManager.LoadScene(1);

        yield return null;
    }

    private IEnumerator BackToMenu()
    {
        // TODO play sound
        this.GetComponent<AudioSource>().clip = btnSelect;
        this.GetComponent<AudioSource>().Play();

        float length = btnSelect.length;

        yield return new WaitForSeconds(length);

        Debug.Log("Return to main menu");

        SceneManager.LoadScene(0);

        yield return null;
    }
}
