using System;
using UnityEngine;

public class PoolEntity : CustomMonoBehaviour
{
    public static Action<PoolEntity> OnReturnToPool;
    
    [SerializeField]
    private string _poolId;
    [SerializeField]
    private Renderer[] _renderers;
    [SerializeField]
    private bool _isActive;

    public bool IsActive => _isActive;

    public string PoolId
    {
        get => _poolId;
        set => _poolId = value;
    }
    public override void EditorInit()
    {
        _renderers = GetComponentsInChildren<Renderer>();
    }

    public virtual void Initialize()
    {
        EnableRenderers(true);
        _isActive = true;
    }

    public virtual void Deactivate()
    {
        EnableRenderers(false);
        _isActive = false;
    }

    public void ReturnToPool()
    {
        Deactivate();
        OnReturnToPool?.Invoke(this);
    }

    private void EnableRenderers(bool enabled)
    {
        foreach (Renderer rend in _renderers)
        {
            rend.enabled = enabled;
        }
    }
}
