using UnityEngine;

public class FSMApuntar : FSMStateInvocacion
{
    [SerializeField] string _stadoAtaque;
    [SerializeField] string _stadoSeguir;
    FormaciondeEjercito _formaciondeEjercito;
    NigromanteController _nigromanteController;

    public override void OnEnterState()
    {
        _formaciondeEjercito = invocacionBase.NigromantePosicion.TryGetComponent(out _formaciondeEjercito) ? _formaciondeEjercito : null;
        _nigromanteController = invocacionBase.NigromantePosicion.TryGetComponent(out _nigromanteController) ? _nigromanteController : null;

        // _formaciondeEjercito.SetEstadoCarga(true);
        // invocacionBase._agente.isStopped = false;
    }

    public override void OnExitState()
    {
        invocacionBase._anim.SetBool(invocacionBase._triggerName, false);
        invocacionBase._agente.ResetPath();
    }

    public override void OnFixedUpdateState()
    {
        if (_nigromanteController.TransformEnemigo != null)
        {
            invocacionBase.MovimientoInvocaciones(_nigromanteController.TransformEnemigo.position, invocacionBase._velocidaDeCrucero);
        }
        else
        {
            invocacionBase.MovimientoInvocaciones(_formaciondeEjercito.DestinyPosition, invocacionBase._velocidaDeCrucero);
        }
    }

    public override void OnUpdateState()
    {
        // if (_nigromanteController.EjercitoEnemigo == null && _nigromanteController.TransformEnemigo == null)
        // {
        //     invocacionBase.StateMaschineController.RequestState(_stadoSeguir);
        //     return;
        // }

        //invocacionBase._enemigo = gameObject.BusquedadeObjeto(invocacionBase._radioBusqueda, invocacionBase._capaObjetivos, invocacionBase._capaRaycast);

        if (gameObject.TieneLineaDeVision(invocacionBase._enemigo, invocacionBase._radioBusqueda, invocacionBase._capaRaycast))
        {
            // Evaluamos si el enemigo ya está a rango de golpe directo
            // float distSqr = (transform.position - invocacionBase._enemigo.position).sqrMagnitude;
            // float rangoSqr = (invocacionBase._distanciaAtaque + 0.3f) * (invocacionBase._distanciaAtaque + 0.3f);
            if (invocacionBase.RangoAtaque())
            {
                // La línea de choque ha hecho contacto: Congelamos el avance del pivote global de la horda
                invocacionBase.StateMaschineController.RequestState(_stadoAtaque);
            }
            else
            {
                invocacionBase.ActualizarSeguimiento();
            }
        }
        else
        {
            invocacionBase.StateMaschineController.RequestState(_stadoSeguir);
        }
    }

    public override void OnLateUpdateState()
    {

    }
}
