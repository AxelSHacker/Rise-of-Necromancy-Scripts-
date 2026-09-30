using System.Collections.Generic;
using UnityEngine;

public class EnemigoTeniente : EnemigoBase, IObservable<InvocacionesObservable>
{
    [SerializeField] float _radioInvocacion;
    [SerializeField] LayerMask _tropasReclutabes;
    [SerializeField] FormaciondeEjercito _formaciondeEjercito;
    [SerializeField] float _velocidadBusqueda = 3.5f;
    int _maxInvocaciones = 10;
    Collider[] _colliders = new Collider[10];
    private List<EnemigoBase> _soldadosReclutados = new List<EnemigoBase>();
    [SerializeField] float _timer = 1f;
    #region Funciones Unity
    void Start()
    {
        _indicePatrullaActual = UnityEngine.Random.Range(0, _puntosDePatrullaje.Length);
        _agenteEnemigo.avoidancePriority = UnityEngine.Random.Range(0, 99);
        _vidaActual = _vidaMaxima;
        _agenteEnemigo.updateRotation = false;
        _ultimaPosicionCapitan = transform.position;

        if (TryGetComponent(out FormaciondeEjercito miFormacion))
        {
            miFormacion._hordaJugador = false;
        }
        this.MuerteEnemigos();
    }
    void Update()
    {
        if (!IsActive || _muerto) return;
        gameObject.AnimatorController(_anim, _agenteEnemigo);

        if (_timer <= 0f)
        {
            Reclutamiento();
            _timer = 1f;
        }
        else
        {
            _timer -= Time.deltaTime;
        }
    }
    private void Reclutamiento()
    {
        // Buscamos soldados alrededor del Teniente
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, _radioInvocacion, _colliders, _tropasReclutabes);

        if (numColliders <= 0) return;

        for (int i = 0; i < numColliders; i++)
        {
            Collider col = _colliders[i];
            if (col.transform == transform) continue;

            if (col.TryGetComponent(out EnemigoBase eBase))
            {
                // Si el soldado ya está muerto o ya pertenece a este Teniente, lo ignoramos
                if (eBase.IsDead || eBase._posicionCapitan == transform) continue;

                //: EJECUCIÓN DEL RECLUTAMIENTO: Extraemos el GameObject del soldado
                GameObject nuevoSoldado = eBase.Reclutar(transform);

                if (nuevoSoldado != null && nuevoSoldado.TryGetComponent(out EnemigoBase ia))
                {
                    // Añadimos el soldado a la lista lógica de la horda del teniente
                    _soldadosReclutados.Add(ia);
                    ia.SacarFormacionEjercito(_formaciondeEjercito);
                    //: VINCULACIÓN A LA FORMACIÓN: Registramos físicamente al nuevo recluta en el script de formación de este Teniente.
                    if (_formaciondeEjercito != null)
                    {
                        //: Registramos al soldado para que la formación le calcule su slot de escolta alrededor del Teniente
                        _formaciondeEjercito.OnZombieResucitado(nuevoSoldado);
                        _maxInvocaciones++;
                    }

                    // Sincronizamos su evento de deceso para limpiar el ejército
                    ia.OnDeath += () =>
                    {
                        _soldadosReclutados.Remove(ia);
                        if (_formaciondeEjercito != null)
                        {
                            _formaciondeEjercito.OnZombieMuerto(nuevoSoldado); // Lo removemos del slot de formación
                            _maxInvocaciones--;
                        }
                    };
                }

                // Notificación al patrón Observable (Tu sistema de eventos con el Nigromante/Player)
                if (_observable != null)
                {
                    for (int o = 0; o < _observable.Count; o++)
                    {
                        _observable[o].OnZombieResucitado(nuevoSoldado);
                    }
                }
            }
        }
    }
    public void MoverEnFormacion(Vector3 direccionAtaque)
    {
        direccionAtaque.y = 0f;
        direccionAtaque.Normalize();

        Vector3 nuevoDestino = transform.position + (direccionAtaque * _distanciaAvanceenFormacion);

        _isPatrolling = false;

        if (_agenteEnemigo != null && _agenteEnemigo.isActiveAndEnabled)
        {
            _agenteEnemigo.speed = _velocidadBusqueda;
            _agenteEnemigo.SetDestination(nuevoDestino);
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radioInvocacion);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _radioBusqueda);
    }
    private List<InvocacionesObservable> _observable;
    public void AddObservable(InvocacionesObservable observable)
    {
        if (_observable == null)
        {
            _observable = new List<InvocacionesObservable>();
        }

        if (_observable.Contains(observable)) return;

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
}
