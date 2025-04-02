using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class ControllerPuzzle : MonoBehaviour
{
    public List<int> indicesCorrectos = new List<int>();
    public List<ControllerPieze> piezas = new List<ControllerPieze>();

    public Image imagenCompleta;
    public GameObject piezaPrefab;
    public Transform panelPuzzle;
    public Transform panelPiezasDisponibles;
    public GameObject celdaPrefab;
    public Button botonContinuar;

    private FirebaseAuth auth;

    public int filas = 3;
    public int columnas = 3;

    private int nivelSeleccionado = 15;

    public List<Transform> celdas = new List<Transform>();

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        botonContinuar.interactable = false;
        GenerarCeldas();
        StartCoroutine(PrepararPuzzle());
    }

    IEnumerator PrepararPuzzle()
    {
        imagenCompleta.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        imagenCompleta.gameObject.SetActive(false);
        GenerarPiezas();
    }

    private void GenerarCeldas()
    {
        for (int i = 0; i < filas * columnas; i++)
        {
            GameObject nuevaCelda = Instantiate(celdaPrefab, panelPuzzle);
            nuevaCelda.name = "Celda " + i;
            celdas.Add(nuevaCelda.transform);
        }
    }

    public void GenerarPiezas()
    {
        piezas.Clear();
        indicesCorrectos.Clear();

        float anchoPieza = imagenCompleta.rectTransform.rect.width / columnas;
        float altoPieza = imagenCompleta.rectTransform.rect.height / filas;

        // Mezclamos los índices antes de generar las piezas
        List<int> indicesMezclados = Enumerable.Range(0, filas * columnas).OrderBy(x => Random.value).ToList();

        for (int i = 0; i < filas * columnas; i++)
        {
            indicesCorrectos.Add(i);

            // Instancia la pieza en el panel de piezas disponibles
            GameObject nuevaPieza = Instantiate(piezaPrefab, panelPiezasDisponibles);
            RectTransform rectTransform = nuevaPieza.GetComponent<RectTransform>();

            rectTransform.sizeDelta = new Vector2(anchoPieza, altoPieza);
            rectTransform.localScale = Vector3.one;
            rectTransform.SetAsLastSibling();

            // Cargar la parte de la imagen correcta en la pieza
            int indiceMezclado = indicesMezclados[i];
            Sprite spriteRecortado = RecortarSprite(indiceMezclado / columnas, indiceMezclado % columnas, anchoPieza, altoPieza);

            ControllerPieze controller = nuevaPieza.GetComponent<ControllerPieze>();
            if (controller != null)
            {
                controller.Configurar(this, indiceMezclado, spriteRecortado, panelPiezasDisponibles);
                controller.indiceActual = -1;
                piezas.Add(controller);
            }
        }
    }

    public Transform ObtenerCeldaBajoCursor(PointerEventData eventData)
    {
        foreach (Transform celda in celdas)
        {
            RectTransform celdaRect = celda.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(celdaRect, eventData.position))
            {
                return celda;
            }
        }
        return null;
    }

    public void VerificarOrden()
    {
        Debug.Log("🔍 Verificando si el puzzle está completo...");

        foreach (ControllerPieze pieza in piezas)
        {
            Debug.Log($"🧩 Pieza {pieza.indiceCorrecto} está en {pieza.indiceActual}");
        }

        bool esCorrecto = piezas.All(pieza => pieza.indiceActual == pieza.indiceCorrecto);

        if (esCorrecto)
        {
            Debug.Log("✅ ¡Puzzle completo!");
            botonContinuar.interactable = true;

            GameObject gestor = GameObject.Find("GestorProgreso");
            if (gestor == null || auth == null) return;

        }
        else
        {
            Debug.Log("❌ Aún hay piezas fuera de lugar.");
            botonContinuar.interactable = false;
        }
    }


    private Sprite RecortarSprite(int fila, int columna, float ancho, float alto)
    {
        Texture2D textura = imagenCompleta.sprite.texture;

        int x = Mathf.FloorToInt(columna * ancho);
        int y = Mathf.FloorToInt(textura.height - ((fila + 1) * alto));

        Rect rect = new Rect(x, y, ancho, alto);
        Sprite spriteRecortado = Sprite.Create(textura, rect, new Vector2(0.5f, 0.5f));

        return spriteRecortado;
    }
    public void ActualizarIndices()
    {
        Debug.Log("🔄 Actualizando índices de las piezas...");

        for (int i = 0; i < celdas.Count; i++)
        {
            ControllerPieze pieza = celdas[i].GetComponentInChildren<ControllerPieze>();
            if (pieza != null)
            {
                pieza.indiceActual = i;  // El índice en la celda es su posición en el puzzle
                Debug.Log($"🧩 Pieza {pieza.indiceCorrecto} ahora tiene índiceActual = {pieza.indiceActual}");
            }
        }
    }

}
