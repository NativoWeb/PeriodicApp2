using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class PuzzleManager : MonoBehaviour, IDropHandler
{
    public Transform panelGrid; // Grid donde están las piezas
    public Button botonContinuar; // Botón de continuar

    private FirebaseAuth auth;

    private int nivelSeleccionado = 12;


    // Números atómicos en orden correcto (de mayor a menor)
    private int[] ordenCorrecto = { 88, 87, 56, 55, 38, 37, 20, 19, 12, 11, 4, 3 };

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        botonContinuar.interactable = false; // Desactivamos el botón
        DesordenarElementos();
    }

    void DesordenarElementos()
    {
        List<Transform> piezas = new List<Transform>();

        foreach (Transform pieza in panelGrid)
        {
            piezas.Add(pieza);
        }

        piezas = piezas.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < piezas.Count; i++)
        {
            piezas[i].SetSiblingIndex(i);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject objetoArrastrado = eventData.pointerDrag;
        if (objetoArrastrado != null)
        {
            objetoArrastrado.transform.SetParent(panelGrid); // Volvemos al Grid
            objetoArrastrado.transform.SetSiblingIndex(ObtenerIndiceMasCercano(objetoArrastrado));
        }

        VerificarOrden();
    }

    int ObtenerIndiceMasCercano(GameObject objeto)
    {
        float distanciaMenor = float.MaxValue;
        int indiceMasCercano = 0;

        for (int i = 0; i < panelGrid.childCount; i++)
        {
            float distancia = Vector3.Distance(panelGrid.GetChild(i).position, objeto.transform.position);
            if (distancia < distanciaMenor)
            {
                distanciaMenor = distancia;
                indiceMasCercano = i;
            }
        }

        return indiceMasCercano;
    }

    public void VerificarOrden()
    {
        Transform[] piezasEnPanel = panelGrid.GetComponentsInChildren<Transform>()
                                                .Where(t => t != panelGrid) // Excluye el propio panel
                                                .ToArray();

        // Obtener los números atómicos en el orden en que están colocadas las piezas
        List<int> numerosActuales = new List<int>();
        foreach (Transform pieza in piezasEnPanel)
        {
            PuzzlePiece puzzlePiece = pieza.GetComponent<PuzzlePiece>();
            if (puzzlePiece != null)
            {
                numerosActuales.Add(puzzlePiece.numeroAtomico); // Asegúrate de que cada pieza tenga su número
            }
        }

        // Comparamos con el orden correcto
        if (numerosActuales.SequenceEqual(ordenCorrecto))
        {
            Debug.Log("✅ ¡Orden correcto!");
            botonContinuar.interactable = true;

            GameObject gestor = GameObject.Find("GestorProgreso");
            if (gestor == null || auth == null) return;

        }
        else
        {
            Debug.Log("❌ Orden incorrecto. Intenta de nuevo.");
            botonContinuar.interactable = false;
        }
    }

}

