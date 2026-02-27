using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ========================================================
    // PARTE 1: ESTRUCTURAS DE DATOS Y VARIABLES GLOBALES
    // ========================================================

    [Header("Estructuras de Datos (Parte 1)")]
    // Vector principal para saber si una casilla está vacía (0), tiene al jugador (1) o a la IA (2)
    int[] vectorCasillas;
    // Almacena el tipo de cada casilla: 0=normal, 1=teleport, 2=volver a tirar, -1=retroceder, 99=victoria
    int[] infoCasillas;
    // Representa las casillas físicas (GameObjects) en la escena de Unity
    GameObject[] vectorObjetos;

    [Header("Fichas y UI (Parte 2)")]
    public GameObject fichaJugador;
    public GameObject fichaIA;
    public Button botonDado;
    public GameObject panelColision; // Panel con los botones para elegir dirección al chocar
    public TextMeshProUGUI textoInfo; // Canvas: Cola de eventos e información principal
    public TextMeshProUGUI textoDado;
    public TextMeshProUGUI textoPosicion;
    public TextMeshProUGUI textoTurnos;
    public GameObject panelFinal;
    public GameObject panelInfoAdicional; // Panel para mostrar la pausa de 1 segundo en casillas especiales
    public TextMeshProUGUI textoInfoAdicional;

    [Header("Estado")]
    public int posJugador = 0;
    public int posIA = 0;
    private int contadorTurnos = 1;
    private bool turnoFinalizado = false; // Bandera para controlar la sincronización de las corrutinas
    private int eleccionColision = 0; // Controla el input de los botones de colisión: 0=nada, 1=anterior, 2=siguiente

    // ========================================================
    // INICIALIZACIÓN
    // ========================================================

    private void Awake()
    {
        // Inicializamos los arrays para 22 posiciones (de la 0 a la 21) 
        // cubriendo desde "casilla0" hasta la victoria en "casilla21".
        vectorCasillas = new int[22];
        infoCasillas = new int[22];
        vectorObjetos = new GameObject[22];

        ConfigurarTablero(); // Asignamos los tipos de casillas especiales a sus índices
    }

    private void Start()
    {
        AsignarObjetosEscena(); // Vinculamos los objetos de la escena al array
        PintarTablero();        // Coloreamos las casillas según su valor en infoCasillas

        textoTurnos.text = "TURNO: " + contadorTurnos;

        // Ocultamos los paneles emergentes al inicio del juego
        panelFinal.SetActive(false);
        panelInfoAdicional.SetActive(false);

        // Iniciamos el ciclo principal del juego mediante una corrutina
        StartCoroutine(FlujoJuego());
    }

    // ========================================================
    // PARTE 2: FLUJO PRINCIPAL DE TURNOS
    // ========================================================

    IEnumerator FlujoJuego()
    {
        // El bucle de turnos se mantiene activo mientras ninguno de los dos jugadores alcance la meta
        while (!comprobarGanador(1) && !comprobarGanador(2))
        {
            textoTurnos.text = "TURNO: " + contadorTurnos;

            // --- TURNO DEL JUGADOR ---
            ActualizarEventos("Turno del Jugador");
            textoPosicion.text = "POSICION JUGADOR: " + posJugador;
            botonDado.interactable = true; // Habilitamos el botón para que el jugador pueda tirar

            // Pausamos la corrutina principal hasta que el jugador complete toda su secuencia de movimiento
            yield return new WaitUntil(() => turnoFinalizado);
            turnoFinalizado = false;

            if (comprobarGanador(1)) break; // Si el jugador gana en su turno, salimos del bucle

            // --- TURNO DE LA IA ---
            yield return new WaitForSeconds(1.0f); // Pequeña pausa para no hacer el cambio de turno muy frenético
            ActualizarEventos("Turno de la IA");
            textoPosicion.text = "POSICION IA: " + posIA;

            moverIA(); // Llamada a la función refactorizada que inicia el turno de la máquina

            // Pausamos la corrutina principal hasta que la IA complete toda su secuencia
            yield return new WaitUntil(() => turnoFinalizado);
            turnoFinalizado = false;

            // Avanzamos el contador de turnos solo si la IA no ha ganado todavía
            if (!comprobarGanador(2))
            {
                contadorTurnos++;
            }
        }

        // --- FIN DEL JUEGO ---
        // Desactivamos el botón definitivamente por seguridad para que el jugador no siga tirando
        botonDado.interactable = false;

        textoInfo.text = comprobarGanador(1) ? "HAS GANADO!" : "LA IA HA GANADO";
        if (panelFinal != null) panelFinal.SetActive(true);
    }

    // ========================================================
    // PARTE 3: REFACTORIZACIÓN (5 FUNCIONES PRINCIPALES SOLICITADAS)
    // ========================================================

    // 1. Simula la tirada del dado, la muestra en UI y devuelve el valor
    public int tirarDado()
    {
        int resultado = Random.Range(1, 7);
        textoDado.text = resultado.ToString();
        return resultado;
    }

    // 2. Ejecuta la tirada y comienza el movimiento del jugador
    public void moverJugador()
    {
        botonDado.interactable = false;
        int dado = tirarDado();
        ActualizarEventos("Jugador saca un " + dado);
        StartCoroutine(ProcesoMovimiento(1, dado));
    }

    // 3. Ejecuta la tirada y comienza el movimiento de la IA
    public void moverIA()
    {
        int dado = tirarDado();
        ActualizarEventos("IA saca un " + dado);
        StartCoroutine(ProcesoMovimiento(2, dado));
    }

    // 4. Comprueba si en numCasilla hay una casilla especial (ni normal ni de victoria)
    public bool comprobarCasillaEspecial(int numCasilla)
    {
        return infoCasillas[numCasilla] != 0 && infoCasillas[numCasilla] != 99;
    }

    // 5. Nos informa si hay ganador pasando el número del jugador (1 = Jugador, 2 = IA)
    public bool comprobarGanador(int jugador)
    {
        return (jugador == 1) ? posJugador >= 21 : posIA >= 21;
    }

    // ========================================================
    // LÓGICA DE MOVIMIENTO, COLISIONES Y CASILLAS ESPECIALES
    // ========================================================

    // Evento vinculado al botón UI del dado
    public void OnClickBotonDado()
    {
        // Si alguien ya ganó (especialmente la IA), el botón no hace nada y bloqueamos interacciones
        if (comprobarGanador(1) || comprobarGanador(2))
        {
            botonDado.interactable = false;
            return;
        }

        moverJugador(); // Inicia el proceso del jugador
    }

    // Corrutina principal que orquesta el orden exacto de cada turno para que no ocurra instantáneamente
    IEnumerator ProcesoMovimiento(int tipoJugador, int pasos)
    {
        int posInicial = (tipoJugador == 1) ? posJugador : posIA;
        int nuevaPos = Mathf.Clamp(posInicial + pasos, 0, 21); // Evitamos desbordamientos del array

        ActualizarEventos((tipoJugador == 1 ? "Jugador" : "IA") + " va a la casilla " + nuevaPos);

        // PASO 1: Mover visualmente la ficha casilla a casilla
        yield return MoverFichaVisual(tipoJugador, posInicial, nuevaPos);
        ActualizarPosicionLogica(tipoJugador, nuevaPos);

        // PASO 2: Comprobar si ha caído en una Casilla Especial
        if (comprobarCasillaEspecial(nuevaPos))
        {
            yield return AplicarEfectoCasilla(tipoJugador, nuevaPos);
            // Actualizamos nuevaPos por si el efecto especial ha movido la ficha a otro sitio
            nuevaPos = (tipoJugador == 1) ? posJugador : posIA;
        }

        // PASO 3: Comprobar colisión si la posición de una ficha coincide con la otra
        int posOponente = (tipoJugador == 1) ? posIA : posJugador;
        if (nuevaPos == posOponente && nuevaPos > 0 && nuevaPos < 21) // Ignoramos colisiones en inicio o meta
        {
            ActualizarEventos("Colisión en la casilla " + nuevaPos + "!");
            yield return ResolverColision(tipoJugador, nuevaPos);
        }

        // Indicamos a FlujoJuego que el turno actual ha concluido totalmente
        turnoFinalizado = true;
    }

    // Aplica los efectos de casillas especiales y maneja los consumibles
    IEnumerator AplicarEfectoCasilla(int tipo, int posActual)
    {
        int efecto = infoCasillas[posActual];

        // --- MOSTRAR MENSAJE ---
        // Mostramos el panel de información adicional durante 2 segundos antes de aplicar el efecto
        panelInfoAdicional.SetActive(true);
        textoInfoAdicional.text = "Casilla Especial! " + ObtenerNombreEfecto(efecto);
        yield return new WaitForSeconds(2f);
        panelInfoAdicional.SetActive(false);

        int destino = posActual;

        // Evaluamos el tipo de efecto que tiene la casilla
        switch (efecto)
        {
            case 1: // Teleport (las casillas 1 y 6 te llevan a la 7 y 13 respectivamente)
                destino = (posActual == 1) ? 7 : 13;
                break;
            case 2: // Volver a tirar (Consumibles - Parte 2 Punto 2)
                infoCasillas[posActual] = 0; // Solo se usa una vez, vuelve a ser neutra
                PintarTablero(); // Repintamos para que cambie de verde a blanco (visual)
                ActualizarEventos("Tira de nuevo!");

                // Reiniciamos la corrutina de movimiento como si fuera un turno extra
                if (tipo == 1) StartCoroutine(ProcesoMovimiento(1, tirarDado()));
                else StartCoroutine(ProcesoMovimiento(2, tirarDado()));
                yield break; // Interrumpimos aquí porque la nueva corrutina tomará el control
            case -1: // Retroceder 3 posiciones
                destino = Mathf.Max(0, posActual - 3); // Prevenimos bajar de la casilla 0
                break;
        }

        // Si la casilla aplicó un desplazamiento, movemos la ficha visual y lógicamente
        if (destino != posActual)
        {
            yield return MoverFichaVisual(tipo, posActual, destino);
            ActualizarPosicionLogica(tipo, destino);
        }
    }

    // Resuelve cuando dos fichas caen exactamente en la misma posición (Parte 1 Punto 1.d)
    IEnumerator ResolverColision(int tipoJugador, int posActual)
    {
        int nuevaPos = posActual;

        if (tipoJugador == 1) // Es el Jugador: Mostrar botones UI
        {
            panelColision.SetActive(true);
            eleccionColision = 0;

            // Esperamos hasta que el usuario pulse un botón y cambie la variable
            yield return new WaitUntil(() => eleccionColision != 0);

            nuevaPos = (eleccionColision == 1) ? posActual - 1 : posActual + 1;

            panelColision.SetActive(false);
            eleccionColision = 0; // Reiniciamos para futuras colisiones
        }
        else // Es la IA: Decide automáticamente
        {
            nuevaPos = DecidirMovimientoIA(posActual);
        }

        // Ajustamos los límites de seguridad, movemos y actualizamos
        nuevaPos = Mathf.Clamp(nuevaPos, 0, 21);
        yield return MoverFichaVisual(tipoJugador, posActual, nuevaPos);
        ActualizarPosicionLogica(tipoJugador, nuevaPos);
    }

    // Algoritmo de decisión para la IA cuando choca (prioriza positivos, evita negativos)
    int DecidirMovimientoIA(int pos)
    {
        int anterior = pos - 1;
        int siguiente = pos + 1;

        // Seguridad para no salirse del tablero
        if (siguiente > 21) return anterior;
        if (anterior < 0) return siguiente;

        // 1. Prioriza casillas con modificadores positivos (1=Teleport, 2=Volver a tirar)
        if (infoCasillas[siguiente] == 1 || infoCasillas[siguiente] == 2) return siguiente;
        if (infoCasillas[anterior] == 1 || infoCasillas[anterior] == 2) return anterior;

        // 2. Evita modificadores negativos (-1=Retroceder)
        if (infoCasillas[siguiente] != -1) return siguiente;
        if (infoCasillas[anterior] != -1) return anterior;

        // 3. En caso de no haber casillas especiales cerca, aleatoriza (Parte 1 Punto 1.d)
        return (Random.value > 0.5f) ? siguiente : anterior;
    }

    // Corrutina para simular el desplazamiento fluido de casilla en casilla (Parte 2 Punto 3)
    IEnumerator MoverFichaVisual(int tipo, int origen, int destino)
    {
        GameObject ficha = (tipo == 1) ? fichaJugador : fichaIA;
        string prefijo = (tipo == 1) ? "POSICION JUGADOR: " : "POSICION IA: ";

        if (origen == destino) yield break;

        // Determinamos la dirección del movimiento (1 para avanzar, -1 para retroceder)
        int paso = (origen < destino) ? 1 : -1;

        // Bucle que itera por cada casilla intermedia
        for (int i = origen; i != destino; i += paso)
        {
            int siguiente = i + paso;
            siguiente = Mathf.Clamp(siguiente, 0, 21);

            // Trasladamos la ficha al Transform del GameObject de la escena
            ficha.transform.position = vectorObjetos[siguiente].transform.position;
            textoPosicion.text = prefijo + siguiente;

            // Pausa entre casillas para que no sea instantáneo
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ========================================================
    // MÉTODOS AUXILIARES Y DE CONFIGURACIÓN
    // ========================================================

    // Mantiene el vectorCasillas actualizado con un 0, 1 o 2
    void ActualizarPosicionLogica(int tipo, int nuevaPos)
    {
        for (int i = 0; i < vectorCasillas.Length; i++) vectorCasillas[i] = 0;

        if (tipo == 1) posJugador = nuevaPos;
        else posIA = nuevaPos;

        vectorCasillas[posJugador] = 1;
        vectorCasillas[posIA] = 2;
    }

    // Define los modificadores internos de cada casilla (Parte 1 Punto 1 y 2)
    void ConfigurarTablero()
    {
        infoCasillas[1] = 1; infoCasillas[6] = 1;            // Teleports
        infoCasillas[12] = 2; infoCasillas[18] = 2;          // Volver a tirar (Consumibles)
        infoCasillas[5] = -1; infoCasillas[10] = -1;         // Retroceder 3 casillas
        infoCasillas[14] = -1; infoCasillas[19] = -1; infoCasillas[20] = -1;
        infoCasillas[21] = 99;                               // Casilla final de victoria
    }

    // Vincula automáticamente los GameObjects físicos al array vectorObjetos basándose en el script Casilla.cs
    void AsignarObjetosEscena()
    {
        GameObject[] casillasTag = GameObject.FindGameObjectsWithTag("casilla");
        foreach (GameObject go in casillasTag)
        {
            Casilla script = go.GetComponent<Casilla>();
            if (script != null && script.numeroCasilla < vectorObjetos.Length)
            {
                vectorObjetos[script.numeroCasilla] = go;
            }
        }
    }

    // Aplica los colores a las casillas de Unity en base a su información lógica
    void PintarTablero()
    {
        for (int i = 0; i < infoCasillas.Length; i++)
        {
            if (vectorObjetos[i] == null) continue;
            RawImage img = vectorObjetos[i].GetComponent<RawImage>();

            switch (infoCasillas[i])
            {
                case 1: img.color = new Color(1.0f, 0.64f, 0.0f); break; // Naranja (Teleport)
                case 2: img.color = Color.green; break;                  // Verde (Consumibles)
                case -1: img.color = Color.red; break;                   // Rojo (Retroceder)
                case 99: img.color = Color.yellow; break;                // Amarillo (Meta)
                case 0: img.color = Color.white; break;                  // Blanco (Normal)
            }
            // Diferenciamos los destinos de los teleports con morado
            if (i == 7 || i == 13) img.color = new Color(0.5f, 0.4f, 0.8f);
        }
    }

    // Añade el sistema de Cola de Eventos para el Canvas y la consola (Parte 3 Punto 2)
    void ActualizarEventos(string mensaje)
    {
        textoInfo.text = mensaje;
        Debug.Log(mensaje);
    }

    // Función auxiliar para traducir los códigos numéricos a texto legible para el usuario
    string ObtenerNombreEfecto(int e)
    {
        if (e == 1) return "Teletransporte";
        if (e == 2) return "Vuelve a tirar";
        if (e == -1) return "Retrocede 3 casillas";
        return "Desconocido";
    }

    // --- MÉTODOS DE BOTONES DE LA UI ---
    public void OnClickAnterior() => eleccionColision = 1;
    public void OnClickSiguiente() => eleccionColision = 2;
    public void ReiniciarJuego() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void SalirJuego() => Application.Quit();
}