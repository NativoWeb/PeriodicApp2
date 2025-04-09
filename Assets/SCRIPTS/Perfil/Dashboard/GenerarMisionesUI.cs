using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using static GeneradorElementosUI;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class GeneradorElementosUI : MonoBehaviour
{
    public GameObject prefabElemento; // Prefab con Image y TMP
    public Transform contenedor; // Donde colocar los elementos
    public Color colorCompletado = Color.green;
    public Color colorIncompleto = Color.gray;
    public Button BtnRanking;

    public TMP_Text TotalMisionesCompletadas;
    public TMP_Text TotalLogrosDesbloqueados;
    public TMP_Text TotalXP;
    public Image avatarImage;

    public TMP_Text DisplayName;
    public TMP_Text Rango;

    private JSONNode jsonData;

    [System.Serializable]
    public class Mision
    {
        public int id;
        public string titulo;
        public string descripcion;
        public string tipo;
        public bool completada;
        public string rutaescena;
    }

    [System.Serializable]
    public class Elemento
    {
        public string nombre;
        public string simbolo;
        public List<Mision> misiones = new List<Mision>();

        public bool EstaCompletado()
        {
            return misiones != null && misiones.Count > 0 && misiones.All(m => m.completada);
        }
    }

    private void Awake()
    {
        CargarJSON();
        ActualizarTotales();
        BtnRanking.onClick.AddListener(MostrarRanking);
    }

    void Start()
    {
        if (jsonData == null || !jsonData.HasKey("Misiones_Categorias") || !jsonData["Misiones_Categorias"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Error: Estructura del JSON no válida.");
            return;
        }

        var categoriasJson = jsonData["Misiones_Categorias"]["Categorias"];

        foreach (KeyValuePair<string, JSONNode> categoria in categoriasJson)
        {
            var categoriaNombre = categoria.Key;
            var categoriaData = categoria.Value;

            if (!categoriaData.HasKey("Elementos")) continue;

            var elementosJson = categoriaData["Elementos"];

            foreach (KeyValuePair<string, JSONNode> elemento in elementosJson)
            {
                string simboloElemento = elemento.Key;
                JSONNode datosElemento = elemento.Value;

                Elemento nuevoElemento = new Elemento
                {
                    simbolo = datosElemento["simbolo"],
                    nombre = datosElemento["nombre"],
                    misiones = new List<Mision>()
                };

                if (datosElemento.HasKey("misiones"))
                {
                    foreach (JSONNode m in datosElemento["misiones"].AsArray)
                    {
                        Mision nuevaMision = new Mision
                        {
                            id = m["id"].AsInt,
                            titulo = m["titulo"],
                            descripcion = m["descripcion"],
                            tipo = m["tipo"],
                            completada = m["completada"].AsBool,
                            rutaescena = m["rutaescena"]
                        };
                        nuevoElemento.misiones.Add(nuevaMision);
                    }
                }
                GenerarElementoUI(nuevoElemento);
            }
        }
    }

    private void CargarJSON()
    {
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON");

        if (string.IsNullOrEmpty(jsonString))
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("Misiones_Categorias");
            if (jsonFile != null)
            {
                jsonString = jsonFile.text;
                PlayerPrefs.SetString("misionesCategoriasJSON", jsonString);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError("❌ No se encontró el archivo JSON en Resources.");
                return;
            }
        }

        jsonData = JSON.Parse(jsonString);
    }

    void GenerarElementoUI(Elemento elemento)
    {
        GameObject nuevo = Instantiate(prefabElemento, contenedor);

        // Color
        Image imagen = nuevo.GetComponent<Image>();
        if (imagen != null)
        {
            imagen.color = elemento.EstaCompletado() ? colorCompletado : colorIncompleto;
        }
        else
        {
            Debug.LogWarning("⚠ Prefab no tiene componente Image.");
        }

        // Texto TMP
        TextMeshProUGUI tmp = nuevo.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = elemento.simbolo;
        }
        else
        {
            Debug.LogWarning("⚠ Prefab no tiene un TMP como hijo.");
        }
    }

    void ActualizarTotales()
    {
        int totalMisiones = 0;
        int totalLogros = 0;

        var categoriasJson = jsonData["Misiones_Categorias"]["Categorias"];

        foreach (KeyValuePair<string, JSONNode> categoria in categoriasJson)
        {
            var categoriaData = categoria.Value;
            if (!categoriaData.HasKey("Elementos")) continue;

            var elementosJson = categoriaData["Elementos"];
            int logrosPorCategoria = 0;
            int misionesPorCategoria = 0;

            Debug.Log($"📁 Categoría: {categoria.Key}");

            // 🔹 Contar misiones y logros por elemento
            foreach (KeyValuePair<string, JSONNode> elemento in elementosJson)
            {
                JSONNode datosElemento = elemento.Value;
                if (!datosElemento.HasKey("misiones")) continue;

                var misiones = datosElemento["misiones"].AsArray;

                int completadasElemento = 0;
                bool todasCompletadas = true;

                foreach (JSONNode m in misiones)
                {
                    bool completada = m["completada"].AsBool;
                    if (completada)
                    {
                        totalMisiones++;
                        misionesPorCategoria++;
                        completadasElemento++;
                    }
                    else
                    {
                        todasCompletadas = false;
                    }
                }

                if (todasCompletadas && misiones.Count > 0)
                {
                    totalLogros++;
                    logrosPorCategoria++;
                }

                Debug.Log($"🔬 Elemento: {datosElemento["nombre"]} ({datosElemento["simbolo"]}) | {completadasElemento}/{misiones.Count} misiones | {(todasCompletadas ? "🏆 Logro" : "⏳ Incompleto")}");
            }

            // 🔹 Revisar misión final de la categoría
            if (categoriaData.HasKey("mision_final"))
            {
                var misionFinal = categoriaData["mision_final"];
                if (misionFinal["completada"].AsBool)
                {
                    totalMisiones++; // +1 misión
                    totalLogros++;   // +1 logro
                    misionesPorCategoria++;
                    logrosPorCategoria++;

                    Debug.Log($"🏁 Misión final de categoría '{categoria.Key}' completada. ¡+1 Misión y +1 Logro!");
                }
                else
                {
                    Debug.Log($"⏳ Misión final de categoría '{categoria.Key}' no completada.");
                }
            }

            Debug.Log($"📊 Resumen '{categoria.Key}': Misiones Completadas = {misionesPorCategoria}, Logros = {logrosPorCategoria}");
        }

        // 🔹 Mostrar totales en UI
        TotalMisionesCompletadas.text = totalMisiones.ToString();
        TotalLogrosDesbloqueados.text = totalLogros.ToString();
        ActualizarDatosUsuario();
        Debug.Log($"✅ TOTAL GLOBAL: Misiones = {totalMisiones}, Logros = {totalLogros}");
    }
    void ActualizarDatosUsuario()
    {
        bool hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (!hayInternet)
        {
            // 📴 Sin conexión: mostrar desde PlayerPrefs
            int xpLocal = PlayerPrefs.GetInt("TempXP", 0);
            string nombreLocal = PlayerPrefs.GetString("DisplayName", "Sin nombre");

            TotalXP.text = xpLocal.ToString();
            DisplayName.text = nombreLocal;

            // Obtener rango por XP
            string rangoLocal = ObtenerRangoSegunXP(xpLocal);
            Rango.text = rangoLocal;
            PlayerPrefs.SetString("Rango", rangoLocal); // actualiza el rango local

            // 🖼 Avatar offline
            string rutaAvatar = ObtenerAvatarPorRango(rangoLocal);
            Sprite avatar = Resources.Load<Sprite>(rutaAvatar);
            if (avatar != null) avatarImage.sprite = avatar;
            else Debug.LogWarning("⚠ Avatar no encontrado en ruta: " + rutaAvatar);

            Debug.Log("📡 Sin internet. Datos cargados desde PlayerPrefs.");
            return;
        }

        // 🌐 Con internet: cargar desde Firestore
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogWarning("⚠ No hay usuario autenticado.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(user.UserId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                var snapshot = task.Result;

                // XP
                int xp = snapshot.ContainsField("xp") ? snapshot.GetValue<int>("xp") : 0;
                TotalXP.text = xp.ToString();
                PlayerPrefs.SetInt("TempXP", xp);

                // Display Name
                string displayName = snapshot.ContainsField("DisplayName") ? snapshot.GetValue<string>("DisplayName") : "Sin nombre";
                DisplayName.text = displayName;
                PlayerPrefs.SetString("DisplayName", displayName);

                // Rango calculado por XP
                string rango = ObtenerRangoSegunXP(xp);
                Rango.text = rango;
                PlayerPrefs.SetString("Rango", rango);

                // 🖼 Avatar online
                string rutaAvatar = ObtenerAvatarPorRango(rango);
                Sprite avatar = Resources.Load<Sprite>(rutaAvatar);
                if (avatar != null) avatarImage.sprite = avatar;
                else Debug.LogWarning("⚠ Avatar no encontrado en ruta: " + rutaAvatar);

                PlayerPrefs.Save();

                Debug.Log("✅ Datos cargados correctamente desde Firestore.");
            }
            else
            {
                Debug.LogWarning("⚠ No se encontró el documento del usuario en Firestore.");
            }
        });
    }

    private void MostrarRanking()
    {
        PlayerPrefs.SetString("PanelRanking", "PanelRanking");
        PlayerPrefs.Save();

        SceneManager.LoadScene("ranking");
    }

    private string ObtenerRangoSegunXP(int xp)
    {
        if (xp >= 10000) return "Leyenda química";
        if (xp >= 6000) return "Sabio de la tabla";
        if (xp >= 3500) return "Maestro de Laboratorio";
        if (xp >= 2300) return "Experto Molecular";
        if (xp >= 1200) return "Cientifico en Formacion";
        if (xp >= 600) return "Promesa quimica";
        if (xp >= 200) return "Aprendiz Atomico";
        return "Novato de laboratorio";
    }

    // ✅ Avatar según rango
    private string ObtenerAvatarPorRango(string rangos)
    {
        switch (rangos)
        {
            case "Novato de laboratorio": return "Avatares/Rango1";
            case "Aprendiz Atomico": return "Avatares/Rango2";
            case "Promesa quimica": return "Avatares/Rango3";
            case "Cientifico en Formacion": return "Avatares/Rango4";
            case "Experto Molecular": return "Avatares/Rango5";
            case "Maestro de Laboratorio": return "Avatares/Rango6";
            case "Sabio de la tabla": return "Avatares/Rango7";
            case "Leyenda química": return "Avatares/Rango8";
            default: return "Avatares/Rango1";
        }
    }
}
