using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using System.Runtime.CompilerServices;
using System.Net;
using Firebase.Database;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class PanelEntrada : MonoBehaviour
{

    // instanciamos variables firebase
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private string userId;

    [Header("panel llenar información si no tiene datos")]
    [SerializeField] public GameObject panelEntrada = null;

 
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        currentUser = auth.CurrentUser;

        if (currentUser == null)
        {
            Debug.LogWarning("[PanelEntrada] No hay usuario autenticado. Omitiendo verificación.");
            return;
        }

        userId = currentUser.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("[PanelEntrada] UserId vacío.");
            return;
        }
        verificarCampos();
    }


    private async void verificarCampos()
    {
        if (!HayInternet())
        {
            Debug.Log("[PanelEntrada] No hay conexión a Internet. Omitiendo verificación.");
            return;
        }

        try
        {
            DocumentReference userRef = db.Collection("users").Document(userId);
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning("[PanelEntrada] El documento del usuario no existe en Firestore. Redirigiendo a Login.");
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                SceneManager.LoadScene("Start");
                return;
            }

            Dictionary<string, object> datos = snapshot.ToDictionary();
            bool tieneedad = datos.ContainsKey("Edad");
            bool tienedepartamento = datos.ContainsKey("Departamento");
            bool tieneciudad = datos.ContainsKey("Ciudad");

            if (!(tieneciudad && tienedepartamento && tieneedad))
            {
                ActivarPanelEntrada();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PanelEntrada] Error verificando campos: {e.Message}");
        }
    }

    void ActivarPanelEntrada()
    {
        panelEntrada.SetActive(true);
    }
   public void IrALlenarDatos()
    {
        PlayerPrefs.SetInt("llenardatos", 1);
        SceneManager.LoadScene("Cuenta");
    }
    public bool HayInternet()
    {
        try
        {
            using (var client = new WebClient())
            using (var stream = client.OpenRead("http://www.google.com"))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

}
