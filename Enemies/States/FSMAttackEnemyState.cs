using System;
using UnityEngine;

public class FSMAttackEnemyState : FSMStateEnemy
{
    [SerializeField] string _statePatrulla;
    [SerializeField] string _statePerseguir;
    Transform _objetivoActual;

    public override void OnEnterState()
    {
        enemies._inCombat = true;
        enemies.AgenteEnemigo.isStopped = false;
    }
    public override void OnExitState()
    {
        if (enemies.SlotsdelObjetivoActual != null)
        {
            enemies.SlotsdelObjetivoActual.LiberarSlots(enemies.gameObject);
        }

        enemies._inCombat = false;

        if (!enemies.TryGetComponent(out EnemigoArco enemigoArco))
        {
            enemies._anim.SetInteger(enemies._triggerName, 2);
        }
    }
    public override void OnUpdateState()
    {
        if (enemies._objetivoFinal == null) enemies.StateMaschineController.RequestState(_statePerseguir);

        _objetivoActual = enemies._objetivoFinal;

        enemies.BusquedaContrarios();
        // 2. Si nos quedamos sin objetivo, liberamos el slot y cambiamos a perseguir/patrullar
        if (enemies._objetivoFinal == null)
        {
            enemies.GestionarCacheSlots();

            enemies.StateMaschineController.RequestState(_statePerseguir);
            return;
        }

        // 3. CAMBIO DE OBJETIVO: Si el nuevo objetivo es diferente al que teníamos
        if (_objetivoActual != null && enemies._objetivoFinal != _objetivoActual)
        {
            enemies.GestionarCacheSlots();
        }

        if (enemies.TryGetComponent(out EnemigoEscudo enemigoEscudo))
        {
            enemies.GestionarCacheSlots();
            // Combate real contra el jugador o zombies
            enemigoEscudo.EnemigoAtacarObjetivo(false);
        }
        else if (enemies.TryGetComponent(out EnemigoTeniente enemigoTeniente))
        {
            enemies.GestionarCacheSlots();
            enemigoTeniente.EnemigoAtacarObjetivo(false);
        }
        else if (enemies.TryGetComponent(out EnemigoArco enemigoArco))
        {
            enemigoArco.gameObject.Apuntado(enemies.ObjetivoFinal, enemies.Animator, enemies.AgenteEnemigo, enemigoArco.AngulosDesface,
                     enemies.DistanciaAtaque, enemies.TriggerName, enemies._estaApuntando);
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
