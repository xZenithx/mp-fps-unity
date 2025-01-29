using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
	public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float playerHealth = 100f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public float GetPlayerHealth() => playerHealth;
}
