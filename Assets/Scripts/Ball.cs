using UnityEngine;

public class Ball : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.IsGameStarted == true) {

            transform.Rotate(new Vector3(0, 0, 180 * Time.deltaTime));
   
        }
    }
}
