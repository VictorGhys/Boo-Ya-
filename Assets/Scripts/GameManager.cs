using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance
    {
        get { return _instance; }
    }

    public static int Score { get; set; } = 0;
    public static float ScoreMultiplier { get; set; } = 0f;
    public static string HighscoreName { get; set; } = "noob";

    // Hud
    [SerializeField]
    private HUD _hud;

    // Difficulty
    private int _difficultyLevel = 1;

    private int _maxDifficultyLevel = 5;
    [SerializeField] private int _difficultyThreshold = 250;
    [SerializeField] private GameObject _lift = null;
    [SerializeField] private GameObject _ghostHunter = null;
    private List<GhostHunter> _ghostHunters = new List<GhostHunter>();
    [SerializeField] private float _speedIncrease = 0.25f;
    [SerializeField] private float _openClosetChanceIncrease = 10f;

    // Audio
    [SerializeField] private AudioClip _liftAudioClip = null;

    private AudioSource _liftAudioSource = null;

    [SerializeField] private AudioSource _scoreIncreaseAudioSource = null;

    // Multipier
    [SerializeField] private float _scoreMultiplierDecrease = 10f;

    [SerializeField] private float[] _scoreMultiplierThresholds = new float[3] { 100, 200, 300 };// first threshold is for x2, second for x3, etc...

    private void Start()
    {
        _ghostHunters.Add(_ghostHunter.GetComponent<GhostHunter>());

        // Find the GameManager
        _instance = FindObjectOfType<GameManager>();
        if (!_instance)
            Debug.LogWarning("No GameManager found");

        //// Keep the GameManager alive when loading new levels
        //DontDestroyOnLoad(gameObject);

        // Audio
        _liftAudioSource = _lift.AddComponent<AudioSource>();
        _liftAudioSource.clip = _liftAudioClip;
        _liftAudioSource.spatialBlend = 0.5f;
        _liftAudioSource.volume = 0.15f;

        // Multiplier
        _hud.SetMaxScoreMultiplier(_scoreMultiplierThresholds[_scoreMultiplierThresholds.Length - 1]);

        // Set default values
        Score = 0;
        ScoreMultiplier = 0;
    }

    private void Update()
    {
        if ((float)Score / (float)_difficultyLevel > _difficultyThreshold)
        {
            if (_difficultyLevel < _maxDifficultyLevel)
            {
                ++_difficultyLevel;
                SpawnGhostHunter();
                IncreaseHunterSpeed();
                IncreaseHunterCheckPercentage();
            }
        }
        if (ScoreMultiplier > 0)
            ScoreMultiplier -= _scoreMultiplierDecrease * Time.deltaTime;
        _hud.SetScoreMultiplier(ScoreMultiplier);
    }

    private void OnDisable()
    {
        ControllerShaker.StopRumble();
    }

    public int IncreaseScore(int amount)
    {
        if (amount < 0)
            return 0;

        int increase = amount;
        // each time the score is above a threshold it is added again
        for (int m = 0; m < _scoreMultiplierThresholds.Length; m++)
        {
            if (ScoreMultiplier > _scoreMultiplierThresholds[m])
            {
                increase += amount;
            }
        }
        Score += increase;
        ScoreMultiplier += amount;

        if (_hud == null)
        {
            _hud = GameObject.Find("HUD_PF").GetComponent<HUD>();
        }
        _hud.SetScore(Score);
        _hud.SetScoreMultiplier(ScoreMultiplier);
        _scoreIncreaseAudioSource.Play();
        return increase;
    }

    public void DecreaseScore(int amount)
    {
        if (amount <= 0)
            Score -= amount;
        if (_hud == null)
        {
            _hud = GameObject.Find("HUD_PF").GetComponent<HUD>();
        }
        _hud.SetScore(Score);
    }

    private void SpawnGhostHunter()
    {
        // Notify the player
        if (_liftAudioSource)
            _liftAudioSource.Play();

        // Spawn the hunter
        if (_ghostHunter && _lift)
        {
            GameObject hunter =
                Instantiate(_ghostHunter, _lift.transform.position, _lift.transform.rotation);
            _ghostHunters.Add(hunter.GetComponent<GhostHunter>());
            LevelManager.Instance.GhostHunterObjects.Add(hunter);
        }

        // Notify player
        _liftAudioSource.Play();
    }

    private void IncreaseHunterSpeed()
    {
        foreach (GhostHunter hunter in _ghostHunters)
        {
            hunter.PatrolSpeed += _speedIncrease;
            hunter.HuntingSpeed += _speedIncrease;
        }
    }

    private void IncreaseHunterCheckPercentage()
    {
        foreach (GhostHunter hunter in _ghostHunters)
        {
            hunter.OpenClosetDoorChance += _openClosetChanceIncrease;
        }
    }
}