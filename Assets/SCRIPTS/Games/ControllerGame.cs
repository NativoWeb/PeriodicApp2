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

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private int nivelactual = 2;
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

                    GuardarProgresoAutomatico(nivelactual);
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

    private async void GuardarProgresoAutomatico(int nivelActualJugado)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ Usuario no autenticado.");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        // Referencias a Firestore
        DocumentReference docGrupo = db.Collection("users").Document(userId)
                                          .Collection("grupos").Document("grupo 1");
        DocumentReference docUsuario = db.Collection("users").Document(userId);

        try
        {
            // Obtener datos actuales
            DocumentSnapshot snapshotGrupo = await docGrupo.GetSnapshotAsync();
            DocumentSnapshot snapshotUsuario = await docUsuario.GetSnapshotAsync();

            int nivelAlmacenado = 1; // Nivel por defecto si no existe
            int xpActual = 0; // XP inicial si no existe el campo
            int xpGanado = 100; // 🔹 Ajusta el XP según el nivel

            // Verificar si el documento "grupo 1" existe
            if (!snapshotGrupo.Exists)
            {
                await docGrupo.SetAsync(new Dictionary<string, object> { { "nivel", nivelAlmacenado } });
                Debug.Log("✅ Documento 'grupo 1' creado con nivel predeterminado.");
            }
            else
            {
                snapshotGrupo.TryGetValue<int>("nivel", out nivelAlmacenado);
            }

            // Verificar si el documento "usuario" existe
            if (snapshotUsuario.Exists)
            {
                snapshotUsuario.TryGetValue<int>("xp", out xpActual);
            }
            else
            {
                await docUsuario.SetAsync(new Dictionary<string, object> { { "xp", xpActual } });
                Debug.Log("✅ Documento 'usuario' creado con XP inicial.");
            }

            // Determinar si se debe subir de nivel o solo sumar XP
            bool subirNivel = nivelActualJugado > nivelAlmacenado;
            int nuevoNivel = subirNivel ? nivelActualJugado : nivelAlmacenado;
            int nuevoXp = xpActual + xpGanado;

            // Actualizar XP en usuario
            await docUsuario.UpdateAsync(new Dictionary<string, object> { { "xp", nuevoXp } });

            if (subirNivel)
            {
                // Actualizar nivel en grupo1 solo si es un nivel más alto
                await docGrupo.UpdateAsync(new Dictionary<string, object> { { "nivel", nuevoNivel } });
                Debug.Log($"✅ Nivel actualizado a {nuevoNivel} en grupo1");
            }
            else
            {
                Debug.Log($"🔹 Nivel no cambiado (jugaste un nivel ya registrado: {nivelAlmacenado})");
            }

            Debug.Log($"✅ XP actualizado a {nuevoXp} en usuario");

            // Guardar en PlayerPrefs para compatibilidad
            PlayerPrefs.SetInt("nivelCompletado", nuevoNivel);
            PlayerPrefs.SetInt("xp", nuevoXp);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al guardar el progreso: {e.Message}");
        }
    }




    public void OnContinuarClick()
    {
        Debug.Log("➡️ Volviendo a la escena de niveles...");
        SceneManager.LoadScene("Grupo1");
    }
}
