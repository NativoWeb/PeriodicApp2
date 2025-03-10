using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PuzzleManager : MonoBehaviour
{
    public Image imagenADividir;
    public int filas = 3;
    public int columnas = 3;
    public GameObject piezaPrefab;
    public Transform panelPiezas;
    public Transform panelTablero;

    private Sprite[,] piezasSprites;

    void Start()
    {
        if (imagenADividir == null || piezaPrefab == null || panelPiezas == null || panelTablero == null)
        {
            Debug.LogError("❌ Asegúrate de asignar todos los objetos en el Inspector.");
            return;
        }

        GenerarPiezas();
    }

    void GenerarPiezas()
    {
        Texture2D texturaOriginal = imagenADividir.sprite.texture;
        int anchoPieza = texturaOriginal.width / columnas;
        int altoPieza = texturaOriginal.height / filas;

        piezasSprites = new Sprite[filas, columnas];
        List<Vector3> posicionesTablero = new List<Vector3>();

        Debug.Log($"🧩 Generando {filas * columnas} piezas...");

        for (int fila = 0; fila < filas; fila++)
        {
            for (int columna = 0; columna < columnas; columna++)
            {
                int yInvertido = texturaOriginal.height - (fila + 1) * altoPieza;
                Rect rect = new Rect(columna * anchoPieza, yInvertido, anchoPieza, altoPieza);

                Texture2D piezaTextura = new Texture2D(anchoPieza, altoPieza);
                piezaTextura.SetPixels(texturaOriginal.GetPixels((int)rect.x, (int)rect.y, anchoPieza, altoPieza));
                piezaTextura.Apply();

                Sprite piezaSprite = Sprite.Create(piezaTextura, new Rect(0, 0, anchoPieza, altoPieza), new Vector2(0.5f, 0.5f), 100f);
                piezasSprites[fila, columna] = piezaSprite;

                GameObject nuevaPieza = Instantiate(piezaPrefab, panelPiezas);
                nuevaPieza.transform.localPosition = Vector3.zero; // 🔄 Asegurar posición correcta
                PuzzlePiece puzzlePiece = nuevaPieza.GetComponent<PuzzlePiece>();

                if (puzzlePiece != null)
                {
                    Vector3 posicionCorrecta = ObtenerPosicionTablero(fila, columna);
                    posicionesTablero.Add(posicionCorrecta);
                    puzzlePiece.ConfigurarPieza(piezaSprite, posicionCorrecta, panelPiezas, panelTablero);
                }
                else
                {
                    Debug.LogError("❌ El prefab de la pieza no tiene el script 'PuzzlePiece'.");
                }
            }
        }

        // 🔄 Mezclar las posiciones antes de asignarlas
        posicionesTablero = posicionesTablero.OrderBy(x => Random.value).ToList();

        Debug.Log("✅ Piezas generadas y mezcladas.");
    }

    Vector3 ObtenerPosicionTablero(int fila, int columna)
    {
        int index = fila * columnas + columna;
        if (index >= panelTablero.childCount)
        {
            Debug.LogError($"❌ El panelTablero no tiene suficientes espacios. Faltan {index - panelTablero.childCount + 1} elementos.");
            return Vector3.zero;
        }

        return panelTablero.GetChild(index).position;
    }
}
