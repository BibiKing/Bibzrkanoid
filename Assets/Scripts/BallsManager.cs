using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public int maxBallsSpawned;

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

        // Garante que a bola fique “presa” ao paddle até o clique
        initialBallRb.bodyType = RigidbodyType2D.Kinematic;
        initialBallRb.linearVelocity = Vector2.zero;
        initialBallRb.angularVelocity = 0f;
        initialBallRb.gravityScale = 0f; // se usar gravidade no projeto

        this.balls = this.balls ?? new List<Ball>();
        this.balls.Add(initialBall);
    }

    private Vector3 StartingBallPosition()
    {
        float ballRadius = ballPrefab.GetComponentInChildren<CircleCollider2D>().bounds.size.y;
        float paddleHeight = Paddle.Instance.gameObject.GetComponent<SpriteRenderer>().bounds.size.y;
        float offset = (ballRadius / 2) + (paddleHeight / 2);

        Vector3 paddlePosition = Paddle.Instance.gameObject.transform.position;
        return new Vector3(paddlePosition.x, paddlePosition.y + offset, 0);
    }

    public void ResetBalls()
    {
        foreach (var ball in this.balls.ToList())
        {
            if (ball != null) Destroy(ball.gameObject);
            
        }

        // Limpa a lista e cria uma única bola inicial
        this.balls.Clear();
        InitBall();
    }

    public void SpawnBalls(Vector3 position, int count, bool isLightningBall)
    {
        for (int i = 0; i < count; i++)
        {
            if (this.balls.Count >= maxBallsSpawned)
            {
                return;
            }
            Ball spawnedBall = Instantiate(ballPrefab, position, Quaternion.identity) as Ball;
            if (isLightningBall)
            {
                spawnedBall.StartLightningBall();
            }

            Rigidbody2D spawnedBallRigidBody = spawnedBall.GetComponent<Rigidbody2D>();
            spawnedBallRigidBody.bodyType = RigidbodyType2D.Dynamic;
            spawnedBallRigidBody.AddForce(new Vector2(0, initialBallSpeed));
            this.balls.Add(spawnedBall);
            
        }
    }
}
