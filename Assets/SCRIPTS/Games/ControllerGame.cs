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

                    GuardarProgresoAutomatico();
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

    private async void GuardarProgresoAutomatico()
    {
        string userId = auth.CurrentUser.UserId;

        // Referencias a Firestore
        DocumentReference docGrupo = db.Collection("users").Document(userId)
                                          .Collection("grupos").Document("grupo 1");
        DocumentReference docUsuario = db.Collection("users").Document(userId);

        // Obtener datos actuales
        DocumentSnapshot snapshotGrupo = await docGrupo.GetSnapshotAsync();
        DocumentSnapshot snapshotUsuario = await docUsuario.GetSnapshotAsync();

        int nivelDesbloqueado = 1; // Nivel por defecto si no existe
        int xpActual = 0; // XP inicial si no existe el campo
        int xpGanado = 100; // 🔹 Ajusta el XP según el nivel

        // Verificar si la colección y el documento existen para "grupo 1"
        if (!snapshotGrupo.Exists)
        {
            // Si no existe el documento "grupo 1", lo creamos con valores predeterminados
            await docGrupo.SetAsync(new Dictionary<string, object>
        {
            { "nivel", nivelDesbloqueado }
        });

            Debug.Log("✅ Documento 'grupo 1' creado con nivel predeterminado.");
        }
        else
        {
            // Si existe, obtenemos el valor del campo "nivel"
            snapshotGrupo.TryGetValue<int>("nivel", out nivelDesbloqueado);
        }

        // Verificar si el documento "usuario" existe
        if (snapshotUsuario.Exists)
        {
            // Obtener el valor actual de XP
            snapshotUsuario.TryGetValue<int>("xp", out xpActual);
        }
        else
        {
            // Si no existe, creamos el documento "usuario" con un XP inicial de 0
            await docUsuario.SetAsync(new Dictionary<string, object>
        {
            { "xp", xpActual }
        });

            Debug.Log("✅ Documento 'usuario' creado con XP inicial.");
        }

        // Actualizar nivel en grupo1
        await docGrupo.UpdateAsync(new Dictionary<string, object>
    {
        { "nivel", nivelDesbloqueado + 1 }
    });

        // Actualizar XP en usuario
        await docUsuario.UpdateAsync(new Dictionary<string, object>
    {
        { "xp", xpActual + xpGanado }
    });

        Debug.Log($"✅ Nivel actualizado a {nivelDesbloqueado + 1} en grupo1");
        Debug.Log($"✅ XP actualizado a {xpActual + xpGanado} en usuario");
    }



    public void OnContinuarClick()
    {
        Debug.Log("➡️ Volviendo a la escena de niveles...");
        SceneManager.LoadScene("Grupo1");
    }
}
