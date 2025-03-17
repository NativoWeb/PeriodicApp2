using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class Tarjeta : MonoBehaviour
{
    public string elementoNombre; // 🔹 Se llenarán desde código
    public string elementoSimbolo;

    private bool revelada = false;

    public TextMeshProUGUI textoTarjeta;
    public Button botonTarjeta;
    private MemoriaQuimica juego;

    public void ConfigurarTarjeta(string nombre, string simbolo, MemoriaQuimica juegoManager)
    {
        elementoNombre = nombre;
        elementoSimbolo = simbolo;
        textoTarjeta.text = "?";  // 🔹 La carta inicia oculta
        juego = juegoManager;
    }

    public void RevelarTarjeta()
    {
        if (!revelada && juego.PuedeSeleccionar())  // Solo permite si el juego lo permite
        {
            StartCoroutine(VoltearCarta(elementoNombre));
            revelada = true;
            juego.VerificarPareja(this);
        }
    }


    public void OcultarTarjeta()
    {
        StartCoroutine(VoltearCarta("?")); // Vuelve a ocultarse
        revelada = false;
    }

    private IEnumerator VoltearCarta(string nuevoTexto)
    {
        float tiempo = 0.2f;
        float rotacionInicial = 0f;
        float rotacionFinal = 90f;

        while (rotacionInicial < rotacionFinal)
        {
            rotacionInicial += Time.deltaTime * (180 / tiempo);
            transform.rotation = Quaternion.Euler(0, rotacionInicial, 0);
            yield return null;
        }

        textoTarjeta.text = nuevoTexto; // Cambia el texto

        while (rotacionInicial < 180f)
        {
            rotacionInicial += Time.deltaTime * (180 / tiempo);
            transform.rotation = Quaternion.Euler(0, rotacionInicial, 0);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

}
