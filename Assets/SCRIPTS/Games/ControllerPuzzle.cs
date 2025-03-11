using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ControllerPuzzle : MonoBehaviour
{
    public Image imagenCompleta;
    public GameObject piezaPrefab;
    public Transform panelPuzzle;

    private GridLayoutGroup layoutGroup; // ✅ Ahora está declarado correctamente
    public int filas = 3;
    public int columnas = 3;
    private List<ControllerPieze> piezas = new List<ControllerPieze>();

    void Start()
    {
        layoutGroup = panelPuzzle.GetComponent<GridLayoutGroup>(); // ✅ Obtiene el GridLayoutGroup
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false; // 🔴 Desactiva LayoutGroup para evitar la reorganización automática
        }

        StartCoroutine(PrepararPuzzle());
    }

    IEnumerator PrepararPuzzle()
    {
        imagenCompleta.gameObject.SetActive(true);
        yield return new WaitForSeconds(5f);
        imagenCompleta.gameObject.SetActive(false);
        GenerarPiezas();
    }

    void GenerarPiezas()
    {
        piezas.Clear();

        float anchoPieza = imagenCompleta.rectTransform.rect.width / columnas;
        float altoPieza = imagenCompleta.rectTransform.rect.height / filas;

        for (int fila = 0; fila < filas; fila++)
        {
            for (int columna = 0; columna < columnas; columna++)
            {
                GameObject nuevaPieza = Instantiate(piezaPrefab, panelPuzzle);
                RectTransform rectTransform = nuevaPieza.GetComponent<RectTransform>();

                // Asegurar que la pieza pertenece al panelPuzzle
                nuevaPieza.transform.SetParent(panelPuzzle, false);  // ⚠️ Esto es clave para que quede dentro

                // Ajustar tamaño de la pieza
                rectTransform.sizeDelta = new Vector2(anchoPieza, altoPieza);

                // Posición correcta en la cuadrícula dentro del panel
                Vector2 posicionCorrecta = new Vector2(columna * anchoPieza, -fila * altoPieza);
                rectTransform.anchoredPosition = posicionCorrecta;

                // Obtener el sprite recortado
                Sprite spriteRecortado = RecortarSprite(fila, columna, anchoPieza, altoPieza);

                ControllerPieze controller = nuevaPieza.GetComponent<ControllerPieze>();
                if (controller != null)
                {
                    controller.Configurar(this, fila * columnas + columna, spriteRecortado);
                    piezas.Add(controller);
                }
            }
        }
    }

    IEnumerator MostrarImagenCompletaYMezclar()
    {
        imagenCompleta.gameObject.SetActive(true);
        yield return new WaitForSeconds(5);
        imagenCompleta.gameObject.SetActive(false);

        MezclarPiezas();
    }


    void MezclarPiezas()
    {
        for (int i = 0; i < piezas.Count; i++)
        {
            int randomIndex = Random.Range(0, piezas.Count);
            Vector3 tempPos = piezas[i].transform.position;
            piezas[i].transform.position = piezas[randomIndex].transform.position;
            piezas[randomIndex].transform.position = tempPos;
        }
    }

    public void DesactivarLayout()
    {
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
            Debug.Log("[ControllerPuzzle] Layout desactivado para permitir movimiento libre.");
        }
    }

    public void ActivarLayout()
    {
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
            Debug.Log("[ControllerPuzzle] Layout restaurado.");
        }
    }
    public void ValidarPuzzle()
    {
        bool completado = true;

        foreach (ControllerPieze pieza in piezas)
        {
            if (!pieza.EnPosicionCorrecta())
            {
                completado = false;
                break; // Si una pieza está mal, no hace falta seguir verificando
            }
        }

        if (completado)
        {
            Debug.Log("[ControllerPuzzle] 🎉 ¡Puzzle completado correctamente!");
        }
        else
        {
            Debug.Log("[ControllerPuzzle] ❌ Aún hay piezas en posiciones incorrectas.");
        }
    }

    private Sprite RecortarSprite(int fila, int columna, float ancho, float alto)
    {
        Texture2D textura = imagenCompleta.sprite.texture;

        int x = (int)(columna * ancho);
        int y = (int)(textura.height - ((fila + 1) * alto));

        Rect rect = new Rect(x, y, ancho, alto);
        Sprite spriteRecortado = Sprite.Create(textura, rect, new Vector2(0.5f, 0.5f));

        return spriteRecortado;
    }

}
