using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections;
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

    // Panel para cerrar sesi�n
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

    // ============================ M�TODOS PRINCIPALES ============================

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();

        StartCoroutine(LoadUserData(userId)); // Cargar datos del usuario
        CargarNivelesPorGrupoUsuario();      // Cargar niveles por grupo
    }

    // ============================ CARGA DE DATOS DE USUARIO ============================

    IEnumerator LoadUserData(string userId)
    {
        var task = GetUserData(userId);
        yield return new WaitUntil(() => task.IsCompleted);
    }

    async Task GetUserData(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Debug.Log($"Usuario encontrado en Firebase: {userId}");

            string rango = snapshot.GetValue<string>("Rango");
            string avatarPath = ObtenerAvatarPorRango(rango);
            Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);

            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
            }
            else
            {
                Debug.LogError($"No se encontr� una ruta v�lida para: {avatarPath}");
            }

            rangotxt.text = "Su rango es: " + rango + "!";
        }
    }

    // ============================ ASIGNACI�N DE AVATAR SEG�N RANGO ============================

    public string ObtenerAvatarPorRango(string rango)
    {
        string avatarPath = rango switch
        {
            "Novato de laboratorio" => "Avatares/nivel1",
            "Arquitecto molecular" => "Avatares/nivel2",
            "Visionario Cu�ntico" => "Avatares/nivel3",
            "Amo del caos qu�mico" => "Avatares/nivel4",
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
                    Debug.LogWarning("Hay m�s grupos que cuadros disponibles.");
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
                    Debug.LogWarning($"No se encontr� la imagen en la ruta: {rutaImagen}");
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

    // ============================ CIERRE DE SESI�N ============================

    public void Logout()
    {
        auth.SignOut(); // Cierra sesi�n en Firebase
        PlayerPrefs.DeleteKey("userId"); // Elimina ID del usuario almacenado
        PlayerPrefs.Save();

        Debug.Log("Sesi�n cerrada correctamente");
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
            Debug.LogError("El panel de logout no est� asignado.");
        }
    }
}
