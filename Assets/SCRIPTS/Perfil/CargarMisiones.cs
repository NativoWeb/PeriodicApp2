using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using TMPro;
using UnityEngine.UI;

public class CargarMisiones : MonoBehaviour
{
    FirebaseFirestore db;

    public Transform content; // El transform del Content dentro del ScrollView
    public GameObject buttonPrefab; // Prefab del bot�n de misi�n

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        CargarMisioness();
    }

    async void CargarMisioness()
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
                newButton.GetComponentInChildren<TextMeshProUGUI>().text = titulo; // Coloca el t�tulo o descripci�n en el bot�n
            }
            else
            {
                Debug.LogWarning("Documento no encontrado.");
            }
        }
    }
}
