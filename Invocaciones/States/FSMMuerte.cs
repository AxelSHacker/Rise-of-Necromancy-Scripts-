using UnityEngine;

public class FSMMuerte : FSMStateInvocacion
{
    FormaciondeEjercito _formaciondeEjercito;
    public override void OnEnterState()
    {
        AudioManager.Instance.ReproducirSonido(invocacionBase._muerte, invocacionBase._audioSource);
        _formaciondeEjercito = invocacionBase.NigromantePosicion.TryGetComponent(out _formaciondeEjercito) ? _formaciondeEjercito : null;
        invocacionBase.MuerteInvocacion();
        _formaciondeEjercito.SetEstadoCarga(false);
        _formaciondeEjercito.SetEstadoCombate(false);
        if(invocacionBase.SlotsdelObjetivoActual != null) invocacionBase.SlotsdelObjetivoActual.LiberarSlots(invocacionBase.gameObject);
    }

    public override void OnExitState()
    {
        
    }

    public override void OnFixedUpdateState()
    {
        
    }

    public override void OnLateUpdateState()
    {
        
    }

    public override void OnUpdateState()
    {
        
    }
}

