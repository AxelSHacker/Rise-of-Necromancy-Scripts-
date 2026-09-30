using UnityEngine;

public abstract class FSMState : MonoBehaviour
{
   public string stateName;

   public abstract void OnEnterState();
   public abstract void OnUpdateState();
   public abstract void OnFixedUpdateState();
   public abstract void OnLateUpdateState();
   public abstract void OnExitState();
}
