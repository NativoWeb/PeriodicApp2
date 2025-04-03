using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using TMPro;
using UnityEngine.UI;
using Firebase.Extensions;
using Firebase.Auth;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EstadisticasController : MonoBehaviour
{
    // Instancias de Firebase
    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // ID del usuario autenticado
    private string userId;

    // Elementos de la UI
    public Image avatarImage;
    public TMP_Text rangotxt;
    public Slider slider;

    // internet
    private bool hayInternet = false;
    // Panel para cerrar sesión
    [SerializeField] private GameObject m_logoutUI = null;

    // Clase para representar cada cuadro de grupo
    [System.Serializable]
    public class CuadroGrupo
    {
        public TMP_Text nombreGrupoText;
        public TMP_Text nivelGrupoText;
        public Image grupoImagen;
    }

    public List<CuadroGrupo> cuadrosGrupos;
    private ListenerRegistration listenerRegistro;

    // ============================ MÉTODOS PRINCIPALES ============================

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;// verficamos si hay wifi 

        if (hayInternet)
        {
            userId = PlayerPrefs.GetString("userId", "").Trim();
            EscucharDatosUsuario();
            CargarNivelesPorGrupoUsuario();

        } else if(hayInternet && string.IsNullOrEmpty(userId))
        {
            Debug.Log("Usuario no autenticado, cargando datos offline");
            MostrarDatosOffline();
        }
        else
        {
            Debug.Log("Usuario no autenticado, cargando datos offline");
            MostrarDatosOffline();
        }
        
    }
    private void MostrarDatosOffline()
    {
        string username = PlayerPrefs.GetString("DisplayName", "");
        string rangos = PlayerPrefs.GetString("Rango", "Novato de laboratorio");
        int xp = PlayerPrefs.GetInt("TempXP", 0);
      

        // mostrar datos del usuario en la interfaz 
        rangotxt.text = rangos;

        string avatarPath = ObtenerAvatarPorRango(rangos);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/defecto");

        avatarImage.sprite = avatarSprite;
    }

    private void OnDestroy()
    {
        if (listenerRegistro != null) listenerRegistro.Stop();
    }

    // ============================ ESCUCHAR DATOS EN TIEMPO REAL ============================

    void EscucharDatosUsuario()
    {
        DocumentReference docRef = db.Collection("users").Document(userId);

        listenerRegistro = docRef.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                Debug.Log($"Datos del usuario actualizados: {userId}");

                string rango = snapshot.GetValue<string>("Rango");
                string avatarPath = ObtenerAvatarPorRango(rango);
                Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);

                if (avatarSprite != null) avatarImage.sprite = avatarSprite;
                else Debug.LogError($"No se encontró la ruta: {avatarPath}");

                rangotxt.text = "Su rango es: " + rango + "!";

                int xpUsuario = snapshot.GetValue<int>("xp");
                ActualizarSlider(xpUsuario, rango);
            }
            else
            {
                Debug.LogError("El usuario no existe en la base de datos.");
            }
        });
    }

    // ============================ CARGA DE NIVELES POR GRUPO ============================

    void CargarNivelesPorGrupoUsuario()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Usuario no autenticado.");
            return;
        }

        CollectionReference gruposRef = db.Collection("users").Document(userId).Collection("grupos");

        gruposRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error al obtener los niveles del usuario.");
                return;
            }

            QuerySnapshot snapshot = task.Result;
            int index = 0;

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (index >= cuadrosGrupos.Count) break;

                string grupoNombre = document.ContainsField("nombre") ? document.GetValue<string>("nombre") : document.Id;
                int nivel = document.ContainsField("nivel") ? document.GetValue<int>("nivel") : 0;
                int nivelMaximo = document.ContainsField("nivel_maximo") ? document.GetValue<int>("nivel_maximo") : 1;
                string rutaImagen = document.ContainsField("ruta_imagen") ? document.GetValue<string>("ruta_imagen") : "GruposImages/default";

                float porcentaje = ((float)nivel / (float)nivelMaximo) * 100f;

                cuadrosGrupos[index].nombreGrupoText.text = grupoNombre;
                cuadrosGrupos[index].nivelGrupoText.text = Mathf.RoundToInt(porcentaje) + "%";

                Sprite avatarSprite = Resources.Load<Sprite>(rutaImagen);
                if (avatarSprite != null) cuadrosGrupos[index].grupoImagen.sprite = avatarSprite;
                else Debug.LogWarning($"No se encontró la imagen: {rutaImagen}");

                index++;
            }

            for (int i = index; i < cuadrosGrupos.Count; i++)
            {
                cuadrosGrupos[i].nombreGrupoText.text = "";
                cuadrosGrupos[i].nivelGrupoText.text = "";
                cuadrosGrupos[i].grupoImagen.sprite = null;
            }
        });
    }

    // ============================ CIERRE DE SESIÓN ============================

    // ========== 🚀 MÉTODO PARA SUBIR JSON A FIRESTORE ==========
    public async Task SubirDatosJSON()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        // Obtener JSON de misiones y categorías desde PlayerPrefs
        string jsonMisiones = PlayerPrefs.GetString("misionesCategoriasJSON", "{}");
        string jsonCategorias = PlayerPrefs.GetString("CategoriasOrdenadas", "{}");

        // Referencias a los documentos dentro de la colección del usuario
        DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");
        DocumentReference categoriasDoc = db.Collection("users").Document(userId).Collection("datos").Document("categorias");

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

        if (jsonCategorias != "{}")
        {
            Dictionary<string, object> dataCategorias = new Dictionary<string, object>
        {
            { "categorias", jsonCategorias },
            { "timestamp", FieldValue.ServerTimestamp }
        };
            tareasSubida.Add(categoriasDoc.SetAsync(dataCategorias, SetOptions.MergeAll));
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

    // ========== 🚀 CIERRE DE SESIÓN + SUBIR JSON ==========
    public async void Logout()
    {
        await SubirDatosJSON(); // Guardar el JSON antes de cerrar sesión

        auth.SignOut();
        PlayerPrefs.DeleteAll();

       //PlayerPrefs.SetString("Estadouser", estadouser);
        PlayerPrefs.Save();

        Debug.Log("✅ Sesión cerrada correctamente.");
        SceneManager.LoadScene("Start");
    }

    // ============================ ACTUALIZAR SLIDER ============================

    public void ActualizarSlider(int xp, string rango)
    {
        int xpMin = 0, xpMax = 1000;

        switch (rango)
        {
            case "Novato de laboratorio": xpMin = 0; xpMax = 1000; break;
            case "Arquitecto molecular": xpMin = 1000; xpMax = 2000; break;
            case "Visionario Cuántico": xpMin = 2000; xpMax = 3000; break;
            case "Amo del caos químico": xpMin = 3000; xpMax = 4000; break;
        }

        int rangoXP = xpMax - xpMin;
        float progreso = Mathf.Clamp01((float)(xp - xpMin) / rangoXP);
        slider.value = progreso;

        Debug.Log($"XP: {xp} | Rango: {rango} | Progreso: {Mathf.RoundToInt(progreso * 100)}%");
    }

    private string ObtenerAvatarPorRango(string rango)
    {
        switch (rango)
        {
            case "Novato de laboratorio": return "Avatares/nivel1";
            case "Arquitecto molecular": return "Avatares/nivel2";
            case "Visionario Cuántico": return "Avatares/defecto";
            case "Amo del caos químico": return "Avatares/nivel4";
            default: return "Avatars/default";
        }
    }

    // ============================ MOSTRAR PANTALLA DE LOGOUT ============================

    public void showlogout()
    {
        if (m_logoutUI != null)
        {
            m_logoutUI.SetActive(true);
        }
        else
        {
            Debug.LogError("El panel de logout no está asignado.");
        }
    }

    public void quitarlogout()
    {
        m_logoutUI.SetActive(false);

    }
}
