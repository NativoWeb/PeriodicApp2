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
    public string userUID; // Debe contener el UID del usuario autenticado

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        userUID = "UQZQOwiNqqN4Tm1xoIKFLmSHzOL2"; // Reemplázalo con el UID real del usuario autenticado
        CargarMisioness();
    }

    async void CargarMisioness()
    {
        Query misionesQuery = db.Collection("misiones");
        QuerySnapshot snapshot = await misionesQuery.GetSnapshotAsync();

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

                GameObject newButton = Instantiate(buttonPrefab, content);

                // Obtener componentes del botón
                TextMeshProUGUI[] textComponents = newButton.GetComponentsInChildren<TextMeshProUGUI>();
                Slider barraProgreso = newButton.GetComponentInChildren<Slider>();

                textComponents[0].text = titulo;
                textComponents[1].text = descripcion;
                textComponents[2].text = $"XP: {xp}";

                // Asignar evento al botón
                Button btn = newButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => CambiarEscena(rutaEscena));
                }
                else
                {
                    Debug.LogWarning("El botón no tiene componente Button.");
                }

                // Cargar el progreso del usuario desde Firestore
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
