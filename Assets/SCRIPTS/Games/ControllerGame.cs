using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;

public class ControllerGame : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private Vector3 posicionInicial;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private static int emparejamientosCorrectos = 0;
    private static int totalEmparejamientos = 4;

    [SerializeField] private Button botonContinuar;
    private static List<Vector3> posicionesIniciales = new List<Vector3>();

    private int xpGanadoPorNivel = 100;
    private int nivelSeleccionado = 3;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(false);
            botonContinuar.onClick.AddListener(OnContinuarClick);
        }
        else
        {
            Debug.LogError("❌ Error: No se ha asignado el botón 'BotonContinuar' en el Inspector.");
        }

        if (posicionesIniciales.Count == 0)
        {
            GenerarPosicionesAleatorias();
        }

        if (posicionesIniciales.Count > 0)
        {
            int index = Random.Range(0, posicionesIniciales.Count);
            posicionInicial = posicionesIniciales[index];
            posicionesIniciales.RemoveAt(index);
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

                    GameObject gestor = GameObject.Find("GestorProgreso");
                    if (gestor == null || auth == null) return;

                    GuardarProgreso gp = gestor.GetComponent<GuardarProgreso>();
                    if (gp == null) return;

                    gp.GuardarProgresoFirestore(nivelSeleccionado + 1, emparejamientosCorrectos, auth); // GUARDAR EN LA BASE DE DATOS

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

        for (int i = 0; i < posicionesIniciales.Count; i++)
        {
            Vector3 temp = posicionesIniciales[i];
            int randomIndex = Random.Range(i, posicionesIniciales.Count);
            posicionesIniciales[i] = posicionesIniciales[randomIndex];
            posicionesIniciales[randomIndex] = temp;
        }
    }

    public void OnContinuarClick()
    {
        Debug.Log("➡️ Volviendo a la escena de niveles...");
        SceneManager.LoadScene("Grupo1");
    }
}
