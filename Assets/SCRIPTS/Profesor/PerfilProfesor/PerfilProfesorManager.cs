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
using UnityEditor.Search;
using UnityEngine.SceneManagement;


public class PerfilProfesorManager : MonoBehaviour
{

    // instanciamos variables firebase
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private string userId; 

    [Header("Información del profesor")]
    public Image imageprofesor;
    public TMP_Text Nombretxt;
    public TMP_Text Emailtxt;
    public TMP_Text edadtxt;
    public TMP_Text departamentotxt;
    public TMP_Text Ciudadtxt;

    [Header("panel llenar información si no tiene datos")]
    [SerializeField] public GameObject panelEntrada = null;
    public Button btnContinuarEditar;

    [Header("Referencia panel editar")]
    [SerializeField] public GameObject panelEditar = null;

    [Header("Referencias Logout")]
    public Button BtnpanelLogout;
    public Button BtnLogout;
    public Button btnOcultarLogout;
    [SerializeField] public GameObject panelLogout = null;

 

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
        // Escuchar cambios en la colección "encuestas"
        db.Collection("users").Document(userId).Listen(snapshot =>
        {
            verificarCampos(); // Llamar a la función cuando haya cambios
        });
        verificarCampos();
        btnContinuarEditar.onClick.AddListener(activarPanelEditar);

        // Eventos del Logout

        // mostrar panel logout
        BtnpanelLogout.onClick.AddListener(showLogout);
        // salir de la aplicación 
        BtnLogout.onClick.AddListener(logout);
        // Ocultar el panel logout 
        btnOcultarLogout.onClick.AddListener(ocultarLogout);

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

            if( tieneciudad && tienedepartamento && tieneedad)
            {
                cargardatosProfesor();
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
    private async void cargardatosProfesor()
    {
        if (!HayInternet())
        {
            Debug.Log("🚫 No hay conexión a Internet. No se puede sincronizar.");
           
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
    void activarPanelEditar()
    {
        if (panelEntrada != null)
        {
            panelEntrada.SetActive(false);
        }
        panelEditar.SetActive(true);
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
  
    public void showLogout()
    {
        panelLogout.SetActive(true);
    }

    public void ocultarLogout()
    {
        if(panelLogout != null)
        {
            panelLogout.SetActive(false);
        }
    }
    public void logout() // ################################################################ Método para cerrar sesión
    {
        // await SubirMisionesJSON(); ponerlo apenas se pueda URGENTE
        auth.SignOut(); // Cierra la sesión en Firebase
        PlayerPrefs.DeleteAll(); // Elimina el ID del usuario guardado
        PlayerPrefs.Save(); // Guarda los cambios

        Debug.Log("Sesión cerrada correctamente");

        // Opcional: Redirigir a la escena de login
        SceneManager.LoadScene("Start");
    }

}
