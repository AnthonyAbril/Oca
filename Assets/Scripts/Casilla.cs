using UnityEngine;

public class Casilla : MonoBehaviour
{
    public int numeroCasilla;

    void Awake()
    {
        // Extrae el número después de la palabra "casilla" (7 caracteres) [cite: 66]
        string casillaString = this.gameObject.name.Substring(7);
        numeroCasilla = int.Parse(casillaString);
    }
}