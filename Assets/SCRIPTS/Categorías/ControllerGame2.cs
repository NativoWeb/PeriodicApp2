using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ControllerGame2 : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private Vector3 posicionInicial;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    public static int emparejamientosCorrectos = 0;
    public static int totalEmparejamientos = 3; // Ajusta según cuántos compuestos quieras formar
    public GameObject botonContinuar;

    // Diccionario de combinaciones químicas correctas
    private Dictionary<string, string> compuestosQuimicos = new Dictionary<string, string>()
    {
        {"Na", "Cl"},  // Na + Cl → NaCl
        {"K", "OH"},   // K + OH → KOH
        {"Li", "O"}    // Li + O → Li₂O
    };

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        posicionInicial = rectTransform.position;

        if (botonContinuar != null)
            botonContinuar.SetActive(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = Input.mousePosition;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        GameObject objetoSoltado = eventData.pointerCurrentRaycast.gameObject;

        if (objetoSoltado != null && objetoSoltado.CompareTag("ZonaEmparejamiento"))
        {
            if (ComprobarEmparejamiento(gameObject.name, objetoSoltado.name))
            {
                Debug.Log("Emparejamiento Correcto: " + gameObject.name + " + " + objetoSoltado.name);
                Destroy(gameObject);
                Destroy(objetoSoltado);
                emparejamientosCorrectos++;

                // Si se completan todos los emparejamientos, activar botón
                if (emparejamientosCorrectos >= totalEmparejamientos && botonContinuar != null)
                {
                    botonContinuar.SetActive(true);
                }
            }
            else
            {
                rectTransform.position = posicionInicial;
            }
        }
        else
        {
            rectTransform.position = posicionInicial;
        }
    }

    private bool ComprobarEmparejamiento(string metal, string elemento)
    {
        return compuestosQuimicos.ContainsKey(metal) && compuestosQuimicos[metal] == elemento;
    }
}
