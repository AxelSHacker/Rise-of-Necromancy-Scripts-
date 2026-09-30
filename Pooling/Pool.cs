using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Pool
{
    public string id;
    public PoolEntity prefab;
    public int prewarn;
    public Queue<PoolEntity> pool = new Queue<PoolEntity>();
}
