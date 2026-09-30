using UnityEngine;

public class FSMStateEnemyPatrol : FSMStateEnemy
{
    [SerializeField] string _statePerseguir;
    public override void OnEnterState()
    {
        
    }
    public override void OnUpdateState()
    {
        enemies.Patrullar();
    }
    public override void OnFixedUpdateState()
    {
        
    }
    public override void OnLateUpdateState()
    {
        
    }
    public override void OnStateDetectionEnter(GameObject gameObject)
    {
        enemies.StateMaschineController.RequestState(_statePerseguir);
    }
    public override void OnStateDetectionExit()
    {
    }
    public override void OnExitState()
    {
        enemies.AgenteEnemigo.ResetPath();
    }
}


