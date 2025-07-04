using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using TMPro;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.IO;
using System;


public class GuardarMisionCompletada : MonoBehaviour
{
    public static GuardarMisionCompletada instancia;
    public Button botonCompletarMision; // Asigna el botón desde el Inspector
    public GameObject imagenMision; // Asigna el objeto desde el Inspector
    public GameObject panel;
    public AudioSource audioSource;
    public TMP_Text TxtXp;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;
    public ParticleSystem particulasMision; // 🌟 Agregar en el Inspector
    private string appIdioma;
    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Evitar duplicados
        }
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        var user = auth.CurrentUser;
        if (user != null)
        {
            userId = user.UserId;
        }
        else
        {
            Debug.LogError("❌ No hay usuario autenticado en Start.");
        }

        if (botonCompletarMision != null)
        {
            botonCompletarMision.onClick.AddListener(MarcarMisionComoCompletada);
            botonCompletarMision.onClick.AddListener(AnimacionMisionCompletada);
        }
        else
        {
            Debug.LogError("❌ botonCompletarMision no está asignado en el Inspector.");
        }
    }

    public void MarcarMisionComoCompletada()
    {
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");

        string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
        int idMision = PlayerPrefs.GetInt("MisionActual", -1);

        if (string.IsNullOrEmpty(elemento) || idMision == -1)
        {
            Debug.LogError("❌ No se encontraron datos válidos en PlayerPrefs.");
            return;
        }

        ActualizarMisionEnJSON(elemento, idMision);
    }

    public void AnimacionMisionCompletada()
    {
        if (panel == null || imagenMision == null) return;

        panel.SetActive(true);
        imagenMision.SetActive(true);
        imagenMision.transform.localScale = Vector3.zero;
        audioSource.Play(); // 🔊 Reproduce el sonido

        // 🟢 Activar y reproducir el efecto de partículas
        if (particulasMision != null)
        {
            particulasMision.gameObject.SetActive(true);
            particulasMision.Play();
        }

        Sequence secuenciaAnimacion = DOTween.Sequence();
        secuenciaAnimacion.Append(imagenMision.transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutBounce))
            .Append(imagenMision.transform.DORotate(new Vector3(0, 0, 10f), 0.3f).SetEase(Ease.InOutSine))
            .Append(imagenMision.transform.DORotate(new Vector3(0, 0, -10f), 0.3f).SetEase(Ease.InOutSine))
            .Append(imagenMision.transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.InOutSine))
            .Append(imagenMision.transform.DOMoveY(imagenMision.transform.position.y + 50, 1f).SetEase(Ease.OutQuad))
            .OnComplete(() => {
                if (particulasMision != null)
                {
                    particulasMision.gameObject.SetActive(false);
                }
                CambiarEscena();
            });
    }

    void CambiarEscena()
    {
        SceneManager.LoadScene("Categorías"); 
    }

    private async void ActualizarMisionEnJSON(string elemento, int idMision)
    {
        // 1) Intentar cargar desde archivo
        string jsonString;
        string fileName = "Json_Misiones.json";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(filePath))
        {
            try
            {
                jsonString = File.ReadAllText(filePath);
                Debug.Log("📁 JSON cargado desde almacenamiento del dispositivo");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error al leer el archivo JSON: {e.Message}");
                return;
            }
        }
        else
        {
            // 2) Fallback: cargar desde Resources/Plantillas_Json
            var textAsset = Resources.Load<TextAsset>("Plantillas_Json/Json_Misiones");
            if (textAsset != null)
            {
                jsonString = textAsset.text;
                Debug.Log("📁 JSON cargado desde Resources/Plantillas_Json");
            }
            else
            {
                Debug.LogError($"❌ No se encontró '{fileName}' ni en persistentDataPath ni en Resources/Plantillas_Json");
                return;
            }
        }

        // 3) Validaciones iniciales
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("❌ No se encontró el JSON ni en archivo ni en PlayerPrefs.");
            return;
        }

        var json = JSON.Parse(jsonString);
        if (!json.HasKey("Misiones") || !json["Misiones"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Estructura del JSON incorrecta o faltan claves principales.");
            return;
        }

        var categorias = json["Misiones"]["Categorias"];
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada", "");

        if (appIdioma == "ingles")
            categoriaSeleccionada = devolverCatTrad(categoriaSeleccionada);


        if (!categorias.HasKey(categoriaSeleccionada) ||
            !categorias[categoriaSeleccionada].HasKey("Elementos") ||
            !categorias[categoriaSeleccionada]["Elementos"].HasKey(elemento))
        {
            Debug.LogError($"❌ No se encontró la categoría '{categoriaSeleccionada}' o el elemento '{elemento}' en el JSON.");
            return;
        }

        var elementoJson = categorias[categoriaSeleccionada]["Elementos"][elemento];
        var misiones = elementoJson["misiones"].AsArray;
        bool cambioRealizado = false;
        bool esUltimaMisionPendiente = true;
        int xpGanado = PlayerPrefs.GetInt("xp_mision", 0);

        // 4) Detectar si quedan misiones pendientes distintas a esta
        foreach (JSONNode m in misiones)
        {
            if (m["id"].AsInt != idMision && !m["completada"].AsBool)
            {
                esUltimaMisionPendiente = false;
                break;
            }
        }

        // 5) Buscar y marcar la misión
        for (int i = 0; i < misiones.Count; i++)
        {
            var mision = misiones[i];
            if (mision["id"].AsInt == idMision)
            {
                if (mision["completada"].AsBool)
                {
                    Debug.Log("⚠️ Misión ya estaba completada. Solo se suma 3 XP.");
                    await ProcesarXP(3);
                    return;
                }

                // Marcar completada
                mision["completada"] = true;
                cambioRealizado = true;

                // Guardar cambios físico y en PlayerPrefs
                GuardarJsonActualizado(filePath, json.ToString());

                Debug.Log("✅ Misión completada por primera vez. Sumando XP.");
                int xp = PlayerPrefs.GetInt("xp_mision", 0);
                TxtXp.text = xp.ToString();
                await ProcesarXP(xp);

                // Si es la última pendiente, gestionar logro de elemento
                if (esUltimaMisionPendiente)
                {
                    Debug.Log("🎉 ¡Última misión del elemento completada!");
                    await ProcesarXP(15);

                    MarcarLogroElementoComoDesbloqueado(json, categoriaSeleccionada, elemento);
                    GuardarJsonActualizado(filePath, json.ToString());
                    Debug.Log("💾 JSON actualizado con logro desbloqueado.");
                }
                return;
            }
        }

        if (!cambioRealizado)
            Debug.LogError($"❌ No se encontró la misión con ID {idMision} dentro de '{elemento}'.");
    }

    string devolverCatTrad(string categoriaSeleccionada)
    {
        switch (categoriaSeleccionada)
        {
            case "Alkali Metals":
                return "Metales Alcalinos";

            case "Alkaline Earth Metals":
                return "Metales Alcalinotérreos";

            case "Transition Metals":
                return "Metales de Transición";

            case "Post-transition Metals":
                return "Metales postransicionales";

            case "Metalloids":
                return "Metaloides";

            case "Nonmetals":
                return "No Metales";

            case "Noble Gases":
                return "Gases Nobles";

            case "Lanthanides":
                return "Lantánidos";

            case "Actinides":
                return "Actinoides";

            case "Unknown Properties":
                return "Propiedades desconocidas";

            default:
                return categoriaSeleccionada;
        }
    }

    private void GuardarJsonActualizado(string filePath, string json)
    {
        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log("💾 Cambios guardados en archivo");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error al guardar el archivo: {e.Message}");
        }
        PlayerPrefs.SetString("misionesCategoriasJSON", json);
        PlayerPrefs.Save();
    }

    private async Task ProcesarXP(int xp)
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            await SubirMisionesJSON();
            SumarXPFirebase(xp);
        }
        else
        {
            SumarXPTemporario(xp);
        }
    }

    private void MarcarLogroElementoComoDesbloqueado(JSONNode json, string categoria, string elemento)
    {
        if (!json.HasKey("Logros") || !json["Logros"].HasKey("Categorias"))
        {
            Debug.LogWarning("⚠️ Estructura de logros no encontrada en el JSON.");
            return;
        }

        var categoriasLogros = json["Logros"]["Categorias"];

        if (!categoriasLogros.HasKey(categoria) ||
            !categoriasLogros[categoria].HasKey("logros_elementos") ||
            !categoriasLogros[categoria]["logros_elementos"].HasKey(elemento))
        {
            Debug.LogWarning($"⚠️ No se encontró el logro del elemento '{elemento}' en la categoría '{categoria}'.");
            return;
        }

        var logroElemento = categoriasLogros[categoria]["logros_elementos"][elemento];

        if (logroElemento["desbloqueado"].AsBool)
        {
            Debug.Log("🔓 Logro del elemento ya estaba desbloqueado.");
            return;
        }

        logroElemento["desbloqueado"] = true;
        Debug.Log($"🏅 Logro del elemento '{elemento}' desbloqueado.");
    }

    public void SumarXPTemporario(int xp)
    {
        int xpTemporal = PlayerPrefs.GetInt("TempXP", 0);
        xpTemporal += xp;
        PlayerPrefs.SetInt("TempXP", xpTemporal);
        PlayerPrefs.Save();
        Debug.Log($"🔄 No hay conexión. XP {xp} guardado en TempXP. Total: {xpTemporal}");
    }

    public async void SumarXPFirebase(int xp)
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(user.UserId);

        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            int xpActual = 0;

            if (snapshot.Exists && snapshot.TryGetValue<int>("xp", out int valorXP))
            {
                xpActual = valorXP;
            }

            int xpNuevo = xpActual + xp;


            await userRef.UpdateAsync("xp", xpNuevo);
            Debug.Log($"✅ XP actualizado en Firebase: {xpNuevo}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al actualizar XP en Firebase: {e.Message}");
        }
    }

    public async Task SubirMisionesJSON()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        string jsonMisiones = "";
        string filePath = Path.Combine(Application.persistentDataPath, "Json_misiones.json");

        // Primero intentar leer el archivo JSON del almacenamiento del dispositivo
        if (File.Exists(filePath))
        {
            try
            {
                jsonMisiones = File.ReadAllText(filePath);
                Debug.Log("📁 JSON encontrado en almacenamiento del dispositivo");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error al leer el archivo JSON: {e.Message}");
                return;
            }
        }
        else
        {
            // Si no existe en el almacenamiento, usar el de PlayerPrefs como respaldo
            jsonMisiones = PlayerPrefs.GetString("misionesCategoriasJSON");
            Debug.Log("📁 Usando JSON de PlayerPrefs (no se encontró archivo)");
        }

        // Referencias a los documentos dentro de la colección del usuario
        DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");

        // Crear tareas para subir ambos JSONs
        List<Task> tareasSubida = new List<Task>();

        if (!string.IsNullOrEmpty(jsonMisiones) && jsonMisiones != "{}")
        {
            Dictionary<string, object> dataMisiones = new Dictionary<string, object>
        {
            { "misiones", jsonMisiones },
            { "timestamp", FieldValue.ServerTimestamp }
        };
            tareasSubida.Add(misionesDoc.SetAsync(dataMisiones, SetOptions.MergeAll));
        }

        if (tareasSubida.Count == 0)
        {
            Debug.LogWarning("⚠️ No hay datos de misiones para subir.");
            return;
        }

        // Esperar a que todas las tareas finalicen
        await Task.WhenAll(tareasSubida);

        Debug.Log("✅ Datos de misiones subidos correctamente.");
    }
}