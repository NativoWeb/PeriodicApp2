using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Extensions;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using DG.Tweening; 
public class SolicitudesAmistadManager : MonoBehaviour
{
    public GameObject[] solicitudPanels; // Asigna los paneles en el inspector
    
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentUserId;

   
    private class FriendRequest
    {
        public string fromUserId;
        public string fromUserName;
        public string fromUserRank;
        public string fromUserAvatar;
        public string documentId;
    }

    private List<FriendRequest> allRequests = new List<FriendRequest>();

    public Button BtnVerSolicitudes;
    public Button BtnA�adirAmigos;

    [Header("Panel que se mover� al mostrar solicitudes")]
    [SerializeField] private RectTransform panelInferiorSolicitudes;

    [Header("Panel general solcitudes")]
    [SerializeField] private GameObject panelgeneralSolicitudes;

    private Vector2 posicionBaseInferiorSolicitudes;

    public void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser == null)
        {
            Debug.LogError("Usuario no autenticado.");
            return;
        }
        // GUARDAMOS LA POSICI�N INICIAL UNA VEZ

        if (panelInferiorSolicitudes != null)
        {
            posicionBaseInferiorSolicitudes = panelInferiorSolicitudes.anchoredPosition;
            Debug.Log("Posici�n base del panel inferior guardada: " + posicionBaseInferiorSolicitudes);
        }

        currentUserId = auth.CurrentUser.UserId;
        LoadPendingRequests();

        BtnVerSolicitudes.onClick.AddListener(VerTodasSolicitudes);
        BtnA�adirAmigos.onClick.AddListener(VerTodosUsuariosSugeridos);

    }

    void VerTodosUsuariosSugeridos()
    {
        PlayerPrefs.SetInt("MostrarSugerencias", 1);
        SceneManager.LoadScene("Amigos");
    }
   public void LoadPendingRequests()
    {
        // desactivamos btn de agregar amigos
        if(BtnA�adirAmigos != null)
            BtnA�adirAmigos.gameObject.SetActive(false);

        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", currentUserId)
          .WhereEqualTo("estado", "pendiente")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (!task.IsCompleted || task.Result == null)
              {
                  
                  return;
              }

              allRequests.Clear();
              var userFetchTasks = new List<Task>();

              foreach (var document in task.Result.Documents)
              {
                  string fromUserId = document.GetValue<string>("idRemitente");

                  var fetchTask = db.Collection("users").Document(fromUserId).GetSnapshotAsync()
                      .ContinueWithOnMainThread(userTask =>
                      {
                          if (userTask.IsCompleted && userTask.Result.Exists)
                          {
                              string rank = userTask.Result.ContainsField("Rango") ?
                                  userTask.Result.GetValue<string>("Rango") : "Novato de laboratorio";

                              string rango = ObtenerAvatarPorRango(rank);// conseguimos el avatar del remitente

                              string fromUserName = userTask.Result.ContainsField("DisplayName") ?
                              userTask.Result.GetValue<string>("DisplayName") : "Desconocido";
                              allRequests.Add(new FriendRequest
                              {
                                  fromUserId = fromUserId,
                                  fromUserName = fromUserName,
                                  fromUserRank = rank,
                                  fromUserAvatar = rango,
                                  documentId = document.Id
                              });
                          }
                      });

                  userFetchTasks.Add(fetchTask);
              }

              Task.WhenAll(userFetchTasks).ContinueWithOnMainThread(finalTask =>
              {
                  DisplayRequests();
              });
          });
    }

   public void DisplayRequests()
    {
        // Desactivar todos los paneles primero
        foreach (var panel in solicitudPanels)
        {
            panel.SetActive(false);
        }

        if (allRequests.Count == 0)
        {
            if (solicitudPanels.Length > 0)
            {
                solicitudPanels[0].SetActive(true);
                solicitudPanels[0].transform.Find("NombreText").GetComponent<TMP_Text>().text = "Sin solicitudes";
                solicitudPanels[0].transform.Find("RangoText").GetComponent<TMP_Text>().text = "Invita a tus amigos";
                BtnA�adirAmigos.gameObject.SetActive(true);
                    
                solicitudPanels[0].transform.Find("AvatarImage").gameObject.SetActive(false);
                solicitudPanels[0].transform.Find("AceptarBtn").gameObject.SetActive(false);
                solicitudPanels[0].transform.Find("RechazarBtn").gameObject.SetActive(false);

               
            }
            // subimos el panel de abajo para quitar espacio en blanco 
            //Vector2 posActual = panelInferiorSolicitudes.anchoredPosition;
            //panelInferiorSolicitudes.anchoredPosition = new Vector2(posActual.x, -450f);

            panelInferiorSolicitudes.anchoredPosition = posicionBaseInferiorSolicitudes;
            AjustarPosicionPanelInferior(1);

            return;
           
        }


        int toShow = Mathf.Min(3, allRequests.Count);

        for (int i = 0; i < toShow; i++)
        {
            var request = allRequests[i];
            GameObject panel = solicitudPanels[i];
            panel.SetActive(true);

            panel.transform.Find("NombreText").GetComponent<TMP_Text>().text = request.fromUserName;
            panel.transform.Find("RangoText").GetComponent<TMP_Text>().text = request.fromUserRank;

            // l�gica para poner el avatar en solicitudes
            Sprite avatarSprite = Resources.Load<Sprite>(request.fromUserAvatar) ?? Resources.Load<Sprite>("Avatares/defecto");

            panel.transform.Find("AvatarImage").GetComponent<Image>().sprite = avatarSprite;

            // Limpiar listeners anteriores
            Button aceptarBtn = panel.transform.Find("AceptarBtn").GetComponent<Button>();
            Button rechazarBtn = panel.transform.Find("RechazarBtn").GetComponent<Button>();

            Image avatarRemitente = panel.transform.Find("AvatarImage").GetComponent<Image>();

            aceptarBtn.onClick.RemoveAllListeners();
            rechazarBtn.onClick.RemoveAllListeners();

            // activar nuevamente los componentes ocultos
            avatarRemitente.gameObject.SetActive(true);
            aceptarBtn.gameObject.SetActive(true);
            rechazarBtn.gameObject.SetActive(true);


            string docId = request.documentId;
            aceptarBtn.onClick.AddListener(() => AcceptRequest(docId));
            rechazarBtn.onClick.AddListener(() => RejectRequest(docId));
        }
        
        if ( panelgeneralSolicitudes != null)
            AjustarPosicionPanelInferior(allRequests.Count);
        
        
    }
    public void AjustarPosicionPanelInferior(int cantidadSolicitudes)
    {
        if (panelInferiorSolicitudes == null) return;
        
            TercerPanelManager.instancia.ResetearPosicion(); // Restauramos

            float offsetY = 0f;
            switch (cantidadSolicitudes)
            {
                case 1: offsetY = 400f; break;
                case 2: offsetY = 200f; break;
                default: offsetY = 0f; break;
            }

            panelInferiorSolicitudes.DOAnchorPos(
                TercerPanelManager.instancia.GetPosicionBase() + new Vector2(0, offsetY),
                0.3f
            ).SetEase(Ease.OutCubic);
        
    }




    void AcceptRequest(string documentId)
    {
        // Primero obtenemos la informaci�n de la solicitud
        var requestDoc = db.Collection("SolicitudesAmistad").Document(documentId);
        requestDoc.GetSnapshotAsync().ContinueWithOnMainThread(getTask =>
        {
            if (!getTask.IsCompleted || !getTask.Result.Exists)
            {
               
                Debug.LogError("Error al obtener solicitud: " + getTask.Exception);
                return;
            }

            // Obtenemos los datos de la solicitud
            string fromUserId = getTask.Result.GetValue<string>("idRemitente");
            string fromUserName = getTask.Result.GetValue<string>("nombreRemitente");

            // Actualizamos el estado de la solicitud
            requestDoc.UpdateAsync("estado", "aceptada").ContinueWithOnMainThread(updateTask =>
            {
                if (!updateTask.IsCompleted)
                {
                    
                    Debug.LogError("Error al actualizar solicitud: " + updateTask.Exception);
                    return;
                }

                // Creamos la referencia a la subcolecci�n de amigos del usuario actual
                var currentUserFriendsRef = db.Collection("users").Document(currentUserId).Collection("amigos");

                // Creamos la referencia a la subcolecci�n de amigos del otro usuario
                var otherUserFriendsRef = db.Collection("users").Document(fromUserId).Collection("amigos");

                // Creamos un diccionario con los datos del amigo a agregar
                var friendData = new Dictionary<string, object>
            {
                { "userId", fromUserId },
                { "DisplayName", fromUserName },
                { "fechaAmistad", FieldValue.ServerTimestamp }
            };

                // Creamos un diccionario con los datos del usuario actual para el otro usuario
                var currentUserData = new Dictionary<string, object>
            {
                { "userId", currentUserId },
                { "DisplayName", auth.CurrentUser.DisplayName ?? "Usuario sin nombre" },
                { "fechaAmistad", FieldValue.ServerTimestamp }
            };

                // Batch para ejecutar ambas operaciones at�micamente
                var batch = db.StartBatch();

                // Agregamos el amigo al usuario actual
                batch.Set(currentUserFriendsRef.Document(fromUserId), friendData);

                // Agregamos el usuario actual como amigo del otro usuario
                batch.Set(otherUserFriendsRef.Document(currentUserId), currentUserData);

                // Ejecutamos el batch
                batch.CommitAsync().ContinueWithOnMainThread(batchTask =>
                {
                    if (batchTask.IsCompleted)
                    {
                        
                        LoadPendingRequests();
                    }
                    else
                    {
                        Debug.LogError("Error en batch: " + batchTask.Exception);
                    }
                });
            });
        });
    }

    void RejectRequest(string documentId)
    {
        db.Collection("SolicitudesAmistad").Document(documentId)
          .DeleteAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  LoadPendingRequests();
              }
              else
              {
                  Debug.LogError("SolicitudesAmistadManager. Error al eliminar solicitud: " + task.Exception);
              }
          });
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
            case "Leyenda qu�mica": return "Avatares/Rango8";
            default: return "Avatares/Rango1";
        }
    }
    void VerTodasSolicitudes()
    {
        PlayerPrefs.SetInt("MostrarSolicitudes", 1);
        SceneManager.LoadScene("Amigos");
    }

}
