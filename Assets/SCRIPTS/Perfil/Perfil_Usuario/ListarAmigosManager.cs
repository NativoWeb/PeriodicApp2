using Firebase.Firestore;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Extensions;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using System.Linq;

public class ListarAmigosManager : MonoBehaviour
{
    [Header("Paneles para listar amigos")]
    [SerializeField] private GameObject panelAmigo1;
    private TMP_Text nombreAmigo1;
    private TMP_Text rangoAmigo1;
    private Image AvatarAmigo1;
    public Button BtnAmigossinfuncionalidad;
    public Button BtnAñadirAmigos;
    public Button BtnVerTodosAmigos;

    [SerializeField] private GameObject panelAmigo2;
    private TMP_Text nombreAmigo2;
    private TMP_Text rangoAmigo2;
    private Image AvatarAmigo2;

    [SerializeField] private GameObject panelAmigo3;
    private TMP_Text nombreAmigo3;
    private TMP_Text rangoAmigo3;
    private Image AvatarAmigo3;

    [Header("Configuración sin amigos")]
    [SerializeField] private string mensajeSinAmigos = "Añade amigos para comenzar";
    [SerializeField] private string rangoDefault = "-";
    [SerializeField] private Color colorTextoSinAmigos = Color.black;

    private bool isLoading = false;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;

    [Header("Referencias a paneles debajo de amigos, para poder moverlos")]
    [SerializeField] private RectTransform panelesInferiores;

    public void Start()
    {
        InitializeUIComponents();
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            userId = auth.CurrentUser.UserId;
            CargarAmigos();
        }
        else
        {
            Debug.Log("ListarAmigosManager.Error: no hay usuario autenticado");
            MostrarEstadoSinAmigos(0);
        }

        BtnVerTodosAmigos.onClick.AddListener(VerTodosAmigos);
        BtnAñadirAmigos.onClick.AddListener(VerTodosUsuariosSugeridos);
        Debug.Log("anchoredPosition Y = " + panelesInferiores.anchoredPosition.y);

    }

    private void InitializeUIComponents()
    {
        // Panel 1
        nombreAmigo1 = panelAmigo1.transform.Find("NombreAmigo").GetComponent<TMP_Text>();
        rangoAmigo1 = panelAmigo1.transform.Find("RangoAmigo").GetComponent<TMP_Text>();
        AvatarAmigo1 = panelAmigo1.transform.Find("AvatarAmigo").GetComponent<Image>();

        // Panel 2
        nombreAmigo2 = panelAmigo2.transform.Find("NombreAmigo").GetComponent<TMP_Text>();
        rangoAmigo2 = panelAmigo2.transform.Find("RangoAmigo").GetComponent<TMP_Text>();
        AvatarAmigo2 = panelAmigo2.transform.Find("AvatarAmigo").GetComponent<Image>();

        // Panel 3
        nombreAmigo3 = panelAmigo3.transform.Find("NombreAmigo").GetComponent<TMP_Text>();
        rangoAmigo3 = panelAmigo3.transform.Find("RangoAmigo").GetComponent<TMP_Text>();
        AvatarAmigo3 = panelAmigo3.transform.Find("AvatarAmigo").GetComponent<Image>();
    }

    public void CargarAmigos()
    {
        if (isLoading) return;

        if (!HayConexion())
        {
            Debug.Log("No hay conexión a internet.");
            MostrarEstadoSinAmigos(0);
            return;
        }

        isLoading = true;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.Log("ListarAmigosManager.Error: ID de usuario vacío");
            isLoading = false;
            MostrarEstadoSinAmigos(0);
            return;
        }

        // Consulta la subcolección de amigos del usuario actual
        db.Collection("users").Document(userId).Collection("amigos")
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              isLoading = false;

              if (task.IsFaulted)
              {
                  Debug.LogError("Error al cargar amigos: " + task.Exception);
                  MostrarEstadoSinAmigos(0);
                  return;
              }

              QuerySnapshot snapshot = task.Result;

              if (snapshot.Count == 0)
              {
                  MostrarEstadoSinAmigos(0);
                  return;
              }

              // Obtener todos los amigos y ordenarlos por fecha de amistad (si es necesario)
              var amigos = snapshot.Documents.ToList();

              // Limpiar todos los paneles primero
              LimpiarPaneles();

              // Mostrar amigos en los primeros paneles disponibles
              for (int i = 0; i < Mathf.Min(amigos.Count, 3); i++)
              {
                  Dictionary<string, object> amigo = amigos[i].ToDictionary();
                  string idAmigo = amigo["userId"].ToString();
                  string nombreAmigo = amigo["DisplayName"].ToString();
                  MostrarAmigoEnPanel(idAmigo, i + 1);
              }

              // Gestionar visibilidad de paneles según cantidad de amigos
              GestionarVisibilidadPaneles(amigos.Count);
          });
    }


    public void LimpiarPaneles()
    {
        // Limpiar panel 1
        if (nombreAmigo1 && rangoAmigo1 != null)
        {
            nombreAmigo1.text = "";
            rangoAmigo1.text = "";
        }

        // Limpiar panel 2
        if (nombreAmigo2 && rangoAmigo2 != null)
        {
            nombreAmigo2.text = "";
            rangoAmigo2.text = "";
        }

        // Limpiar panel 3
        if (nombreAmigo3 && rangoAmigo3 != null)
        {
            nombreAmigo3.text = "";
            rangoAmigo3.text = "";
        }
    }

    private void MostrarAmigoEnPanel(string amigoId, int panelIndex)
    {
        db.Collection("users").Document(amigoId).GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error al cargar datos del amigo: " + task.Exception);
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    Dictionary<string, object> datosAmigo = snapshot.ToDictionary();

                    string rango = datosAmigo.ContainsKey("Rango") ? datosAmigo["Rango"].ToString() : "Sin rango";
                    string nombreAmigo = datosAmigo.ContainsKey("DisplayName") ? datosAmigo["DisplayName"].ToString() : "Desconocido";
                    string avatarPath = ObtenerAvatarPorRango(rango);
                    Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/defecto");

                    switch (panelIndex)
                    {
                        case 1:
                            nombreAmigo1.text = nombreAmigo;
                            rangoAmigo1.text = rango;
                            AvatarAmigo1.sprite = avatarSprite;
                            break;
                        case 2:
                            nombreAmigo2.text = nombreAmigo;
                            rangoAmigo2.text = rango;
                            AvatarAmigo2.sprite = avatarSprite;
                            break;
                        case 3:
                            nombreAmigo3.text = nombreAmigo;
                            rangoAmigo3.text = rango;
                            AvatarAmigo3.sprite = avatarSprite;
                            break;
                    }
                }
            });
    }

    private void GestionarVisibilidadPaneles(int cantidadAmigos)
    {
        // Activar solo los paneles necesarios
        panelAmigo1.SetActive(cantidadAmigos >= 1);
        panelAmigo2.SetActive(cantidadAmigos >= 2);
        panelAmigo3.SetActive(cantidadAmigos >= 3);

        // Si no hay amigos, mostrar estado especial
        if (cantidadAmigos == 0)
        {
            MostrarEstadoSinAmigos(cantidadAmigos);
        }
        AjustarPosicionPanelesInferiores(cantidadAmigos);
    }
    private void AjustarPosicionPanelesInferiores(int cantidadAmigos)
    {
        // Posiciones sugeridas (ajústalas según tu diseño)
        float yCon3Amigos = -944f;
        float yCon2Amigos = -708f;
        float yCon1Amigo = -450f;

        if (panelesInferiores == null) return;

        Vector2 posActual = panelesInferiores.anchoredPosition;

        switch (cantidadAmigos)
        {
            case 1:
                panelesInferiores.anchoredPosition = new Vector2(posActual.x, yCon1Amigo);
                break;
            case 2:
                panelesInferiores.anchoredPosition = new Vector2(posActual.x, yCon2Amigos);
                break;
            default: // 3 o más
                panelesInferiores.anchoredPosition = new Vector2(posActual.x, yCon3Amigos);
                break;
        }
    }


    private void MostrarEstadoSinAmigos(int cantidadAmigos)
    {
        if (cantidadAmigos == 0)
        {
            // Activar solo el primer panel
            panelAmigo1.SetActive(true);
            panelAmigo2.SetActive(false);
            panelAmigo3.SetActive(false);

            // Configurar el panel 1 con el mensaje
            nombreAmigo1.text = mensajeSinAmigos;
            rangoAmigo1.text = rangoDefault;

            // desactivar componenetes no necesarios si no tiene amigos
            AvatarAmigo1.GetComponent<Image>().enabled = false;
            BtnAmigossinfuncionalidad.gameObject.SetActive(false);
            BtnAñadirAmigos.gameObject.SetActive(true);


            // Aplicar estilo especial para el mensaje "sin amigos"
            nombreAmigo1.color = colorTextoSinAmigos;
            rangoAmigo1.color = colorTextoSinAmigos;

            // subimos el panel de abajo para quitar espacio en blanco 
            Vector2 posActual = panelesInferiores.anchoredPosition;
            panelesInferiores.anchoredPosition = new Vector2(posActual.x, -450f);
        }
    }

    private string ObtenerAvatarPorRango(string rangos)
    {
        switch (rangos)
        {
            case "Novato de laboratorio": return "Avatares/Rango1";
            case "Aprendiz Atomico": return "Avatares/Rango2";
            case "Promesa quimica": return "Avatares/Rango3";
            case "Cientifico en Formacion": return "Avatares/Rango4";
            case "Experto Molecular": return "Avatares/Rango5";
            case "Maestro de Laboratorio": return "Avatares/Rango6";
            case "Sabio de la tabla": return "Avatares/Rango7";
            case "Leyenda química": return "Avatares/Rango8";
            default: return "Avatares/Rango1";
        }
    }

    public bool HayConexion()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    void VerTodosUsuariosSugeridos()
    {
        PlayerPrefs.SetInt("MostrarSugerencias", 1);
        SceneManager.LoadScene("Amigos");
    }

    void VerTodosAmigos()
    {
        SceneManager.LoadScene("Amigos");
    }
}