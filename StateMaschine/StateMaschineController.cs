using System;
using UnityEngine;

public class StateMaschineController : MonoBehaviour
{
    [SerializeField] FSMState[] states;
    [SerializeField] protected FSMState _currentState;
    protected FSMState _requestedState;
    public FSMState CurrentState => _currentState;

    void Start()
    {
        if (states.Length == 0 || states == null)
        {
            Debug.LogError("No hay estados[Start] asignados al StateMaschineController.");
            return;
        }
        SetState(states[0]);
    }
    void Update()
    {
        if (_currentState == null)
        {
            Debug.LogError("No se ha solicitado[Update] un estado para el StateMaschineController.");
            return;
        }
        _currentState.OnUpdateState();
    }
    void FixedUpdate()
    {
        if (_currentState == null)
        {
            Debug.LogError("No se ha solicitado[FixedUpdate] un estado para el StateMaschineController.");
            return;
        }
        _currentState.OnFixedUpdateState();
    }
    void LateUpdate()
    {
        if (_currentState == null)
        {
            Debug.LogError("No se ha solicitado[LateUpdate] un estado para el StateMaschineController.");
            return;
        }
        _currentState.OnLateUpdateState();

        if (_requestedState != null && _requestedState != _currentState)
        {
            SetState(_requestedState);
            _requestedState = null;
        }
    }
    private void SetState(FSMState nextFSMState)
    {
        if (_currentState != null)
        {
            _currentState.OnExitState();
        }
        nextFSMState.OnEnterState();
        _currentState = nextFSMState;
    }
    public void RequestState(string stateName)
    {
        if (stateName == null)
        {
            Debug.LogError("Ya hay un State solicitado para el StateMaschineController.");
            return;
        }
        _requestedState = GetStateWithName(stateName);
    }
    FSMState GetStateWithName(string stateName)
    {
        for(int i = 0; i < states.Length; i++)
        {
            if (states[i].stateName == stateName)
            {
                return states[i];
            }
        }
        Debug.LogError("No se ha encontrado un estado con el nombre: " + stateName);
        return null;
    }
}
