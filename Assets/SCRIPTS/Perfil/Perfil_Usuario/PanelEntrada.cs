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

        // incializamos las variables firebase
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        currentUser = auth.CurrentUser;

        userId = currentUser.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.Log("Sin usuario autenticado");
            return;
        }
        verificarCampos();
    }


    private async void verificarCampos()
    {
        if (!HayInternet())
        {
            Debug.Log("🚫 No hay conexión a Internet. No se puede sincronizar.");

        }
        DocumentReference userRef = db.Collection("users").Document(userId);

        DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
        if (snapshot.Exists)
        {
            Dictionary<string, object> datos = snapshot.ToDictionary();
            bool tieneedad = datos.ContainsKey("Edad");
            bool tienedepartamento = datos.ContainsKey("Departamento");
            bool tieneciudad = datos.ContainsKey("Ciudad");

            if (tieneciudad && tienedepartamento && tieneedad)
            {
                return;
            }
            else
            {
                ActivarPanelEntrada();
            }

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
