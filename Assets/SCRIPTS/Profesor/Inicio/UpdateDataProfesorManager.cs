using UnityEngine;
using Firebase.Firestore;
using Firebase.Database;
using System.Collections;
using Firebase.Auth;
using System.Collections.Generic;
using System;


public class UpdateDataProfesorManager : MonoBehaviour
{
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string userId;

    // internet
    bool hayInternet = false;
    

    void Start()
    {
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if( hayInternet)
        {
            // inicializamos database 
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            currentUser = auth.CurrentUser;
            userId = currentUser.UserId;

            Debug.Log(" Actualizando datos desde Update Data Profesores");
            GetUserData();
        }
        else
        {
            Debug.Log("No hay Conexión a internet, en el momento no se pueden actualizar datos del usuario");
        }

    }
    private async void GetUserData()
    {
        DocumentReference userRef = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                int xpActual = snapshot.GetValue<int>("xp"); // XP actual en Firebase
                PlayerPrefs.SetInt("TempXP", xpActual);
                string username = snapshot.GetValue<string>("DisplayName");
                PlayerPrefs.SetString("DisplayName", username);
                string ocupacion = snapshot.GetValue<string>("Ocupacion");
                PlayerPrefs.SetString("TempOcupacion", ocupacion);
                string rango = snapshot.GetValue<string>("Rango");
                PlayerPrefs.SetString("Rango", rango);
                string avatar = snapshot.GetValue<string>("avatar");
                PlayerPrefs.SetString("TempAvatar", avatar);

                Dictionary<string, object> datos = snapshot.ToDictionary();
                bool tieneEdad = datos.ContainsKey("Edad");
                bool tieneDepartamento = datos.ContainsKey("Departamento");
                bool tieneCiudad = datos.ContainsKey("Ciudad");

                if (tieneEdad && tieneDepartamento && tieneCiudad)
                {
                    int edad = snapshot.GetValue<int>("Edad");
                    PlayerPrefs.SetInt("Edad", edad);
                    string departamento = snapshot.GetValue<string>("Departamento");
                    PlayerPrefs.SetString("Departamento", departamento);
                    string ciudad = snapshot.GetValue<string>("Ciudad");
                    PlayerPrefs.SetString("Ciudad", ciudad);


                }

             Debug.Log("Get-user-Data desde Update Data Profesor puso bien los player prefs");
                Debug.Log($"la ocupación desde updatedataProfesor es: {ocupacion}");
            }
        }catch(Exception e)
        {
            Debug.Log($"Problema: {e.Message}");
        }
    }

}
