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
    private ListenerRegistration listener; // 🔄 Referencia al listener

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        if (notificationPanel != null)
            notificationPanel.SetActive(false);

        CheckPendingRequests();
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
        if (notificationPanel == null || notificationCountText == null) return; // ❗ Seguridad

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

    void SetupRealTimeListener()
    {
        listener = db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", currentUserId)
          .WhereEqualTo("estado", "pendiente")
          .Listen(snapshot =>
          {
              if (this == null || gameObject == null) return; // 🛡️ Protege de destrucción
              UpdateNotificationUI(snapshot.Count);
          });
    }

    void OnDestroy()
    {
        listener?.Stop(); // ❌ Cancela el listener si el objeto es destruido
    }
}
