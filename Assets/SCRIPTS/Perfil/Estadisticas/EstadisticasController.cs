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

        userId = PlayerPrefs.GetString("userId", "").Trim();

        EscucharDatosUsuario();
        CargarNivelesPorGrupoUsuario();
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
    private async Task SubirMisionesJSON()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        string jsonMisiones = PlayerPrefs.GetString("misionesJSON", "{}"); // Obtener el JSON de PlayerPrefs

        if (jsonMisiones == "{}")
        {
            Debug.LogWarning("⚠️ No hay datos de misiones guardados.");
            return;
        }

        // Convertir JSON a Dictionary para Firestore
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "misiones", jsonMisiones },
            { "timestamp", FieldValue.ServerTimestamp }
        };

        // Subir a Firestore dentro del documento del usuario
        DocumentReference userDoc = db.Collection("users").Document(userId);

        await userDoc.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("✅ Misiones JSON guardadas en Firestore.");
            }
            else
            {
                Debug.LogError("❌ Error al guardar el JSON en Firestore: " + task.Exception);
            }
        });
    }

    // ========== 🚀 CIERRE DE SESIÓN + SUBIR JSON ==========
    public async void Logout()
    {
        await SubirMisionesJSON(); // Guardar el JSON antes de cerrar sesión

        auth.SignOut();
        PlayerPrefs.DeleteKey("userId"); // Elimina ID del usuario almacenado
        PlayerPrefs.DeleteKey("userEmail");
        PlayerPrefs.DeleteKey("userPassword");
        PlayerPrefs.DeleteKey("Estadouser");
        PlayerPrefs.DeleteKey("DisplayName");
        PlayerPrefs.DeleteKey("XP");
        PlayerPrefs.SetInt("rememberMe", 0); 
        PlayerPrefs.DeleteKey("misionesJSON"); // Eliminar datos locales
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

}
