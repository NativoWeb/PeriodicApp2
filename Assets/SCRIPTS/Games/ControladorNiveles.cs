using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Importar SceneManager
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;

public class ControladorNiveles : MonoBehaviour
{
    public Button[] botonesNiveles; // Asigna los botones en el Inspector
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    public static int nivelSeleccionado;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        CargarProgreso();
        AsignarEventosBotones(); // Llamamos a la función para asignar eventos
        AsignarEventosBotonesPlantilla();
    }

    async void CargarProgreso()
    {
        string userId = auth.CurrentUser.UserId;
        DocumentReference docGrupo = db.Collection("users").Document(userId)
                                      .Collection("grupos").Document("grupo 1");

        DocumentSnapshot snapshot = await docGrupo.GetSnapshotAsync();

        int nivelDesbloqueado = 1; // Nivel por defecto

        if (snapshot.Exists && snapshot.TryGetValue<int>("nivel", out int nivelGuardado))
        {
            nivelDesbloqueado = nivelGuardado;
        }

        Debug.Log($"🔹 Nivel desbloqueado en Firestore: {nivelDesbloqueado}");

        // Activar los botones según el nivel desbloqueado
        for (int i = 0; i < botonesNiveles.Length; i++)
        {
            botonesNiveles[i].interactable = (i < nivelDesbloqueado);
        }
    }

    void AsignarEventosBotones()
    {
        // Lista de botones específicos a los que se les asignará el evento
        int[] botonesPermitidos = { 2, 5, 8, 11, 14, 17, 20, 23, 26 };

        for (int i = 0; i < botonesPermitidos.Length; i++)
        {
            int index = botonesPermitidos[i] - 1; // Convertir a índice (restando 1)

            if (index >= 0 && index < botonesNiveles.Length) // Evitar errores de índice
            {
                int nivel = index + 1; // Convertir índice a número de nivel
                botonesNiveles[index].onClick.AddListener(() => SeleccionarNivel(nivel));
            }
        }
    }

    void SeleccionarNivel(int nivel)
    {

        nivelSeleccionado = nivel;
        PlayerPrefs.SetInt("nivelSeleccionado", nivel); // Guardar nivel en PlayerPrefs
        PlayerPrefs.Save(); // Asegurar que se guarde
        Debug.Log($"✅ Nivel {nivel} seleccionado. Cargando escena del quiz...");
        SceneManager.LoadScene("Oracion"); // Cambiar a la escena del quiz
    }

    void AsignarEventosBotonesPlantilla()
    {
        // Lista de botones específicos a los que se les asignará el evento
        int[] botonesPermitidos = { 1, 4, 7, 10, 13, 14, 19, 22, 25 };

        for (int i = 0; i < botonesPermitidos.Length; i++)
        {
            int index = botonesPermitidos[i] - 1; // Convertir a índice (restando 1)

            if (index >= 0 && index < botonesNiveles.Length) // Evitar errores de índice
            {
                int nivel = index + 1; // Convertir índice a número de nivel
                botonesNiveles[index].onClick.AddListener(() => SeleccionarNivelPlantilla(nivel));
            }
        }
    }

    void SeleccionarNivelPlantilla(int nivel)
    {

        nivelSeleccionado = nivel;
        PlayerPrefs.SetInt("nivelSeleccionado", nivel); // Guardar nivel en PlayerPrefs
        PlayerPrefs.Save(); // Asegurar que se guarde
        Debug.Log($"✅ Nivel {nivel} seleccionado. Cargando escena del quiz...");
        SceneManager.LoadScene("Plantilla"); // Cambiar a la escena del quiz
    }
}
