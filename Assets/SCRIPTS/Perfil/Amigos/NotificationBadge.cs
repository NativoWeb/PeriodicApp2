using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
using TMPro;

public class NotificationBadge : MonoBehaviour
{
    public GameObject notificationPanel; // Panel que contendrá el número
    public TMP_Text notificationCountText; // Texto para mostrar el número

    private FirebaseFirestore db;
    private string currentUserId;

    void Start()
    {
        // Inicializar Firebase
        db = FirebaseFirestore.DefaultInstance;
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        // Ocultar panel al inicio
        notificationPanel.SetActive(false);

        // Comprobar solicitudes pendientes
        CheckPendingRequests();

        // Opcional: Escuchar cambios en tiempo real
        SetupRealTimeListener();
    }

    void CheckPendingRequests()
    {
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", currentUserId)
          .WhereEqualTo("estado", "pendiente")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("Error al obtener solicitudes: " + task.Exception);
                  return;
              }

              int pendingCount = task.Result.Count;
              UpdateNotificationUI(pendingCount);
          });
    }

    void UpdateNotificationUI(int count)
    {
        if (count > 0)
        {
            notificationPanel.SetActive(true);
            notificationCountText.text = count.ToString();
        }
        else
        {
            notificationPanel.SetActive(false);
        }
    }

    // Opcional: Escucha en tiempo real
    void SetupRealTimeListener()
    {
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", currentUserId)
          .WhereEqualTo("estado", "pendiente")
          .Listen(snapshot =>
          {
              // snapshot ya ES un QuerySnapshot
              int newCount = snapshot.Count; // ✅ Correcto
              UpdateNotificationUI(newCount);
          });
    }
}