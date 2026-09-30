using UnityEngine;

public class FSMMuerteEnemyState : FSMStateEnemy
{
    public override void OnEnterState()
    {
        enemies.AgenteEnemigo.isStopped = true;
        enemies.AgenteEnemigo.enabled = false;
        if (enemies.TryGetComponent(out EnemigoTeniente _enemigoTeniente))
        {
            _enemigoTeniente.FormaciondeEjercito.enabled = false;
        }
        enemies.QuitarFormacionEjercito();
        enemies.MuerteEnemigos();
        if (enemies.SlotsdelObjetivoActual != null) { enemies.SlotsdelObjetivoActual.LiberarSlots(enemies.gameObject); }

    }

    public override void OnExitState()
    {
        enemies.AgenteEnemigo.isStopped = false;
    }

    public override void OnFixedUpdateState()
    {
    }

    public override void OnLateUpdateState()
    {
    }

    public override void OnStateDetectionEnter(GameObject gameObject)
    {
    }

    public override void OnStateDetectionExit()
    {
    }

    public override void OnUpdateState()
    {
    }
}
