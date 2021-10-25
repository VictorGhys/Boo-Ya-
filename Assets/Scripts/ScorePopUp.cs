using TMPro;
using UnityEditor;
using UnityEngine;

public class ScorePopUp : MonoBehaviour
{
    private TextMeshPro _textMesh;

    private Color _color;
    private float _disappearTime;
    private const float _maxDisappearTime = 1;
    private Vector3 _moveVector;
    private static int _sortingOrder;

    [SerializeField]
    private float _moveSpeed = 30f;

    [SerializeField]
    private Vector3 _moveDir = new Vector3(1, 1);

    public static ScorePopUp Create(Transform pf, Vector3 position, int score)
    {
        Transform scorePopUpTransform = Instantiate(pf, position, Quaternion.identity);
        ScorePopUp scorePopUp = scorePopUpTransform.GetComponent<ScorePopUp>();
        scorePopUp.SetUp(score);
        return scorePopUp;
    }

    private void Awake()
    {
        _textMesh = transform.GetComponent<TextMeshPro>();
    }

    public void SetUp(int score)
    {
        _textMesh.SetText(score.ToString());
        _color = _textMesh.color;
        _disappearTime = _maxDisappearTime;

        _sortingOrder++;
        _textMesh.sortingOrder = _sortingOrder;

        _moveVector = _moveDir * _moveSpeed;
    }

    private void Update()
    {
        transform.position += _moveVector * Time.deltaTime;
        _moveVector -= _moveVector * 8f * Time.deltaTime;
        if (_disappearTime > _maxDisappearTime * 0.5f)
        {
            // First half of the popup lifetime
            const float increaseScaleAmount = 1;
            transform.localScale += Vector3.one * increaseScaleAmount * Time.deltaTime;
        }
        else
        {
            // Second half of the popup lifetime
            const float decreaseScaleAmount = 1;
            transform.localScale -= Vector3.one * decreaseScaleAmount * Time.deltaTime;
        }
        _disappearTime -= Time.deltaTime;
        if (_disappearTime < 0)
        {
            // Start disappearing
            float disappearSpeed = 5;
            _color.a -= disappearSpeed * Time.deltaTime;
            _textMesh.color = _color;
            if (_color.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }
}