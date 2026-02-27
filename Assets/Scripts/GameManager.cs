using System.Collections;
using System.Collections.Generic;
using TMPro; // Recomendado para el Canvas
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // REQUERIDO PARA REINICIAR

public class GameManager : MonoBehaviour
{
    [Header("Estructuras de Datos (Parte 1)")]
    int[] vectorCasillas; // 0=vacío, 1=jugador, 2=IA [cite: 37-39]
    int[] infoCasillas;   // Tipos: 0, 1, 2, -1, 99 [cite: 42-48]
    GameObject[] vectorObjetos;

    [Header("Fichas y UI (Parte 2)")]
    public GameObject fichaJugador;
    public GameObject fichaIA;
    public Button botonDado;
    public GameObject panelColision; // Panel con botones "Anterior" y "Siguiente"
    public TextMeshProUGUI textoInfo; // Para la cola de eventos [cite: 101]
    public TextMeshProUGUI textoDado;
    public TextMeshProUGUI textoPosicion;
    public TextMeshProUGUI textoTurnos;
    public GameObject panelFinal;
    public Button botonRestart;

    [Header("Estado")]
    public int posJugador = 0;
    public int posIA = 0;
    private int contadorTurnos = 1; // Empezamos en el turno 1
    private bool turnoFinalizado = false;
    private int eleccionColision = 0; // 0: nada, 1: anterior, 2: siguiente

    private void Awake()
    {
        vectorCasillas = new int[22];
        infoCasillas = new int[22];
        vectorObjetos = new GameObject[22];
        ConfigurarTablero();
    }

    private void Start()
    {
        AsignarObjetosEscena();
        PintarTablero();
        textoTurnos.text = "TURNO: " + contadorTurnos; // Mostrar "TURNO: 1" al empezar
        StartCoroutine(FlujoJuego());
        panelFinal.SetActive(false);
    }

    // --- FLUJO DE TURNOS (PARTE 2) ---
    IEnumerator FlujoJuego()
    {
        while (!comprobarGanador(1) && !comprobarGanador(2))
        {
            textoTurnos.text = "TURNO: " + contadorTurnos;

            // --- TURNO JUGADOR ---
            textoInfo.text = "Turno del Jugador";
            // IMPORTANTE: Forzamos que el marcador muestre la posición del JUGADOR 
            // antes de que este pueda interactuar.
            textoPosicion.text = "POSICION JUGADOR: " + posJugador;

            botonDado.interactable = true;
            yield return new WaitUntil(() => turnoFinalizado);
            turnoFinalizado = false;

            if (comprobarGanador(1)) break;

            // --- TURNO IA ---
            yield return new WaitForSeconds(1.5f);
            textoInfo.text = "Turno de la IA";
            // Forzamos que el marcador muestre la posición de la IA antes de que se mueva
            textoPosicion.text = "POSICION IA: " + posIA;

            int dadoIA = tirarDado();
            yield return new WaitForSeconds(1.0f);

            // El movimiento de la IA actualizará el texto paso a paso
            moverIA(dadoIA);

            // Aumentamos ligeramente este tiempo para asegurar que la corrutina 
            // de movimiento visual de la IA haya terminado antes de pasar al siguiente turno
            yield return new WaitForSeconds(3.0f);

            if (!comprobarGanador(2))
            {
                contadorTurnos++;
            }
        }
        textoInfo.text = comprobarGanador(1) ? "HAS GANADO!" : "LA IA HA GANADO";

        // ACTIVAR EL PANEL FINAL
        if (panelFinal != null)
        {
            panelFinal.SetActive(true);
        }
    }

    // --- NUEVO MÉTODO PARA EL BOTÓN RESTART ---
    public void ReiniciarJuego()
    {
        // Recarga la escena activa actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SalirJuego()
    {
        Application.Quit();
    }

    // Se llama desde el botón del dado en el Canvas
    public void OnClickBotonDado()
    {
        StartCoroutine(TurnoJugadorSecuencial());
    }
    IEnumerator TurnoJugadorSecuencial()
    {
        botonDado.interactable = false;

        // 1. Tirada de dado
        int dado = tirarDado();

        // 2. PAUSA de 1 segundo para que el usuario vea su dado
        yield return new WaitForSeconds(1.0f);

        // 3. Mover la ficha
        moverJugador(dado);
    }

    public void moverJugador(int pasos)
    {
        StartCoroutine(ProcesoMovimiento(1, pasos));
    }

    public void moverIA(int pasos)
    {
        StartCoroutine(ProcesoMovimiento(2, pasos));
    }

    IEnumerator ProcesoMovimiento(int tipoJugador, int pasos)
    {
        // Guardamos la posición inicial para la animación
        int posInicial = (tipoJugador == 1) ? posJugador : posIA;

        // 1. Cálculo inicial
        int nuevaPos = posInicial + pasos;
        if (nuevaPos > 21) nuevaPos = 21;
        if (nuevaPos < 0) nuevaPos = 0;

        // 2. Resolución de COLISIÓN
        int posOponente = (tipoJugador == 1) ? posIA : posJugador;
        if (nuevaPos == posOponente && nuevaPos != 0 && nuevaPos != 21)
        {
            if (tipoJugador == 1)
            {
                panelColision.SetActive(true);
                eleccionColision = 0;
                yield return new WaitUntil(() => eleccionColision != 0);
                nuevaPos = (eleccionColision == 1) ? nuevaPos - 1 : nuevaPos + 1;
                panelColision.SetActive(false);
                eleccionColision = 0;
            }
            else
            {
                nuevaPos = DecidirMovimientoIA(nuevaPos);
            }
            nuevaPos = Mathf.Clamp(nuevaPos, 0, 21);
        }

        // 3. Ejecución del movimiento (PASO A PASO)
        // Primero movemos visualmente desde el origen hasta la nuevaPos
        yield return MoverFichaVisual(tipoJugador, posInicial, nuevaPos);
        // Luego actualizamos la lógica interna
        ActualizarPosicionLogica(tipoJugador, nuevaPos);

        // 4. Efectos de casilla (Teleports, Retrocesos, etc.)
        if (comprobarCasillaEspecial(nuevaPos))
        {
            // Guardamos donde estamos antes del efecto
            int posAntesEfecto = nuevaPos;

            // AplicarEfectoCasilla ahora debe gestionar sus propios movimientos paso a paso
            yield return AplicarEfectoCasilla(tipoJugador, nuevaPos);
        }

        if (tipoJugador == 1) turnoFinalizado = true;
    }

    // --- LÓGICA AUXILIAR ---
    int DecidirMovimientoIA(int posColision)
    {
        int anterior = posColision - 1;
        int siguiente = posColision + 1;

        // Asegurar que no nos salimos del array
        if (siguiente > 21) return anterior;
        if (anterior < 0) return siguiente;

        // Prioridad 1: Casillas con modificadores positivos (Teleport=1, Tirar de nuevo=2) 
        if (infoCasillas[siguiente] == 1 || infoCasillas[siguiente] == 2) return siguiente;
        if (infoCasillas[anterior] == 1 || infoCasillas[anterior] == 2) return anterior;

        // Prioridad 2: Evitar casillas negativas (Retroceder=-1) 
        if (infoCasillas[siguiente] != -1) return siguiente;
        if (infoCasillas[anterior] != -1) return anterior;

        // Prioridad 3: Si ambas son iguales (normales o malas), aleatorio [cite: 265]
        return (Random.value > 0.5f) ? siguiente : anterior;
    }

    IEnumerator AplicarEfectoCasilla(int tipo, int posActual)
    {
        int efecto = infoCasillas[posActual];
        yield return new WaitForSeconds(0.5f);

        int destino = posActual;

        switch (efecto)
        {
            case 1: // Teleport
                destino = (posActual == 1) ? 7 : 13;
                break;
            case 2: // Volver a tirar
                infoCasillas[posActual] = 0;
                PintarTablero();
                if (tipo == 1) moverJugador(tirarDado()); else moverIA(tirarDado());
                yield break; // Salimos porque moverJugador iniciará su propia corrutina
            case -1: // Retroceder 3
                destino = Mathf.Max(0, posActual - 3);
                break;
        }

        if (destino != posActual)
        {
            // Animamos el efecto especial paso a paso
            yield return MoverFichaVisual(tipo, posActual, destino);
            ActualizarPosicionLogica(tipo, destino);
        }
    }

    void ActualizarPosicionLogica(int tipo, int nuevaPos)
    {
        if (tipo == 1)
        {
            vectorCasillas[posJugador] = (posJugador == posIA) ? 2 : 0;
            posJugador = nuevaPos;
            vectorCasillas[posJugador] = 1;
        }
        else
        {
            vectorCasillas[posIA] = (posIA == posJugador) ? 1 : 0;
            posIA = nuevaPos;
            vectorCasillas[posIA] = 2;
        }
    }

    IEnumerator MoverFichaVisual(int tipo, int origen, int destino)
    {
        GameObject ficha = (tipo == 1) ? fichaJugador : fichaIA;
        string prefijo = (tipo == 1) ? "POSICION JUGADOR: " : "POSICION IA: ";

        int paso = (origen < destino) ? 1 : -1;

        for (int i = origen + paso; ; i += paso)
        {
            ficha.transform.position = vectorObjetos[i].transform.position;

            textoPosicion.text = prefijo + i;

            yield return new WaitForSeconds(0.25f);
            if (i == destino) break;
        }
    }

    public bool comprobarCasillaEspecial(int numCasilla)
    {
        return infoCasillas[numCasilla] != 0;
    }

    public bool comprobarGanador(int jugador)
    {
        return (jugador == 1) ? posJugador >= 21 : posIA >= 21;
    }

    // --- MÉTODOS DE LA PARTE 1 ---

    void ConfigurarTablero()
    {
        for (int i = 0; i < infoCasillas.Length; i++)
        {
            vectorCasillas[i] = 0;
            infoCasillas[i] = 0; // Por defecto: Casilla normal [cite: 42]
        }

        // a. Teleports: 1 y 6 [cite: 14, 43]
        infoCasillas[1] = 1;
        infoCasillas[6] = 1;

        // b. Volver a tirar: 12 y 18 [cite: 15, 44]
        infoCasillas[12] = 2;
        infoCasillas[18] = 2;

        // c. Retroceder 3: 5, 10, 14, 19 y 20 [cite: 16, 46]
        infoCasillas[5] = -1;
        infoCasillas[10] = -1;
        infoCasillas[14] = -1;
        infoCasillas[19] = -1;
        infoCasillas[20] = -1;

        // d. Victoria: 21 [cite: 17, 48]
        infoCasillas[21] = 99;
    }

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

    void PintarTablero()
    {
        for (int i = 0; i < infoCasillas.Length; i++)
        {
            if (vectorObjetos[i] == null) continue;
            RawImage img = vectorObjetos[i].GetComponent<RawImage>();

            switch (infoCasillas[i])
            {
                case 1: img.color = new Color(1.0f, 0.64f, 0.0f); break; // Naranja
                case 2: img.color = Color.green; break;                // Verde
                case -1: img.color = Color.red; break;                 // Rojo
                case 99: img.color = Color.yellow; break;              // Meta
            }
            // Destinos de teleport (7 y 13) según imagen
            if (i == 7 || i == 13) img.color = new Color(0.5f, 0.4f, 0.8f); // Morado
        }
    }

    // --- ESTRUCTURA REQUERIDA PARA LA PARTE 3 --- 

    public int tirarDado()
    {
        int resultado = Random.Range(1, 7);
        Debug.Log("Dado: " + resultado);
        textoDado.text = resultado.ToString();
        return resultado;
    }

    // Función para el botón "Anterior"
    public void OnClickAnterior()
    {
        eleccionColision = 1; // 1 representa retroceder una casilla
    }

    // Función para el botón "Siguiente"
    public void OnClickSiguiente()
    {
        eleccionColision = 2; // 2 representa avanzar una casilla
    }
}