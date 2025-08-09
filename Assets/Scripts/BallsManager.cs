using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallsManager : MonoBehaviour
{
    #region Singleton

    private static BallsManager _instance;

    public static BallsManager Instance => _instance;

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

    [SerializeField]
    private Ball ballPrefab;
    private Ball initialBall;
    private Rigidbody2D initialBallRb;

    public float initialBallSpeed = 250;
    
    public List<Ball> balls {  get; private set; }

    private void Start()
    {
        InitBall();
    }

    private void Update()
    {
        if (!GameManager.Instance.IsGameStarted)
        {
            initialBall.transform.position = StartingBallPosition();
        }
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.IsGameStarted)
        {
            initialBallRb.bodyType = RigidbodyType2D.Dynamic;
            initialBallRb.AddForce(new Vector2(0, initialBallSpeed));
            GameManager.Instance.IsGameStarted = true;
        }

    }

    private void InitBall()
    {
        initialBall = Instantiate(ballPrefab, StartingBallPosition(), Quaternion.identity);
        initialBallRb = initialBall.GetComponent<Rigidbody2D>();

        this.balls = new List<Ball>()
        {
            initialBall
        };
    }

    private Vector3 StartingBallPosition()
    {
        float ballRadius = ballPrefab.GetComponentInChildren<SpriteRenderer>().bounds.size.y;
        float paddleHeight = Paddle.Instance.gameObject.GetComponent<SpriteRenderer>().bounds.size.y;
        float offset = (ballRadius / 2) + (paddleHeight / 2);

        Vector3 paddlePosition = Paddle.Instance.gameObject.transform.position;
        return new Vector3(paddlePosition.x, paddlePosition.y + offset, 0);
    }
}
