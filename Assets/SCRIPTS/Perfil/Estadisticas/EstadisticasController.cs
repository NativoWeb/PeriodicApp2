using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Auth;
using System.Collections.Generic;

public class EstadisticasController : MonoBehaviour
{
    // Instancias de Firebase
    private FirebaseAuth auth;
    public FirebaseFirestore db;

    // ID del usuario autenticado
    public string userId;

    // Elementos de la UI
    public Image avatarImage;
    public TMP_Text rangotxt;

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

    // Lista de cuadros de grupo (se asignan manualmente en el inspector)
    public List<CuadroGrupo> cuadrosGrupos;

    // Referencia al Slider en la UI
    public Slider slider;

    ListenerRegistration listenerRegistro;

    // ============================ MÉTODOS PRINCIPALES ============================

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();

        EscucharDatosUsuario(); // Escuchar datos del usuario en tiempo real
        CargarNivelesPorGrupoUsuario(); // Cargar niveles por grupo
    }

    private void OnDestroy()
    {
        // Detener el listener cuando se cierre o cambie de escena
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
                Debug.Log($"Datos del usuario actualizados en tiempo real: {userId}");

                string rango = snapshot.GetValue<string>("Rango");
                string avatarPath = ObtenerAvatarPorRango(rango);
                Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);

                if (avatarSprite != null)
                {
                    avatarImage.sprite = avatarSprite;
                }
                else
                {
                    Debug.LogError($"No se encontró una ruta válida para: {avatarPath}");
                }

                rangotxt.text = "Su rango es: " + rango + "!";

                // XP para actualizar el slider
                int xpUsuario = snapshot.GetValue<int>("xp");
                ActualizarSlider(xpUsuario, rango);
            }
            else
            {
                Debug.LogError("El usuario no existe en la base de datos.");
            }
        });
    }

    // ============================ ASIGNACIÓN DE AVATAR SEGÚN RANGO ============================

    public string ObtenerAvatarPorRango(string rango)
    {
        string avatarPath = rango switch
        {
            "Novato de laboratorio" => "Avatares/nivel1",
            "Arquitecto molecular" => "Avatares/nivel2",
            "Visionario Cuántico" => "Avatares/nivel3",
            "Amo del caos químico" => "Avatares/nivel4",
            _ => "Avatares/defecto"
        };

        Debug.Log($"Ruta del avatar: {avatarPath}");
        return avatarPath;
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

            Debug.Log($"Cantidad de grupos en Firestore: {snapshot.Count}");

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (index >= cuadrosGrupos.Count)
                {
                    Debug.LogWarning("Hay más grupos que cuadros disponibles.");
                    break;
                }

                string grupoNombre = document.ContainsField("nombre") ? document.GetValue<string>("nombre") : document.Id;
                int nivel = document.ContainsField("nivel") ? document.GetValue<int>("nivel") : 0;
                int nivelMaximo = document.ContainsField("nivel_maximo") ? document.GetValue<int>("nivel_maximo") : 1;
                string rutaImagen = document.ContainsField("ruta_imagen") ? document.GetValue<string>("ruta_imagen") : "GruposImages/default";

                float porcentaje = ((float)nivel / (float)nivelMaximo) * 100f;

                cuadrosGrupos[index].nombreGrupoText.text = grupoNombre;
                cuadrosGrupos[index].nivelGrupoText.text = Mathf.RoundToInt(porcentaje) + "%";

                Sprite avatarSprite = Resources.Load<Sprite>(rutaImagen);
                if (avatarSprite != null)
                {
                    cuadrosGrupos[index].grupoImagen.sprite = avatarSprite;
                }
                else
                {
                    Debug.LogWarning($"No se encontró la imagen en la ruta: {rutaImagen}");
                }

                index++;
            }

            // Limpiar cuadros restantes si hay menos grupos que cuadros
            for (int i = index; i < cuadrosGrupos.Count; i++)
            {
                cuadrosGrupos[i].nombreGrupoText.text = "";
                cuadrosGrupos[i].nivelGrupoText.text = "";
                cuadrosGrupos[i].grupoImagen.sprite = null;
            }
        });
    }

    // ============================ CIERRE DE SESIÓN ============================

    public void Logout()
    {
        auth.SignOut(); // Cierra sesión en Firebase
        PlayerPrefs.DeleteKey("userId"); // Elimina ID del usuario almacenado
        PlayerPrefs.DeleteKey("userEmail");
        PlayerPrefs.DeleteKey("userPassword");
        PlayerPrefs.DeleteKey("Estadouser");
        PlayerPrefs.DeleteKey("XP");
        PlayerPrefs.SetInt("rememberMe", 0);
        PlayerPrefs.Save();
        Debug.Log("Sesión cerrada correctamente");
        SceneManager.LoadScene("Login"); // Redirige a la escena de login

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

    // ============================ ACTUALIZAR SLIDER ============================

    public void ActualizarSlider(int xp, string rango)
    {
        int xpMin = 0;
        int xpMax = 1000;

        if (rango == "Novato de laboratorio")
        {
            xpMin = 0; xpMax = 1000;
        }
        else if (rango == "Arquitecto molecular")
        {
            xpMin = 1000; xpMax = 2000;
        }
        else if (rango == "Visionario Cuántico")
        {
            xpMin = 2000; xpMax = 3000;
        }
        else if (rango == "Amo del caos químico")
        {
            xpMin = 3000; xpMax = 4000;
        }

        int rangoXP = xpMax - xpMin;
        if (rangoXP <= 0) rangoXP = 1; // Evitar división por 0

        float progreso = Mathf.Clamp01((float)(xp - xpMin) / rangoXP);
        slider.value = progreso;

        Debug.Log($"XP: {xp} | Rango: {rango} | Progreso: {Mathf.RoundToInt(progreso * 100)}%");
    }
}
