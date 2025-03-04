using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using TMPro;
using UnityEngine.UI;

public class FirestoreManager : MonoBehaviour
{
    FirebaseFirestore db;

    public Transform content; // El transform del Content dentro del ScrollView
    public GameObject buttonPrefab; // Prefab del bot�n de misi�n

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        CargarMisiones();
    }

    async void CargarMisiones()
    {
        Query misionesQuery = db.Collection("misiones");
        QuerySnapshot snapshot = await misionesQuery.GetSnapshotAsync();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            if (document.Exists)
            {
                // Obtener los datos de la misi�n
                string titulo = document.GetValue<string>("titulo");
                string descripcion = document.GetValue<string>("descripcion");
                int xp = document.GetValue<int>("xp");
                string rutaEscena = document.GetValue<string>("rutaEscena");

                // Mostrar los datos en el log
                Debug.Log($"Misi�n: {titulo}, {descripcion}, XP: {xp}, Escena: {rutaEscena}");

                // Crear un nuevo bot�n y a�adirlo al Content del ScrollView
                GameObject newButton = Instantiate(buttonPrefab, content);

                // Asignar el t�tulo al primer TextMeshProUGUI
                TextMeshProUGUI[] textComponents = newButton.GetComponentsInChildren<TextMeshProUGUI>();

                // Aseg�rate de que el primer TextMeshProUGUI es para el t�tulo
                textComponents[0].text = titulo; // Coloca el t�tulo en el primer TextMeshProUGUI

                // Asignar la descripci�n al segundo TextMeshProUGUI
                textComponents[1].text = descripcion; // Coloca la descripci�n en el segundo TextMeshProUGUI

                // Asignar los XP al tercer TextMeshProUGUI
                textComponents[2].text = $"XP: {xp}"; // Coloca los XP en el tercer TextMeshProUGUI
            }
            else
            {
                Debug.LogWarning("Documento no encontrado.");
            }
        }
    }
}
