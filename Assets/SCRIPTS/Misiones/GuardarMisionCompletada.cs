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
        SceneManager.LoadScene("Categorias"); 
    }

    private async void ActualizarMisionEnJSON(string elemento, int idMision)
    {
        // Primero intentar cargar desde archivo
        string jsonString = "";
        string filePath = Path.Combine(Application.persistentDataPath, "Json_misiones.json");

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
            jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
            Debug.Log("📁 Usando JSON de PlayerPrefs (no se encontró archivo)");
        }

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

        // Verificar si hay misiones pendientes en el mismo elemento
        foreach (JSONNode mision in misiones) // Especificar el tipo JSONNode
        {
            if (mision["id"].AsInt != idMision && !mision["completada"].AsBool)
            {
                esUltimaMisionPendiente = false;
                break;
            }
        }

        // Corregido: Acceder correctamente a los valores del KeyValuePair
        foreach (var categoria in categorias)
        {
            var elementos = categoria.Value["Elementos"]; // Accedemos al Value primero

            if (elementos.HasKey(elemento))
            {
                for (int i = 0; i < misiones.Count; i++)
                {
                    var mision = misiones[i];

                    if (mision["id"].AsInt == idMision)
                    {
                        if (mision["completada"].AsBool)
                        {
                            Debug.Log("⚠️ Misión ya estaba completada. Solo se suma 3 XP.");
                            if (Application.internetReachability != NetworkReachability.NotReachable)
                            {
                                await SubirMisionesJSON();
                                SumarXPFirebase(3);
                                TxtXp.text = 3.ToString();
                            }
                            else
                            {
                                SumarXPTemporario(3);
                                TxtXp.text = 3.ToString();
                            }
                            return;
                        }

                        mision["completada"] = true;
                        cambioRealizado = true;

                        try
                        {
                            File.WriteAllText(filePath, json.ToString());
                            PlayerPrefs.SetString("misionesCategoriasJSON", json.ToString());
                            PlayerPrefs.Save();
                            Debug.Log("💾 Cambios guardados en archivo y PlayerPrefs");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"❌ Error al guardar el archivo: {e.Message}");
                            PlayerPrefs.SetString("misionesCategoriasJSON", json.ToString());
                            PlayerPrefs.Save();
                        }

                        Debug.Log("✅ Misión completada por primera vez. Sumando XP.");
                        int xp = PlayerPrefs.GetInt("xp_mision", 0);
                        TxtXp.text = xp.ToString();

                        if (Application.internetReachability != NetworkReachability.NotReachable)
                        {
                            await SubirMisionesJSON();
                            SumarXPFirebase(xp);
                        }
                        else
                        {
                            SumarXPTemporario(xp);
                        }

                        if (esUltimaMisionPendiente)
                        {
                            Debug.Log("🎉 ¡Última misión del elemento completada!");
                            int xpLogroElemento = 15;
                            if (Application.internetReachability != NetworkReachability.NotReachable)
                            {
                                SumarXPFirebase(xpLogroElemento);
                            }
                            else
                            {
                                SumarXPTemporario(xpLogroElemento);
                            }
                        }
                        return;
                    }
                }
                Debug.LogWarning("⚠️ Misión con ese ID no encontrada en el elemento.");
            }
        }

        if (!cambioRealizado)
        {
            Debug.LogError($"❌ No se encontró la misión con ID {idMision} dentro de '{elemento}'.");
        }
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