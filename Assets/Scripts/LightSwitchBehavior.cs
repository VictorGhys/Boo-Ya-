using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSwitchBehavior : BaseInteractable
{
    [SerializeField] private GameObject[] _lights = null;
    private bool _isLit = true;

    [SerializeField] private AudioSource _switchAudio = null;
    [SerializeField] private GameObject _flickedIndicator = null;
    [SerializeField] private float _indicatorTime = 0.25f;

    public bool IsLit
    {
        get { return _isLit; }
    }

    private void Start()
    {
        //TurnOnLights();
        HideIndicator();
    }

    private void Update()
    {
        base.Update();

        foreach (GameObject light in _lights)
        {
            if (!light)
                return;

            if (_isInteractedWith)
            {
                ShowIndicator();
                Invoke("HideIndicator", _indicatorTime);

                if (_isLit)
                    TurnOffLights();
                else
                    TurnOnLights();
            }
        }
    }

    public void TurnOffLights()
    {
        // Turn off all connected lights
        foreach (GameObject light in _lights)
        {
            if (light)
                light.SetActive(false);
        }
        _isLit = false;
        _isInteractedWith = false;

        // Scare the NPCs in the room
        ScareNPCs();
        AlertNPCs();

        // Play sound
        if (_switchAudio)
        {
            _switchAudio.pitch = 0.9f;
            _switchAudio.Play();
        }
    }

    public void TurnOnLights()
    {
        // Turn on all connected lights
        foreach (GameObject light in _lights)
        {
            if (light)
            {
                light.SetActive(true);
                _isLit = true;
                _isInteractedWith = false;
            }
        }

        // Play sound
        if (_switchAudio)
        {
            _switchAudio.pitch = 1.1f;
            _switchAudio.Play();
        }
    }

    private void ShowIndicator()
    {
        _flickedIndicator.SetActive(true);
    }

    private void HideIndicator()
    {
        _flickedIndicator.SetActive(false);
    }
}