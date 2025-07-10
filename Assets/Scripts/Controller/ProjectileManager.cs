using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    [Header("Projectile Prefabs")]
    [SerializeField] private GameObject defaultProjectilePrefab;
    [SerializeField] private Transform projectileContainer;

    [Header("Pooling")]
    [SerializeField] private int poolSize = 50;
    [SerializeField] private bool usePooling = true;

    private Queue<GameObject> projectilePool = new Queue<GameObject>();
    private static ProjectileManager instance;

    public static ProjectileManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<ProjectileManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ProjectileManager");
                    instance = go.AddComponent<ProjectileManager>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else if (instance != this)
            Destroy(gameObject);
    }

    void InitializePool()
    {
        if (!usePooling || defaultProjectilePrefab == null)
            return;

        if (projectileContainer == null)
        {
            GameObject container = new GameObject("ProjectileContainer");
            container.transform.SetParent(transform);
            projectileContainer = container.transform;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile = Instantiate(defaultProjectilePrefab, projectileContainer);
            projectile.SetActive(false);
            projectilePool.Enqueue(projectile);
        }
    }

    public GameObject SpawnProjectile(Vector3 spawnPosition, Vector2 direction, ProjectileData data, GameObject prefab = null, Enemy target = null, GameObject[] inFlightEffects = null, GameObject[] onContactEffects = null)
    {
        GameObject projectileObj;

        if (usePooling && prefab == null && projectilePool.Count > 0)
        {
            projectileObj = projectilePool.Dequeue();
            projectileObj.transform.position = spawnPosition;
            projectileObj.SetActive(true);
        }
        else
        {
            GameObject prefabToUse = prefab != null ? prefab : defaultProjectilePrefab;
            if (prefabToUse == null)
            {
                Debug.LogError("No projectile prefab available!");
                return null;
            }

            projectileObj = Instantiate(prefabToUse, spawnPosition, Quaternion.identity, projectileContainer);
        }

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, data, target, inFlightEffects, onContactEffects);

            if (usePooling && prefab == null)
                projectile.OnProjectileDestroyed += ReturnToPool;
        }
        else
        {
            Debug.LogWarning("Projectile component not found on instantiated prefab!");
            Destroy(projectileObj);
            return null;
        }

        return projectileObj;
    }

    void ReturnToPool(Projectile projectile)
    {
        if (projectile != null && projectile.gameObject != null)
        {
            projectile.OnProjectileDestroyed -= ReturnToPool;
            projectile.gameObject.SetActive(false);
            projectilePool.Enqueue(projectile.gameObject);
        }
    }

    public static ProjectileData CreateProjectileData(float damage, float speed, DebuffType debuffType = DebuffType.Bleed,
                                                     float debuffDuration = 0f, float debuffIntensity = 0f)
    {
        ProjectileData data = new ProjectileData
        {
            damage = damage,
            speed = speed,
            debuffType = debuffType,
            debuffDuration = debuffDuration,
            debuffIntensity = debuffIntensity
        };
        return data;
    }
}