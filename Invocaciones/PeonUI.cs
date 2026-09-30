using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // IMPRESCINDIBLE para detectar arrastre en UI
// Las interfaces IDragHandler e IEndDragHandler obligan al script a tener los métodos OnDrag y OnEndDrag
public class PeonUI : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public int idSlot;                  // El identificador único de este soldado en la formación (0, 1, 2...)
    public string _tipoSoldado;         // El nombre del soldado que representa; este peón
    private RectTransform _rectTransform; // El componente de posicionamiento UI de este objeto
    private System.Action<PeonUI, bool> _onMoved; // El canal (Action) para avisar al manager que nos hemos movido
    private System.Action<int> _onEliminar; // El canal (Action) para avisar al manager que nos hemos eliminado
    [SerializeField] TextMeshProUGUI posLista;

    [SerializeField] Button botonEliminar;
    // Método que llama el manager al instanciar este peón en la pantalla
    public void Inicializar(int id, System.Action<PeonUI, bool> onMovedCallback, System.Action<int> onEliminarCallback)
    {
        idSlot = id;
        posLista.text = (id + 1).ToString();
        _rectTransform = GetComponent<RectTransform>();
        _onMoved = onMovedCallback; // Guardamos la función del manager para llamarla luego
        _onEliminar = onEliminarCallback; // Guardamos la función del manager para llamarla luego

        if (botonEliminar != null)
        {
            botonEliminar.onClick.RemoveAllListeners(); // Limpiamos cualquier listener previo para evitar duplicados
            botonEliminar.onClick.AddListener(() => OnClickEliminar()); // Añadimos un nuevo listener que ejecutará la acción de eliminar el nodo
        }
    }
    void OnClickEliminar()
    {
        _onEliminar?.Invoke(idSlot);
    }
    // Este método se ejecuta AUTOMÁTICAMENTE en cada frame que mantengas pulsado y muevas el ratón sobre el peón
    public void OnDrag(PointerEventData eventData)
    {
        // 'eventData.delta' es la cantidad de píxeles que se ha movido el ratón en este frame (ej. +5 píxeles a la derecha).
        // Lo dividimos por la escala del Canvas principal ('transform.root.localScale.x') para que el arrastre 
        // funcione a la misma velocidad en cualquier resolución de pantalla (4K, 1080p, etc.).
        _rectTransform.anchoredPosition += eventData.delta / transform.root.localScale.x;

        // Invocamos el Action enviándonos a nosotros mismos (this) como argumento.
        // Esto hace que el ScriptableObject se actualice en tiempo real mientras arrastras.
        _onMoved?.Invoke(this, false);
    }

    // Este método se ejecuta AUTOMÁTICAMENTE en el frame exacto en el que sueltas el botón del ratón
    public void OnEndDrag(PointerEventData eventData)
    {
        // Avisamos una última vez al manager para asegurar que la posición final del peón quede bien guardada.
        _onMoved?.Invoke(this, true);
    }
}
