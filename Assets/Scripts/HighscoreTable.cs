using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class HighscoreTable : MonoBehaviour
{
    [SerializeField]
    private Transform _entryContainer;

    [SerializeField]
    private Transform _entryTemplate;

    private int _amountDisplayed = 10;

    private List<Transform> _highscoreEntryTransforms;
    private const string _table = "highscoreTable";

    [SerializeField]
    private float _templateHeight = 60;

    [SerializeField]
    private NameInput _nameInput;

    [System.Serializable]
    private class HighscoreEntry
    {
        public int score;
        public string name;
    }

    private class Highscores
    {
        public List<HighscoreEntry> highscoreEntries;
    }

    private void Start()
    {
        _entryTemplate.gameObject.SetActive(false);

        //AddHighscoreEntry(99981, "victor");

        // Read in Data
        string jsonString = PlayerPrefs.GetString(_table);
        Highscores highscores = JsonUtility.FromJson<Highscores>(jsonString);

        _highscoreEntryTransforms = new List<Transform>();

        _nameInput.Show("azertyuiopqsdfghjklmwxcvbn", 10, name =>
        {
            // Look if the player is worthy of a spot on the highscore board
            if (_highscoreEntryTransforms.Count < _amountDisplayed ||
                GameManager.Score > highscores.highscoreEntries[_amountDisplayed - 1].score)
            {
                GameManager.HighscoreName = name;
                _nameInput.SaveName(name);
                Debug.Log($"new name is {name}");
                AddHighscoreEntry(GameManager.Score, GameManager.HighscoreName);
                string newJsonString = PlayerPrefs.GetString(_table);
                highscores = JsonUtility.FromJson<Highscores>(newJsonString);
                // Clear the transforms.
                foreach (var highscoreEntryTransform in _highscoreEntryTransforms)
                {
                    Destroy(highscoreEntryTransform.gameObject);
                }

                _highscoreEntryTransforms.Clear();
                // Sort entries
                highscores.highscoreEntries.Sort(delegate (HighscoreEntry h1, HighscoreEntry h2) { return h2.score.CompareTo(h1.score); });
                // Create transforms.
                foreach (var highscoreEntry in highscores.highscoreEntries)
                {
                    CreateHighscoreEntryTransform(highscoreEntry, _entryContainer, _highscoreEntryTransforms);
                }
            }
        });
        // Sort entries
        highscores.highscoreEntries.Sort(delegate (HighscoreEntry h1, HighscoreEntry h2) { return h2.score.CompareTo(h1.score); });
        // Create transforms.
        foreach (var highscoreEntry in highscores.highscoreEntries)
        {
            CreateHighscoreEntryTransform(highscoreEntry, _entryContainer, _highscoreEntryTransforms);
        }
    }

    private void CreateHighscoreEntryTransform(HighscoreEntry highscoreEntry, Transform container,
        List<Transform> transformList)
    {
        Transform entryTransform = Instantiate(_entryTemplate, container);
        RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
        entryRectTransform.anchoredPosition = new Vector2(0, -_templateHeight * transformList.Count);
        entryTransform.gameObject.SetActive(true);

        int rank = transformList.Count + 1;
        string rankString;
        switch (rank)
        {
            default:
                rankString = rank + "TH";
                break;

            case 1:
                rankString = "1ST"; break;

            case 2:
                rankString = "2ND"; break;

            case 3:
                rankString = "3RD"; break;
        }

        Transform rankTransform = entryTransform.Find("rank");
        rankTransform.GetComponent<TextMeshProUGUI>().SetText(rankString);

        int score = highscoreEntry.score;
        entryTransform.Find("score").GetComponent<TextMeshProUGUI>().SetText(score.ToString());

        string name = highscoreEntry.name;
        entryTransform.Find("name").GetComponent<TextMeshProUGUI>().SetText(name);
        // Set background visible on odds and not on evens, easier to read
        entryRectTransform.Find("background").gameObject.SetActive(rank % 2 == 1);

        transformList.Add(entryTransform);
    }

    private void AddHighscoreEntry(int score, string name)
    {
        // Create Highscores
        HighscoreEntry highscoreEntry = new HighscoreEntry { score = score, name = name };

        // Load saved Highscores
        string jsonString = PlayerPrefs.GetString(_table);
        Highscores highscores = JsonUtility.FromJson<Highscores>(jsonString);

        if (highscores == null)
        {
            highscores = new Highscores();
            highscores.highscoreEntries = new List<HighscoreEntry>();
        }
        // Add new entry to Highscores
        highscores.highscoreEntries.Add(highscoreEntry);
        if (highscores.highscoreEntries.Count > _amountDisplayed)
        {
            // Sort entries
            highscores.highscoreEntries.Sort(delegate (HighscoreEntry h1, HighscoreEntry h2) { return h2.score.CompareTo(h1.score); });
            highscores.highscoreEntries.RemoveAt(_amountDisplayed);
        }
        // Save updated highscores
        string json = JsonUtility.ToJson(highscores);
        PlayerPrefs.SetString(_table, json);
        PlayerPrefs.Save();
    }

    public void ClearHighscores()
    {
        foreach (var highscoreEntryTransform in _highscoreEntryTransforms)
        {
            Destroy(highscoreEntryTransform.gameObject);
        }
        _highscoreEntryTransforms.Clear();

        Highscores highscores = new Highscores();
        highscores.highscoreEntries = new List<HighscoreEntry>();
        string json = JsonUtility.ToJson(highscores);
        PlayerPrefs.SetString(_table, json);
        PlayerPrefs.Save();
    }
}