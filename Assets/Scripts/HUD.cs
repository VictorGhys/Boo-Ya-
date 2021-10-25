using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Slider _invisibilitySlider;
    [SerializeField] private Slider _booSlider;
    [SerializeField] private TextMeshProUGUI _score;
    [SerializeField] private Image _playerIcon;
    [SerializeField] private Image _playerIconInvisible;
    [SerializeField] private Slider _scoreMultiplierSlider;
    [SerializeField] private Gradient _gradient;
    [SerializeField] private Image _multiplierImage;

    // Score Animation
    [SerializeField] private float _maxScoreAnimationTime = 2f;

    [SerializeField] private float _scaleAmount = 1f;
    private float _scoreAnimationTime;

    private void Update()
    {
        if (_scoreAnimationTime >= 0)
        {
            if (_scoreAnimationTime > _maxScoreAnimationTime * 0.5f)
            {
                // First half of the animation
                _score.transform.localScale += Vector3.one * _scaleAmount * Time.deltaTime;
            }
            else
            {
                // Second half of the animation
                _score.transform.localScale -= Vector3.one * _scaleAmount * Time.deltaTime;
            }
            _scoreAnimationTime -= Time.deltaTime;
        }
    }

    public void SetInvisibleCoolDown(float cooldown)
    {
        _invisibilitySlider.value = cooldown;
    }

    public void SetMaxInvisibleCoolDown(float maxCooldown)
    {
        _invisibilitySlider.maxValue = maxCooldown;
        _invisibilitySlider.value = maxCooldown;
    }

    public void SetBooCoolDown(float cooldown)
    {
        _booSlider.value = cooldown;
    }

    public void SetMaxBooCoolDown(float maxCooldown)
    {
        _booSlider.maxValue = maxCooldown;
        _booSlider.value = maxCooldown;
    }

    public void SetScore(int score)
    {
        _score.SetText(score.ToString());
        _scoreAnimationTime = _maxScoreAnimationTime;
        _score.transform.localScale = Vector3.one;
    }

    public void UpdatePlayerIcon(bool isInvisible)
    {
        if (isInvisible)
        {
            _playerIcon.enabled = false;
            _playerIconInvisible.enabled = true;
        }
        else
        {
            _playerIcon.enabled = true;
            _playerIconInvisible.enabled = false;
        }
    }

    public void SetScoreMultiplier(float scoreMultiplier)
    {
        _scoreMultiplierSlider.value = scoreMultiplier;
        _multiplierImage.color = _gradient.Evaluate(scoreMultiplier / _scoreMultiplierSlider.maxValue);
    }

    public void SetMaxScoreMultiplier(float maxScoreMultiplier)
    {
        _scoreMultiplierSlider.maxValue = maxScoreMultiplier;
        _multiplierImage.color = _gradient.Evaluate(_scoreMultiplierSlider.value / _scoreMultiplierSlider.maxValue);
    }
}