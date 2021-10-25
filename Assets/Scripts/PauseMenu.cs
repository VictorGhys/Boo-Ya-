using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private PlayerControls _controls;

    public static bool GameIsPaused { get; set; } = false;

    [SerializeField]
    private GameObject _pauseMenuUI;

    [SerializeField]
    private PlayerCharacter _player;

    [SerializeField]
    private GameObject _resumeButton;

    private EventSystem _eventSystem;

    private void Awake()
    {
        _pauseMenuUI.SetActive(false);
        _controls = new PlayerControls();
        _controls.GamePlay.Pause.performed += ctx => TogglePause();
        _eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
    }

    private void Update()
    {
        if (_eventSystem.currentSelectedGameObject == null && _pauseMenuUI.activeSelf)
        {
            _eventSystem.SetSelectedGameObject(_resumeButton);
        }
    }

    private void OnEnable()
    {
        _controls.GamePlay.Enable();
    }

    private void OnDisable()
    {
        _controls.GamePlay.Disable();
    }

    private void TogglePause()
    {
        if (_player)
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private void Pause()
    {
        _pauseMenuUI.SetActive(true);
        Time.timeScale = 0.0001f;
        GameIsPaused = true;
        _eventSystem.SetSelectedGameObject(_resumeButton);
        _player.DisableControls();
    }

    public void Resume()
    {
        _pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        _eventSystem.SetSelectedGameObject(null);
        _player.EnableControls();
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Application.Quit();
    }
}