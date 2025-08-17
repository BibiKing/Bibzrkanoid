using TMPro;
using UnityEngine;

public class ScoreTextPopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Color textColor;

    [SerializeField] private float lifetime = 0.2f;
    [SerializeField] private float disappearSpeed = 10f;
    [SerializeField] private float moveSpeed = 0.2f;
    float aliveTime;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public static ScoreTextPopup Create(Vector3 position, int points)
    {
        Transform scoreTMP = Instantiate(AssetsManager.Instance.scoreTextPopupPrefab, position, Quaternion.identity);
        ScoreTextPopup scoreText = scoreTMP.GetComponent<ScoreTextPopup>();
        scoreText.Setup(points);

        return scoreText;
    }

    public void Setup(int points)
    {
        textMesh.SetText(points.ToString());
        textColor = textMesh.color;
        aliveTime = 0;
    }

    private void Update()
    {
        transform.position += new Vector3(0, moveSpeed) * Time.deltaTime;
        float scaleReducing = Time.deltaTime * disappearSpeed * 0.1f;
        transform.position += new Vector3(0, scaleReducing, 0);
        aliveTime += Time.deltaTime;
        if (aliveTime > lifetime)
        {
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;
            if(textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }


}
