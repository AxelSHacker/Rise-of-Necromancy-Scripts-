using UnityEngine;

public abstract class FSMStateEnemy : FSMState
{
   [SerializeField] protected EnemigoBase enemies;
   public abstract void OnStateDetectionEnter(GameObject gameObject);
   public abstract void OnStateDetectionExit();
}