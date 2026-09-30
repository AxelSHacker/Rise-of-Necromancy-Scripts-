using UnityEngine;

public class FSMCargar : FSMStateInvocacion
{
    [SerializeField] string _stadoAtaque;
    [SerializeField] string _stadoSeguir;
    FormaciondeEjercito _formaciondeEjercito;
    NigromanteController _nigromanteController;

    public override void OnEnterState()
    {
        _formaciondeEjercito = invocacionBase.NigromantePosicion.TryGetComponent(out _formaciondeEjercito) ? _formaciondeEjercito : null;
        _nigromanteController = invocacionBase.NigromantePosicion.TryGetComponent(out _nigromanteController) ? _nigromanteController : null;

        _formaciondeEjercito.SetEstadoCarga(true);
        invocacionBase._agente.isStopped = false;

    }
    public override void OnUpdateState()
    {
        if (_nigromanteController.EjercitoEnemigo == null && _nigromanteController.TransformEnemigo == null)
        {
            _formaciondeEjercito.CambiarState(_stadoSeguir);
            return;
        }

        //if (invocacionBase._enemigo != null)
        //{
        // // Evaluamos si el enemigo ya está a rango de golpe directo
        // float distSqr = (transform.position - invocacionBase._enemigo.position).sqrMagnitude;
        // float rangoSqr = (invocacionBase._distanciaAtaque + 0.3f) * (invocacionBase._distanciaAtaque + 0.3f);

        if (invocacionBase.RangoAtaque())
        {
            // La línea de choque ha hecho contacto: Congelamos el avance del pivote global de la horda
            //_formaciondeEjercito.CambiarState(_stadoAtaque);
            invocacionBase.StateMaschineController.RequestState(_stadoAtaque);
        }
        else
        {
            invocacionBase.ActualizarSeguimiento();
        }
        //}
    }

    public override void OnExitState()
    {
        _formaciondeEjercito.SetEstadoCarga(false);
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
    public override void OnLateUpdateState()
    {

    }
}