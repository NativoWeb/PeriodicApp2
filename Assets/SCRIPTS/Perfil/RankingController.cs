using Firebase.Firestore;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RankingController : MonoBehaviour
{
    public TMP_Text posicionText;  // Texto donde se mostrará la posición
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
            posicionText.text = "Posición: No disponible";
        }
    }

    async void ObtenerPosicionUsuario()
    {
        Query rankingQuery = db.Collection("users").OrderByDescending("xp");
        QuerySnapshot snapshot = await rankingQuery.GetSnapshotAsync();

        int posicion = 1; // Empezamos en la posición 1

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (doc.Id == userId)  // Si encontramos al usuario logueado
            {
                posicionText.text = "Posición: #" + posicion;
                Debug.Log($"El usuario {userId} está en la posición {posicion} del ranking.");
                return; // Salimos del bucle
            }
            posicion++; // Si no es el usuario, aumentamos la posición
        }

        // Si no lo encontró en la base de datos
        posicionText.text = "Posición: No encontrada";
        Debug.LogError("No se encontró al usuario en el ranking.");
    }
}
