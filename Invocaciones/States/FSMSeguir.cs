using UnityEngine;

public class FSMSeguir : FSMStateInvocacion
{
    FormaciondeEjercito _formaciondeEjercito;
    [SerializeField] string _stadoAtaque;
    InvocacionArco _invocacionArco;
    [SerializeField] string _stadoCargar;


    public override void OnEnterState()
    {
        _formaciondeEjercito = invocacionBase.NigromantePosicion.TryGetComponent(out _formaciondeEjercito) ? _formaciondeEjercito : null;
        _invocacionArco = invocacionBase.TryGetComponent(out _invocacionArco) ? _invocacionArco : null;

        _formaciondeEjercito.SeguirNigromante();
        invocacionBase._agente.isStopped = false;
    }
    public override void OnUpdateState()
    {
        if (!invocacionBase._estaOrientado)
        {
            invocacionBase.LogicaResureccion(invocacionBase._posicionEnFormacion);
        }

        invocacionBase.MovimientoInvocaciones(invocacionBase._posicionEnFormacion, invocacionBase._velocidaDeCrucero);

        if (invocacionBase._enemigo != null)
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
}
