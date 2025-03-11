using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class GroupManager : MonoBehaviour
{
    public GameObject buttonPrefab;  // Prefab del botón
    public Transform content;        // Contenedor donde se agregarán los botones
    private FirebaseFirestore db;

    // Start is called before the first frame update
    void Start()
    {
        // Inicializar Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            db = FirebaseFirestore.GetInstance(app);  // Inicializa Firestore
            LoadGroups();
        });


    }

    //Cargar los grupos desde Firestore
    void LoadGroups()
    {
        CollectionReference gruposRef = db.Collection("grupos");

        gruposRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error al cargar los grupos desde Firebase: " + task.Exception);
                return;
            }

            QuerySnapshot snapshot = task.Result;

            if (snapshot.Count > 0)
            {
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        string groupName = document.GetValue<string>("nombre");
                        string groupDescription = document.GetValue<string>("descripcion");
                        string gameScene = document.GetValue<string>("juegoEscena");

                        Debug.Log($"Grupo: {groupName}, Descripción: {groupDescription}, Escena: {gameScene}");

                        // Crear el botón en el hilo principal
                        CreateGroupButton(groupName, groupDescription, gameScene);
                    }
                }
            }
            else
            {
                Debug.LogWarning("No hay documentos en la colección 'grupos'.");
            }
        });
    }



    // Crear un botón para cada grupo
    void CreateGroupButton(string groupName, string groupDescription, string gameScene)
    {
        // Instanciar un nuevo botón a partir del prefab
        GameObject newButton = Instantiate(buttonPrefab, content);
        newButton.SetActive(true);

        // Verificar que el prefab tenga el componente Text para el nombre del grupo
        TextMeshProUGUI[] buttonTexts = newButton.GetComponentsInChildren<TextMeshProUGUI>();

        if (buttonTexts.Length >= 2) // Verifica que haya al menos dos componentes Text
        {
            // Asignar el texto del primer componente Text (para el nombre)
            buttonTexts[0].text = groupName;

            // Asignar el texto del segundo componente Text (para la descripción)
            buttonTexts[1].text = groupDescription;

        }
        else
        {
            Debug.LogError("El prefab de botón no tiene suficientes componentes Text.");
        }

        // Configurar el evento para cuando se haga clic en el botón
        Button button = newButton.GetComponent<Button>();
        button.onClick.AddListener(() => OnGroupSelected(gameScene));
        // Forzar actualización del layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    }


    // Acción cuando se selecciona un grupo
    void OnGroupSelected(string gameScene)
    {
        // Cargar la escena relacionada con el grupo
        Debug.Log($"Grupo seleccionado. Cargando la escena: {gameScene}");
        SceneManager.LoadScene(gameScene);
    }
}
