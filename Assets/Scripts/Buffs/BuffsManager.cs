using System.Collections.Generic;
using UnityEngine;

public class BuffsManager : MonoBehaviour
{
    #region Singleton

    private static BuffsManager _instance;

    public static BuffsManager Instance => _instance;


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

    public List<Buff> availableBuffs;
    public List<Buff> availableDebuffs;

    [Range(0, 100)]
    public float buffChance;

    [Range(0, 100)]
    public float debuffChance;
    
}
