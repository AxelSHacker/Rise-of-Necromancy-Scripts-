using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class EditorFormacionUI : MonoBehaviour
{
    [Header("Configuración de Escala")]
    //: Factor de conversión para transformar metros del mundo 3D a píxeles en la pantalla 2D.
    [SerializeField] private float _pixelesPorMetro = 40f;

    [Header("Sprites Fantasma de Planificación")]
    [SerializeField] private Sprite _spriteFantasmaEscudo;
    [SerializeField] private Sprite _spriteFantasmaArco;
    [SerializeField] private Sprite _spriteFantasmaLanza;

    [Header("Referencias de Sistemas")]
    //: Referencia al script principal que controla el movimiento e índices de la horda.
    [SerializeField] private FormaciondeEjercito _sistemaEjercito;

    [Header("Referencias UI")]
    //: Panel de la interfaz que actuará como padre y contenedor físico de los peones que instanciemos.
    [SerializeField] private RectTransform _panelContenedor;
    //: El objeto visual (botón/imagen) que representa a cada soldado individual en el tablero táctico.
    [SerializeField] private GameObject _prefabPeonUI;

    [Header("Sprites de Estado (Opcional)")]
    [Tooltip("Sprite para mostrar cuando el hueco de la formación esté vacío")]
    //: Imagen de fondo por defecto para identificar ranuras libres sin esbirros asignados.
    [SerializeField] Sprite _spriteHuecoVacio;

    [Header("ScriptableObject a Guardar")]
    //: El contenedor persistente en disco que almacena las coordenadas en metros de este diseño.
    [SerializeField] FormacionManualLibre _formacionScriptable;
    [SerializeField] FormacionManualLibre[] formacionesManualesLibres;
    [SerializeField] FormacionManualLibre[] _formacionEnMemoria;

    [Header("Utiles")]
    [SerializeField] int _idSlots; // Distancia mínima en metros que deben mantener los peones entre sí para evitar superposición visual

    [Header("Getters")]
    public FormacionManualLibre FormacionScriptable => _formacionScriptable;
    public FormacionManualLibre[] FormacionesEnMemoria => _formacionEnMemoria;
    public event Action OnFormationChanged;
    //: Lista en memoria que rastrea los scripts de los peones visuales renderizados actualmente en la UI.
    private List<PeonUI> _peonesActivos = new List<PeonUI>();

    private void Awake() // Método ejecutado por Unity al inicializar el objeto
    {
        InicializarFormacionesEnMemoria(); // Crea copias independientes en memoria para no modificar los assets del disco directamente
    }

    private void OnEnable() => ConstruirPanelEditor(); // Al activar el componente visual, dibuja todos los peones en el panel
    private void OnDisable() => LimpiarPanel(); // Al desactivar el componente visual, destruye los elementos de la interfaz

    private void InicializarFormacionesEnMemoria() // Método que clona las formaciones para runtime
    {
        if (formacionesManualesLibres == null || formacionesManualesLibres.Length == 0) return; // Validación de seguridad contra arrays vacíos o nulos

        _formacionEnMemoria = new FormacionManualLibre[formacionesManualesLibres.Length]; // Asigna el tamaño del nuevo array en memoria

        for (int i = 0; i < formacionesManualesLibres.Length; i++) // Recorre cada elemento del array de assets originales
        {
            if (formacionesManualesLibres[i] != null) // Comprueba que la casilla del array no esté vacía
            {
                _formacionEnMemoria[i] = formacionesManualesLibres[i].CrearCopiaRuntime(); // Genera una copia aislada de la formación
            }
        }

        _formacionScriptable = _formacionEnMemoria[0]; // Establece la primera formación clonada como la activa por defecto
    }

    public void ConstruirPanelEditor() // Método para regenerar y dibujar completamente la UI del editor
    {
        LimpiarPanel(); // Destruye todos los elementos gráficos antiguos antes de volver a instanciar

        if (_sistemaEjercito == null) return;

        _formacionScriptable = _sistemaEjercito.CurrentFormacion;

        if (_formacionScriptable == null) return; // Si el ejército no tiene formación asignada, detiene la ejecución

        // Asegurar la inicialización de la lista de nodos si está vacía
        if (_formacionScriptable.nodosAsignados == null || _formacionScriptable.nodosAsignados.Count == 0) // Verifica si existen nodos definidos
        {
            InicializarNodosPorDefecto(); // Si la formación viene en blanco, crea una distribución estándar de posiciones
        }

        List<GameObject> soldadosVivos = _sistemaEjercito.ObtenerSoldadosVivos(); // Obtiene la lista actual de tropas vivas en batalla
        int totalNodos = _formacionScriptable.nodosAsignados.Count; // Guarda la cantidad total de posiciones asignadas en el editor

        for (int i = 0; i < totalNodos; i++) // Recorre cada uno de los nodos guardados en la formación
        {
            DatosNodoFormacion datosNodo = _formacionScriptable.nodosAsignados[i]; // Extrae la información estructural del nodo actual

            // Regla de vanguardia: Las primeras 6 posiciones se fuerzan como Escudo
            if (i >= 0 && i <= 5) // Comprueba si el índice actual pertenece a la primera fila de la vanguardia
            {
                datosNodo.tipoSoldadoRequerido = "Escudo"; // Asigna obligatoriamente la restricción de tipo Escudo a las primeras 6 plazas
            }

            GameObject goPeon = Instantiate(_prefabPeonUI, _panelContenedor); // Instancia el prefab UI como hijo del panel contenedor

            int iDNodo = i; // Guarda el índice del nodo actual para su uso en callbacks y referencias

            if (!goPeon.TryGetComponent(out PeonUI scriptPeon) ||  // Intenta obtener el script PeonUI y valida si existe
                !goPeon.TryGetComponent(out RectTransform rectPeon) ||  // Intenta obtener el RectTransform y valida si existe
                !goPeon.TryGetComponent(out Image imagePeon)) // Intenta obtener el componente Image y valida si existe
            {
                Debug.LogError("[EditorFormacionUI] El prefab de Peón UI no contiene los componentes requeridos."); // Reporta un error descriptivo si falta algo
                continue; // Salta esta iteración del bucle para evitar fallos por referencia nula
            }

            GameObject soldadoAsignado = (i < soldadosVivos.Count) ? soldadosVivos[i] : null; // Evalúa si hay un soldado vivo disponible para esta plaza

            // Configuración visual según ocupación
            if (soldadoAsignado != null) // Si la posición dispone de un soldado real con vida
            {
                ConfigurarPeonConSoldadoVivo(soldadoAsignado, datosNodo, imagePeon); // Pinta el icono real del soldado en cuestión
            }
            else // Si la posición está desocupada por falta de tropas
            {
                ConfigurarPeonFantasma(datosNodo, imagePeon); // Pinta una representación visual fantasma o translúcida
            }

            // Anclaje al centro del contenedor UI
            rectPeon.anchorMin = Vector2.one * 0.5f; // Fija el ancla mínima del elemento UI exactamente al centro del contenedor (0.5, 0.5)
            rectPeon.anchorMax = Vector2.one * 0.5f; // Fija el ancla máxima del elemento UI exactamente al centro del contenedor (0.5, 0.5)
            rectPeon.pivot = Vector2.one * 0.5f; // Fija el punto pivote de transformación del elemento UI en su centro geométrico

            Vector3 posMetros = datosNodo.posicionMetros; // Lee la posición matemática en metros almacenada en el nodo de datos
            rectPeon.anchoredPosition = new Vector2(posMetros.x * _pixelesPorMetro, posMetros.z * _pixelesPorMetro); // Convierte metros 3D (X,Z) a píxeles 2D (X,Y) en la UI

            scriptPeon.Inicializar(i, OnPeonMoverse, OnPeonEliminar); // Configura el id del peón y le asigna la función callback para cuando se arrastre
            _peonesActivos.Add(scriptPeon); // Añade el nuevo peón a la lista de rastreo de elementos activos
        }

        MarcarAssetSucio(); // Registra los cambios en el editor si se está ejecutando desde Unity Editor
    }
    void OnPeonEliminar(int idNodo) // Callback invocado al presionar el botón de eliminar en un peón
    {
        if (_sistemaEjercito != null) // Comprueba que la referencia al sistema de ejército no sea nula
        {
            _sistemaEjercito.EliminarSoldado(idNodo); // Llama al método del sistema para eliminar el soldado correspondiente al id del nodo
        }

        ConstruirPanelEditor(); // Reconstruye la UI para reflejar los cambios tras la eliminación
    }
    private void ConfigurarPeonConSoldadoVivo(GameObject soldado, DatosNodoFormacion datosNodo, Image image) // Configura la representación de unidades reales
    {
        if (soldado.TryGetComponent(out InvocacionEscudo escudo)) // Comprueba si el GameObject es una unidad tipo Escudo
        {
            image.sprite = escudo._formationIcon; // Asigna el sprite específico guardado en el componente Escudo
            datosNodo.tipoSoldadoRequerido = "Escudo"; // Actualiza la etiqueta del tipo requerido en los datos del nodo
        }
        else if (soldado.TryGetComponent(out InvocacionArco arco)) // Comprueba si el GameObject es una unidad tipo Arco
        {
            image.sprite = arco._formationIcon; // Asigna el sprite específico guardado en el componente Arco
            datosNodo.tipoSoldadoRequerido = "Arco"; // Actualiza la etiqueta del tipo requerido en los datos del nodo
        }
        else if (soldado.TryGetComponent(out InvocacionAlabarda alabarda)) // Comprueba si el GameObject es una unidad tipo Alabarda
        {
            image.sprite = alabarda._formationIcon; // Asigna el sprite específico guardado en el componente Alabarda
            datosNodo.tipoSoldadoRequerido = "Alabarda"; // Actualiza la etiqueta del tipo requerido en los datos del nodo
        }

        image.color = Color.white; // Aplica opacidad completa (100%) indicando que la unidad está viva y presente
    }

    private void ConfigurarPeonFantasma(DatosNodoFormacion datosNodo, Image image) // Configura la representación de espacios vacíos
    {
        image.sprite = datosNodo.tipoSoldadoRequerido switch // Selecciona el sprite adecuado basándose en el string del tipo requerido mediante pattern matching
        {
            "Escudo" => _spriteFantasmaEscudo != null ? _spriteFantasmaEscudo : _spriteHuecoVacio, // Usa fantasma escudo o el de fallback si es nulo
            "Arco" => _spriteFantasmaArco != null ? _spriteFantasmaArco : _spriteHuecoVacio, // Usa fantasma arco o el de fallback si es nulo
            "Lanza" => _spriteFantasmaLanza != null ? _spriteFantasmaLanza : _spriteHuecoVacio, // Usa fantasma lanza o el de fallback si es nulo
            _ => _spriteHuecoVacio // Para cualquier otro caso no especificado, utiliza el sprite genérico vacío
        };

        image.color = new Color(1f, 1f, 1f, 0.25f); // Aplica un tinte semifinal con 25% de opacidad para lograr el efecto "fantasma"
    }

    private void InicializarNodosPorDefecto() // Crea la disposición inicial de nodos cuando el asset está completamente vacío
    {
        _formacionScriptable.nodosAsignados = new List<DatosNodoFormacion>(); // Instancia una nueva lista vacía de nodos en el ScriptableObject
        int creados = 0; // Contador de nodos válidos posicionados correctamente
        int intento = 0; // Contador de iteraciones y posiciones evaluadas en el patrón de cuadrícula
        int maxFormacion = _sistemaEjercito != null ? _sistemaEjercito.MaxFormacion : 16; // Obtiene el número límite de unidades (16 por defecto)

        while (creados < maxFormacion) // Continúa iterando hasta haber creado todas las plazas solicitadas
        {
            int columna = intento % 4 - 2; // Calcula la coordenada X de la matriz (distribuyendo las columnas entre -2 y 1)
            int fila = -(intento / 4); // Calcula la coordenada Z de la matriz hacia atrás (filas negativas)

            Vector3 posicionCalculada = new Vector3(columna * 1.5f, 0f, fila * 1.5f); // Convierte los índices de cuadrícula a coordenadas de metros con separación de 1.5m

            // Evitamos que caiga sobre el espacio asignado al jugador (origen 0,0)
            if (Vector3.Distance(posicionCalculada, Vector3.zero) < 0.8f) // Verifica la proximidad del nodo al origen exacto del jugador
            {
                intento++; // Incrementa el índice de evaluación
                continue; // Salta la creación en esta coordenada para no tapar la posición central del líder
            }

            DatosNodoFormacion nuevoNodo = new DatosNodoFormacion // Crea una nueva instancia contenedora de datos para la posición
            {
                posicionMetros = posicionCalculada, // Asigna la posición espacial calculada en metros
                tipoSoldadoRequerido = (creados <= 5) ? "Escudo" : "Libre" // Asigna la vanguardia obligatoria a las primeras 6 casillas
            };

            _formacionScriptable.nodosAsignados.Add(nuevoNodo); // Guarda el nuevo nodo en la colección del ScriptableObject
            creados++; // Incrementa el contador de nodos creados con éxito
            intento++; // Avanza al siguiente intento de la matriz
        }

        MarcarAssetSucio(); // Fuerza el guardado de los cambios del asset en el editor
    }

    private void OnPeonMoverse(PeonUI peonMovido, bool esFinal) // Callback invocado en el evento de arrastre de los elementos peón
    {
        if (!peonMovido.TryGetComponent(out RectTransform rectPeon)) return; // Obtiene el RectTransform del elemento arrastrado; interrumpe si no existe

        // Limitar movimiento dentro de los márgenes del panel
        if (_panelContenedor != null) // Comprueba si existe la referencia al panel UI
        {
            rectPeon.anchoredPosition = ClampearPosicionUI(rectPeon.anchoredPosition, rectPeon.rect.size); // Mantiene las coordenadas dentro del rectángulo del panel
        }

        Vector2 posicionUI = rectPeon.anchoredPosition; // Recupera la posición en píxeles ajustada dentro de la UI

        // Snapping en metros (malla de 0.5m)
        float metroX = Mathf.Round((posicionUI.x / _pixelesPorMetro) * 2f) / 2f; // Convierte la posición X de píxeles a metros y aplica redondeo en pasos de 0.5m
        float metroZ = Mathf.Round((posicionUI.y / _pixelesPorMetro) * 2f) / 2f; // Convierte la posición Y de píxeles a metros (eje Z) con redondeo de 0.5m

        Vector3 posicionMetrosCalculada = new Vector3(metroX, 0f, metroZ); // Reconstruye el Vector3 en metros (con el eje Y de altura en 0)

        GuardarPosicionEnScriptableObject(peonMovido.idSlot, posicionMetrosCalculada); // Escribe la nueva posición calculada dentro del ScriptableObject

        if (_sistemaEjercito != null) // Comprueba la conexión con el controlador del ejército
        {
            _sistemaEjercito.OrganizarFormacion(); // Notifica al ejército para que reposicione a los soldados en el mundo real en tiempo real
        }

        if (esFinal) // Comprueba si este frame corresponde al momento en que el usuario soltó el arrastre (OnEndDrag)
        {
            Vector2 posicionFinalPixeles = new Vector2(metroX * _pixelesPorMetro, metroZ * _pixelesPorMetro); // Convierte la posición alineada (snapped) a píxeles UI

            if (_panelContenedor != null) // Valida que el contenedor exista
            {
                posicionFinalPixeles = ClampearPosicionUI(posicionFinalPixeles, rectPeon.rect.size); // Asegura que las coordenadas finales snapped no sobrepasen el panel
            }

            rectPeon.anchoredPosition = posicionFinalPixeles; // Ajusta suavemente la posición del icono UI al punto del imán (snap grid)
            OnFormationChanged?.Invoke(); // Lanza el evento global notificando que la formación terminó de ser modificada
        }
    }

    private Vector2 ClampearPosicionUI(Vector2 posicion, Vector2 tamanoPeon) // Función auxiliar para restringir la posición UI dentro del contenedor
    {
        Vector2 tamanoPanel = _panelContenedor.rect.size; // Obtiene el ancho y alto del panel contenedor en píxeles

        float xMax = (tamanoPanel.x / 2f) - (tamanoPeon.x / 2f); // Calcula el límite máximo horizontal positivo/negativo considerando el ancho del peón
        float yMax = (tamanoPanel.y / 2f) - (tamanoPeon.y / 2f); // Calcula el límite máximo vertical positivo/negativo considerando el alto del peón

        return new Vector2( // Retorna la posición restringida usando Clamp
            Mathf.Clamp(posicion.x, -xMax, xMax), // Limita X entre el borde izquierdo y derecho del panel
            Mathf.Clamp(posicion.y, -yMax, yMax)  // Limita Y entre el borde inferior y superior del panel
        );
    }

    private void GuardarPosicionEnScriptableObject(int id, Vector3 nuevaPosicionMetros) // Actualiza las coordenadas de un slot específico en los datos
    {
        while (_formacionScriptable.nodosAsignados.Count <= id) // Si el listado de datos no cuenta con suficientes elementos para cubrir el ID recibido
        {
            _formacionScriptable.nodosAsignados.Add(new DatosNodoFormacion()); // Añade elementos por defecto hasta igualar el número de índice requerido
        }

        _formacionScriptable.nodosAsignados[id].posicionMetros = nuevaPosicionMetros; // Guarda el nuevo vector en metros en la estructura del nodo seleccionado
        MarcarAssetSucio(); // Marca la necesidad de guardar cambios en la base de datos del proyecto
    }

    public void LimpiarPanel() // Método para destruir de forma limpia los elementos de la interfaz
    {
        foreach (PeonUI peon in _peonesActivos) // Recorre la lista de referencias de peones UI activos
        {
            if (peon != null) // Verifica que el objeto exista y no haya sido destruido previamente
            {
                Destroy(peon.gameObject); // Elimina de la escena el GameObject asociado al elemento de la interfaz
            }
        }
        _peonesActivos.Clear(); // Vacía completamente la lista de referencias en C#
    }

    private void MarcarAssetSucio() // Método de utilidad para la integración nativa con el motor en fase de edición
    {
#if UNITY_EDITOR // Directiva de compilación: Este código solo se compila e incluye dentro del editor de Unity (se ignora en builds)
        if (_formacionScriptable != null) // Valida que exista un objeto ScriptableObject cargado
        {
            UnityEditor.EditorUtility.SetDirty(_formacionScriptable); // Informa a Unity que el asset ha sido modificado y debe marcarse para guardado (ctrl+s)
        }
#endif // Fin de la condición del preprocesador
    }
}