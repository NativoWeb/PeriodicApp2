using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ControllerGame : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private Vector3 posicionInicial;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private static int emparejamientosCorrectos = 0;
    private static int totalEmparejamientos = 4;

    [SerializeField] private Button botonContinuar;
    private static List<Vector3> posicionesIniciales = new List<Vector3>();

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("❌ Error: No se ha asignado el botón 'BotonContinuar' en el Inspector.");
        }

        // Generar posiciones aleatorias una sola vez
        if (posicionesIniciales.Count == 0)
        {
            GenerarPosicionesAleatorias();
        }

        // Asignar una posición aleatoria única a cada objeto
        if (posicionesIniciales.Count > 0)
        {
            int index = Random.Range(0, posicionesIniciales.Count);
            posicionInicial = posicionesIniciales[index];
            posicionesIniciales.RemoveAt(index); // Evitar repetir posiciones
            rectTransform.position = posicionInicial;
        }
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
                Debug.Log($"✅ Emparejamiento correcto: {gameObject.name} con {objetoSoltado.name}");

                Destroy(gameObject);
                Destroy(objetoSoltado);

                emparejamientosCorrectos++;

                if (emparejamientosCorrectos >= totalEmparejamientos && botonContinuar != null)
                {
                    Debug.Log("🎉 Todos los emparejamientos correctos. Activando el botón...");
                    botonContinuar.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.Log("❌ Emparejamiento incorrecto. Reintentando...");
                rectTransform.position = posicionInicial;
            }
        }
        else
        {
            rectTransform.position = posicionInicial;
        }
    }

    private bool ComprobarEmparejamiento(string simbolo, string nombreElemento)
    {
        return (simbolo == "K" && nombreElemento == "Potasio") ||
               (simbolo == "Rb" && nombreElemento == "Rubidio") ||
               (simbolo == "Na" && nombreElemento == "Sodio") ||
               (simbolo == "Li" && nombreElemento == "Litio");
    }

    private void GenerarPosicionesAleatorias()
    {
        posicionesIniciales.Clear();
        posicionesIniciales.Add(new Vector3(250, 1930, 0));
        posicionesIniciales.Add(new Vector3(250, 1500, 0));
        posicionesIniciales.Add(new Vector3(250, 1050, 0));
        posicionesIniciales.Add(new Vector3(250, 580, 0));

        // Mezclar posiciones para mayor aleatoriedad
        for (int i = 0; i < posicionesIniciales.Count; i++)
        {
            Vector3 temp = posicionesIniciales[i];
            int randomIndex = Random.Range(i, posicionesIniciales.Count);
            posicionesIniciales[i] = posicionesIniciales[randomIndex];
            posicionesIniciales[randomIndex] = temp;
        }
    }
}
