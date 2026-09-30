
using UnityEngine;
using UnityEngine.AI;

public class InvocacionEscudo : InvocacionBase
{
    public Sprite _formationIcon;
    #region Unity Methods
    //Funcion para aniadir componentes mediante un botton

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

    #endregion




    #region Funciones Funcinales


    #endregion

}



















































