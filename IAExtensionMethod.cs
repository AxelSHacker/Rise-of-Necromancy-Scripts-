using UnityEngine;
using UnityEngine.AI;

public static class IAExtensionMethod
{
    #region Funciones Comunes
    public static void CalcularVelocidadObjetivo(this GameObject thisiA, Transform posicionObjetivo, ref Vector3 ultimaPosicionObjetivo, ref float velocidadObjetivo)
    {
        if (Time.deltaTime == 0 || Time.timeScale == 0) return;

        if (posicionObjetivo == null) return;

        // SOLUCIÓN: Si es el primer frame o la posición se quedó a cero, 
        // sincronizamos la posición sin calcular velocidad para evitar el pico gigante.
        if (ultimaPosicionObjetivo == Vector3.zero)
        {
            ultimaPosicionObjetivo = posicionObjetivo.position;
            velocidadObjetivo = 0f; // Arranca parado
            return;
        }

        // Calculamos cuánto se movió el player de forma real
        float distanciaRecorrida = Vector3.Distance(posicionObjetivo.position, ultimaPosicionObjetivo);

        // Evitamos micro-picos extraños si el juego da un tirón de frames lógico
        float velocidadInstantanea = distanciaRecorrida / Time.deltaTime;

        // Suavizamos el valor
        velocidadObjetivo = Mathf.Lerp(velocidadObjetivo, velocidadInstantanea, Time.deltaTime * 10f);

        // Guardamos la posición actual para el siguiente frame
        ultimaPosicionObjetivo = posicionObjetivo.position;
    }
    public static Transform BusquedadeObjeto(this GameObject thisIA, float radioBusqueda, LayerMask objetivosbuffer, LayerMask _detectionRaycast)
    {
        // 1. Creamos un "radar" físico que solo detecta colliders en las capas seleccionadas
        Collider[] posiblesObjetivos = Physics.OverlapSphere(thisIA.transform.position, radioBusqueda, objetivosbuffer);

        if (posiblesObjetivos.Length == 0) return null;

        Transform objetivoMasCercano = null;
        float distanciaMinima = Mathf.Infinity; // Empezamos con una distancia infinita
        Vector3 posicionIA = thisIA.transform.position;

        // 2. Iteramos sobre los colliders detectados para encontrar el más cercano
        foreach (Collider col in posiblesObjetivos)
        {
            //Validacion de componentes
            bool esAgenteValido = col.TryGetComponent(out NavMeshAgent navMes) && navMes.enabled;
            bool esPlayerValido = col.TryGetComponent(out CharacterController controller) && controller.enabled;
            bool objetoDestruible = col.TryGetComponent(out HouseController destructible) && destructible.enabled;

            // Si no tiene ninguno de los dos componentes activos, lo ignoramos
            if (!esAgenteValido && !esPlayerValido && !objetoDestruible) continue;
            Vector3 direction = col.bounds.center - thisIA.transform.position;
            // Opcional: Si el script está en el mismo objeto que el collider, nos ignoramos a nosotros mismos
            if (Physics.Raycast(thisIA.transform.position, direction.normalized, out RaycastHit hitInfo, radioBusqueda, _detectionRaycast))
            {
                // Si el Raycast impacta en el enemigo (o en su jerarquía)
                if (hitInfo.transform == col.transform || hitInfo.transform.IsChildOf(col.transform))
                {
                    float distSqr = direction.sqrMagnitude;
                    if (distSqr < distanciaMinima)
                    {
                        distanciaMinima = distSqr;
                        objetivoMasCercano = col.transform;
                    }
                }
            }
        }
        // 5. Devolvemos el objetivo más cercano encontrado, o null si no se detectó ninguno
        return objetivoMasCercano;
    }
    /// Control seguro de disparo de animaciones de ataque sin spammear Triggers
    private static void EjecutarAnimacionAtaque(Animator anim, string attackName)
    {
        if (anim == null) return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        int _tipoAtaque = 2;
        // Dispara la animacion SOLO si no estamos ya reproduciendo esa animación ni pasando a ella
        if (anim.TryGetComponent(out InvocacionArco invocacionArco) && !stateInfo.IsName(attackName) && !anim.IsInTransition(0))
        {
            anim.SetTrigger(attackName);
        }
        else if (!stateInfo.IsName(attackName) && !anim.IsInTransition(0))
        {
            // if(anim.TryGetComponent(out InvocacionAlabarda invocacionAlabarda) && invocacionAlabarda)
            // {
            //     _tipoAtaque = Random.Range(0, 2);
            //     anim.SetInteger(attackName, _tipoAtaque);
            // }

            _tipoAtaque = Random.Range(0, 2);
            anim.SetInteger(attackName, _tipoAtaque);
        }
    }
    public static void RotacionHaciaObjetivo(this GameObject thisIA, Transform objetivo)
    {
        // Calculamos la dirección hacia el objetivo y la rotación necesaria para mirarlo
        Vector3 dirrecion = objetivo.position - thisIA.transform.position;
        dirrecion.y = 0;

        // Si la dirección es muy pequeña, no rotamos para evitar problemas de rotación
        if (dirrecion.sqrMagnitude < 0.01f) return;

        //Calculamos la dirección hacia el objetivo y la rotación necesaria para mirarlo
        Quaternion direccionRotacion = Quaternion.LookRotation(dirrecion);

        if (Quaternion.Angle(thisIA.transform.rotation, direccionRotacion) > 2.0f)
        {
            // Rotamos suavemente hacia el objetivo usando Slerp para una transición suave
            thisIA.transform.rotation = Quaternion.Slerp(thisIA.transform.rotation,
                                                               direccionRotacion,
                                                               Time.deltaTime * 8f);
        }
    }
    public static void RotacionDirreccionObjetivo(this GameObject thisIA, Vector3 objetivo, NavMeshAgent agent, float MultiplicadorRotacion)
    {
        // Calculamos la dirección hacia el objetivo y la rotación necesaria para mirarlo
        Vector3 dirrecion = agent.steeringTarget - thisIA.transform.position;
        dirrecion.y = 0;

        // Si la dirección es muy pequeña, no rotamos para evitar problemas de rotación
        if (dirrecion.sqrMagnitude < 0.01f) return;

        //Calculamos la dirección hacia el objetivo y la rotación necesaria para mirarlo
        Quaternion direccionRotacion = Quaternion.LookRotation(dirrecion);
        if (Quaternion.Angle(thisIA.transform.rotation, direccionRotacion) > 2.0f)
        {
            // Rotamos suavemente hacia el objetivo usando Slerp para una transición suave
            thisIA.transform.rotation = Quaternion.Slerp(thisIA.transform.rotation,
                                                               direccionRotacion,
                                                               Time.deltaTime * MultiplicadorRotacion * 8f);
        }
    }
    public static void RotacionDeObjetivo(this GameObject thisIA, Transform objetivo)
    {
        if (objetivo == null) return;

        // Calculamos la dirección hacia el objetivo y la rotación necesaria para mirarlo
        Vector3 dirrecion = objetivo.forward;
        dirrecion.y = 0;

        //Si al direcion a la que se mira es 0 pasamos para eliminar errores
        if (dirrecion.sqrMagnitude < 0.001f) return;

        //Calculamos la dirección hacia el objetivo y la rotación necesaria para mirarlo
        Quaternion direccionRotacion = Quaternion.LookRotation(dirrecion);

        //Calculamos el angulo entre la rotacion actual y la deseada
        float angulo = Quaternion.Angle(thisIA.transform.rotation, direccionRotacion);

        //Si el angulo es muy pequenio no rotamos mas
        if (angulo < 2f) return;

        //Si estas de espaldas aumentamos la velocidad locamente
        float velovidadBaseGiro = (angulo > 150f) ? 800f : 450f;
        //Rotacion mas continua mejor que suabizada
        thisIA.transform.rotation = Quaternion.RotateTowards(thisIA.transform.rotation,
                                                      direccionRotacion,
                                                      velovidadBaseGiro * Time.deltaTime);
    }
    public static void Apuntado(this GameObject thisIA, Transform objetivo, Animator animator, NavMeshAgent agent,
                               float anguloDesface, float distanciaAtaque, string triggerName, string estaApuntando)
    {
        // 1. FILTRO DE SEGURIDAD: Si la posición del objetivo es (0,0,0) (vacía/no válida), sale de la función para evitar fallos.
        if (objetivo == null) return;
        // Configura la distancia de parada del NavMeshAgent usando el rango de ataque del arma a distancia.
        agent.stoppingDistance = distanciaAtaque;
        // Sube la calidad del esquema de esquivado del agente para que no se choque bruscamente con otros aliados.
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // Le ordena al agente caminar hacia la posición del objetivo.
        agent.SetDestination(objetivo.position);

        // Calculamos el vector de dirección restando la posición del objetivo menos la posición de la IA.
        Vector3 direccionObjetivo = objetivo.position - thisIA.transform.position;

        // Forzamos a que la Y sea 0 para que la IA no rote hacia arriba o hacia abajo si el objetivo está en una cuesta.
        direccionObjetivo.y = 0f;

        // Si la distancia al cuadrado hacia el objetivo es mayor que un mínimo (para evitar que rote como loco si está encima de él)...
        if (direccionObjetivo.sqrMagnitude > 0.1f)
        {
            // ...calcula la rotación matemática pura necesaria para mirar directamente al frente del objetivo.
            Quaternion rotacionHaciaObjetivo = Quaternion.LookRotation(direccionObjetivo);
            // Crea una rotación de desfase basada en el ángulo (por ejemplo, 90 grados si el arquero dispara de perfil).
            Quaternion desfacePerfil = Quaternion.Euler(0f, anguloDesface, 0f);

            // Multiplica las rotaciones para combinar el mirar al enemigo con el desfase de perfil (el orden de multiplicación importa en Quaternions).
            Quaternion rotacionFianl = rotacionHaciaObjetivo * desfacePerfil;

            // Aplica una rotación suave e interpolada (Slerp) desde la rotación actual de la IA hacia la rotación final calculada.
            thisIA.transform.rotation = Quaternion.Slerp(thisIA.transform.rotation, rotacionFianl, Time.deltaTime * 15f);
        }
        if (Vector3.Distance(thisIA.transform.position, objetivo.position) <= distanciaAtaque)
        {
            agent.isStopped = true;
            animator.SetBool(estaApuntando, true);
            // Para evitar los "ataques fantasmas" de tus arqueros al morir el enemigo, cámbialo por esto:
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(triggerName) && !animator.IsInTransition(0))
            {
                animator.SetTrigger(triggerName);
            }
        }
        else
        {
            agent.isStopped = false;
            animator.SetBool(estaApuntando, false);
        }

    }
    public static void AnimatorController(this GameObject thisIA, Animator animator, NavMeshAgent agent)
    {
        animator.SetFloat("Velocidad", agent.velocity.magnitude);
    }
    public static bool TieneLineaDeVision(this GameObject thisIA, Transform objetivo, float radioBusqueda, LayerMask detectionRaycast)
    {
        if (objetivo == null) return false;

        // Validación de componentes
        bool esAgenteValido = objetivo.TryGetComponent(out NavMeshAgent navMes) && navMes.enabled;
        bool esPlayerValido = objetivo.TryGetComponent(out CharacterController controller) && controller.enabled;
        bool objetoDestruible = objetivo.TryGetComponent(out HouseController destructible) && destructible.enabled;

        if (!esAgenteValido && !esPlayerValido && !objetoDestruible) return false;

        // Origen a la altura de los ojos (evita el suelo y los pies)
        Vector3 origenRay = thisIA.transform.position + Vector3.up * 1.0f;
        Vector3 centroObjetivo = objetivo.position + Vector3.up * 1.0f;

        if (objetivo.TryGetComponent(out Collider col))
        {
            centroObjetivo = col.bounds.center;
        }

        Vector3 direccion = centroObjetivo - origenRay;
        float distanciaAlObjetivo = direccion.magnitude;

        if (distanciaAlObjetivo > radioBusqueda) return false;

        // Ignorar si estamos literalmente pegados (evita fallos de precisión matemática a distancia cero)
        if (distanciaAlObjetivo < 0.2f) return true;

        // Trazar el Raycast desde el origen desplazado
        if (Physics.Raycast(origenRay, direccion.normalized, out RaycastHit hitInfo, radioBusqueda, detectionRaycast))
        {
            return hitInfo.transform == objetivo || hitInfo.transform.IsChildOf(objetivo);
        }

        return false;
    }
    #endregion





    #region Funciones Exclusivas de las invocaciones
    public static void SetDestination(this InvocacionBase iA, Vector3 destinationPoint)
    {
        iA._posicionEnFormacion = destinationPoint;

        if (!iA._muerto && !iA._enCombate)
        {
            iA._agente.SetDestination(destinationPoint);
        }
    }
    public static void BaseInvocaciones(this InvocacionBase iA)
    {
        iA._agente.updateRotation = false;
        iA._agente.acceleration = 100f;
        iA._agente.autoBraking = false;
        iA._agente.stoppingDistance = iA.DistanciaAtaque;
        iA._agente.angularSpeed = 0;
    }
    public static void MovimientoInvocaciones(this InvocacionBase iA, Vector3 destino, float velocidadReferencia)
    {
        if (iA._agente == null) return;

        float distancia = Vector3.Distance(iA.transform.position, iA._posicionEnFormacion);

        iA._agente.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        iA._agente.stoppingDistance = 0f;

        // 1. ESTADO DE PARADA: Cuando el pivote/jugador se frena del todo y la tropa está en sitio
        if (velocidadReferencia < 0.1f && distancia <= iA._umbraDeMovimiento)
        {
            iA._agente.speed = 0f;
            iA.gameObject.RotacionDeObjetivo(iA._posicionModelo);
            return;
        }
        // 2. ESTADO DE MOVIMIENTO Y CARGA
        if (iA._estaOrientado)
        {
            // Tomamos la velocidad más alta relevante para asegurar que en la carga a enemigos mantenga el ritmo
            float velocidadBase = Mathf.Max(velocidadReferencia, iA._velocidadPlayer);

            float velocidadPersonal = velocidadBase * iA.multiplicadodorVelocidad;

            float velocidadDeseada;
            if (distancia > iA._umbraDeMovimiento)
            {
                // Transición suave basada en la distancia sin saltos bruscos de velocidad
                float factorDistancia = distancia - iA._umbraDeMovimiento;
                velocidadDeseada = velocidadBase + (factorDistancia * iA.multiplicadorDeRecuperacion);

                // Limitamos a un máximo razonable de Sprint
                velocidadDeseada = Mathf.Min(velocidadDeseada, velocidadBase * 1.4f);
            }
            else
            {
                // Dentro de la distancia objetivo, mantiene la velocidad de marcha/carga sin frenazos bruscos
                velocidadDeseada = velocidadPersonal;
            }
            iA.gameObject.RotacionDirreccionObjetivo(destino, iA._agente, iA.multiplicadorRotacion);
            // Aplicación directa e instantánea sin Lerps desfasados por deltaTimes
            iA._agente.speed = velocidadDeseada;
        }
    }
    public static void MuerteInvocacion(this InvocacionBase iA)
    {
        //Activamos al boleana que hacer que el upddate deje de funcionar y se quede quieto
        iA._muerto = true;
        //Activamos la animacion de muerte
        iA._anim.SetBool("Muerte", true);
        iA._anim.SetLayerWeight(1, 0f);
        //Desactivamos el agente para que no intente moverse
        iA._agente.isStopped = true;
        //Desactivamso el collider para que no moleste al moverse
        iA._capsuleCollider.enabled = false;
        //Buscamos el componenente de la formacion para eliminarnos de la formacion
        if (iA._posicionNigromante != null)
        {
            if (iA._posicionNigromante.TryGetComponent(out FormaciondeEjercito formaciondeEjercito))
            {
                formaciondeEjercito.OnZombieMuerto(iA.gameObject);
            }
        }

        iA.OnDeath?.Invoke();
    }
    public static void Resucitar(this InvocacionBase iA)
    {
        // 1. Accedemos directamente a los campos del script 'inv'
        iA._muerto = false;
        iA._anim.SetLayerWeight(1, 1f);

        // 2. Usamos el gameObject que ya pertenece al componente
        iA.gameObject.layer = iA._capaZombie;

        // 3. Gestión del Agente
        if (iA._agente != null)
        {
            iA._agente.enabled = true;
            if (iA._agente.isOnNavMesh)
            {
                iA._agente.ResetPath();
            }
        }

        // 4. Visuales y Vida
        if (iA._anim != null)
        {
            iA._anim.SetBool("Muerte", false);
        }
        iA._vidaActual = iA._vidaMaxima;

        // 5. Lógica de Capas (Bitwise operations)
        // Agregamos la capa de enemigos a sus objetivos
        iA._capaObjetivos |= (1 << iA._capaEnemigo);

        // 6. Configuración del Collider
        if (iA._capsuleCollider != null)
        {
            iA._capsuleCollider.excludeLayers = LayerMask.GetMask("Zombie", "Player");
        }
    }
    public static void InvocacionAtacarObjetivo(this InvocacionBase iA, bool enFormacion = false)
    {
        // 1. Guardas de seguridad
        if (iA == null || iA._enemigo == null) return;
        iA._estaOrientado = true;
        iA._agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // Aseguramos la gestión de slots del objetivo actual
        iA.GestionarCacheSlots();

        Vector3 posAtacante = iA.transform.position;
        Vector3 posCentroEnemigo = iA._enemigo.position;
        bool slotsAsignados = false;
        Vector3 puntoDestino = posCentroEnemigo; // Destino base si no hay slots
        // =========================================================================
        // 2. CONSULTA Y ASIGNACIÓN DE SLOTS TÁCTICOS (Solo si NO está en formación)
        // =========================================================================
        if (!enFormacion)
        {
            iA.GestionarCacheSlots();
            ObjetivoConSlotsAnillos gestorSlots = iA.SlotsdelObjetivoActual;

            if (gestorSlots != null)
            {
                int indiceSlot = iA.ObtenerIndiceSlot();
                // Pide o confirma su slot libre más cercano
                Vector3? posSlot = gestorSlots.PedirSlots(iA.gameObject, indiceSlot);

                if (posSlot.HasValue)
                {
                    puntoDestino = posSlot.Value;
                    slotsAsignados = true;
                }
                else
                {
                    // Si el objetivo está saturado, calcula posición en la cola de espera
                    slotsAsignados = false;
                    Vector3 dirEspera = (posAtacante - posCentroEnemigo).normalized;
                    if (dirEspera == Vector3.zero) dirEspera = -iA.transform.forward;
                    puntoDestino = posCentroEnemigo + dirEspera * (iA._distanciaAtaque + 1.5f);
                }
            }
        }

        // =========================================================================
        // 3. COMPROBACIÓN DE LLEGADA AL DESTINO
        // =========================================================================
        bool enPosicionAtaque = false;

        // Distancia directa contra el centro del objetivo (para validar si llega el golpe)
        float distSqrAlCentro = (posAtacante - posCentroEnemigo).sqrMagnitude;
        float distSqrAlSlot = (posAtacante - puntoDestino).sqrMagnitude;

        float rangoAtaqueSqr = Mathf.Pow(iA._distanciaAtaque + 0.4f, 2); // Rango de ataque con pequeño margen
        float margenLlegadaalSlot = 0.4f; // (Aumentado a 0.9m de margen en vez de 0.5m)

        if (!enFormacion && slotsAsignados)
        {
            if (distSqrAlSlot <= margenLlegadaalSlot)
            {
                enPosicionAtaque = true;
            }
            else if (distSqrAlCentro <= rangoAtaqueSqr && (iA._agente.velocity.sqrMagnitude < 0.1f || !iA._agente.hasPath))
            {
                enPosicionAtaque = true; // Consideramos que está en posición de ataque si está dentro del rango de ataque y casi detenido
            }
        }
        else
        {
            // Ataque en formación o sin gestor de slots: usa distancia al centro
            enPosicionAtaque = distSqrAlCentro <= rangoAtaqueSqr;
        }

        // =========================================================================
        // 4. EJECUCIÓN DE ATAQUE O NAVEGACIÓN
        // =========================================================================
        if (enPosicionAtaque)
        {
            // Frenar la unidad sobre el slot/posición
            if (iA._agente != null && iA._agente.isOnNavMesh)
            {
                iA._agente.isStopped = true;
            }

            // Orientar la mirada hacia el centro del enemigo
            Vector3 direccion = posCentroEnemigo - posAtacante;
            direccion.y = 0f;

            if (direccion != Vector3.zero)
            {
                iA.transform.rotation = Quaternion.LookRotation(direccion);
            }

            EjecutarAnimacionAtaque(iA._anim, iA._triggerName);
        }
        else if (!enFormacion)
        {
            // Navegación individual hacia el slot asignado
            if (iA._agente != null && iA._agente.isOnNavMesh)
            {
                iA._agente.isStopped = false;
                iA._agente.SetDestination(puntoDestino);
            }

            //iA.MovimientoInvocaciones(puntoDestino, iA._velocidaDeCrucero);
        }
    }
    public static void LogicaResureccion(this InvocacionBase iA, Vector3 destino)
    {
        if (destino == Vector3.zero) return;
        Vector3 direccionalDestino = destino - iA.transform.position;
        direccionalDestino.y = 0f;
        iA._anim.Rebind();

        if (direccionalDestino.magnitude < 0.2f)
        {
            iA._estaOrientado = true;
            iA._agente.isStopped = false;
            return;
        }

        if (direccionalDestino != Vector3.zero)
        {
            //Calculamos la rotacion
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionalDestino);
            //Rotamos 
            iA.transform.rotation = Quaternion.Slerp(iA.transform.rotation, rotacionObjetivo, Time.deltaTime * 10f);
            //Angulos que faltan para llegar a la rotacion que busco
            float anguloFaltante = Quaternion.Angle(iA.transform.rotation, rotacionObjetivo);
            //Frotera para activar el movimiento
            if (anguloFaltante < 5f)
            {
                iA._estaOrientado = true;
                iA._agente.isStopped = false;
            }
        }
        //Si el bool se activa nos movemos a donde deberiamos ir
        if (iA._estaOrientado)
        {
            iA._anim.SetTrigger("Resucitado");
            iA.MovimientoInvocaciones(destino, iA._velocidaDeCrucero);
        }
        //destativamos el movimiento del NavMesh para que no se lie el solo
        else
        {
            iA._agente.isStopped = true;
            iA._agente.velocity = Vector3.zero;
        }
    }
    public static void ConfigurarSalirDelPool(this InvocacionBase iA, Transform lider, Vector3 puntoFormacion, Transform modelo)
    {
        iA._posicionNigromante = lider;
        iA._posicionModelo = modelo;
        iA._posicionEnFormacion = puntoFormacion;
        iA._ultimaPosicionPlayer = lider.position;
        iA._nigromanteController = lider.GetComponent<CharacterController>();
        iA._velocidadPlayer = 0;
        iA._ultimaPosicionPlayer = lider.position;
        iA._agente.speed = 0;
        iA._agente.Warp(iA.transform.position);
        iA.Resucitar();
    }
    #endregion





    #region Funciones Exclusivas de los enemigos
    public static void SetDestinationEnemies(this EnemigoBase iA, Vector3 destinationPoint)
    {
        if (iA.IsDead || iA._enCombate) return;

        iA._posicionEnFormacion = destinationPoint;

        if (iA._agenteEnemigo != null && iA._agenteEnemigo.enabled)
        {
            iA._agenteEnemigo.SetDestination(destinationPoint);
        }
    }
    public static void SeguirTeniente(this EnemigoBase iA)
    {
        if (Time.deltaTime == 0 || Time.timeScale == 0) return;

        float distancia = Vector3.Distance(iA.transform.position, iA._posicionEnFormacion);

        //: 2. Condición de parada (Si el teniente está quieto y el soldado llegó a su silla)
        if (distancia < 0.2f)
        {
            iA._agenteEnemigo.speed = 0f;
            iA.gameObject.RotacionDirreccionObjetivo(iA._posicionEnFormacion, iA._agenteEnemigo, iA.MultiplicadorRotacion);
        }
        else
        {
            //: 3. A TODO GAS: El soldado está lejos de su posición asignada
            if (distancia > 1f)
            {
                // Compara la velocidad del teniente contra el Sprint de 6f para recuperar terreno rápido
                iA._agenteEnemigo.speed = Mathf.Max(iA._velocidadCapitan, 5f);
                iA.gameObject.RotacionDirreccionObjetivo(iA._posicionEnFormacion, iA._agenteEnemigo, iA.MultiplicadorRotacion);
            }
            //: 4. AMORTIGUACIÓN: El soldado está llegando a su silla
            else
            {
                // Suaviza la velocidad para igualar EXACTAMENTE la del Teniente, evitando tirones
                iA._agenteEnemigo.speed = Mathf.Lerp(iA._agenteEnemigo.speed, iA._velocidadCapitan, distancia / 0.4f);

                // Va encarando el frente del Teniente antes de pisar el punto final
                iA.gameObject.RotacionDeObjetivo(iA._posicionCapitan);
            }
        }

        // Le mandamos al NavMeshAgent que siga el slot calculado de la formación
        iA._agenteEnemigo.SetDestination(iA._posicionEnFormacion);
    }
    public static void EnemigoAtacarObjetivo(this EnemigoBase enemisIA, bool enFormacion = false)
    {
        // 1. Guardas de seguridad
        if (enemisIA == null || enemisIA._agenteEnemigo == null || enemisIA._objetivoFinal == null) return;

        if (enemisIA._agenteEnemigo.isOnNavMesh)
        {
            enemisIA._agenteEnemigo.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        Vector3 posAtacante = enemisIA.transform.position;
        Vector3 posCentroEnemigo = enemisIA._objetivoFinal.position;

        Vector3 puntoDestino = posCentroEnemigo; // Destino base si no hay slots
        bool slotsLlenos = false;
        ObjetivoConSlotsAnillos gestorSlots = null;

        // =========================================================================
        // 2. CONSULTA Y ASIGNACIÓN DE SLOTS TÁCTICOS (Solo si NO está en formación)
        // =========================================================================
        if (!enFormacion)
        {
            enemisIA.GestionarCacheSlots();
            gestorSlots = enemisIA.SlotsdelObjetivoActual;

            if (gestorSlots != null)
            {
                int indiceSlot = enemisIA.ObtenerIndiceSlot();
                // Solicita o confirma el slot libre más cercano
                Vector3? posSlot = gestorSlots.PedirSlots(enemisIA.gameObject, indiceSlot);

                if (posSlot.HasValue)
                {
                    puntoDestino = posSlot.Value;
                }
                else
                {
                    // SI TODOS LOS SLOTS ESTÁN OCUPADOS:
                    slotsLlenos = true;

                    // Calculamos un punto de espera seguro en el perímetro
                    Vector3 dirEspera = (posAtacante - posCentroEnemigo).normalized;
                    if (dirEspera == Vector3.zero) dirEspera = -enemisIA.transform.forward;
                    puntoDestino = posCentroEnemigo + dirEspera * (enemisIA._distanciaAtaque + 1.5f);
                }
            }
        }

        // =========================================================================
        // 3. COMPROBACIÓN DE LLEGADA AL DESTINO
        // =========================================================================
        bool enPosicionAtaque = false;

        // Distancia directa contra el centro del objetivo (para validar si llega el golpe)
        float distSqrAlCentro = (posAtacante - posCentroEnemigo).sqrMagnitude;
        float rangoAtaqueSqr = Mathf.Pow(enemisIA._distanciaAtaque + 0.4f, 2); // Rango de ataque con pequeño margen

        if (!enFormacion && gestorSlots != null && !slotsLlenos)
        {
            // Medición contra la coordenada del slot asignado
            float distSqrAlSlot = (posAtacante - puntoDestino).sqrMagnitude;

            // Aumentamos el margen de llegada al slot a 0.6m (0.36f al cuadrado)
            float margenLlegadaSlotSqr = 0.13f;

            bool llegoAlSlot = distSqrAlSlot <= margenLlegadaSlotSqr;
            bool estaEnRangoGolpe = distSqrAlCentro <= rangoAtaqueSqr;

            // Está en posición si tocó el slot O si ya está a distancia de golpear al objetivo
            enPosicionAtaque = llegoAlSlot || estaEnRangoGolpe;
        }
        else
        {
            enPosicionAtaque = distSqrAlCentro <= rangoAtaqueSqr;
        }

        // =========================================================================
        // 4. EJECUCIÓN: ATAQUE EN POSICIÓN vs NAVEGACIÓN
        // =========================================================================
        if (enPosicionAtaque)
        {
            // Frenar la unidad sobre su slot o posición de ataque
            if (enemisIA._agenteEnemigo != null && enemisIA._agenteEnemigo.isOnNavMesh)
            {
                enemisIA._agenteEnemigo.isStopped = true;
            }

            // Girar la mirada hacia el objetivo
            Vector3 direccion = posCentroEnemigo - posAtacante;
            direccion.y = 0f;

            if (direccion != Vector3.zero)
            {
                enemisIA.transform.rotation = Quaternion.LookRotation(direccion);
            }

            EjecutarAnimacionAtaque(enemisIA._anim, enemisIA._triggerName);
        }
        else if (!enFormacion)
        {
            // Mover el enemigo hacia el slot asignado
            if (enemisIA._agenteEnemigo != null && enemisIA._agenteEnemigo.isOnNavMesh)
            {
                enemisIA._agenteEnemigo.isStopped = false;
                enemisIA._agenteEnemigo.stoppingDistance = 0.1f;
                enemisIA._agenteEnemigo.SetDestination(puntoDestino);
            }

            enemisIA.RotacionEnemigos(puntoDestino);
        }
    }
    public static GameObject Reclutar(this EnemigoBase enemisIA, Transform lider)
    {
        // Comprobamos si el líder que llama tiene el script del Teniente
        if (lider.TryGetComponent(out EnemigoTeniente teniente))
        {
            // Si el soldado ya estaba muerto, el teniente no puede reclutarlo
            if (enemisIA.IsDead) return null;

            // // Aquí cambiamos el estado del enemigo para que sepa que ahora obedece al Teniente
            enemisIA._posicionCapitan = lider;
            enemisIA.StateMaschineController.RequestState("Formacion");


            // Devolvemos el propio GameObject del soldado vivo, porque NO ha muerto
            return enemisIA.gameObject;
        }
        //Asumimos que el líder es el jugador o que se va a resucitar
        if (enemisIA._capaObjetivos == enemisIA._capaZombie) return null;

        InvocacionBase resucitar = PoolManager.Instance.Pull(enemisIA._tipoInvocacion, enemisIA.transform.position, Quaternion.identity) as InvocacionBase;
        if (resucitar != null)
        {
            resucitar.ConfigurarSalirDelPool(lider, lider.position, lider.GetChild(0));

            GameObject zombieLevantado = resucitar.gameObject;

            //Aviso para el sistema de misiones: este cadaver concreto acaba de ser resucitado
            enemisIA.OnResucitado?.Invoke();

            //Devolvemos el enemigo al Pool
            enemisIA.ReturnToPool();

            return zombieLevantado;
        }
        return null;
    }
    public static void BusquedaContrarios(this EnemigoBase enemisIA)
    {
        Transform amenazaCercana = enemisIA.gameObject.BusquedadeObjeto(enemisIA._radioBusqueda, enemisIA._capaObjetivos, enemisIA._capaRaycast);

        // 2. MÁQUINA DE ESTADOS TÁCTICA
        if (amenazaCercana != null)
        {
            if (enemisIA._objetivoFinal != null && enemisIA._objetivoFinal != amenazaCercana)
            {
                // Forzamos la liberación del slot del enemigo viejo inmediatamente para evitar el enganche
                if (enemisIA._objetivoFinal.TryGetComponent(out ObjetivoConSlotsAnillos gestorViejo))
                {
                    gestorViejo.LiberarSlots(enemisIA.gameObject);
                }
            }
            // Prioridad absoluta: Si hay un zombie o el jugador cerca, ese es nuestro objetivo final
            enemisIA._objetivoFinal = amenazaCercana;
            enemisIA._posicionCapitan = null;
        }
        else
        {
            // Comprobamos si este soldado pertenece al grupo de un Teniente
            // Nota: Asegúrate de que tu variable en EnemigoBase se llame '_posicionTeniente' o '_posicionNigromante' (el líder que lo recluta)
            if (enemisIA._posicionCapitan != null)
            {
                // No patrullamos por libre: volvemos a mirar a nuestro jefe y marchamos en formación
                enemisIA._objetivoFinal = null;
            }
            else
            {
                // --- CASO C: SIN LÍDER Y SIN AMENAZAS (PATRULLA PASIVA) ---
                // Si no tiene jefe que lo comande ni nadie a quien pegar, limpia datos y camina por sus waypoints
                enemisIA._objetivoFinal = null;
            }
        }
    }
    public static void Patrullar(this EnemigoBase enemisIA)
    {
        //Comprobacion
        if (enemisIA._puntosDePatrullaje.Length == 0)
        {
            return;
        }
        //Asignamos indice actua
        enemisIA.RotacionEnemigos(enemisIA._puntosDePatrullaje[enemisIA._indicePatrullaActual].position);

        if (!enemisIA._agenteEnemigo.pathPending && enemisIA._agenteEnemigo.remainingDistance <= enemisIA._agenteEnemigo.stoppingDistance)
        {
            //aniadimos un nuevo indice y destino
            enemisIA._indicePatrullaActual = (enemisIA._indicePatrullaActual + 1) % enemisIA._puntosDePatrullaje.Length;
            enemisIA._estaOrientado = false;

            enemisIA._agenteEnemigo.SetDestination(enemisIA._puntosDePatrullaje[enemisIA._indicePatrullaActual].position);
        }
    }
    public static void RotacionEnemigos(this EnemigoBase enemisIA, Vector3 destino)
    {
        //No se evanden obstaculos
        enemisIA._agenteEnemigo.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        if (destino == Vector3.zero) return;
        //Calculamos la direccion a la que tenemos que dirigirnos
        Vector3 direccionalDestino = enemisIA._agenteEnemigo.steeringTarget - enemisIA.transform.position;
        direccionalDestino.y = 0f;

        //rotacion siempre aactivada para suavizar
        if (direccionalDestino.sqrMagnitude > 0.05f)
        {
            //Rotamos hacia la direccion asignada 
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionalDestino);
            enemisIA.transform.rotation = Quaternion.Slerp(enemisIA.transform.rotation, rotacionObjetivo, Time.deltaTime * 10f);

            //Comprobamos que ya ha rotadio lo suficiente para empezar a moverse
            if (Quaternion.Angle(enemisIA.transform.rotation, rotacionObjetivo) < 25f)
            {
                enemisIA._estaOrientado = true;
            }
            else
            {
                enemisIA._estaOrientado = false;
            }
        }
        //Nos aseguramos de que el agente se pueda mover si ya ha rotado y lo enviamos a la localizacion
        if (enemisIA._estaOrientado)
        {
            enemisIA._agenteEnemigo.isStopped = false;
            enemisIA._agenteEnemigo.speed = enemisIA._velocidadDePatrulla;

        }
        else
        {
            enemisIA._agenteEnemigo.speed = 0;
            enemisIA._agenteEnemigo.isStopped = false;

        }
    }
    public static void MuerteEnemigos(this EnemigoBase enemisIA)
    {
        if (enemisIA._muerto == true)
        {
            if (enemisIA.TryGetComponent(out EnemigoEscudo enemigoEscudo)) enemigoEscudo._anim.SetInteger("TipoAtaque", 2);
            //enemisIA._agenteEnemigo.isStopped = true;
            // Eliminamos la capa de invocables de los objetivos para que el radar de combate deje de detectarlos
            enemisIA._capaObjetivos = 0;
            // Aniadimos la capa invocable cuando muere y no tiene la capa d zombie
            enemisIA.gameObject.layer = enemisIA._capaInvocable;
            //: FRENO DE EMERGENCIA: Vaciamos el combate y los objetivos en este mismo frame.
            enemisIA._inCombat = false;
            enemisIA._objetivoFinal = null;
            enemisIA._capsuleCollider.isTrigger = true;
            //: APAGAR SENSORES: Desactivamos el script del sensor para que deje de lanzar eventos OnDetectionEnter.
            if (enemisIA._viewSensor != null) enemisIA._viewSensor.enabled = false;
            //Activamos la animacion de muerte
            enemisIA._anim.SetBool("Muerte", true);

            enemisIA.OnDeath?.Invoke();
        }
    }
    #endregion
}

