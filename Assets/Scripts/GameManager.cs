using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Recomendado para el Canvas

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

    [Header("Estado")]
    public int posJugador = 0;
    public int posIA = 0;
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
        StartCoroutine(FlujoJuego());
    }

    // --- FLUJO DE TURNOS (PARTE 2) ---
    IEnumerator FlujoJuego()
    {
        while (!comprobarGanador(1) && !comprobarGanador(2))
        {
            // TURNO JUGADOR
            textoInfo.text = "Turno del Jugador";
            textoPosicion.text = "POSICION: " + posJugador;
            botonDado.interactable = true;
            yield return new WaitUntil(() => turnoFinalizado);
            turnoFinalizado = false;

            if (comprobarGanador(1)) break;

            // TURNO IA
            yield return new WaitForSeconds(1.5f);
            textoInfo.text = "Turno de la IA";
            textoPosicion.text = "POSICION: " + posIA;

            // 1. Tirada de dado de la IA
            int dadoIA = tirarDado();

            // 2. PAUSA de 1 segundo para que el usuario vea el dado de la IA
            yield return new WaitForSeconds(1.0f);

            // 3. Mover ficha de la IA
            moverIA(dadoIA);

            // Esperar a que la IA termine su movimiento (incluyendo efectos)
            // Usamos un pequeño delay extra para que no empiece el turno del jugador de golpe
            yield return new WaitForSeconds(2.0f);
        }
        textoInfo.text = comprobarGanador(1) ? "HAS GANADO!" : "LA IA HA GANADO";
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
        textoPosicion.text = "POSICION: " + posJugador;
        StartCoroutine(ProcesoMovimiento(1, pasos));
        textoPosicion.text = "POSICION: " + posJugador;
    }

    public void moverIA(int pasos)
    {
        textoPosicion.text = "POSICION: " + posIA;
        StartCoroutine(ProcesoMovimiento(2, pasos));
        textoPosicion.text = "POSICION: " + posIA;
    }

    IEnumerator ProcesoMovimiento(int tipoJugador, int pasos)
    {
        // 1. Cálculo inicial
        int nuevaPos = (tipoJugador == 1) ? posJugador + pasos : posIA + pasos;
        if (nuevaPos > 21) nuevaPos = 21;

        // 2. Resolución de COLISIÓN (Punto 1.d de la Parte 2)
        int posOponente = (tipoJugador == 1) ? posIA : posJugador;

        // Si la casilla de destino es la del oponente (y no es salida/meta)
        if (nuevaPos == posOponente && nuevaPos != 0 && nuevaPos != 21)
        {
            if (tipoJugador == 1) // Jugador elige con botones [cite: 263]
            {
                panelColision.SetActive(true);
                eleccionColision = 0;
                yield return new WaitUntil(() => eleccionColision != 0);

                nuevaPos = (eleccionColision == 1) ? nuevaPos - 1 : nuevaPos + 1;

                panelColision.SetActive(false);
                eleccionColision = 0;
            }
            else // IA decide automáticamente 
            {
                // Forzamos a la IA a calcular una posición distinta
                nuevaPos = DecidirMovimientoIA(nuevaPos);
            }

            // Ajuste de seguridad para no salir del tablero tras el rebote
            nuevaPos = Mathf.Clamp(nuevaPos, 0, 21);
        }

        // 3. Ejecución del movimiento final
        ActualizarPosicionLogica(tipoJugador, nuevaPos);
        yield return MoverFichaVisual(tipoJugador, nuevaPos);

        // 4. Efectos de casilla (después de mover)
        if (comprobarCasillaEspecial(nuevaPos))
        {
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

    IEnumerator AplicarEfectoCasilla(int tipo, int pos)
    {
        int efecto = infoCasillas[pos];
        yield return new WaitForSeconds(0.5f);

        switch (efecto)
        {
            case 1: // Teleport [cite: 14]
                int destino = (pos == 1) ? 7 : 13;
                ActualizarPosicionLogica(tipo, destino);
                yield return MoverFichaVisual(tipo, destino);
                break;
            case 2: // Volver a tirar [cite: 15]
                infoCasillas[pos] = 0; // Se vuelve neutra (Extra 2) 
                PintarTablero();
                if (tipo == 1) moverJugador(tirarDado()); else moverIA(tirarDado());
                    break;
            case -1: // Retroceder 3 [cite: 16]
                int retro = Mathf.Max(0, pos - 3);
                ActualizarPosicionLogica(tipo, retro);
                yield return MoverFichaVisual(tipo, retro);
                break;
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

    IEnumerator MoverFichaVisual(int tipo, int destino)
    {
        GameObject ficha = (tipo == 1) ? fichaJugador : fichaIA;
        // Desplazamiento suave entre casillas 
        ficha.transform.position = vectorObjetos[destino].transform.position;
        yield return null;
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