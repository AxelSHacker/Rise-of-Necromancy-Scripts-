using UnityEngine;


public class FSMShootEnemyState : FSMStateEnemy
{
    [SerializeField] string _statePatrulla;
    [SerializeField] EnemigoArco enemysArco;
    public override void OnEnterState()
    {
        if (enemies._objetivoFinal == null) enemies.StateMaschineController.RequestState(_statePatrulla);
        
    }
    public override void OnUpdateState()
    {

        enemies.gameObject.Apuntado(enemies.ObjetivoFinal, enemies.Animator, enemies.AgenteEnemigo, enemysArco.AngulosDesface,
                             enemies.DistanciaAtaque, enemies.TriggerName, enemies._estaApuntando);
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
    public override void OnStateDetectionEnter(GameObject gameObject)
    {

    }
    public override void OnStateDetectionExit()
    {
        
    }
}
