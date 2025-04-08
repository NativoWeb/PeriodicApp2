using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;

public class SearchUsers : MonoBehaviour
{
    public TMP_InputField searchInput;
    public Button searchButton;
    public Transform resultsContainer;
    public GameObject userResultPrefab; // Prefab con nombre y botón de agregar

    FirebaseFirestore db;
    string currentUserId;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        searchButton.onClick.AddListener(() => SearchUser(searchInput.text));
    }

    void SearchUser(string username)
    {
        if (string.IsNullOrEmpty(username)) return;

        // Limpiar resultados anteriores
        foreach (Transform child in resultsContainer)
        {
            Destroy(child.gameObject);
        }

        db.Collection("users")
            .WhereGreaterThanOrEqualTo("DisplayName", username)
            .WhereLessThanOrEqualTo("DisplayName", username + "\uf8ff")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Error buscando usuarios: " + task.Exception);
                    return;
                }

                Debug.Log($"Usuarios encontrados: {task.Result.Count}");

                int userCount = 0; // Contador para saber si hay resultados válidos

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    string userId = doc.Id;

                    // Omitir el usuario actual
                    if (userId == currentUserId)
                        continue;

                    string name = doc.GetValue<string>("DisplayName");
                    string rank = doc.GetValue<string>("Rango");

                    Debug.Log($"Usuario encontrado: {name} - Rango: {rank}");

                    // Instanciar el prefab
                    GameObject userEntry = Instantiate(userResultPrefab, resultsContainer);

                    // Buscar los elementos de UI dentro del prefab
                    TMP_Text nameText = userEntry.transform.Find("NombreText").GetComponent<TMP_Text>();
                    TMP_Text rankText = userEntry.transform.Find("RangoText").GetComponent<TMP_Text>();
                    Button addButton = userEntry.transform.Find("AñadirBtn").GetComponent<Button>();

                    // Asignar datos al prefab
                    nameText.text = name;
                    rankText.text = "Rango: " + rank;

                    // Verificar si ya se envió la solicitud o si ya son amigos
                    CheckFriendStatus(userId, addButton);

                    // Configurar evento del botón
                    addButton.onClick.AddListener(() => AddFriend(userId, name, addButton));

                    userCount++; // Incrementar contador de usuarios válidos
                }

                // Si no se encontraron usuarios válidos
                if (userCount == 0)
                {
                    Debug.LogWarning("No se encontraron usuarios con ese nombre.");
                }
            });
    }


    void CheckFriendStatus(string userId, Button button)
    {
        db.Collection("Solicitudes_Amistad").Document(currentUserId)
            .Collection("Usuarios").Document(userId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    string status = task.Result.GetValue<string>("status");

                    if (status == "Pendiente")
                    {
                        SetButtonState(button, Color.green, "Solicitud enviada", false);
                    }
                    else if (status == "Aceptada")
                    {
                        SetButtonState(button, Color.red, "Amigos", false);
                    }
                }
            });
    }

    void AddFriend(string userId, string username, Button button)
    {
        var friendData = new Dictionary<string, object>
        {
            { "DisplayName", username },
            { "status", "Pendiente" } // Estado de solicitud en espera
        };

        db.Collection("Solicitudes_Amistad").Document(currentUserId)
            .Collection("Usuarios").Document(userId)
            .SetAsync(friendData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Solicitud de amistad enviada a: " + username);
                    SetButtonState(button, Color.green, "Solicitud enviada", false);
                }
                else
                {
                    Debug.LogError("Error al enviar solicitud: " + task.Exception);
                }
            });
    }

    void SetButtonState(Button button, Color color, string text, bool interactable)
    {
        button.GetComponent<Image>().color = color;
        button.GetComponentInChildren<TMP_Text>().text = text;
        button.interactable = interactable;
    }
}
