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
        vectorCasillas = new int[22];
        infoCasillas = new int[22];
        vectorObjetos = new GameObject[22];
        ConfigurarTablero();
    }

    private void Start()
    {
        AsignarObjetosEscena();
        PintarTablero();
        textoTurnos.text = "TURNO: " + contadorTurnos;
        panelFinal.SetActive(false);
        panelInfoAdicional.SetActive(false);
        StartCoroutine(FlujoJuego());
    }

    // --------------------------------------------------------
    // PARTE 2: FLUJO PRINCIPAL DE TURNOS
    // --------------------------------------------------------
    IEnumerator FlujoJuego()
    {
        while (!comprobarGanador(1) && !comprobarGanador(2))
        {
            textoTurnos.text = "TURNO: " + contadorTurnos;

            // --- TURNO JUGADOR ---
            ActualizarEventos("Turno del Jugador");
            textoPosicion.text = "POSICION JUGADOR: " + posJugador;
            botonDado.interactable = true;

            yield return new WaitUntil(() => turnoFinalizado);
            turnoFinalizado = false;

            if (comprobarGanador(1)) break;

            // --- TURNO IA ---
            yield return new WaitForSeconds(1.0f);
            ActualizarEventos("Turno de la IA");
            textoPosicion.text = "POSICION IA: " + posIA;

            moverIA();

            yield return new WaitUntil(() => turnoFinalizado);
            turnoFinalizado = false;

            if (!comprobarGanador(2))
            {
                contadorTurnos++;
            }
        }

        // --- FIN DEL JUEGO ---
        // Al terminar el bucle, desactivamos el botón definitivamente por seguridad
        botonDado.interactable = false;

        textoInfo.text = comprobarGanador(1) ? "HAS GANADO!" : "LA IA HA GANADO";
        if (panelFinal != null) panelFinal.SetActive(true);
    }

    // --------------------------------------------------------
    // PARTE 3: REFACTORIZACIÓN (5 FUNCIONES PRINCIPALES)
    // --------------------------------------------------------

    public int tirarDado()
    {
        int resultado = Random.Range(1, 7);
        textoDado.text = resultado.ToString();
        return resultado;
    }

    public void moverJugador()
    {
        botonDado.interactable = false;
        int dado = tirarDado();
        ActualizarEventos("Jugador saca un " + dado);
        StartCoroutine(ProcesoMovimiento(1, dado));
    }

    public void moverIA()
    {
        int dado = tirarDado();
        ActualizarEventos("IA saca un " + dado);
        StartCoroutine(ProcesoMovimiento(2, dado));
    }

    public bool comprobarCasillaEspecial(int numCasilla)
    {
        return infoCasillas[numCasilla] != 0 && infoCasillas[numCasilla] != 99;
    }

    public bool comprobarGanador(int jugador)
    {
        return (jugador == 1) ? posJugador >= 21 : posIA >= 21;
    }

    // --------------------------------------------------------
    // LÓGICA DE MOVIMIENTO, COLISIONES Y CASILLAS ESPECIALES
    // --------------------------------------------------------

    // MODIFICACIÓN: Comprobamos si el juego ha terminado antes de mover
    public void OnClickBotonDado()
    {
        // Si alguien ya ganó (especialmente la IA), el botón no hace nada
        if (comprobarGanador(1) || comprobarGanador(2))
        {
            botonDado.interactable = false;
            return;
        }

        moverJugador();
    }

    IEnumerator ProcesoMovimiento(int tipoJugador, int pasos)
    {
        int posInicial = (tipoJugador == 1) ? posJugador : posIA;
        int nuevaPos = Mathf.Clamp(posInicial + pasos, 0, 21);

        ActualizarEventos((tipoJugador == 1 ? "Jugador" : "IA") + " va a la casilla " + nuevaPos);

        yield return MoverFichaVisual(tipoJugador, posInicial, nuevaPos);
        ActualizarPosicionLogica(tipoJugador, nuevaPos);

        if (comprobarCasillaEspecial(nuevaPos))
        {
            yield return AplicarEfectoCasilla(tipoJugador, nuevaPos);
            nuevaPos = (tipoJugador == 1) ? posJugador : posIA;
        }

        int posOponente = (tipoJugador == 1) ? posIA : posJugador;
        if (nuevaPos == posOponente && nuevaPos > 0 && nuevaPos < 21)
        {
            ActualizarEventos("Colisión en la casilla " + nuevaPos + "!");
            yield return ResolverColision(tipoJugador, nuevaPos);
        }

        turnoFinalizado = true;
    }

    IEnumerator AplicarEfectoCasilla(int tipo, int posActual)
    {
        int efecto = infoCasillas[posActual];
        panelInfoAdicional.SetActive(true);
        textoInfoAdicional.text = "Casilla Especial! " + ObtenerNombreEfecto(efecto);
        yield return new WaitForSeconds(2f);
        panelInfoAdicional.SetActive(false);

        int destino = posActual;

        switch (efecto)
        {
            case 1:
                destino = (posActual == 1) ? 7 : 13;
                break;
            case 2:
                infoCasillas[posActual] = 0;
                PintarTablero();
                ActualizarEventos("Tira de nuevo!");
                if (tipo == 1) StartCoroutine(ProcesoMovimiento(1, tirarDado()));
                else StartCoroutine(ProcesoMovimiento(2, tirarDado()));
                yield break;
            case -1:
                destino = Mathf.Max(0, posActual - 3);
                break;
        }

        if (destino != posActual)
        {
            yield return MoverFichaVisual(tipo, posActual, destino);
            ActualizarPosicionLogica(tipo, destino);
        }
    }

    IEnumerator ResolverColision(int tipoJugador, int posActual)
    {
        int nuevaPos = posActual;
        if (tipoJugador == 1)
        {
            panelColision.SetActive(true);
            eleccionColision = 0;
            yield return new WaitUntil(() => eleccionColision != 0);
            nuevaPos = (eleccionColision == 1) ? posActual - 1 : posActual + 1;
            panelColision.SetActive(false);
            eleccionColision = 0;
        }
        else
        {
            nuevaPos = DecidirMovimientoIA(posActual);
        }
        nuevaPos = Mathf.Clamp(nuevaPos, 0, 21);
        yield return MoverFichaVisual(tipoJugador, posActual, nuevaPos);
        ActualizarPosicionLogica(tipoJugador, nuevaPos);
    }

    int DecidirMovimientoIA(int pos)
    {
        int anterior = pos - 1;
        int siguiente = pos + 1;
        if (siguiente > 21) return anterior;
        if (anterior < 0) return siguiente;
        if (infoCasillas[siguiente] == 1 || infoCasillas[siguiente] == 2) return siguiente;
        if (infoCasillas[anterior] == 1 || infoCasillas[anterior] == 2) return anterior;
        if (infoCasillas[siguiente] != -1) return siguiente;
        if (infoCasillas[anterior] != -1) return anterior;
        return (Random.value > 0.5f) ? siguiente : anterior;
    }

    IEnumerator MoverFichaVisual(int tipo, int origen, int destino)
    {
        GameObject ficha = (tipo == 1) ? fichaJugador : fichaIA;
        string prefijo = (tipo == 1) ? "POSICION JUGADOR: " : "POSICION IA: ";
        if (origen == destino) yield break;
        int paso = (origen < destino) ? 1 : -1;
        for (int i = origen; i != destino; i += paso)
        {
            int siguiente = i + paso;
            siguiente = Mathf.Clamp(siguiente, 0, 21);
            ficha.transform.position = vectorObjetos[siguiente].transform.position;
            textoPosicion.text = prefijo + siguiente;
            yield return new WaitForSeconds(0.25f);
        }
    }

    void ActualizarPosicionLogica(int tipo, int nuevaPos)
    {
        for (int i = 0; i < vectorCasillas.Length; i++) vectorCasillas[i] = 0;
        if (tipo == 1) posJugador = nuevaPos;
        else posIA = nuevaPos;
        vectorCasillas[posJugador] = 1;
        vectorCasillas[posIA] = 2;
    }

    void ConfigurarTablero()
    {
        infoCasillas[1] = 1; infoCasillas[6] = 1;
        infoCasillas[12] = 2; infoCasillas[18] = 2;
        infoCasillas[5] = -1; infoCasillas[10] = -1;
        infoCasillas[14] = -1; infoCasillas[19] = -1; infoCasillas[20] = -1;
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
                case 1: img.color = new Color(1.0f, 0.64f, 0.0f); break;
                case 2: img.color = Color.green; break;
                case -1: img.color = Color.red; break;
                case 99: img.color = Color.yellow; break;
                case 0: img.color = Color.white; break;
            }
            if (i == 7 || i == 13) img.color = new Color(0.5f, 0.4f, 0.8f);
        }
    }

    void ActualizarEventos(string mensaje)
    {
        textoInfo.text = mensaje;
        Debug.Log(mensaje);
    }

    string ObtenerNombreEfecto(int e)
    {
        if (e == 1) return "Teletransporte";
        if (e == 2) return "Vuelve a tirar";
        if (e == -1) return "Retrocede 3 casillas";
        return "Desconocido";
    }

    public void OnClickAnterior() => eleccionColision = 1;
    public void OnClickSiguiente() => eleccionColision = 2;
    public void ReiniciarJuego() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void SalirJuego() => Application.Quit();
}