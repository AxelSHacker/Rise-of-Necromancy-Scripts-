using UnityEngine;
using UnityEngine.AI;
public class InvocacionArco : InvocacionBase
{
    [SerializeField] Transform _shootingPoint;
    public Sprite _formationIcon;
    [SerializeField] string _bulletType = "Arrow";

    #region Unity Methods
    void Start()
    {
        _vidaActual = _vidaMaxima;
        this.BaseInvocaciones();

        if (_agente == null) _agente = GetComponent<NavMeshAgent>();
        if (_capsuleCollider == null) _capsuleCollider = GetComponent<CapsuleCollider>();

        if (_posicionNigromante != null)
        {
            _nigromanteController = _posicionNigromante.GetComponent<CharacterController>();
            _ultimaPosicionPlayer = _posicionNigromante.position;
        }

    }
    void LateUpdate()
    {
        if (!IsActive || _muerto || _posicionNigromante == null) return;

        gameObject.CalcularVelocidadObjetivo(_posicionNigromante, ref _ultimaPosicionPlayer, ref _velocidadPlayer);
        gameObject.AnimatorController(_anim, _agente);

    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radioBusqueda);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _radioBusqueda);
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

        _agente.isStopped = false;
    }

    #endregion




    #region Funciones Funcinales

    #endregion
}
