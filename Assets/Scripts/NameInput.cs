using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class NameInput : MonoBehaviour
{
    [SerializeField]
    private UnityEngine.UI.Button _submitButton;

    [SerializeField]
    private TMP_InputField _inputField;

    private EventSystem _eventSystem;

    [SerializeField]
    private GameObject _playAgain;

    [SerializeField]
    private TMP_Text _nameField;

    private const string _playerprefsName = "name";

    private class PlayerName
    {
        public string nameString;
    }

    private void Awake()
    {
        _eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

        string jsonString = PlayerPrefs.GetString(_playerprefsName);
        PlayerName playerName = JsonUtility.FromJson<PlayerName>(jsonString);
        if (playerName != null && playerName.nameString != null && playerName.nameString.Length >= 1)
        {
            GameManager.HighscoreName = playerName.nameString;
        }

        if (_nameField)
        {
            _nameField.text = GameManager.HighscoreName;
        }
        gameObject.SetActive(false);
    }

    public void ChangeHighscoreName()
    {
        Show("azertyuiopqsdfghjklmwxcvbn", 10, name =>
        {
            GameManager.HighscoreName = name;
            if (_nameField)
            {
                _nameField.text = GameManager.HighscoreName;
            }
            Debug.Log($"new name is {name}");
            SaveName(name);
        });
    }

    public void Show(string validCharacters, int characterLimit, Action<string> submitAction)
    {
        gameObject.SetActive(true);
        _eventSystem.SetSelectedGameObject(_submitButton.gameObject);
        _inputField.text = GameManager.HighscoreName;
        _inputField.characterLimit = characterLimit;
        _inputField.onValidateInput = (string TextMeshPro, int charIndex, char addedChar) =>
        {
            return ValidateChar(validCharacters, addedChar);
        };
        _submitButton.onClick.AddListener(() =>
        {
            Hide();
            submitAction(_inputField.text);
        });
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _eventSystem.SetSelectedGameObject(_playAgain);
    }

    private char ValidateChar(string validCharacters, char addedChar)
    {
        if (validCharacters.IndexOf(addedChar) != -1)
        {
            return addedChar;
        }
        else
        {
            return '\0';
        }
    }

    public void SaveName(string name)
    {
        // Save updated name
        PlayerName playerName = new PlayerName();
        playerName.nameString = name;
        string json = JsonUtility.ToJson(playerName);
        PlayerPrefs.SetString(_playerprefsName, json);
        PlayerPrefs.Save();
    }
}