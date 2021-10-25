using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private EventSystem _eventSystem;

    [SerializeField]
    private GameObject _playButton;

    private void Awake()
    {
        _eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
    }

    private void Update()
    {
        if (_eventSystem.currentSelectedGameObject == null)
        {
            _eventSystem.SetSelectedGameObject(_playButton);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Time.timeScale = 1f;
    }
}