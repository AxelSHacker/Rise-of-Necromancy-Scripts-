using System.Collections.Generic;
using UnityEngine;
// Componente que se añade a cualquier objetivo/enemigo para permitir que multiples unidades lo rodeen en filas
public class ObjetivoConSlotsAnillos : MonoBehaviour
{
    // Clase interna serializable para definir las propiedades de cada anillo de combate desde el Inspector
    [System.Serializable]
    public class AnilloAtaque
    {
        //Nombre descritivo para identificar la capa en el editor
        public string nombreAnillo = "Fila Melee";
        //Distancia desde el centro del objetivo hasta donde se coloque el contrario
        public float radio = 1f;
        //Cantidad de Slots alrededor de nuestro objeto
        public int totalSlots = 5;
        //Array Interno para almacenar las referencias de los contrarios que ocupen un slots
        public GameObject[] slots;
        //Metodo de apoyo para incializar la structura de datos del array sin perder referencia
        public void Inicializar()
        {
            //Solo instaciamos el array si aun no exist o si cambio el tamanio
            if (slots == null || slots.Length != totalSlots)
            {
                //Creamos el array con el tamanio fijado en totalSlots
                slots = new GameObject[totalSlots];
            }
        }
    }
    [Header("Configuración de Capas de Combate")]
    [Tooltip("Anillo 0: Espadas (cerca), Anillo 1: Alabardas (medio), etc.")]
    // Array principal que contiene todas las capas/anillos configurables para este enemigo
    [SerializeField] private AnilloAtaque[] anillos;

    void Awake()
    {
        InicializarAnillos();
    }

    private void InicializarAnillos()
    {
        //Comprobacion de seguridad  si no se configuran anillos no hacemos nada
        if (anillos == null) return;
        //Iteramos por cada anillo configurado en el array principal
        foreach (var anillo in anillos)
        {
            //Ejecutamos la resrva de memoria del array de slots de ese anillo
            anillo.Inicializar();
        }
    }
    // =========================================================================
    // PEDIR SLOT EN ANILLO ESPECÍFICO
    // =========================================================================

    //Metodo principal llamado por los atacants para reservar u obtener su posicion de combat en un anillo
    public Vector3? PedirSlots(GameObject atacante, int indiceAnillo)
    {
        // Cancelamos y devolvemos nulo si la unidad solicitante es invalida o no existen anillos configurados
        if (atacante == null || anillos == null || anillos.Length == 0) return null;
        //Aseguramos que el indice pedido no seea menor a 0 ni mayor al ultimo anillo disponible
        indiceAnillo = Mathf.Clamp(indiceAnillo, 0, anillos.Length - 1);
        //Obtenemos la referencia directa a la estructura del anillo correspondiente
        AnilloAtaque anillo = anillos[indiceAnillo];
        //Verificamos que el anillo seleccionado tenga su array instanciado
        anillo.Inicializar();

        // ---------------------------------------------------------------------
        // PASO 1: Auto-limpieza de objetos desactivados (Object Pooling)
        // ---------------------------------------------------------------------

        //Recorremos las posiciones del anillo seleccionado
        for (int i = 0; i < anillo.slots.Length; i++)
        {
            if (anillo.slots[i] != null && !anillo.slots[i].activeInHierarchy)
            {
                anillo.slots[i] = null;
            }
        }

        //Guardamos la posicion actual en el mundo tridimensional de la unidad atacante
        Vector3 posAtacante = atacante.transform.position;

        // ---------------------------------------------------------------------
        // PASO 2: Comprobar si la unidad YA tiene un slot reservado en este anillo
        // ---------------------------------------------------------------------

        //Variable para registrar l indice del slot qu ya posea la unidad (-1 significa que no tiene ninguno)
        int slotActual = -1;
        //Variable para almacenar la distancia al cuadrado hacia su slots actual (inicializada a infinito)
        float distanciaActualSqr = Mathf.Infinity;
        //Recorremos las posiciones del aniullo para comprobar si la unidad ya figuraba como ocupante
        for (int i = 0; i < anillo.slots.Length; i++)
        {
            //Comprobacion d referencia dirctas en memoria entre la  unidad y el reguistro de slots
            if (anillo.slots[i] == atacante)
            {
                //Guardamos el indice donde fue localizada la unidad
                slotActual = i;
                //Calculamos la distancia al cuadrado desde la unidad hasta el punto
                distanciaActualSqr = (posAtacante - CalcularPosicion(anillo, i)).sqrMagnitude;
                // Rompemos el bucle al encontrar coincidencia para no consumir ciclos innecesarios
                break;
            }
        }

        // ---------------------------------------------------------------------
        // PASO 3: Buscar si hay un slot LIBRE significativamente más cerca
        // ---------------------------------------------------------------------

        // Indice para registrar la mejor opción libre encontrada (-1 = ninguna opcion mejor)
        int mejorSlotLibre = -1;

        // Margen de tolerancia de 0.5m (0.25 al cuadrado) para evitar oscilaciones entre dos slots cercanos
        float margenCambioSqr = 0.25f;

        // Aplicamos el margen a la distancia actual: el nuevo slot debe ser al menos 0.5m más cercano para ser elegido
        float mejorDistanciaSqr = distanciaActualSqr - margenCambioSqr;

        // Recorremos de nuevo las posiciones buscando huecos libres
        for (int i = 0; i < anillo.slots.Length; i++)
        {
            // Filtramos únicamente los slots vacíos
            if (anillo.slots[i] == null)
            {
                // Calculamos la distancia al cuadrado de la unidad hasta este hueco libre
                float distSqr = (posAtacante - CalcularPosicion(anillo, i)).sqrMagnitude;

                // Si esta distancia es menor que el limite fijado con la histeresis...
                if (distSqr < mejorDistanciaSqr)
                {
                    // Actualizamos la mejor distancia registrada hasta el momento
                    mejorDistanciaSqr = distSqr;

                    // Registramos el indice del nuevo slot mas óptimo
                    mejorSlotLibre = i;
                }
            }
        }

        // ---------------------------------------------------------------------
        // PASO 4: Asignación o Mantenimiento
        // ---------------------------------------------------------------------

        // Si se ha detectado un slot libre mas conveniente...
        if (mejorSlotLibre != -1)
        {
            // Si la unidad ya poseía un slot anterior distinto, vaciamos la ocupación antigua
            if (slotActual != -1)
            {
                anillo.slots[slotActual] = null;
            }

            // Asignamos la referencia de la unidad al nuevo slot
            anillo.slots[mejorSlotLibre] = atacante;

            // Retornamos las coordenadas globales calculadas para el nuevo slot asignado
            return CalcularPosicion(anillo, mejorSlotLibre);
        }

        // Si no habia ningun slot libre mejor, pero la unidad ya disponia de uno previo...
        if (slotActual != -1)
        {
            // Conservamos la reserva y devolvemos la posición actualizada del slot en el mundo
            return CalcularPosicion(anillo, slotActual);
        }

        // Si todos los slots del anillo están ocupados y la unidad no tenia plaza previa, retornamos nulo
        return null;
    }

    // =========================================================================
    // LIBERAR SLOTS
    // =========================================================================

    /// Método publico para limpiar la reserva de una unidad (al morir, cambiar de objetivo o cancelar ataque)
    public void LiberarSlots(GameObject atacante)
    {
        // Cancelamos si la referencia es nula o no hay anillos configurados
        if (atacante == null || anillos == null) return;

        // Recorremos absolutamente todas las capas de anillos registradas
        foreach (var anillo in anillos)
        {
            // Si el anillo no tiene instanciado el array de slots, pasamos al siguiente
            if (anillo.slots == null) continue;

            // Recorremos los slots del anillo actual
            for (int i = 0; i < anillo.slots.Length; i++)
            {
                // Si encontramos a la unidad registrada en este slot...
                if (anillo.slots[i] == atacante)
                {
                    // Ponemos a null la casilla para liberar la plaza
                    anillo.slots[i] = null;

                    // Salimos del método inmediatamente ya que una unidad solo puede ocupar un slot a la vez
                    return;
                }
            }
        }
    }

    // =========================================================================
    // MATEMÁTICA Y POSICIONAMIENTO
    // =========================================================================

    /// Calcula la posición espacial en el mundo 3D para un slot especifico en un anillo determinado
    private Vector3 CalcularPosicion(AnilloAtaque anillo, int index)
    {
        // Calculamos los grados correspondientes al indice dividiendo los 360 grados del circulo equitativamente
        float angle = (360f / anillo.totalSlots) * index;

        // Convertimos los grados sexagesimales a radianes (multiplicando por Deg2Rad) para las funciones de C#
        float radians = angle * Mathf.Deg2Rad;

        // Generamos un vector de dirección unitario en el plano horizontal (XZ) con seno y coseno
        Vector3 direction = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));

        // Multiplicamos el vector direccion por el radio del anillo y le sumamos la posición actual del objetivo
        return transform.position + (direction * anillo.radio);
    }

    // =========================================================================
    // GIZMOS (DIBUJADO EN ESCENA)
    // =========================================================================

    /// Dibuja los slots en la vista de escena de Unity cuando el objeto esta seleccionado
    private void OnDrawGizmosSelected()
    {
        // Si el array de anillos no esta creado, cancelamos el renderizado de Gizmos
        if (anillos == null) return;

        // Paleta de colores para diferenciar visualmente cada anillo en la vista de escena
        Color[] colores = new Color[] { Color.red, Color.yellow, Color.cyan, Color.magenta };

        // Recorremos cada uno de los anillos para dibujarlos en pantalla
        for (int a = 0; a < anillos.Length; a++)
        {
            // Obtenemos la referencia del anillo en la iteracion actual
            AnilloAtaque anillo = anillos[a];

            // Asignamos el color rotativo correspondiente a la capa actual usando el operador modulo (%)
            Gizmos.color = colores[a % colores.Length];

            // Determinamos la cantidad de posiciones a dibujar atendiendo a si estamos en ejecucion o en editor
            int cantidadSlots = (anillo.slots != null && anillo.slots.Length > 0)
                                ? anillo.slots.Length
                                : anillo.totalSlots;

            // Recorremos cada slot del anillo
            for (int i = 0; i < cantidadSlots; i++)
            {
                // Obtenemos las coordenadas en el mundo para el slot
                Vector3 pos = CalcularPosicion(anillo, i);

                // Si el juego esta en marcha y el slot actual esta asignado a un atacante...
                if (Application.isPlaying && anillo.slots != null && anillo.slots[i] != null)
                {
                    // Dibujamos una esfera solida para indicar que la plaza esta OCUPADA
                    Gizmos.DrawSphere(pos, 0.15f);
                }
                else
                {
                    // Dibujamos un anillo/circulo de alambre para indicar que la plaza esta LIBRE
                    Gizmos.DrawWireSphere(pos, 0.2f);
                }
            }
        }
    }
}
