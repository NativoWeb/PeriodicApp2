using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using System.Runtime.CompilerServices;
using System.Net;
using Firebase.Database;
using System;


public class PerfilProfesorManager : MonoBehaviour
{

    [Header("Información del profesor")]
    public Image imageprofesor;
    public TMP_Text Nombretxt;
    public TMP_Text Emailtxt;
    public TMP_Text edadtxt;
    public TMP_Text departamentotxt;
    public TMP_Text Ciudadtxt;

    // instanciamos variables firebase
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private string userId; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
        cargardatosProfesor();
    }
    private async void cargardatosProfesor()
    {
        if (!HayInternet())
        {
            Debug.Log("🚫 No hay conexión a Internet. No se puede sincronizar.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                string avatar = snapshot.GetValue<string>("avatar");
                string nombre = snapshot.GetValue<string>("DisplayName");
                string email = snapshot.GetValue<string>("Email");
                int edad = snapshot.GetValue<int>("Edad");
                string departamento = snapshot.GetValue<string>("Departamento");
                string Ciudad = snapshot.GetValue<string>("Ciudad");


                // asignamos la información a la UI
                
                Nombretxt.text = nombre;
                Emailtxt.text = email;
                edadtxt.text = edad.ToString();
                departamentotxt.text = departamento;
                Ciudadtxt.text = Ciudad;
                Sprite avatarSprite = Resources.Load<Sprite>(avatar) ?? Resources.Load<Sprite>("Avatares/Rango8");

                imageprofesor.sprite = avatarSprite;

            }
        }catch(Exception e)
        {
            Debug.Log($"error al intentar conseguir datos de firestore{e.Message}");
        }
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
