using System.Linq;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    private static PoolManager _instance;
    public static PoolManager Instance => _instance;
    
    [SerializeField]
    private Pool[] _pools;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        PoolEntity.OnReturnToPool += Push;
    }

    void OnDisable()
    {
        PoolEntity.OnReturnToPool -= Push;
    }

    void Start()
    {
       
    }

    private Pool GetPool(string poolId)
    {
        if (_pools == null) {
        Debug.LogError("El diccionario de Pools no ha sido inicializado.");
        return null;
    }
        Pool pool = _pools.SingleOrDefault(p => p.id == poolId);

        if (pool.prefab == null)
        {
            Debug.LogError("[PoolManager] - Hay más de un pool con el id " + poolId);
            pool = _pools.FirstOrDefault(p => p.id == poolId);
        }

        return pool;
    }

    private PoolEntity CreatePoolEntity(string poolId)
    {
        Pool pool = GetPool(poolId);
        PoolEntity entity = Instantiate(pool.prefab, transform);
        entity.PoolId = pool.id;
        return entity;
    }

    public void InitializePools()
    {
        foreach (Pool pool in _pools)
        {
            for (int i = 0; i < pool.prewarn; i++)
            {
                PoolEntity entity = CreatePoolEntity(pool.id);
                entity.Deactivate();
                pool.pool.Enqueue(entity);
            }
        }
    }

    public PoolEntity Pull(string poolId, Vector3 position, Quaternion rotation)
    {
        Pool pool = GetPool(poolId);
        if (!pool.pool.TryDequeue(out PoolEntity entity))
        {
            entity = CreatePoolEntity(poolId);
        }

        if (entity != null)
        {
            entity.transform.position = position;
            entity.transform.rotation = rotation;
            entity.Initialize();
        }

        return entity;
    }

    public void Push(PoolEntity entity)
    {
        Pool pool = GetPool(entity.PoolId);
        pool.pool.Enqueue(entity);
    }
}
