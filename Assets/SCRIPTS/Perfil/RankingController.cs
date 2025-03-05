using Firebase.Firestore;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RankingController : MonoBehaviour
{
    public TMP_Text posicionText;  // Texto donde se mostrar� la posici�n
    private FirebaseFirestore db;
    private string userId;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "");  // Obtener el ID del usuario logueado

        if (!string.IsNullOrEmpty(userId))
        {
            ObtenerPosicionUsuario();
        }
        else
        {
            posicionText.text = "Posici�n: No disponible";
        }
    }

    async void ObtenerPosicionUsuario()
    {
        Query rankingQuery = db.Collection("users").OrderByDescending("xp");
        QuerySnapshot snapshot = await rankingQuery.GetSnapshotAsync();

        int posicion = 1; // Empezamos en la posici�n 1

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (doc.Id == userId)  // Si encontramos al usuario logueado
            {
                posicionText.text = "Posici�n: #" + posicion;
                Debug.Log($"El usuario {userId} est� en la posici�n {posicion} del ranking.");
                return; // Salimos del bucle
            }
            posicion++; // Si no es el usuario, aumentamos la posici�n
        }

        // Si no lo encontr� en la base de datos
        posicionText.text = "Posici�n: No encontrada";
        Debug.LogError("No se encontr� al usuario en el ranking.");
    }
}
