using UnityEngine;
using System.Collections.Generic;

public class FormaciondeEjercito : MonoBehaviour, InvocacionesObservable
{
    #region Estructura de Cache

    private struct SlotCache
    {
        public Transform Transform;
        public InvocacionBase Invocacion;
        public EnemigoBase Enemigo;
    }

    private List<SlotCache> _slotsCacheados = new List<SlotCache>();

    #endregion

    #region Variables

    [Header("Estructura de la Formación")]
    // Lista principal que contiene las referencias a las unidades activas en cada espacio (slot) de la formación.
    [SerializeField] internal List<GameObject> _slotsFormacion = new List<GameObject>();

    [Header("Referencias de Transform y Rotación")]
    // Objeto de referencia para la posición y orientación base de la formación (ej. el jugador/nigromante o un pivote).
    [SerializeField] NigromanteController _nigromanteController;
    [SerializeField] EnemigoTeniente _tenienteController;
    [SerializeField] Transform _posicionModelo;
    [SerializeField] Transform _posicionPivote;
    [SerializeField] EditorFormacionUI _editorFormacionUI;
    [SerializeField] HUDController _hudController;
    Quaternion _rotacionModelo;
    Quaternion _rotacionDestino;
    [SerializeField] float _velocidadRotacion = 5f;
    [SerializeField] private float _tiempoEsperaEstable = 0.25f; // Tiempo que debe estar quieto tras girar
    private float _contadorEstabilidad;

    [Header("Navegación y Estados")]
    // Posición de destino a la que se debe mover la formación cuando no sigue al nigromante.
    Vector3 _destinyPosition;
    // Indica si la formación debe seguir al nigromante o moverse de forma independiente a un punto fijo.
    bool _followingNecromancer = true;
    Quaternion _rotationDestiny;
    // Marca si el pivote de la formación está girando activamente.
    bool _rotacionPlayer = false;
    // Bandera para notificar si la estructura de la horda ha cambiado (altas o bajas).
    bool _hordaACambiado = false;
    // Define si esta formación pertenece al jugador o a una facción enemiga.
    public bool _hordaJugador = true;
    [SerializeField] float _velocidaMinima = 1f;
    float _velocidadPlayer = 0f;
    [SerializeField] string _stadoSeguir;


    [Header("Datos de ScriptableObjects de Formación")]
    // Datos de la formación actualmente activa (definición gráfica de las posiciones relativas).
    [SerializeField] private FormacionManualLibre _currentFormationData;
    // Lista de formaciones disponibles entre las que se puede cambiar dinámicamente.
    //[SerializeField] private List<FormacionManualLibre> _formationsData;

    [Header("Límites de Capacidad")]
    // Cantidad máxima de unidades que caben en la formación actual.
    [SerializeField] public int _maxFormacion;
    // Cantidad de unidades vivas asignadas actualmente a la formación.
    [SerializeField] int _soldadosActuales;

    [Header("Combate"), SerializeField]
    bool _atacandoEnemigos = false;
    bool _cargandoEnemigos = false;
    public bool _ataqueEnFormacion = true;

    [Header("Getters")]

    public Vector3 DestinyPosition => _destinyPosition;
    // Propiedades públicas de acceso a los datos internos
    public int MaxFormacion => _maxFormacion;
    public int SoldadosActuales => _soldadosActuales;
    public FormacionManualLibre CurrentFormacion => _currentFormationData;
    public bool AtacandoEnemigos => _atacandoEnemigos;
    public bool CargandoEnemigos => _cargandoEnemigos;
    public bool AtaqueEnFormacion => _ataqueEnFormacion;

    public System.Action OnChangeFormation;
    #endregion

    #region Unity Methods
    void Start()
    {
        // Asigna la formación por defecto si hay datos disponibles en la lista y no se especificó ninguna.
        if (_hordaJugador && _editorFormacionUI.FormacionesEnMemoria != null && _editorFormacionUI.FormacionesEnMemoria.Length > 0 && _currentFormationData == null)
        {
            _currentFormationData = _editorFormacionUI.FormacionScriptable;
        }
        // Inicialización de rotaciones de trabajo
        _rotationDestiny = Quaternion.identity;
        _rotacionModelo = _posicionModelo != null ? _posicionModelo.rotation : transform.rotation;
        _hordaACambiado = true;
        if (_posicionPivote == null)
        {
            GameObject pivoteGO = new GameObject($"PivoteFormacion_{gameObject.name}");
            _posicionPivote = pivoteGO.transform;
            _posicionPivote.position = transform.position;
            _posicionPivote.rotation = transform.rotation;
        }
        // Limpia la lista de slots e inicializa la capacidad
        _slotsFormacion.Clear();

        // Rellena la lista de slots con nulos según la capacidad máxima.
        for (int i = 0; i < _maxFormacion; i++)
        {
            _slotsFormacion.Add(null);
        }

        RecalcularCacheSlots();
    }

    void Update()
    {
        // Recalcula constantemente las posiciones en el mundo de cada soldado de la formación.
        OrganizarFormacion();

        if (_nigromanteController != null && _ataqueEnFormacion)
        {
            if (_nigromanteController.EjercitoEnemigo != null)
            {
                AvanzarContraFormacion(_nigromanteController.EjercitoEnemigo);
            }
            else if (_nigromanteController.TransformEnemigo != null)
            {
                AvanzarContraSoldado(_nigromanteController.TransformEnemigo.position);
            }
        }
    }

    void OnDrawGizmos()
    {
        // Dibuja en el editor de Unity la posición de destino fija (esfera roja) y su orientación (rayo azul).
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_destinyPosition, 0.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(_destinyPosition, _rotationDestiny * Vector3.forward * 2f);
    }

    private void OnDestroy()
    {
        // Desregistra esta clase del patrón Observer para evitar fugas de memoria al destruir el objeto.
        NigromanteController nigromante = FindAnyObjectByType<NigromanteController>();
        if (nigromante != null)
            nigromante.RemoveObservable(this);
    }
    #endregion

    #region Métodos de Cache
    /// <summary>
    /// Reconstruye la lista de referencias cacheadas. Se invoca únicamente
    /// al añadir o remover unidades de la formación.
    /// </summary>
    public void RecalcularCacheSlots()
    {
        _slotsCacheados.Clear();
        for (int i = 0; i < _slotsFormacion.Count; i++)
        {
            GameObject item = _slotsFormacion[i];
            if (item == null)
            {
                _slotsCacheados.Add(default);
                continue;
            }

            SlotCache slot = new SlotCache
            {
                Transform = item.transform,
                Invocacion = item.GetComponent<InvocacionBase>(),
                Enemigo = item.GetComponent<EnemigoBase>()
            };
            _slotsCacheados.Add(slot);
        }
    }
    #endregion

    #region Métodos de Gestión y Movimiento
    /// Devuelve la lista completa de slots de la formación (con unidades asignadas y espacios vacíos).
    public List<GameObject> ObtenerSoldadosVivos()
    {
        return _slotsFormacion;
    }
    /// Cambia el comportamiento de la formación para que deje de seguir al nigromante 
    /// y se desplace hacia un punto concreto del mundo.
    public void AvanzarContraSoldado(Vector3 positionDestiny)
    {
        if (_atacandoEnemigos) return;

        _followingNecromancer = false;
        _cargandoEnemigos = true;

        float velocidadBase = ObtenerVelocidadMaxima();
        _posicionPivote.position = Vector3.MoveTowards(_posicionPivote.position, positionDestiny, velocidadBase * Time.deltaTime);

        Vector3 direccion = positionDestiny - _posicionPivote.position;
        direccion.y = 0f; // Mantiene el pivote completamente plano sobre el suelo

        if (direccion != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            float velocidadRotacion = 360f;
            _posicionPivote.rotation = Quaternion.RotateTowards(_posicionPivote.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }
    }
    public void AvanzarenFormacion(Vector3 destino)
    {
        _followingNecromancer = false;

        float velocidadBase = Mathf.Max(_velocidaMinima, _velocidadPlayer);
        _posicionPivote.position = destino;

        Vector3 direccion = destino - _posicionPivote.position;
        direccion.y = 0f; // Mantiene el pivote completamente plano sobre el suelo

        if (direccion != Vector3.zero)
        {
            Quaternion.LookRotation(direccion);
        }
    }
    /// Vuelve a vincular el movimiento de la formación a la posición del Nigromante.
    public void SeguirNigromante()
    {
        _followingNecromancer = true;
        OrganizarFormacion();
    }
    /// Asigna las posiciones destino del NavMeshAgent/IA de cada unidad en base a su slot sin hacer TryGetComponent.
    public void OrganizarFormacion()
    {
        if (_currentFormationData == null || _slotsFormacion.Count == 0 || !_ataqueEnFormacion) return;

        // Si la formación requiere redimensionar nodos gráficos en el ScriptableObject, lo ejecuta.
        if (_currentFormationData is FormacionManualLibre manualLibre && manualLibre.nodosAsignados != null)
        {
            manualLibre.RedimensionarNodos(_maxFormacion);
        }

        Transform referenciaTransform = _posicionModelo != null ? _posicionModelo : transform;

        // 1. Calculamos cuánto ha girado respecto al frame anterior
        float anguloCambioEsteFrame = Quaternion.Angle(referenciaTransform.rotation, _rotacionModelo);

        // 2. Si hay giro perceptible, reiniciamos el temporizador
        if (anguloCambioEsteFrame > 0.1f)
        {
            _contadorEstabilidad = _tiempoEsperaEstable;
        }
        else
        {
            _contadorEstabilidad -= Time.deltaTime;
        }

        bool jugadorEstaEstable = _contadorEstabilidad <= 0f;
        _rotacionPlayer = anguloCambioEsteFrame > 0.1f;

        if (jugadorEstaEstable || _atacandoEnemigos || _cargandoEnemigos)
        {
            _rotacionDestino = Quaternion.Slerp(referenciaTransform.rotation, _rotacionModelo, Time.deltaTime * _velocidadRotacion);
        }

        if (_followingNecromancer)
        {
            _posicionPivote.position = referenciaTransform.position;
            _posicionPivote.rotation = _rotacionDestino;
        }

        int totalSlots = _slotsCacheados.Count;
        for (int i = 0; i < totalSlots; i++)
        {
            SlotCache slot = _slotsCacheados[i];
            if (slot.Transform == null) continue;

            Vector3 posLocal = _currentFormationData.GetPosicion(i, totalSlots);
            Vector3 posMundo = _posicionPivote.position + (_posicionPivote.rotation * posLocal);

            if (slot.Invocacion != null)
            {
                slot.Invocacion.SetDestination(posMundo);

                _velocidaMinima = slot.Invocacion._velocidaDeCrucero;
                _velocidadPlayer = slot.Invocacion._velocidadPlayer;
            }
            else if (slot.Enemigo != null)
            {
                slot.Enemigo.SetDestinationEnemies(posMundo);
                slot.Enemigo._posicionEnFormacion = posMundo;
            }
        }

        _rotacionModelo = referenciaTransform.rotation;
    }
    /// Revisa la capacidad del ScriptableObject y expande la lista de slots si es necesario.
    private void AsegurarCapacidadDeSlots()
    {
        if (_currentFormationData is FormacionManualLibre manualLibre && manualLibre.nodosAsignados != null)
        {
            _maxFormacion = Mathf.Max(_maxFormacion, manualLibre.nodosAsignados.Count);
        }

        while (_slotsFormacion.Count < _maxFormacion)
        {
            _slotsFormacion.Add(null);
        }
    }
    public void CambiarFormacion(string name)
    {
        for (int i = 0; i < _editorFormacionUI.FormacionesEnMemoria.Length; i++)
        {
            if (_editorFormacionUI.FormacionesEnMemoria[i].nombreFormacion == name)
            {
                _currentFormationData = _editorFormacionUI.FormacionesEnMemoria[i];
                _editorFormacionUI.LimpiarPanel();
                _editorFormacionUI.ConstruirPanelEditor();
                OnChangeFormation?.Invoke();
                break;
            }
        }
    }
    public Vector3 ObtenerCentroMasa()
    {
        Vector3 sumaPosiciones = Vector3.zero;
        int sodadosContados = 0;

        for (int i = 0; i < _slotsFormacion.Count; i++)
        {
            if (_slotsFormacion[i] != null && _slotsFormacion[i].activeInHierarchy)
            {
                sumaPosiciones += _slotsFormacion[i].transform.position;
                sodadosContados++;
            }
        }

        if (sodadosContados > 0)
        {
            return sumaPosiciones / sodadosContados;
        }
        return transform.position;
    }
    public void AvanzarContraFormacion(FormaciondeEjercito formacionEnemiga)
    {
        if (formacionEnemiga == null || _atacandoEnemigos) return;

        _followingNecromancer = false;
        _cargandoEnemigos = true;
        _destinyPosition = formacionEnemiga.ObtenerCentroMasa();

        float velocidadBase = ObtenerVelocidadMaxima();
        _posicionPivote.position = Vector3.MoveTowards(_posicionPivote.position, _destinyPosition, velocidadBase * Time.deltaTime);

        Vector3 direccion = _destinyPosition - _posicionPivote.position;
        direccion.y = 0f;

        if (direccion != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            float velocidadRotacion = 360f;
            _posicionPivote.rotation = Quaternion.RotateTowards(_posicionPivote.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }
    }
    public void CambiarState(string state)
    {
        int totalSlots = _slotsCacheados.Count;
        for (int i = 0; i < totalSlots; i++)
        {
            if (_slotsCacheados[i].Transform == null) continue;

            if (_slotsCacheados[i].Transform.TryGetComponent(out StateMaschineController component))
            {
                component.RequestState(state);
            }
        }
    }
    public void SetEstadoCombate(bool estado)
    {
        _atacandoEnemigos = estado;
    }
    public void SetEstadoCarga(bool estado)
    {
        _cargandoEnemigos = estado;
    }
    public void EliminarSoldado(int indice)
    {
        // 1. Validar rango básico de la lista
        if (_slotsFormacion == null || indice < 0 || indice >= _slotsFormacion.Count) return;

        GameObject soldado = _slotsFormacion[indice];

        // 2. Si el slot está vacío, salir inmediatamente para evitar procesar nulos
        if (soldado == null) return;

        // 3. Actualizar la interfaz de UI de forma segura
        if (_hudController != null)
        {
            _hudController.UpdateMinionsBar(Mathf.Max(0, _soldadosActuales - 1));
        }

        // 4. Obtener el componente y procesar el retorno al pool
        InvocacionBase invocacionBase = soldado.GetComponent<InvocacionBase>();

        if (invocacionBase != null)
        {
            invocacionBase.OnDeactivate?.Invoke();
            invocacionBase.ReturnToPool();
        }

        // 5. Notificar la baja al sistema de formación enviando un objeto válido
        OnZombieMuerto(soldado);
    }
    public float ObtenerVelocidadMaxima()
    {
        if (_slotsFormacion == null || _slotsFormacion.Count == 0)
        {
            return Mathf.Max(_velocidaMinima, _velocidadPlayer);
        }

        float velocidadMaximaHallada = 0f;

        for (int i = 0; i < _slotsFormacion.Count; i++)
        {
            // 1. Guardar el elemento de la lista directamente
            var slot = _slotsFormacion[i];

            // 2. Comprobar NULO PRIMERO (Evita la NullReferenceException)
            if (slot == null) continue;

            // 3. Extraer el componente InvocacionBase de forma segura
            // Funciona tanto si _slotsFormacion es List<Transform>, List<GameObject> o List<InvocacionBase>
            if (slot.TryGetComponent(out InvocacionBase aliado))
            {
                if (aliado != null && aliado.gameObject.activeInHierarchy)
                {
                    float velAliado = aliado.VelocidaddeCrucero * aliado.multiplicadodorVelocidad;

                    if (velAliado > velocidadMaximaHallada)
                    {
                        velocidadMaximaHallada = velAliado;
                    }
                }
            }
        }

        // El pivote debe ir, como mínimo, a la velocidad del jugador o del aliado más rápido
        return Mathf.Max(velocidadMaximaHallada, _velocidadPlayer, _velocidaMinima);
    }
    #endregion

    #region Observable / Gestión de Bajas y Altas (Sin Reserva)
    public void OnZombieResucitado(GameObject resucitadoZombie)
    {
        if (resucitadoZombie == null || _slotsFormacion.Contains(resucitadoZombie)) return;

        if (resucitadoZombie.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
        {
            resucitadoZombie.SetActive(true);
            agent.Warp(resucitadoZombie.transform.position);
        }

        AsegurarCapacidadDeSlots();
        string tipo = ObtenerTipoPorPoolId(resucitadoZombie);

        if (tipo == "Escudo")
        {
            for (int i = 0; i < _slotsFormacion.Count; i++)
            {
                if (_slotsFormacion[i] == null)
                {
                    AsignarSoldadoASlot(i, resucitadoZombie);
                    return;
                }
            }
        }
        else if (tipo == "Alabarda")
        {
            for (int i = 4; i < _slotsFormacion.Count; i++)
            {
                if (_slotsFormacion[i] == null)
                {
                    AsignarSoldadoASlot(i, resucitadoZombie);
                    return;
                }
            }
        }
        else if (tipo == "Arco")
        {
            for (int i = _slotsFormacion.Count - 1; i >= 0; i--)
            {
                if (_slotsFormacion[i] == null)
                {
                    AsignarSoldadoASlot(i, resucitadoZombie);
                    return;
                }
            }
        }

        for (int i = 0; i < _slotsFormacion.Count; i++)
        {
            if (_slotsFormacion[i] == null)
            {
                AsignarSoldadoASlot(i, resucitadoZombie);
                return;
            }
        }
    }
    private void AsignarSoldadoASlot(int indiceSlot, GameObject soldado)
    {
        _slotsFormacion[indiceSlot] = soldado;
        soldado.SetActive(true);
        _soldadosActuales++;
        _hordaACambiado = true;

        RecalcularCacheSlots();
    }
    public void OnZombieMuerto(GameObject zombieMuerto)
    {
        int asientoLiberado = _slotsFormacion.IndexOf(zombieMuerto);

        if (asientoLiberado != -1)
        {
            _slotsFormacion[asientoLiberado] = null;
            _soldadosActuales--;
            _hordaACambiado = true;

            RecalcularCacheSlots();
        }
    }
    private string ObtenerTipoPorPoolId(GameObject soldado)
    {
        if (soldado.TryGetComponent(out PoolEntity entity))
        {
            if (entity.PoolId == "InvocableEscudo" || entity.PoolId == "EnemigoEscudo") return "Escudo";
            if (entity.PoolId == "InvocableAlabarda" || entity.PoolId == "EnemigoLanza") return "Alabarda";
            if (entity.PoolId == "InvocacionArco" || entity.PoolId == "EnemigoArco") return "Arco";
        }
        return "Libre";
    }
    #endregion
}