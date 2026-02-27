using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // --------------------------------------------------------
    // PARTE 1: ESTRUCTURAS DE DATOS Y VARIABLES GLOBALES
    // --------------------------------------------------------
    [Header("Estructuras de Datos (Parte 1)")]
    int[] vectorCasillas; // Controla quién está en cada casilla: 0=vacío, 1=jugador, 2=IA
    int[] infoCasillas;   // Tipos de casilla: 0=normal, 1=teleport, 2=volver a tirar, -1=retroceder, 99=victoria
    GameObject[] vectorObjetos; // Almacena las referencias a los GameObjects físicos de la escena

    [Header("Fichas y UI (Parte 2)")]
    public GameObject fichaJugador;
    public GameObject fichaIA;
    public Button botonDado;
    public GameObject panelColision;
    public TextMeshProUGUI textoInfo; // Canvas: Cola de eventos e información de turnos
    public TextMeshProUGUI textoDado;
    public TextMeshProUGUI textoPosicion;
    public TextMeshProUGUI textoTurnos;
    public GameObject panelFinal;
    public GameObject panelInfoAdicional;
    public TextMeshProUGUI textoInfoAdicional;

    [Header("Estado")]
    public int posJugador = 0;
    public int posIA = 0;
    private int contadorTurnos = 1;
    private bool turnoFinalizado = false;
    private int eleccionColision = 0; // Utilizado por los botones de colisión: 0=nada, 1=anterior, 2=siguiente

    private void Awake()
    {
        // Inicializamos las estructuras para 22 posiciones (de la 0 a la 21) 
        // para cubrir desde "casilla0" hasta la victoria en "casilla21"
        vectorCasillas = new int[22];
        infoCasillas = new int[22];
        vectorObjetos = new GameObject[22];
        ConfigurarTablero(); // Asignamos los tipos de casillas especiales
    }

    private void Start()
    {
        AsignarObjetosEscena(); // Vinculamos los objetos de la escena al array
        PintarTablero();        // Coloreamos las casillas según su tipo
        textoTurnos.text = "TURNO: " + contadorTurnos;

        // Ocultamos los paneles que no deben verse al inicio
        panelFinal.SetActive(false);
        panelInfoAdicional.SetActive(false);

        // Iniciamos el ciclo principal del juego
        StartCoroutine(FlujoJuego());
    }

    // --------------------------------------------------------
    // PARTE 2: FLUJO PRINCIPAL DE TURNOS
    // --------------------------------------------------------
    IEnumerator FlujoJuego()
    {
        // El bucle se mantiene mientras ningún jugador haya llegado a la meta
        while (!comprobarGanador(1) && !comprobarGanador(2))
        {
            textoTurnos.text = "TURNO: " + contadorTurnos;

            // --- TURNO JUGADOR ---
            ActualizarEventos("Turno del Jugador");
            textoPosicion.text = "POSICION JUGADOR: " + posJugador;
            botonDado.interactable = true; // Permitimos al jugador interactuar

            // Esperamos hasta que el jugador termine su turno completo (movimiento + efectos + colisión)
            yield return new WaitUntil(() => turnoFinalizado);
            turnoFinalizado = false;

            if (comprobarGanador(1)) break;

            // --- TURNO IA ---
            yield return new WaitForSeconds(1.0f); // Pausa para no hacer el cambio frenético
            ActualizarEventos("Turno de la IA");
            textoPosicion.text = "POSICION IA: " + posIA;

            moverIA(); // Se llama a la función sin parámetros de la Parte 3 (Refactorización)

            // Esperamos hasta que la IA termine todo su proceso
            yield return new WaitUntil(() => turnoFinalizado);
            turnoFinalizado = false;

            // Si la IA no ha ganado, avanzamos el turno general
            if (!comprobarGanador(2))
            {
                contadorTurnos++;
            }
        }

        // --- FIN DEL JUEGO ---
        textoInfo.text = comprobarGanador(1) ? "HAS GANADO!" : "LA IA HA GANADO";
        if (panelFinal != null) panelFinal.SetActive(true);
    }

    // --------------------------------------------------------
    // PARTE 3: REFACTORIZACIÓN (5 FUNCIONES PRINCIPALES)
    // --------------------------------------------------------

    // 1. Hace la animación/lógica del dado y devuelve la tirada
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

    // 4. Comprueba si la casilla es especial (distinta de 0 y de la meta 99)
    public bool comprobarCasillaEspecial(int numCasilla)
    {
        return infoCasillas[numCasilla] != 0 && infoCasillas[numCasilla] != 99;
    }

    // 5. Verifica si un jugador ha alcanzado o superado la casilla final (21)
    public bool comprobarGanador(int jugador)
    {
        return (jugador == 1) ? posJugador >= 21 : posIA >= 21;
    }

    // --------------------------------------------------------
    // LÓGICA DE MOVIMIENTO, COLISIONES Y CASILLAS ESPECIALES
    // --------------------------------------------------------

    // Evento del botón del Canvas para que el jugador tire el dado
    public void OnClickBotonDado()
    {
        moverJugador();
    }

    // Corrutina principal que orquesta el orden exacto de cada turno
    IEnumerator ProcesoMovimiento(int tipoJugador, int pasos)
    {
        int posInicial = (tipoJugador == 1) ? posJugador : posIA;
        int nuevaPos = Mathf.Clamp(posInicial + pasos, 0, 21); // Evitamos salirnos del tablero

        ActualizarEventos((tipoJugador == 1 ? "Jugador" : "IA") + " va a la casilla " + nuevaPos);

        // 1. Mover visualmente paso a paso
        yield return MoverFichaVisual(tipoJugador, posInicial, nuevaPos);
        ActualizarPosicionLogica(tipoJugador, nuevaPos);

        // 2. Comprobar Casilla Especial (Muestra mensaje -> Espera -> Aplica efecto)
        if (comprobarCasillaEspecial(nuevaPos))
        {
            yield return AplicarEfectoCasilla(tipoJugador, nuevaPos);
            nuevaPos = (tipoJugador == 1) ? posJugador : posIA; // Actualizamos por si se ha movido por el efecto
        }

        // 3. Comprobar Colisión con el oponente (solo si no es inicio ni meta)
        int posOponente = (tipoJugador == 1) ? posIA : posJugador;
        if (nuevaPos == posOponente && nuevaPos > 0 && nuevaPos < 21)
        {
            ActualizarEventos("Colisión en la casilla " + nuevaPos + "!");
            yield return ResolverColision(tipoJugador, nuevaPos);
        }

        // Marcamos que toda la secuencia del turno ha terminado
        turnoFinalizado = true;
    }

    // Gestiona los efectos de las casillas y los consumibles
    IEnumerator AplicarEfectoCasilla(int tipo, int posActual)
    {
        int efecto = infoCasillas[posActual];

        // --- MODIFICACIÓN SOLICITADA ---
        // Mostramos el panel de información adicional durante 2 segundos antes de aplicar el efecto
        panelInfoAdicional.SetActive(true);
        textoInfoAdicional.text = "Casilla Especial! " + ObtenerNombreEfecto(efecto);

        yield return new WaitForSeconds(2f); // Esperar 2 segundos (según el código original modificado) antes del efecto

        panelInfoAdicional.SetActive(false);
        // -------------------------------

        int destino = posActual;

        // Evaluamos qué tipo de casilla especial es
        switch (efecto)
        {
            case 1: // Teleport (las casillas 1 y 6 te llevan a la 7 y 13)
                destino = (posActual == 1) ? 7 : 13;
                break;
            case 2: // Volver a tirar (Casillas 12 y 18 son CONSUMIBLES)
                infoCasillas[posActual] = 0; // Se vuelve neutra para el resto de la partida
                PintarTablero(); // Repintamos para que cambie a color blanco
                ActualizarEventos("Tira de nuevo!");

                // Reiniciamos un proceso de movimiento extra y salimos de este
                if (tipo == 1) StartCoroutine(ProcesoMovimiento(1, tirarDado()));
                else StartCoroutine(ProcesoMovimiento(2, tirarDado()));
                yield break;
            case -1: // Retroceder 3 casillas
                destino = Mathf.Max(0, posActual - 3);
                break;
        }

        // Si el efecto nos cambia de posición, hacemos el movimiento visual hacia el nuevo destino
        if (destino != posActual)
        {
            yield return MoverFichaVisual(tipo, posActual, destino);
            ActualizarPosicionLogica(tipo, destino);
        }
    }

    // Resuelve cuando dos fichas caen en la misma posición
    IEnumerator ResolverColision(int tipoJugador, int posActual)
    {
        int nuevaPos = posActual;

        if (tipoJugador == 1) // Si es el Jugador, habilitamos los botones de UI
        {
            panelColision.SetActive(true);
            eleccionColision = 0;
            yield return new WaitUntil(() => eleccionColision != 0); // Esperamos pulsación

            // Calculamos nueva posición basada en el botón pulsado
            nuevaPos = (eleccionColision == 1) ? posActual - 1 : posActual + 1;

            panelColision.SetActive(false);
            eleccionColision = 0;
        }
        else // Si es la IA, decide su mejor opción mediante algoritmo
        {
            nuevaPos = DecidirMovimientoIA(posActual);
        }

        // Aseguramos límites y aplicamos el pequeño ajuste visual y lógico
        nuevaPos = Mathf.Clamp(nuevaPos, 0, 21);
        yield return MoverFichaVisual(tipoJugador, posActual, nuevaPos);
        ActualizarPosicionLogica(tipoJugador, nuevaPos);
    }

    // Lógica de Inteligencia Artificial para colisiones
    int DecidirMovimientoIA(int pos)
    {
        int anterior = pos - 1;
        int siguiente = pos + 1;

        // Seguridad por si está en los bordes
        if (siguiente > 21) return anterior;
        if (anterior < 0) return siguiente;

        // Prioridad 1: Buscar modificadores positivos (1 = teleport, 2 = tirar extra)
        if (infoCasillas[siguiente] == 1 || infoCasillas[siguiente] == 2) return siguiente;
        if (infoCasillas[anterior] == 1 || infoCasillas[anterior] == 2) return anterior;

        // Prioridad 2: Evitar modificadores negativos (-1)
        if (infoCasillas[siguiente] != -1) return siguiente;
        if (infoCasillas[anterior] != -1) return anterior;

        // Prioridad 3: Si ambas son neutras o iguales, elegir aleatoriamente
        return (Random.value > 0.5f) ? siguiente : anterior;
    }

    // Corrutina para simular el desplazamiento casilla a casilla (no instantáneo)
    IEnumerator MoverFichaVisual(int tipo, int origen, int destino)
    {
        GameObject ficha = (tipo == 1) ? fichaJugador : fichaIA;
        string prefijo = (tipo == 1) ? "POSICION JUGADOR: " : "POSICION IA: ";

        if (origen == destino) yield break;

        // Determinamos si avanzamos o retrocedemos
        int paso = (origen < destino) ? 1 : -1;

        // Bucle para iterar por cada casilla intermedia
        for (int i = origen; i != destino; i += paso)
        {
            int siguiente = i + paso;
            siguiente = Mathf.Clamp(siguiente, 0, 21);

            // Movemos la ficha a la posición física del objeto casilla
            ficha.transform.position = vectorObjetos[siguiente].transform.position;
            textoPosicion.text = prefijo + siguiente;

            yield return new WaitForSeconds(0.25f); // Pausa para fluidez de la animación
        }
    }

    // --------------------------------------------------------
    // MÉTODOS AUXILIARES Y DE CONFIGURACIÓN
    // --------------------------------------------------------

    // Actualiza la posición en el vector interno de control
    void ActualizarPosicionLogica(int tipo, int nuevaPos)
    {
        // Limpiamos todo el vector
        for (int i = 0; i < vectorCasillas.Length; i++) vectorCasillas[i] = 0;

        // Actualizamos la variable de estado correspondiente
        if (tipo == 1) posJugador = nuevaPos;
        else posIA = nuevaPos;

        // Asignamos las posiciones actuales
        vectorCasillas[posJugador] = 1;
        vectorCasillas[posIA] = 2;
    }

    // Define por código dónde están los eventos del tablero
    void ConfigurarTablero()
    {
        infoCasillas[1] = 1; infoCasillas[6] = 1;            // Teleports
        infoCasillas[12] = 2; infoCasillas[18] = 2;          // Volver a tirar
        infoCasillas[5] = -1; infoCasillas[10] = -1;         // Retroceder 3
        infoCasillas[14] = -1; infoCasillas[19] = -1; infoCasillas[20] = -1;
        infoCasillas[21] = 99;                               // Victoria
    }

    // Busca los gameobjects con tag "casilla" y los ordena en el array
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

    // Colorea las casillas físicas según la información lógica
    void PintarTablero()
    {
        for (int i = 0; i < infoCasillas.Length; i++)
        {
            if (vectorObjetos[i] == null) continue;
            RawImage img = vectorObjetos[i].GetComponent<RawImage>();

            switch (infoCasillas[i])
            {
                case 1: img.color = new Color(1.0f, 0.64f, 0.0f); break; // Naranja (Teleport)
                case 2: img.color = Color.green; break;                  // Verde (Volver a tirar)
                case -1: img.color = Color.red; break;                   // Rojo (Trampa)
                case 99: img.color = Color.yellow; break;                // Amarillo (Meta)
                case 0: img.color = Color.white; break;                  // Blanco (Normal / Consumida)
            }

            // Destinos de los teleports en color morado
            if (i == 7 || i == 13) img.color = new Color(0.5f, 0.4f, 0.8f);
        }
    }

    // Gestiona la Cola de Eventos del Canvas
    void ActualizarEventos(string mensaje)
    {
        textoInfo.text = mensaje;
        Debug.Log(mensaje);
    }

    // Traductor de códigos para mostrarlos por pantalla en el mensaje adicional
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