using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CargarMisiones: MonoBehaviour
{
    FirebaseFirestore db;
    public Transform content;
    public GameObject buttonPrefab;
    public string userUID;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        // llamamos el userId que se guarda para cada usuario en el registro y se guardo con el play prefers
        userUID = PlayerPrefs.GetString("userId", "").Trim();
        // verificamos que userUID no sea nulo
        if (string.IsNullOrEmpty(userUID))
        {
            Debug.LogWarning("Error: no se encontro userUID válido");
        }
        else
        {
            Debug.LogAssertion("userUID cargado correctamente" + userUID);
        }

        CargarMisioness();
    }

    // Método para obtener el nivel del usuario desde Firestore
    async Task<int> ObtenerNivelUsuario(string userId)
    {
        // Referencia a la colección de usuarios en Firestore
        DocumentReference userRef = db.Collection("users").Document(userId);

        // Obtenemos el documento del usuario
        DocumentSnapshot userSnap = await userRef.GetSnapshotAsync();

        if (userSnap.Exists)
        {
            // Obtenemos el nivel del usuario
            int nivel = userSnap.GetValue<int>("nivel");
            Debug.Log($"Nivel del usuario: {nivel}");
            return nivel;
        }
        else
        {
            Debug.LogError("No se encontró el usuario en Firestore.");
            return 0; // Devolvemos 0 si no se encuentra el usuario
        }
    }

    async void CargarMisioness()
    {
        // Obtener el nivel del usuario
        int nivelUsuario = await ObtenerNivelUsuario(userUID);

        // Filtrar misiones por nivel
        Query misionesQuery = db.Collection("misiones").WhereEqualTo("nivelRequerido", nivelUsuario);
        QuerySnapshot snapshot = await misionesQuery.GetSnapshotAsync();

        // Recorremos las misiones obtenidas
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            if (document.Exists)
            {
                string titulo = document.GetValue<string>("titulo");
                string descripcion = document.GetValue<string>("descripcion");
                int xp = document.GetValue<int>("xp");
                string rutaEscena = document.GetValue<string>("rutaEscena");
                string misionID = document.Id; // ID de la misión

                Debug.Log($"Misión: {titulo}, {descripcion}, XP: {xp}, Escena: {rutaEscena}");

                // Instanciamos el botón para mostrar la misión
                GameObject newButton = Instantiate(buttonPrefab, content);

                // Obtener componentes del botón
                TextMeshProUGUI[] textComponents = newButton.GetComponentsInChildren<TextMeshProUGUI>();
                Slider barraProgreso = newButton.GetComponentInChildren<Slider>();

                // Asignamos la información al botón
                textComponents[0].text = titulo;
                textComponents[1].text = descripcion;
                textComponents[2].text = $"XP: {xp}";

                // Asignar evento al botón para cambiar de escena
                Button btn = newButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => CambiarEscena(rutaEscena));
                }
                else
                {
                    Debug.LogWarning("El botón no tiene componente Button.");
                }

                // Cargar el progreso del usuario en la misión
                await CargarProgreso(userUID, misionID, barraProgreso);
            }
            else
            {
                Debug.LogWarning("Documento no encontrado.");
            }
        }
    }


    // Método para cargar el progreso del usuario
    async Task CargarProgreso(string userId, string missionId, Slider barraProgreso)
    {
        DocumentReference docRef = db.Collection("progreso_misiones").Document(userId).Collection("misiones").Document(missionId);
        DocumentSnapshot docSnap = await docRef.GetSnapshotAsync();

        if (docSnap.Exists)
        {
            int progreso = docSnap.GetValue<int>("progreso");
            barraProgreso.value = progreso / 100f; // Normalizar entre 0 y 1
            Debug.Log($"✅ Progreso de {missionId}: {progreso}%");
        }
        else
        {
            barraProgreso.value = 0f; // Si no existe, empieza en 0
            Debug.Log($"⚠️ No se encontró progreso para {missionId}, iniciando en 0%");
        }
    }

    void CambiarEscena(string rutaEscena)
    {
        Debug.Log("Intentando cargar la escena: " + rutaEscena);
        if (Application.CanStreamedLevelBeLoaded(rutaEscena))
        {
            Debug.Log("Cambiando a la escena: " + rutaEscena);
            SceneManager.LoadScene(rutaEscena);
        }
        else
        {
            Debug.LogError("❌ ERROR: La escena '" + rutaEscena + "' no está en Build Settings o tiene un nombre incorrecto.");
        }
    }
}
