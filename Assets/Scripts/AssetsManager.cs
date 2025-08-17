using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class AssetsManager : MonoBehaviour
{
    #region Singleton

    private static AssetsManager _instance;

    public static AssetsManager Instance => _instance;

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

    [Header("---Ui---")]
    [SerializeField] public CameraShake cameraShake;
    [SerializeField] public TMP_Text scoreTextTMP;
    [SerializeField] public TMP_Text targetTextTMP;
    [SerializeField] public TMP_Text levelTextTMP;
    [SerializeField] public Transform paddleLifePrefab;
    [SerializeField] public Transform remainingBallsPrefab;
    [SerializeField] public Transform scoreTextPopupPrefab;

    [Header("---Buffs---")]
    [SerializeField] public List<Sprite> buffsSprites;

    [Header("---Ball---")]
    [SerializeField] public Ball ballPrefab;

    [Header("---Bricks---")]
    [SerializeField] public Brick brickPrefab;
    [SerializeField] public Sprite regularBrickSprite;
    [SerializeField] public Sprite hardBrickSprite;
    [SerializeField] public Sprite indestructibleBrickSprite;
    [SerializeField] public ColorPallete[] colorPalettes;

    [Header("---Paddle---")]
    [SerializeField] public Projectile bulletPrefab;

    [Header("---FX---")]
    [SerializeField] public EffectSpawner smallExplosion;
}

[System.Serializable]
public class ColorPallete
{
    public Color[] colors;
}
