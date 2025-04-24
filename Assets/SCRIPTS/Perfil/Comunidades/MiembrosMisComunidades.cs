using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections.Generic;
public class MiembrosMisComunidades : MonoBehaviour
{
    public string comunidadId;

    public GameObject tarjetaUsuarioPrefab;
    public Transform contenedorMiembros;

    public GameObject panelMiembros; // <- Este es el Panel que tiene el ScrollView
    public Button botonVerMiembros; // <- El botón que debe presionarse

    private bool miembrosCargados = false;

    void Start()
    {
        panelMiembros.SetActive(false); // Ocultamos al inicio
        botonVerMiembros.onClick.AddListener(TogglePanelMiembros);
    }

    void TogglePanelMiembros()
    {
        bool estaActivo = panelMiembros.activeSelf;

        // Alternar visibilidad
        panelMiembros.SetActive(!estaActivo);

        if (!miembrosCargados && !estaActivo)
        {
            CargarMiembrosDeComunidad(comunidadId);
            miembrosCargados = true;
        }
    }

    void CargarMiembrosDeComunidad(string idComunidad)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("comunidades").Document(idComunidad).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                DocumentSnapshot doc = task.Result;
                List<object> miembrosList = doc.GetValue<List<object>>("miembros");

                foreach (object miembroIdObj in miembrosList)
                {
                    string miembroId = miembroIdObj.ToString();
                    CargarYMostrarUsuario(miembroId);
                }
            }
        });
    }

    void CargarYMostrarUsuario(string userId)
    {
        FirebaseFirestore.DefaultInstance.Collection("usuarios").Document(userId)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    DocumentSnapshot userDoc = task.Result;
                    string username = userDoc.GetValue<string>("username");
                    string rango = userDoc.GetValue<string>("rango"); 

                    GameObject nuevaTarjeta = Instantiate(tarjetaUsuarioPrefab, contenedorMiembros);
                    nuevaTarjeta.transform.localScale = Vector3.one;

                    // Buscar los textos dentro del prefab
                    TextMeshProUGUI textoNombre = nuevaTarjeta.transform.Find("TextoNombre").GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI textoRango = nuevaTarjeta.transform.Find("TextoRango").GetComponent<TextMeshProUGUI>();

                    textoNombre.text = username;
                    textoRango.text = rango;
                }
                else
                {
                    Debug.LogWarning("No se encontró el usuario con ID: " + userId);
                }
            });
    }

}
