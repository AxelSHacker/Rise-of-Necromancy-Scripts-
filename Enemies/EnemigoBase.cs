using UnityEngine;
using System;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;
using Unity.Cinemachine.Samples;
using UnityEngine.Events;


public abstract class EnemigoBase : PoolEntity, IDamageabe<float>
{
    #region Variables
    public Action OnInitialize;
    public Action<Vector3> OnImpact;

    [Header("Referencias")]
    [SerializeField] internal NavMeshAgent _agenteEnemigo;
    [SerializeField] internal Transform _posicionCapitan;
    [SerializeField] internal Animator _anim;
    [SerializeField] internal CapsuleCollider _capsuleCollider;
    public Transform[] _puntosDePatrullaje;
    [SerializeField] internal string _tipoInvocacion;
    [SerializeField] Image _healBar;
    internal Vector3 _posicionEnFormacion;
    [SerializeField] internal Transform _objetivoFinal;
    internal Transform _posicionObjetivo;
    internal Vector3 _ultimaPosicionCapitan;
    [SerializeField] internal ViewSensor _viewSensor;
    [SerializeField] internal StateMaschineController _stateMaschineController;
    [SerializeField] FormaciondeEjercito _formacionEjercito;
    [SerializeField] Escudo _escudo;

    [Header("Velocidades")]
    [SerializeField] internal float _velocidadDeAtaque;
    [SerializeField] internal float _velocidadDePatrulla;
    internal float _velocidadCapitan;
    [SerializeField] internal float _multiplicadorRotacion = 1.5f;


    [Header("Radios de Accion")]
    [SerializeField] internal float _radioBusqueda;
    [SerializeField] internal float _distanciaAtaque;

    [Header("Layers")]
    [SerializeField] internal LayerMask _capaObjetivos;
    [SerializeField] internal LayerMask _capaRaycast;
    [SerializeField] internal int _capaZombie;
    [SerializeField] internal int _capaInvocable;
    [SerializeField] internal int _capaEnemigo;

    [Header("Patrullaje")]
    [SerializeField] string _patrullaState;
    internal int _indicePatrullaActual = 0;
    internal bool _estaOrientado;
    internal bool _isPatrolling = true;

    [Header("Persecusion")]
    [SerializeField] string _statePerseguir;
    Vector3 _direccionImpacto;

    [Header("Ataque")]
    [SerializeField] internal string _triggerName;
    public string _estaApuntando;
    [SerializeField] internal float _distanciaAvanceenFormacion = 12f;
    Transform _objetivoGuardado;
    internal bool _inCombat = false;
    [SerializeField] ObjetivoConSlotsAnillos _slotsdelObjetivoActual;
    public ObjetivoConSlotsAnillos SlotsdelObjetivoActual => _slotsdelObjetivoActual;


    [Header("Vida")]
    [SerializeField] internal float _vidaMaxima;
    [SerializeField] internal float _vidaActual;
    [SerializeField] internal bool _muerto = false;
    public Action OnDeath;
    public Action OnResucitado;
    [SerializeField] string _stateMuerte;

    [Header("Recompensa Experiencia")]
    [SerializeField] internal int _experienceReward = 15;
    public static event Action<int> OnEnemyKilled;

    [Header("Getters")]
    public StateMaschineController StateMaschineController => _stateMaschineController;
    public Transform ObjetivoFinal => _objetivoFinal;
    public Animator Animator => _anim;
    public NavMeshAgent AgenteEnemigo => _agenteEnemigo;
    public float DistanciaAtaque => _distanciaAtaque;
    public string TriggerName => _triggerName;
    public bool _enCombate => _objetivoFinal != null;
    public Vector3 DireccionFlecha => _direccionImpacto;
    public FormaciondeEjercito FormaciondeEjercito => _formacionEjercito;
    public Transform PosicionObjetivo => _posicionObjetivo;
    public float MultiplicadorRotacion => _multiplicadorRotacion;

    [Header("Eventos de Unity")]
    public UnityEvent OnInizialize;
    public UnityEvent OnDeactivate;
    public UnityEvent OnDeadUE;
    #endregion




    #region  Funciones
    public override void EditorInit()
    {
        base.EditorInit();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _agenteEnemigo = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
    }
    void OnEnable()
    {
        OnImpact += Impactado;
        _viewSensor.OnDetectionEnter += OnViewDetectionEnter;
        _viewSensor.OnDetectionExit += OnViewDetectionExit;
    }
    void OnDisable()
    {
        OnImpact -= Impactado;
        _viewSensor.OnDetectionEnter -= OnViewDetectionEnter;
        _viewSensor.OnDetectionExit -= OnViewDetectionExit;
    }
    internal void GestionarCacheSlots()
    {
        // 1. Si el objetivo no ha cambiado, salimos
        if (_objetivoGuardado == _objetivoFinal) return;

        // 2. LIBERAR SLOT ANTERIOR:
        // Accedemos a .gameObject.activeInHierarchy si _objetivoGuardado es un Transform
        if (_objetivoGuardado != null && _objetivoGuardado.gameObject.activeInHierarchy)
        {
            if (_objetivoGuardado.TryGetComponent(out ObjetivoConSlotsAnillos gestorViejo))
            {
                gestorViejo.LiberarSlots(gameObject);
            }
        }

        // 3. Actualizamos la caché con el nuevo objetivo
        _objetivoGuardado = _objetivoFinal;

        // 4. ASIGNAR NUEVO GESTOR:
        // Separamos la comprobación de null/active para evitar el error de variable no asignada en el out
        _slotsdelObjetivoActual = null;

        if (_objetivoFinal != null && _objetivoFinal.gameObject.activeInHierarchy)
        {
            if (_objetivoFinal.TryGetComponent(out ObjetivoConSlotsAnillos gestorNuevo))
            {
                _slotsdelObjetivoActual = gestorNuevo;
            }
        }
    }
    private void OnViewDetectionEnter(GameObject gameObject)
    {
        // Salvavidas: si por algún motivo no hay ningún estado activo, no hacemos nada para evitar que explote
        if (_stateMaschineController.CurrentState == null) return;

        // Convertimos el estado genérico (_currentState) en nuestro tipo específico de estado de enemigo (FSMStateEnemy)
        // Esto se llama "Casting" y nos permite acceder a sus funciones específicas
        FSMStateEnemy stateEnemy = _stateMaschineController.CurrentState as FSMStateEnemy;
        // Guardamos al Player en nuestra variable de objetivo
        _posicionObjetivo = gameObject.transform;
        // Le avisamos al estado actual de que hemos visto a alguien, pasándole el GameObject detectado
        stateEnemy.OnStateDetectionEnter(gameObject);
    }
    private void OnViewDetectionExit()
    {
        FSMStateEnemy stateEnemy = _stateMaschineController.CurrentState as FSMStateEnemy;
        // Le avisamos al estado actual de que hemos perdido el rastro del objetivo
        if (stateEnemy == null) return;
        ContrarioPerdido();
        stateEnemy.OnStateDetectionExit();
        // Vaciamos el objetivo porque ya no vemos a nadie
    }
    private void Impactado(Vector3 flecha)
    {
        if (_muerto || _inCombat) return;
        _inCombat = true;
        _direccionImpacto = flecha - transform.position;
        _stateMaschineController.RequestState(_statePerseguir);
    }
    private void ContrarioPerdido()
    {
        if (_muerto || _enCombate) return;
        _posicionObjetivo = null;
        _objetivoFinal = null;
        _isPatrolling = true;
    }
    public void SacarFormacionEjercito(FormaciondeEjercito enemigoFormaciondeEjercito)
    {
        _formacionEjercito = enemigoFormaciondeEjercito;
    }
    public void QuitarFormacionEjercito()
    {
        _formacionEjercito = null;
    }
    public int ObtenerIndiceSlot()
    {
        // Rango 0: Espadas / Dagas / Puños (Cuerpo a cuerpo corto <= 1.8m)
        if (PoolId == "EnemigoEscudo") return 0;

        // Rango 1: Lanzadores de lanza / Alabardas / Mandobles (1.8m a 3.5m)
        if (PoolId == "EnemigoAlabarda") return 1;

        // Rango 2: Arqueros / Magos (Distancia larga > 3.5m)
        return 2;
    }
    #endregion



    #region IDamageable
    public float Maxhealt => _vidaMaxima;
    public float CurrentHealt => _vidaActual;
    public bool IsDead => _muerto;

    public void TakeDamage(float damage, Vector3 impactPoint = default)
    {
        _vidaActual -= damage;
        _vidaActual = Mathf.Clamp(_vidaActual, 0, _vidaMaxima);
        _healBar.fillAmount = _vidaActual / _vidaMaxima;

        //: CONDICIÓN DE MUERTE: Evaluamos si la salud ha llegado a cero.
        if (_vidaActual <= 0 && !_muerto) //: Añadimos control para que solo entre una vez
        {
            //: Ejecutamos tu función que cambia las capas a invocable y activa la animación.
            _muerto = true;
            //: SOLICITUD DE TRANSICIÓN: Pedimos el estado de muerte de forma limpia.
            _stateMaschineController.RequestState(_stateMuerte);
            OnDeadUE?.Invoke();
            OnEnemyKilled?.Invoke(_experienceReward);
            //: Liberamos los slots ocupados.
            if (_slotsdelObjetivoActual != null)
            {
                _slotsdelObjetivoActual.LiberarSlots(gameObject);
                _slotsdelObjetivoActual = null;
            }
        }
    }
    #endregion




    #region Coroutine

    #endregion





    #region PoolEntity
    public override void Initialize()
    {
        base.Initialize();

        OnInizialize?.Invoke();
        _capsuleCollider.enabled = true;
        gameObject.layer = _capaEnemigo;
        if (_escudo != null) _escudo.ResetearEstado();
        _agenteEnemigo.enabled = true;
        _agenteEnemigo.isStopped = false;
        _anim.SetBool("Muerte", false);
        _healBar.enabled = true;
        _viewSensor.enabled = true;
        _muerto = false;
        _stateMaschineController.enabled = true;
        _capsuleCollider.isTrigger = true;
        _stateMaschineController.RequestState(_patrullaState);
        OnInitialize?.Invoke();
    }

    public override void Deactivate()
    {
        base.Deactivate();
        _capsuleCollider.enabled = false;
        _agenteEnemigo.enabled = false;
        _healBar.enabled = false;
        _viewSensor.enabled = false;
        _stateMaschineController.enabled = false;

        OnDeath = null;
        OnResucitado = null;
    }
    #endregion

}
