using UnityEngine;

public class FSMPerseguirState : FSMStateEnemy
{
    [SerializeField] string _stateAtaque;
    [SerializeField] string _statePatrulla;
    bool _enemigoArco;
    public override void OnEnterState()
    {
        _enemigoArco = TryGetComponent(out EnemigoArco enemigoArco);
        if (_enemigoArco)
        {
            enemies.Animator.SetBool(enemigoArco._estaApuntando, false);
        }
    }
    public override void OnExitState()
    {

    }
    public override void OnUpdateState()
    {
        enemies._objetivoFinal = gameObject.BusquedadeObjeto(enemies._radioBusqueda, enemies._capaObjetivos, enemies._capaRaycast);
        
        if (enemies._objetivoFinal != null)
        {
            enemies.StateMaschineController.RequestState(_stateAtaque);
        }
        else if (enemies.PosicionObjetivo != null)
        {
            enemies.AgenteEnemigo.SetDestination(enemies.PosicionObjetivo.position);
            enemies.RotacionEnemigos(enemies.PosicionObjetivo.position);
        }
        else
        {
            enemies.StateMaschineController.RequestState(_statePatrulla);
        }
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

}
