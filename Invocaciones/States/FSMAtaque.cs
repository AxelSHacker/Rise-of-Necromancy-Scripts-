using UnityEngine;

public class FSMAtaque : FSMStateInvocacion
{
    FormaciondeEjercito _formaciondeEjercito;
    NigromanteController _nigromanteController;
    InvocacionArco _invocacionArco;
    [SerializeField] string _stateCargar;
    Transform _enemigoDetectado;
    public override void OnEnterState()
    {
        _formaciondeEjercito = invocacionBase.NigromantePosicion.TryGetComponent(out _formaciondeEjercito) ? _formaciondeEjercito : null;
        _nigromanteController = invocacionBase.NigromantePosicion.TryGetComponent(out _nigromanteController) ? _nigromanteController : null;
        _invocacionArco = invocacionBase.TryGetComponent(out _invocacionArco) ? _invocacionArco : null;
        // if (_invocacionArco != null)
        // {
        //_formaciondeEjercito.SetEstadoCombate(true);
        // }

        // invocacionBase._agente.isStopped = false;
    }

    public override void OnUpdateState()
    {

        if (invocacionBase.RangoAtaque())
        {
            invocacionBase.InvocacionAtacarObjetivo(_formaciondeEjercito.AtaqueEnFormacion);
        }
        else if (gameObject.TieneLineaDeVision(invocacionBase._enemigo, invocacionBase._radioBusqueda, invocacionBase._capaRaycast))
        {
            invocacionBase.ActualizarSeguimiento();
        }
        else
        {
            invocacionBase.StateMaschineController.RequestState(_stateCargar);
        }
    }
    public override void OnExitState()
    {
        // _formaciondeEjercito.SetEstadoCombate(false);
        invocacionBase._anim.SetInteger(invocacionBase._triggerName, 2);
    }

    public override void OnFixedUpdateState()
    {

    }
    public override void OnLateUpdateState()
    {

    }

}
