using TMPro;
using UnityEngine;

public class KeyboardForController : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _inputField;

    public void AddLetter(string letter)
    {
        if (_inputField.text.Length < _inputField.characterLimit)
        {
            _inputField.text = _inputField.text + letter;
        }
    }

    public void RemoveLastLetter()
    {
        if (_inputField.text.Length > 0)
        {
            _inputField.text = _inputField.text.Remove(_inputField.text.Length - 1);
        }
    }
}