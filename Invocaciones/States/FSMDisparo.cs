using UnityEngine;

public class FSMDisparo : FSMStateInvocacion
{
    FormaciondeEjercito _formaciondeEjercito;
    NigromanteController _nigromanteController;
    [SerializeField] string _stateCargar;

    public override void OnEnterState()
    {
        _formaciondeEjercito = invocacionBase.NigromantePosicion.TryGetComponent(out _formaciondeEjercito) ? _formaciondeEjercito : null;
    }

    public override void OnUpdateState()
    {
        //invocacionBase._enemigo = gameObject.BusquedadeObjeto(invocacionBase._radioBusqueda, invocacionBase._capaObjetivos, invocacionBase._capaRaycast);

        // if (invocacionBase._enemigo != null)
        // {
        //     invocacionBase.gameObject.Apuntado(invocacionBase._enemigo, invocacionBase._anim, invocacionBase._agente, invocacionBase._anguloDesface,
        //                         invocacionBase._distanciaAtaque, invocacionBase._triggerName, invocacionBase._estaApuntando);
        // }

        if (gameObject.TieneLineaDeVision(invocacionBase._enemigo, invocacionBase._radioBusqueda, invocacionBase._capaRaycast))
        {
            // Evaluamos si el enemigo ya está a rango de golpe directo
            // float distSqr = (transform.position - invocacionBase._enemigo.position).sqrMagnitude;
            // float rangoSqr = (invocacionBase._distanciaAtaque + 0.3f) * (invocacionBase._distanciaAtaque + 0.3f);
            if (invocacionBase.RangoAtaque())
            {
                invocacionBase.gameObject.Apuntado(invocacionBase._enemigo, invocacionBase._anim, invocacionBase._agente, invocacionBase._anguloDesface,
                                invocacionBase._distanciaAtaque, invocacionBase._triggerName, invocacionBase._estaApuntando);
            }
        }
        else
        {
            invocacionBase.StateMaschineController.RequestState(_stateCargar);
        }
    }
    
    public override void OnExitState()
    {
        invocacionBase._anim.SetBool(invocacionBase._triggerName, false);
    }

    public override void OnFixedUpdateState()
    {

    }
    public override void OnLateUpdateState()
    {

    }
}
