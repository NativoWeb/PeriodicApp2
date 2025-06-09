using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class GeneradorElementosUI : MonoBehaviour
{
    [Header("Paneles y botones")]
    public GameObject PanelDatos;
    public Button BtnDatos;
    public GameObject PanelLogros;
    public Button BtnLogros;
    public GameObject PanelNotificaciones;
    public Button BtnNotificaciones;

    [Header("Datos Basicos")]
    public TMP_Text TotalMisionesCompletadas;
    public TMP_Text TotalLogrosDesbloqueados;
    public TMP_Text TotalXP;
    public TMP_Text PosicioRanking;
    public Image avatarImage;
    public TMP_Text DisplayName;
    public TMP_Text Rango;

    private JSONNode jsonData;

    private string userId;
    private string rango;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

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
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();

        CargarJSON();
        BtnDatos.onClick.AddListener(AbrirPanelDatos);
        BtnLogros.onClick.AddListener(AbrirPanelLogros);
        BtnNotificaciones.onClick.AddListener(AbrirPanelNotificaciones);
    }

    private void CargarJSON()
    {
        string rutaArchivo = Path.Combine(Application.persistentDataPath, "Json_Misiones.json");
        Debug.Log(rutaArchivo);

        if (File.Exists(rutaArchivo))
        {
            string jsonString = File.ReadAllText(rutaArchivo);
            jsonData = JSON.Parse(jsonString);
            Debug.Log("✅ Json_Misiones.json cargado desde persistentDataPath." + rutaArchivo);

            // Ya que jsonData se cargó, podemos actualizar totales:
            ActualizarTotales();
            ObtenerPosicionUsuario();
        }
        else
        {
            Debug.LogWarning("⚠️ Json_Misiones.json no encontrado en persistentDataPath, intentando cargar desde StreamingAssets...");
            StartCoroutine(CargarDesdeResources("Json_Misiones.json", (json) =>
            {
                jsonData = JSON.Parse(json);
                Debug.Log("📄 Json_Misiones.json cargado temporalmente desde StreamingAssets.");

                // Aquí ahora sí llamar a ActualizarTotales y ObtenerPosicionUsuario
                ActualizarTotales();
                ObtenerPosicionUsuario();
            }));
        }
    }

    void Start()
    {
        CargarJSON();

        if (jsonData == null || !jsonData.HasKey("Misiones") || !jsonData["Misiones"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Error: Estructura del JSON no válida.");
            return;
        }

        var categoriasJson = jsonData["Misiones"]["Categorias"];

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
                    simbolo = simboloElemento,
                    nombre = simboloElemento, // O usa otro campo si está disponible
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
            }
        }

        ActualizarTotales();
    }

    private IEnumerator CargarDesdeResources(string nombreArchivo, System.Action<string> callback)
    {
        string ruta = $"Plantillas_Json/{Path.GetFileNameWithoutExtension(nombreArchivo)}";

        TextAsset archivo = Resources.Load<TextAsset>(ruta);

        yield return null; // Necesario para que funcione como Coroutine

        if (archivo != null)
        {
            if (string.IsNullOrEmpty(archivo.text))
            {
                Debug.LogWarning($"⚠️ El archivo {nombreArchivo} está vacío en Resources.");
            }
            callback(archivo.text);
        }
        else
        {
            Debug.LogError($"❌ No se encontró el archivo {nombreArchivo} en Resources/Plantillas_Json/");
            callback(null);
        }
    }


    void ActualizarTotales()
    {
        int totalMisiones = 0;
        int totalLogros = 0;

        var categoriasJson = jsonData["Misiones"]["Categorias"];

        foreach (KeyValuePair<string, JSONNode> categoria in categoriasJson)
        {
            var categoriaData = categoria.Value;
            if (!categoriaData.HasKey("Elementos")) continue;

            var elementosJson = categoriaData["Elementos"];
            int logrosPorCategoria = 0;
            int misionesPorCategoria = 0;

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
            }

            if (categoriaData.HasKey("mision_final"))
            {
                var misionFinal = categoriaData["mision_final"];
                if (misionFinal["completada"].AsBool)
                {
                    totalMisiones++;
                    totalLogros++;
                    misionesPorCategoria++;
                    logrosPorCategoria++;
                }
            }
        }

        // 🔹 Mostrar totales en UI
        TotalMisionesCompletadas.text = totalMisiones.ToString();
        TotalLogrosDesbloqueados.text = totalLogros.ToString();
        ActualizarDatosUsuario();
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
            if (avatar != null)
            {
                avatarImage.sprite = avatar;
            }
            else
            {
                Debug.LogWarning("⚠ Avatar no encontrado en ruta: " + rutaAvatar);
            }
            return;
        }

        // 🌐 Con internet: cargar desde Firestore
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
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
                rango = ObtenerRangoSegunXP(xp);
                Rango.text = rango;
                PlayerPrefs.SetString("Rango", rango);

                //Actualiza rango en firebase
                ActualizarRangoSegunXP(xp);

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

    public async void ActualizarRangoSegunXP(int xp)
    {
        string nuevoRango = ObtenerRangoSegunXP(xp);
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("Rango", nuevoRango);
        rango = nuevoRango;
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

    private void AbrirPanelDatos()
    {
        PanelDatos.SetActive(true);
        PanelLogros.SetActive(false);
        PanelNotificaciones.SetActive(false);
    }

    private void AbrirPanelLogros()
    {
        PanelLogros.SetActive(true);
        PanelDatos.SetActive(false);
        PanelNotificaciones.SetActive(false);
    }

    private void AbrirPanelNotificaciones()
    {
        PanelNotificaciones.SetActive(true);
        PanelDatos.SetActive(false);
        PanelLogros.SetActive(false);
    }

    // Función para obtener la posición del usuario en el ranking
    async void ObtenerPosicionUsuario()
    {
        // Realiza una consulta para obtener los usuarios ordenados por XP en orden descendente (de mayor a menor)
        Query rankingQuery = db.Collection("users").OrderByDescending("xp");
        // Ejecuta la consulta y obtiene los datos
        QuerySnapshot snapshot = await rankingQuery.GetSnapshotAsync();

        // Si no hay usuarios en la base de datos
        if (snapshot.Count == 0)
        {
            Debug.LogWarning("No hay usuarios registrados en la base de datos.");
            PosicioRanking.text = "Posición: No disponible"; // Muestra mensaje indicando que no hay usuarios
            return; // Sale de la función si no hay usuarios
        }

        int posicion = 1; // Comienza desde la posición 1 en el ranking
        bool encontrado = false; // Variable para indicar si se encuentra al usuario

        // Recorre todos los usuarios del ranking
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            // Si el ID del documento coincide con el ID del usuario actual
            if (doc.Id == userId)
            {
                encontrado = true; // Marca que se encontró al usuario-
                PosicioRanking.text = "" + posicion; // Muestra la posición en el ranking
                PlayerPrefs.SetInt("posicion", posicion); // guardo posición para mostrarla offline --------------------------------
                Debug.Log($"El usuario {userId} está en la posición {posicion} del ranking.");
                break; // Sale del ciclo ya que se encontró al usuario
            }
            posicion++; // Incrementa la posición para el siguiente usuario
        }

        // Si no se encontró al usuario
        if (!encontrado)
        {
            Debug.LogError("No se encontró al usuario en el ranking.");
            PosicioRanking.text = "Posición: No encontrada"; // Muestra un mensaje de error
        }
    }
}
