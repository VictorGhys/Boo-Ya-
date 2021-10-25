using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BBUnity.Actions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using Vector3 = UnityEngine.Vector3;

public sealed class LevelManager : MonoBehaviour
{
    // Instance
    private static LevelManager _instance;

    public static LevelManager Instance
    {
        get
        {
            return _instance;
        }
    }

    // Level objects
    private GameObject[] _npcObjects = null;

    private GameObject[] _interactableObjects = null;
    private List<GameObject> _ghostHunterObjects;

    public List<GameObject> GhostHunterObjects
    {
        get { return _ghostHunterObjects; }
    }

    // Tags
    private const string _npcTag = "NPC";

    private const string _interactableTag = "Interact";

    // Audio
    [SerializeField] private AudioSource _levelMusic = null;

    [SerializeField] private AudioSource _rainSounds = null;
    [SerializeField] private AudioSource _thunderSound = null;
    private float _thunderTimer = 0.0f;
    private float _minThunderPitch = 0.8f;
    private float _maxThunderPitch = 1.2f;
    [SerializeField] private float _minThunderInterval = 5;
    [SerializeField] private float _maxThunderInterval = 10;
    private float _currentThunderInterval = 60.0f;

    [SerializeField] private GameObject _lighning = null;
    private List<Light> _lighningLights = null;
    private const float _minFlashInterval = 0.5f;
    private const float _maxFlashInterval = 1.5f;
    [SerializeField] private GameObject _lighningBeams = null;

    [SerializeField] private List<AudioSource> _woodCrackSounds = new List<AudioSource>();
    [SerializeField] private float _minCrackTime = 10.0f;
    [SerializeField] private float _maxCrackTime = 30.0f;

    private void Awake()
    {
        _instance = FindObjectOfType<LevelManager>();
    }

    // Find all interactables in the current level
    public void FindNPCs()
    {
        _npcObjects = null;

        _npcObjects = GameObject.FindGameObjectsWithTag(_npcTag);
        if (_npcObjects == null || _npcObjects.Length == 0)
            Debug.Log("No NPC's found in current level");
    }

    public void FindGhostHunters()
    {
        _ghostHunterObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("GhostHunter"));
        if (_ghostHunterObjects == null || _ghostHunterObjects.Count == 0)
            Debug.Log("No ghosthunters found in the current level");
    }

    // Find all interactabes in the current level
    public void FindInteractables()
    {
        _interactableObjects = null;

        _interactableObjects = GameObject.FindGameObjectsWithTag(_interactableTag);
        if (_interactableObjects == null || _interactableObjects.Length == 0)
            Debug.Log("No interactables found in the current level");
    }

    // get all NPCs in a certain room
    public List<GameObject> GetAllNPCsInRoom(int roomId)
    {
        List<GameObject> npcsInThisRoom = new List<GameObject>();
        foreach (GameObject npc in _npcObjects)
        {
            NPCBehavior npcBehavior = npc.GetComponent<NPCBehavior>();
            if (npcBehavior.RoomId == roomId)
                npcsInThisRoom.Add(npc);
        }

        if (npcsInThisRoom.Count == 0)
            Debug.Log($"No NPCs in room {roomId}");

        return npcsInThisRoom;
    }

    public List<GameObject> GetAllInteractablesInRoom(int roomId)
    {
        List<GameObject> interactablesInThisRoom = new List<GameObject>();
        foreach (GameObject interactable in _interactableObjects)
        {
            if (!interactable)
                continue;

            BaseInteractable baseInteractable = interactable.GetComponent<BaseInteractable>();
            if (baseInteractable && baseInteractable.RoomId == roomId)
                interactablesInThisRoom.Add(interactable);
        }

        if (interactablesInThisRoom.Count == 0)
            Debug.Log("No interactactables in room");

        return interactablesInThisRoom;
    }

    public List<GameObject> GetLightsInRoom(int roomId)
    {
        List<GameObject> lightsInThisRoom = new List<GameObject>();

        foreach (GameObject interactable in _interactableObjects)
        {
            if (!interactable)
                continue;

            LightSwitchBehavior lightSwitchBehavior =
                interactable.GetComponent<LightSwitchBehavior>();
            if (lightSwitchBehavior)
            {
                if (lightSwitchBehavior.RoomId == roomId)
                    lightsInThisRoom.Add(interactable);
            }
        }

        return lightsInThisRoom;
    }

    public bool AreLightsOnInRoom(int roomId)
    {
        List<GameObject> objects = GetLightsInRoom(roomId);

        foreach (GameObject interactable in objects)
        {
            if (!interactable)
                continue;

            LightSwitchBehavior lightSwitchBehavior =
                interactable.GetComponent<LightSwitchBehavior>();
            if (!lightSwitchBehavior)
                continue;

            if (!lightSwitchBehavior.IsLit)
                return false;
        }
        return true;
    }

    public GameObject GetClosestGhostHunter(Vector3 position)
    {
        // Check if there are ghosthunters
        if (_ghostHunterObjects.Count == 0)
            return null;

        GameObject closestHunter = _ghostHunterObjects[0];
        //closestHunter.transform.position = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        foreach (GameObject hunter in _ghostHunterObjects)
        {
            if (Vector3.Distance(hunter.transform.position, position) <
                Vector3.Distance(closestHunter.transform.position, position))
            {
                closestHunter = hunter;
            }
        }

        return closestHunter;
    }

    public void AlertClosestGhostHunter(Vector3 sourcePosition)
    {
        // Find the closest hunter
        GameObject ghostHunter = GetClosestGhostHunter(transform.position);

        // If a hunter is found, alert
        if (ghostHunter)
        {
            GhostHunter hunterBehavior = ghostHunter.GetComponent<GhostHunter>();
            hunterBehavior.HasHearedScream = true;
            hunterBehavior.HearedScreamLocation = sourcePosition;
        }
    }

    private void Start()
    {
        // At the start of the game, find all objects in the level
        FindNPCs();
        FindGhostHunters();
        FindInteractables();

        // Start audio
        _levelMusic.Play();
        _rainSounds.Play();
        float randomTime = Random.Range(_minCrackTime, _maxCrackTime);
        Invoke("PlayWoodCrack", randomTime);

        if (_lighning)
            _lighningLights = new List<Light>(_lighning.GetComponentsInChildren<Light>());
    }

    // Update is called once per frame
    private void Update()
    {
        _thunderTimer += Time.deltaTime;
        if (_thunderTimer >= _currentThunderInterval)
        {
            // Lightning
            StartLightning();

            // Thunder
            PlayThunder();
        }
    }

    private void StartLightning()
    {
        _lighningBeams.SetActive(true);
        foreach (Light light in _lighningLights)
        {
            int lightningFlashes = 5;
            for (int count = 0; count < lightningFlashes; ++count)
            {
                StartCoroutine(FlashLight(light));
                if (count == lightningFlashes - 1)
                    Invoke("DisableLights", _maxFlashInterval * 1.25f);
            }
        }
    }

    private void PlayThunder()
    {
        _currentThunderInterval = Random.Range(_minThunderInterval, _maxThunderInterval);
        _thunderTimer = 0.0f;
        _thunderSound.pitch = Random.Range(_minThunderPitch, _maxThunderPitch);
        _thunderSound.Play();
    }

    public void OnPlayerDeath()
    {
        foreach (GameObject npcObject in _npcObjects)
        {
            NPCBehavior npcBehavior = npcObject.GetComponent<NPCBehavior>();
            npcBehavior.StartDancing();

            StartLightning();
            PlayThunder();
        }

        foreach (GameObject ghostHunterObject in _ghostHunterObjects)
        {
            GhostHunter ghostHunter = ghostHunterObject.GetComponent<GhostHunter>();
            ghostHunter.StartDancing();
        }
    }

    private IEnumerator FlashLight(Light light)
    {
        yield return new WaitForSeconds(Random.Range(_minFlashInterval, _maxFlashInterval));
        _lighning.SetActive(true);
        light.enabled = !light.enabled;
    }

    private void DisableLights()
    {
        _lighningBeams.SetActive(false);
        _lighning.SetActive(false);

        foreach (Light light in _lighningLights)
            light.enabled = false;
    }

    private void PlayWoodCrack()
    {
        int randomIndex = Random.Range(0, _woodCrackSounds.Count - 1);
        float randomPitch = Random.Range(0.8f, 1.2f);
        _woodCrackSounds[randomIndex].pitch = randomPitch;
        _woodCrackSounds[randomIndex].Play();

        float randomTime = Random.Range(_minCrackTime, _maxCrackTime);
        Invoke("PlayWoodCrack", randomTime);
    }
}