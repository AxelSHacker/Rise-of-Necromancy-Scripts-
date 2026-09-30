
using System.Data;
using UnityEngine;
public class EnemigoEscudo : EnemigoBase
{
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
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radioBusqueda);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _radioBusqueda);
    }

    #endregion






}


