﻿using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using TMPro;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase.Extensions;
using System.Collections;
using Google.Protobuf.WellKnownTypes;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.Video; // Agregar esto al inicio


//using System.Drawing.Text;

public class GuardarMisionCompletada : MonoBehaviour
{
    public Button botonCompletarMision; // Asigna el botón desde el Inspector
    public GameObject imagenMision; // Asigna el objeto desde el Inspector
    public GameObject panel;
    public AudioSource audioSource;
    public TMP_Text TxtXp;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;
    public ParticleSystem particulasMision; // 🌟 Agregar en el Inspector


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
        Handheld.Vibrate();

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
        SceneManager.LoadScene("Escena_Alcalinos"); // Reemplaza con el nombre de la escena destino
    }

    private async void ActualizarMisionEnJSON(string elemento, int idMision)
    {
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("❌ No se encontró el JSON en PlayerPrefs.");
            return;
        }

        var json = JSON.Parse(jsonString);
        if (!json.HasKey("Misiones_Categorias") || !json["Misiones_Categorias"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Estructura del JSON incorrecta o faltan claves principales.");
            return;
        }

        var categorias = json["Misiones_Categorias"]["Categorias"];
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

        foreach (KeyValuePair<string, JSONNode> categoria in categorias)
        {
            var nombreCategoria = categoria.Key;
            var elementos = categoria.Value["Elementos"];

            if (elementos.HasKey(elemento))
            {
                for (int i = 0; i < misiones.Count; i++)
                {
                    var mision = misiones[i];

                    if (mision["id"].AsInt == idMision)
                    {
                        // Validar si YA está completada
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
                            return; // salir sin hacer nada
                        }

                        // Si NO está completada, marcarla como completada y sumar XP
                        mision["completada"] = true;
                        PlayerPrefs.SetString("misionesCategoriasJSON", json.ToString());
                        PlayerPrefs.Save();

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
                        return;
                    }
                }

                Debug.LogWarning("⚠️ Misión con ese ID no encontrada en el elemento.");
            }
        }

        if (cambioRealizado)
        {
            // Guardar JSON actualizado en PlayerPrefs
            PlayerPrefs.SetString("misionesCategoriasJSON", json.ToString());
            PlayerPrefs.Save();
            Debug.Log($"✅ JSON actualizado para la misión {idMision} del elemento {elemento}");

            // Comprobamos conexión
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                await SubirMisionesJSON();
                SumarXPFirebase(xpGanado);

                if (esUltimaMisionPendiente)
                {
                    Debug.Log("🎉 ¡Última misión del elemento completada! Desbloqueando logro...");
                    int xpLogroElemento = 15;
                    SumarXPFirebase(xpLogroElemento);

                }
            }
            else
            {
                SumarXPTemporario(xpGanado);

                if (esUltimaMisionPendiente)
                {
                    Debug.Log("🎉 ¡Última misión del elemento completada sin conexión! Logro guardado.");
                    int xpLogroElemento = 15;
                    SumarXPTemporario(xpLogroElemento);
                }
            }
        }
        else
        {
            Debug.LogError($"❌ No se encontró la misión con ID {idMision} dentro de '{elemento}'.");
        }
    }

    void SumarXPTemporario(int xp)
    {
        int xpTemporal = PlayerPrefs.GetInt("TempXP", 0);
        xpTemporal += xp;
        PlayerPrefs.SetInt("TempXP", xpTemporal);
        PlayerPrefs.Save();
        Debug.Log($"🔄 No hay conexión. XP {xp} guardado en TempXP. Total: {xpTemporal}");
    }

    async void SumarXPFirebase(int xp)
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

        // Obtener JSON de misiones y categorías desde PlayerPrefs
        string jsonMisiones = PlayerPrefs.GetString("misionesCategoriasJSON");

        // Referencias a los documentos dentro de la colección del usuario
        DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");

        // Crear tareas para subir ambos JSONs
        List<Task> tareasSubida = new List<Task>();

        if (jsonMisiones != "{}")
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
            Debug.LogWarning("⚠️ No hay datos de misiones ni categorías para subir.");
            return;
        }

        // Esperar a que todas las tareas finalicen
        await Task.WhenAll(tareasSubida);

        Debug.Log("✅ Datos de misiones y categorías subidos en documentos separados.");
    }
}