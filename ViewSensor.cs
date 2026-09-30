using UnityEngine;
using UnityEngine.AI;
using System;
using Unity.VisualScripting;

public class ViewSensor : MonoBehaviour
{
    public Action<GameObject> OnDetectionEnter;
    public Action OnDetectionExit;
    public float RangoActual => _rangoActual;
    public float Angle => _angle;
    [SerializeField] float _rangoActual = 5f;
    [SerializeField] float _angle = 45f;
    [SerializeField] LayerMask _detectionLayer;
    [SerializeField] LayerMask _detectionRaycast;
    [SerializeField] Collider[] _detectionBuffet;
    RaycastHit _lastHit;
    bool _hasDetection = false;
    bool _hasDetectionLastFrame = false;
    [SerializeField] GameObject _objetivoActual;

    public Action OnRangoChanged;
    
    void Awake()
    {
        _detectionBuffet = new Collider[10];
    }
    void FixedUpdate()
    {
        EscanearObjetivos();
        _hasDetectionLastFrame = _hasDetection;
    }
    public void CambioRango(float rangoAtaque)
    {
        Debug.Log("CambioRango");
        OnRangoChanged?.Invoke();
        _rangoActual = rangoAtaque;
    }
    private void EscanearObjetivos()
    {
        // ETAPA 1: BUSCAR CANDIDATOS EN LA ESFERA
        Array.Clear(_detectionBuffet, 0, _detectionBuffet.Length);
        int resultCount = Physics.OverlapSphereNonAlloc(transform.position, _rangoActual, _detectionBuffet, _detectionLayer);

        GameObject objetivoValido = null;
        float menorDistanciaSqr = Mathf.Infinity;

        for (int i = 0; i < resultCount; i++)
        {
            Collider col = _detectionBuffet[i];
            if (col == null || col.gameObject == gameObject) continue;

            // Filtro NavMesh: Comprobar que el enemigo esté activo
            if (col.TryGetComponent(out NavMeshAgent agent))
            {
                if (!agent.enabled || !agent.isOnNavMesh) continue;
            }

            // ETAPA 2: FILTRO DE ÁNGULO (CONO)
            Vector3 direction = col.bounds.center - transform.position;
            Vector3 directionAplanada = new Vector3(direction.x, 0, direction.z);
            float detectionAngle = Vector3.Angle(transform.forward, directionAplanada);

            if (detectionAngle <= _angle)
            {
                // ETAPA 3: RAYCAST CON MÁSCARA (Atraviesa tus invocaciones)
                // Usamos _raycastLayers para que solo choque con Muros o Enemigos
                if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hitInfo, _rangoActual * 2, _detectionRaycast))
                {
                    // Si el Raycast impacta en el enemigo (o en su jerarquía)
                    if (hitInfo.transform == col.transform || hitInfo.transform.IsChildOf(col.transform))
                    {
                        float distSqr = direction.sqrMagnitude;
                        if (distSqr < menorDistanciaSqr)
                        {
                            menorDistanciaSqr = distSqr;
                            objetivoValido = col.gameObject;
                        }
                    }
                }
            }
        }

        // GESTIÓN DE ESTADO Y EVENTOS
        if (_objetivoActual == null && objetivoValido != null)
        {
            _objetivoActual = objetivoValido;
            OnDetectionEnter?.Invoke(_objetivoActual);
        }
        else if (objetivoValido != _objetivoActual)
        {
            _objetivoActual = objetivoValido;
            if (_objetivoActual != null)
            {
                OnDetectionEnter?.Invoke(_objetivoActual);
            }
            else
            {
                OnDetectionExit?.Invoke();
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _rangoActual);
        Vector3 rightLimit = Quaternion.Euler(0f, _angle, 0f) * transform.forward;
        Vector3 leftLimit = Quaternion.Euler(0f, -_angle, 0f) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + rightLimit.normalized * _rangoActual);
        Gizmos.DrawLine(transform.position, transform.position + leftLimit.normalized * _rangoActual);
    }
}