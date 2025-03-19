using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;

public class ControladorNiveles : MonoBehaviour
{
    public Button[] botonesNiveles; // Asigna los botones en el Inspector
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private ListenerRegistration listener;

    public static int nivelSeleccionado;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        if (TieneInternet())
        {
            CargarProgreso();
            SuscribirListener();
        }
        else
        {
            Debug.LogWarning("⚠ No hay conexión a Internet. Se usará el nivel guardado localmente.");
            AplicarNivelDesdePlayerPrefs();
        }

        AsignarEventosBotones();
        AsignarEventosBotonesPlantilla();
    }

    bool TieneInternet()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    async void CargarProgreso()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        DocumentReference docGrupo = db.Collection("users").Document(userId)
                                      .Collection("grupos").Document("grupo 1");

        DocumentSnapshot snapshot = await docGrupo.GetSnapshotAsync();
        int nivelDesbloqueado = 1;

        if (snapshot.Exists && snapshot.TryGetValue<int>("nivel", out int nivelGuardado))
        {
            nivelDesbloqueado = nivelGuardado;
        }

        Debug.Log($"🔹 Nivel desbloqueado en Firestore: {nivelDesbloqueado}");

        // Guardar el nivel en PlayerPrefs por si pierde conexión más tarde
        PlayerPrefs.SetInt("nivelDesbloqueado", nivelDesbloqueado);
        PlayerPrefs.Save();

        // Activar los botones según el nivel desbloqueado
        for (int i = 0; i < botonesNiveles.Length; i++)
        {
            botonesNiveles[i].interactable = (i < nivelDesbloqueado);
        }
    }

    private void SuscribirListener()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        DocumentReference docGrupo = db.Collection("users").Document(userId)
                                      .Collection("grupos").Document("grupo 1");

        listener = docGrupo.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                Debug.Log("🔄 Cambio detectado en Firestore. Recargando progreso...");
                CargarProgreso();
            }
        });
    }

    private void OnDestroy()
    {
        listener?.Stop();
    }

    void AsignarEventosBotones()
    {
        int[] botonesPermitidos = { 2, 5, 8, 11, 14 };

        for (int i = 0; i < botonesPermitidos.Length; i++)
        {
            int index = botonesPermitidos[i] - 1;

            if (index >= 0 && index < botonesNiveles.Length)
            {
                int nivel = index + 1;
                botonesNiveles[index].onClick.AddListener(() => SeleccionarNivel(nivel));
            }
        }
    }

    void SeleccionarNivel(int nivel)
    {
        if (TieneInternet())
        {
            GuardarNivelEnFirestore(nivel);
        }
        else
        {
            Debug.LogWarning("⚠ No hay conexión a Internet. Nivel guardado localmente.");
            PlayerPrefs.SetInt("nivelSeleccionado", nivel);
            PlayerPrefs.Save();
        }

        nivelSeleccionado = nivel;
        Debug.Log($"✅ Nivel {nivel} seleccionado. Cargando escena del quiz...");
        SceneManager.LoadScene("Oracion");
    }

    void AsignarEventosBotonesPlantilla()
    {
        int[] botonesPermitidos = { 1, 4, 7, 10, 13 };

        for (int i = 0; i < botonesPermitidos.Length; i++)
        {
            int index = botonesPermitidos[i] - 1;

            if (index >= 0 && index < botonesNiveles.Length)
            {
                int nivel = index + 1;
                botonesNiveles[index].onClick.AddListener(() => SeleccionarNivelPlantilla(nivel));
            }
        }
    }

    void SeleccionarNivelPlantilla(int nivel)
    {
        if (TieneInternet())
        {
            GuardarNivelEnFirestore(nivel);
        }
        else
        {
            Debug.LogWarning("⚠ No hay conexión a Internet. Nivel guardado localmente.");
            PlayerPrefs.SetInt("nivelSeleccionado", nivel);
            PlayerPrefs.Save();
        }

        nivelSeleccionado = nivel;
        Debug.Log($"✅ Nivel {nivel} seleccionado. Cargando escena del quiz...");
        SceneManager.LoadScene("Plantilla");
    }

    async void GuardarNivelEnFirestore(int nivel)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        DocumentReference docGrupo = db.Collection("users").Document(userId)
                                      .Collection("grupos").Document("grupo 1");

        await docGrupo.SetAsync(new { nivel }, SetOptions.MergeAll);
        Debug.Log($"✅ Nivel {nivel} guardado en Firestore.");
    }

    void AplicarNivelDesdePlayerPrefs()
    {

        int nivelDesbloqueado = PlayerPrefs.GetInt("Nivel", 1);
        Debug.Log($"🔹 Cargando nivel desde PlayerPrefs: {nivelDesbloqueado}");

        for (int i = 0; i < botonesNiveles.Length; i++)
        {
            botonesNiveles[i].interactable = (i < nivelDesbloqueado);
        }
    }
}
