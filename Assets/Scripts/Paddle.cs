using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Paddle : MonoBehaviour
{
    #region Singleton

    private static Paddle _instance;

    public static Paddle Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    #endregion

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    [Header("Paddle Movement")]
    [SerializeField]
    private float paddleSpeed = 5.0f;
    private float movement;

    private void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        PaddleMovement();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movement = context.ReadValue<float>();
    }

    private void PaddleMovement()
    {
        float frameWidth = 0.15f;
        float leftClamp = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0)).x + frameWidth;
        float rightClamp = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x - frameWidth;
        float paddleWidth = spriteRenderer.bounds.size.x;

        transform.Translate(movement * paddleSpeed * Time.deltaTime, 0, 0);

        Vector3 position = transform.position;

        position.x = Mathf.Clamp(position.x, leftClamp + paddleWidth/2, rightClamp - paddleWidth / 2);

        transform.position = position;

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Ball")
        {
            Rigidbody2D ballRb = collision.gameObject.GetComponent<Rigidbody2D>();
            Vector3 hitPoint = collision.contacts[0].point;
            Vector3 paddleCenter = new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y);

            ballRb.linearVelocity = Vector2.zero;

            float difference = paddleCenter.x - hitPoint.x;

            if (hitPoint.x < paddleCenter.x)
            {
                ballRb.AddForce(new Vector2(-(Mathf.Abs(difference * 200)),BallsManager.Instance.initialBallSpeed));
            } else
            {
                ballRb.AddForce(new Vector2(Mathf.Abs(difference * 200), BallsManager.Instance.initialBallSpeed));
            }
        }
    }
}
