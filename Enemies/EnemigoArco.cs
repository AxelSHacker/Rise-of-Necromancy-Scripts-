using UnityEngine;

public class EnemigoArco : EnemigoBase
{
    [SerializeField] float _angulosDesface = 62;
    [SerializeField] string _bulletType = "Arrow";
    [SerializeField] Transform _shootingPoint;
    [Header("Getters")]
    public float AngulosDesface => _angulosDesface;
    #region Funciones Unity
    void Start()
    {
        _indicePatrullaActual = UnityEngine.Random.Range(0, _puntosDePatrullaje.Length);
        _vidaActual = _vidaMaxima;
        _agenteEnemigo.updateRotation = false;
        this.MuerteEnemigos();
    }
    void Update()
    {
        if (!IsActive || _muerto) return;

        gameObject.AnimatorController(_anim, _agenteEnemigo);
    }
    #endregion


    #region Funciones
    public void Shoot()
    {
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.Euler(Vector3.zero);

        //Origen de la bala 
        position = _shootingPoint.position;
        rotation = Quaternion.LookRotation(_shootingPoint.forward);

        //Lanzamos una bala desde el pool manager
        PoolManager.Instance.Pull(_bulletType, position, rotation);

        if (_agenteEnemigo.isActiveAndEnabled) _agenteEnemigo.isStopped = false;
    }

    #endregion
}






