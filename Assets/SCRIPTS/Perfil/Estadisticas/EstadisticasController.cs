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
    private FirebaseAuth auth;// iniciamos el auth para cerrarlo con el boton de cerrar
    public FirebaseFirestore db;// instanciamos db
    public string userId;

    //instanciamos variables para poner dentro de unity
    public Image avatarImage;
    public TMP_Text rangotxt;

    [System.Serializable]
    public class CuadroGrupo
    {
        public TMP_Text nombreGrupoText; // Texto para el nombre del grupo
        public TMP_Text nivelGrupoText;  // Texto para el nivel del grupo
        public Image grupoImagen;
    }

    public List<CuadroGrupo> cuadrosGrupos; // Lista de los 18 cuadros (Asignar manualmente en el Inspector)

    //panel para cerrar sesion
    [SerializeField] private GameObject m_logoutUI = null;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();

        // Iniciar la carga de datos de usuario sin perder await
        StartCoroutine(LoadUserData(userId));

        CargarNivelesPorGrupoUsuario();
    }

    IEnumerator LoadUserData(string userId)
    {
        var task = GetUserData(userId);
        yield return new WaitUntil(() => task.IsCompleted);
    }

    //funcion para mostrar el panel y cerrar sesion
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
    async Task GetUserData(string userId)//################# obtener datos para pasar a la UI
    {
        DocumentReference docRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Debug.Log($"Usuario encontrado en firebase : {userId}");
            string username = snapshot.GetValue<string>("DisplayName");
            string avatarUrl = snapshot.GetValue<string>("avatar");
            string rango = snapshot.GetValue<string>("Rango");

            string avatarPath = ObtenerAvatarPorRango(rango);// llamamos función para poder tener una ruta que pasar a la imagen 
            Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);

            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
            }
            else
            {
                Debug.LogError($"no se encontro una ruta valida para : {avatarPath}");
                avatarImage.sprite = Resources.Load<Sprite>(avatarPath);
            }
            rangotxt.text = "Su rango es :" + rango + "!";

        }

    }

    public string ObtenerAvatarPorRango(string rango)//################################# funcion para obtener la ruta de la imagen que se va a poner dentro de la imagen 
    {
        string avatarPath = string.Empty;
        if (rango == "Novato de laboratorio")
        {
            avatarPath = "Avatares/nivel1";
        }
        else if (rango == "Arquitecto molecular")
        {
            avatarPath = "Avatares/nivel2";
        }
        else if (rango == "Visionario Cuántico")
        {
            avatarPath = "Avatares/nivel3";
        }
        else if (rango == "Amo del caos químico")
        {
            avatarPath = "Avatares/nivel4";
        }
        else
        {
            avatarPath = "Avatares/defecto";
        }

        Debug.Log($"la ruta del avatar es: {avatarPath}");

        return avatarPath;
    }


    public void Logout() // ################################################################ Método para cerrar sesión
    {
        auth.SignOut(); // Cierra la sesión en Firebase
        PlayerPrefs.DeleteKey("userId"); // Elimina el ID del usuario guardado
        PlayerPrefs.Save(); // Guarda los cambios

        Debug.Log("Sesión cerrada correctamente");

        // Opcional: Redirigir a la escena de login
        SceneManager.LoadScene("Login");
    }
    void CargarNivelesPorGrupoUsuario()// ######################################################## cargar los progresos por grupo
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Usuario no autenticado");
            return;
        }

        CollectionReference gruposRef = db.Collection("users").Document(userId).Collection("grupos");

        gruposRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error al obtener los niveles del usuario");
                return;
            }

            QuerySnapshot snapshot = task.Result;
            int index = 0; // Para recorrer la lista de cuadros

            Debug.Log($"Cantidad de grupos en Firestore: {snapshot.Count}");

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Debug.Log($"Procesando grupo: {document.Id}"); // Verificar el nombre del grupo

                // Verifica si hay más cuadros que grupos
                if (index >= cuadrosGrupos.Count)
                {
                    Debug.LogWarning("Hay más grupos que cuadros disponibles.");
                    break; // Salir del bucle si no hay más cuadros.
                }

                string grupoNombre = document.ContainsField("nombre") ? document.GetValue<string>("nombre") : document.Id; // Si no existe 'nombre', usamos el ID del grupo
                int nivel = document.ContainsField("nivel") ? document.GetValue<int>("nivel") : 0; // Nivel actual
                int nivelMaximo = document.ContainsField("nivel_maximo") ? document.GetValue<int>("nivel_maximo") : 1; // Nivel máximo (por defecto 1 para evitar división por cero)
                // Obtener la ruta de la imagen desde Firestore (nuevo campo 'ruta_imagen')
                string rutaImagen = document.ContainsField("ruta_imagen") ? document.GetValue<string>("ruta_imagen") : "GruposImages/default"; // Ruta por defecto si no existe
                // Calcular porcentaje de progreso
                float porcentaje = ((float)nivel / (float)nivelMaximo) * 100f;

                // Asignar nombre del grupo al cuadro correspondiente
                cuadrosGrupos[index].nombreGrupoText.text = grupoNombre;
                Debug.Log($"Asignando nombre al cuadro {index}: {grupoNombre}");

                // Mostrar porcentaje redondeado en el texto de nivel
                cuadrosGrupos[index].nivelGrupoText.text = Mathf.RoundToInt(porcentaje) + "%";
                Debug.Log($"Asignando progreso al cuadro {index}: {Mathf.RoundToInt(porcentaje)}%");


                // Cargar la imagen desde la carpeta Resources usando la ruta
                Sprite avatarSprite = Resources.Load<Sprite>(rutaImagen);
                if (avatarSprite != null)
                {
                    // Asignamos la imagen al componente Image del cuadro correspondiente
                    cuadrosGrupos[index].grupoImagen.sprite = avatarSprite;
                    Debug.Log($"Imagen cargada correctamente para el grupo: {grupoNombre}");
                }
                else
                {
                    Debug.LogWarning($"No se encontró la imagen en la ruta: {rutaImagen}");
                }

                index++; // Aumentamos el índice para el siguiente cuadro
            }

            // Si hay más cuadros que grupos, limpiamos los cuadros restantes (opcional).
            if (index < cuadrosGrupos.Count)
            {
                Debug.Log("Limpiando cuadros restantes...");
                for (int i = index; i < cuadrosGrupos.Count; i++)
                {
                    cuadrosGrupos[i].nombreGrupoText.text = "";
                    cuadrosGrupos[i].nivelGrupoText.text = "";
                    cuadrosGrupos[i].grupoImagen.sprite = null; // Limpiar la imagen
                }
            }
        });
    }





}
