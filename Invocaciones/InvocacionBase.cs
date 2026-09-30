using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;
using Unity.VisualScripting;

public class InvocacionBase : PoolEntity, IDamageabe<float>
{
    public Action OnInitialize;
    #region Variables
    [Header("Referencias")]
    [SerializeField] internal Transform _posicionNigromante;
    [SerializeField] internal Transform _posicionModelo;
    [SerializeField] internal Transform _enemigo;
    [SerializeField] internal NavMeshAgent _agente;
    [SerializeField] internal Animator _anim;
    internal CharacterController _nigromanteController;
    [SerializeField] internal CapsuleCollider _capsuleCollider;
    [SerializeField] internal Image _healtBar;
    [SerializeField] StateMaschineController _stateMaschineController;
    [SerializeField] GameObject _resucitarEffect;
    [SerializeField] internal ParticleSystem _efectoMuerte;
    [SerializeField] internal AudioSource _audioSource;
    [SerializeField] internal AudioClip _impacto;
    [SerializeField] internal AudioClip _muerte;
    [SerializeField] AudioClip _levantarMuerto;
    [SerializeField] Escudo _escudo;

    [Header("Skills")]
    [SerializeField] internal GameObject _effectDefense;
    [SerializeField] internal GameObject _effectAttack;
    [Header("Defenses")]
    [SerializeField] internal bool _skillDefenseActive;
    [SerializeField] internal float _skillDefenseTime = 3f;
    [SerializeField] internal float _skillDefenseTimer = 3f;
    [Header("Attack")]
    [SerializeField] internal float _addVelocidadDeAtaque = 4f;
    [SerializeField] internal float _velocidadDeAtaqueOg = 4f;
    [SerializeField] internal bool _skillAttackActive;
    [SerializeField] internal float _skillAttackTime = 3f;
    [SerializeField] internal float _skillAttackTimer = 3f;

    [Header("Velocidades")]
    internal float _velocidadPlayer;
    [SerializeField] internal float _velocidadDeAtaque = 4f;

    [SerializeField] internal float _velocidaDeCrucero = 3.5f;
    public float multiplicadodorVelocidad;
    public float multiplicadorDeRecuperacion;
    public float multiplicadorRotacion;

    [Header("Posiciones")]
    internal Vector3 _posicionEnFormacion;
    internal Vector3 _ultimaPosicionPlayer;
    internal float _umbraDeMovimiento = 1f;
    internal bool _estaOrientado = false;

    [Header("Radios de Accion")]
    [SerializeField] internal float _distanciaAtaque = 0.5f;
    [SerializeField] internal float _radioBusqueda;
    [SerializeField] internal float _radioBusquedaLibre = 10f;
    [SerializeField] internal float _radioBusquedaFormacion = 20f;

    [Header("Layers")]
    [SerializeField] internal LayerMask _capaObjetivos;
    [SerializeField] internal LayerMask _capaRaycast;
    [SerializeField] internal int _capaZombie;
    [SerializeField] internal int _capaInvocable;
    [SerializeField] internal int _capaPlayer;
    [SerializeField] internal int _capaEnemigo;

    [Header("Vida")]
    [SerializeField] internal float _vidaMaxima;
    [SerializeField] internal bool _muerto;
    [SerializeField] internal float _vidaActual;
    [SerializeField] float _curaciónRecibida;
    [SerializeField] string _stateMuerte;

    [Header("Seguimiento")]
    float _tiempoSiguienteActualizacion = 0.3f;
    Vector3 _ultimaPosicionDestino;
    float _umbralMovimientoSqr = 0.2f;
    float umbralLineal = 0.2f;

    [Header("Ataque")]
    [SerializeField] internal string _triggerName;
    [SerializeField] internal float _anguloDesface = 43f;
    [SerializeField] float _comprovacionEnemiga = 0.33f;
    float _tiempoSiguienteComprovacion;
    public bool _enCombate => _enemigo != null;

    [SerializeField] ObjetivoConSlotsAnillos _slotsdelObjetivoActual;
    public ObjetivoConSlotsAnillos SlotsdelObjetivoActual => _slotsdelObjetivoActual;
    [SerializeField] Transform _objetivoGuardado;
    [SerializeField] private int _zombieType = 1;
    [SerializeField] internal string _estaApuntando;
    [SerializeField] string _stateSeguir;

    public int ZombieType => _zombieType;
    public Transform NigromantePosicion => _posicionNigromante;
    public StateMaschineController StateMaschineController => _stateMaschineController;
    public float VelocidadDelPlayer => _velocidadPlayer;
    public float RadioBusquedaFormacion => _radioBusquedaFormacion;
    public float RadioBusquedaLibre => _radioBusquedaLibre;
    public float DistanciaAtaque => _distanciaAtaque;
    public float VelocidaddeCrucero => _velocidaDeCrucero;
    public Action OnDeath;

    [Header("Eventos de Unity")]
    public UnityEvent OnInizialize;
    public UnityEvent OnDeactivate;
    public UnityEvent OnDeadUE;
    #endregion


    #region Funciones

    void Update()
    {
        if (!IsActive) return;

        if (Time.time >= _tiempoSiguienteComprovacion)
        {
            DeteccionEnemiga();
            _tiempoSiguienteComprovacion = Time.time + _comprovacionEnemiga;
        }
        UpdateSkills();
    }
    private void DeteccionEnemiga()
    {
        // Si no tenemos enemigo, buscamos uno nuevo en el radio de búsqueda
        if (_enemigo == null)
        {
            _enemigo = gameObject.BusquedadeObjeto(_radioBusqueda, _capaObjetivos, _capaRaycast);
            GestionarCacheSlots(); // Aseguramos que la caché de slots se actualiza al detectar uno nuevo
        }
        else
        {
            // Si el enemigo ya no está activo en la escena (murió o volvió al pool)
            if (_enemigo.TryGetComponent(out EnemigoBase enemigoBase))
            {
                if (enemigoBase.CurrentHealt <= 0)
                {
                    _enemigo = null;
                    GestionarCacheSlots();
                    return;
                }
            }

            // Si YA TENEMOS enemigo, la validación de si sigue siendo válido a corta distancia 
            // la debe manejar el Estado de Ataque/Persecución, NO este sensor general.
            // Solo lo limpiamos aquí si se ha alejado MÁS ALLÁ del radio de búsqueda + un margen.
            float distSqr = (transform.position - _enemigo.position).sqrMagnitude;
            float radioMaximoSqr = (_radioBusqueda + 2f) * (_radioBusqueda + 2f);

            if (distSqr > radioMaximoSqr)
            {
                Debug.Log("Enemigo no nulo, pero esta muy lejos");
                _enemigo = null; // Se alejó demasiado
                GestionarCacheSlots();
            }
        }
    }
    public void ActualizarSeguimiento()
    {
        if (_enemigo == null) return;

        // 1. Filtro de tiempo (Cooldown de re-pathing)
        if (Time.time < _tiempoSiguienteActualizacion) return;

        Vector3 posActualEnemigo = _enemigo.position;
        float distanciaDesplazadaSqr = (posActualEnemigo - _ultimaPosicionDestino).sqrMagnitude;

        // 2. Comprobación: Se actualiza si el enemigo se movió más del umbral 
        // O si el agente no tiene una ruta activa actualmente (caso inicial)
        if (distanciaDesplazadaSqr >= _umbralMovimientoSqr)
        {
            _ultimaPosicionDestino = posActualEnemigo; // 1º Actualizamos la variable
            _agente.SetDestination(_ultimaPosicionDestino); // 2º Enviamos la ruta actualizada

            // 3. Reiniciamos el temporizador de refresco
            _tiempoSiguienteActualizacion = Time.time + _comprovacionEnemiga;
        }
    }
    public bool RangoAtaque()
    {
        if (_enemigo == null) return false;

        // Evaluamos si el enemigo ya está a rango de golpe directo
        float distSqr = (transform.position - _enemigo.position).sqrMagnitude;
        float rangoSqr = (_distanciaAtaque + 0.3f) * (_distanciaAtaque + 0.3f);

        if (distSqr <= rangoSqr)
        {
            // La línea de choque ha hecho contacto: Congelamos el avance del pivote global de la horda
            return true;
        }
        else
        {
            return false;
        }

    }
    public void UpdateSkills()
    {
        if (_skillDefenseActive)
        {
            _skillDefenseTimer -= Time.deltaTime;

            if (_skillDefenseTimer <= 0)
            {
                _effectDefense.SetActive(false);
                _skillDefenseTimer = _skillDefenseTime;
                _skillDefenseActive = false;
            }
        }

        if (_skillAttackActive)
        {
            _skillAttackTimer -= Time.deltaTime;

            if (_skillAttackTimer <= 0)
            {
                _effectAttack.SetActive(false);
                _velocidadDeAtaque = _velocidadDeAtaqueOg;
                _skillAttackTimer = _skillAttackTime;
                _skillAttackActive = false;
            }
        }
    }
    public override void EditorInit()
    {
        base.EditorInit();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _agente = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
    }
    public void GestionarCacheSlots()
    {
        // 1. Si el objetivo no ha cambiado, salimos
        if (_objetivoGuardado == _enemigo) return;

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
        _objetivoGuardado = _enemigo;

        // 4. ASIGNAR NUEVO GESTOR:
        // Separamos la comprobación de null/active para evitar el error de variable no asignada en el out
        _slotsdelObjetivoActual = null;

        if (_enemigo != null && _enemigo.gameObject.activeInHierarchy)
        {
            if (_enemigo.TryGetComponent(out ObjetivoConSlotsAnillos gestorNuevo))
            {
                _slotsdelObjetivoActual = gestorNuevo;
            }
        }
    }
    // Método para determinar la capa/anillo de la unidad según su tipo de arma
    public int ObtenerIndiceSlot()
    {
        // Rango 0: Espadas / Dagas / Puños (Cuerpo a cuerpo corto <= 1.8m)
        if (PoolId == "InvocableEscudo") return 0;

        // Rango 1: Lanzadores de lanza / Alabardas / Mandobles (1.8m a 3.5m)
        if (PoolId == "InvocableAlabarda") return 1;

        // Rango 2: Arqueros / Magos (Distancia larga > 3.5m)
        return 2;
    }
    public virtual void ImpactoFlecha(Vector3 posicionFlecha, GameObject gameObject)
    {
        if (gameObject != this.gameObject || _muerto) return;

        // 1. Calculamos el vector dirección en el espacio global (Mundo)
        Vector3 direccionGlobal = posicionFlecha - transform.position;
        direccionGlobal.Normalize();

        // 2. Convertimos ese vector global a la dirección LOCAL del personaje
        // Esto traduce las coordenadas del mundo a: ¿Está adelante, atrás o a los lados respecto a mis ojos?
        Vector3 destino = transform.position + (direccionGlobal * 10f);

        if (_agente != null && _agente.isActiveAndEnabled)
        {
            //_agente.SetDestination(destino);
        }
    }
    private void Inicializacion()
    {
        AudioManager.Instance.ReproducirSonido(_levantarMuerto, _audioSource);
        _estaOrientado = false;
        _velocidaDeCrucero = UnityEngine.Random.Range(1.5f, 2.5f);
        multiplicadodorVelocidad = UnityEngine.Random.Range(0.85f, 1f);
        multiplicadorDeRecuperacion = UnityEngine.Random.Range(0.95f, 1.3f);
        multiplicadorRotacion = UnityEngine.Random.Range(1.5f, 2.2f);
        _healtBar.fillAmount = 1f;
        _umbralMovimientoSqr = umbralLineal * umbralLineal;
    }
    #endregion




    #region IDamageable
    public float Maxhealt => _vidaMaxima;
    public float CurrentHealt => _vidaActual;
    public bool IsDead => _muerto;
    public void TakeDamage(float damage, Vector3 impactPoint = default)
    {
        if (_skillDefenseActive)
        {
            _vidaActual -= damage / 2;
            _vidaActual = Mathf.Clamp(_vidaActual, 0, _vidaMaxima);
            _healtBar.fillAmount = _vidaActual / _vidaMaxima;
        }
        else
        {
            _vidaActual -= damage;
            _vidaActual = Mathf.Clamp(_vidaActual, 0, _vidaMaxima);
            _healtBar.fillAmount = _vidaActual / _vidaMaxima;
        }

        if (_vidaActual == 0)
        {
            _stateMaschineController.RequestState(_stateMuerte);

            if (_slotsdelObjetivoActual != null)
            {
                _slotsdelObjetivoActual.LiberarSlots(gameObject);
            }
        }
    }
    #endregion




    #region Coroutine
    #endregion




    #region InvocacionesObservable
    private List<InvocacionesObservable> _observable;
    public void AddObservable(InvocacionesObservable observable)
    {
        if (_observable == null)
        {
            _observable = new List<InvocacionesObservable>();
        }
        _observable.Add(observable);

    }
    public void RemoveObservable(InvocacionesObservable observable)
    {
        if (_observable == null)
        {
            _observable = new List<InvocacionesObservable>();
        }
        _observable.Remove(observable);
    }
    #endregion





    #region PoolEntity
    public override void Initialize()
    {
        base.Initialize();
        OnInizialize?.Invoke();
        OnInitialize?.Invoke();
        Inicializacion();
        _agente.enabled = true;
        _stateMaschineController.RequestState(_stateSeguir);
    }
    public override void Deactivate()
    {
        base.Deactivate();
        OnDeactivate?.Invoke();
        _agente.enabled = false;
        OnDeath = null;
    }
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Heal") && !_muerto)
        {
            _vidaActual += _curaciónRecibida;
            _vidaActual = Mathf.Clamp(_vidaActual, 0, _vidaMaxima);
            _healtBar.fillAmount = _vidaActual / _vidaMaxima;
            _anim.SetTrigger("Curarse");
        }

        if (other.CompareTag("Attack") && !_muerto)
        {
            _skillAttackActive = true;
            _effectAttack.SetActive(true);
            _velocidadDeAtaque = _velocidadDeAtaque * _addVelocidadDeAtaque;
            _anim.SetTrigger("Buff");
            Debug.Log("Attack");
        }

        if (other.CompareTag("Defense") && !_muerto)
        {
            _effectDefense.SetActive(true);
            _skillDefenseActive = true;
            _anim.SetTrigger("Buff");
            Debug.Log("Defense");
        }
    }
}
