using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Asegúrate de importar esto

public class CargarMisiones : MonoBehaviour
{
    FirebaseFirestore db;

    public Transform content; // El transform del Content dentro del ScrollView
    public GameObject buttonPrefab; // Prefab del botón de misión

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
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
                // Obtener los datos de la misión
                string titulo = document.GetValue<string>("titulo");
                string descripcion = document.GetValue<string>("descripcion");
                int xp = document.GetValue<int>("xp");
                string rutaEscena = document.GetValue<string>("rutaEscena");

                // Mostrar los datos en el log
                Debug.Log($"Misión: {titulo}, {descripcion}, XP: {xp}, Escena: {rutaEscena}");

                // Crear un nuevo botón y añadirlo al Content del ScrollView
                GameObject newButton = Instantiate(buttonPrefab, content);

                // Asignar el título al primer TextMeshProUGUI
                TextMeshProUGUI[] textComponents = newButton.GetComponentsInChildren<TextMeshProUGUI>();
                textComponents[0].text = titulo;
                textComponents[1].text = descripcion;
                textComponents[2].text = $"XP: {xp}";

                // Asegurarse de que el botón tenga el componente Button
                Button btn = newButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        Debug.Log("Botón presionado, cargando escena...");
                        CambiarEscena(rutaEscena);
                    });
                }
                else
                {
                    Debug.LogWarning("El botón no tiene componente Button.");
                }

                // Forzar la actualización del layout para que el botón se ajuste al texto
                LayoutRebuilder.ForceRebuildLayoutImmediate(newButton.GetComponent<RectTransform>());
            }
            else
            {
                Debug.LogWarning("Documento no encontrado.");
            }
        }
    }

    // Método para cambiar de escena
    void CambiarEscena(string rutaEscena)
    {
        Debug.Log("Intentando cargar la escena: " + rutaEscena);  // Asegúrate de ver la ruta en la consola
        if (Application.CanStreamedLevelBeLoaded(rutaEscena)) // Verifica si la escena existe
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
