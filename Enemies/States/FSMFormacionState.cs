using UnityEngine;

public class FSMFormacionState : FSMStateEnemy
{
    [SerializeField] string _stateAtaque;
    [SerializeField] string _statePatrulla;
    public override void OnEnterState()
    {
        // Aquí cambiamos el estado del enemigo para que sepa que ahora obedece al Teniente
        // Por ejemplo, desactivamos su patrulla libre para que se una a la formación:
        enemies._isPatrolling = false;
        if (enemies._agenteEnemigo != null && enemies._agenteEnemigo.enabled)
        {
            enemies._agenteEnemigo.velocity = Vector3.zero; // Quitamos toda la fuerza física acumulada
            enemies._agenteEnemigo.ResetPath();         // Borramos la ruta de patrulla vieja
            enemies._agenteEnemigo.speed = 0f;
        }
        enemies._estaOrientado = false;
    }

    public override void OnExitState()
    {
        enemies.QuitarFormacionEjercito();
    }
    public override void OnUpdateState()
    {
        if (enemies._posicionCapitan == null) enemies.StateMaschineController.RequestState(_statePatrulla);

    }

    public override void OnFixedUpdateState()
    {
    }

    public override void OnLateUpdateState()
    {
        gameObject.CalcularVelocidadObjetivo(enemies._posicionCapitan, ref enemies._ultimaPosicionCapitan, ref enemies._velocidadCapitan);
        enemies.SeguirTeniente();
    }

    public override void OnStateDetectionEnter(GameObject gameObject)
    {
        enemies.StateMaschineController.RequestState(_stateAtaque);
    }

    public override void OnStateDetectionExit()
    {

    }

}
