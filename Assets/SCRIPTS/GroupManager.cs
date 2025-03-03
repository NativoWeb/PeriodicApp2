using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.SceneManagement;

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
            LoadGroups();  // Cargar los grupos de Firestore
        });
    }

    // Cargar los grupos desde Firestore
    void LoadGroups()
    {
        // Referencia a la colección "grupos" en Firestore
        CollectionReference gruposRef = db.Collection("grupos");

        // Obtener todos los documentos de la colección "grupos"
        gruposRef.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                QuerySnapshot snapshot = task.Result;

                // Comprobamos si hay documentos
                if (snapshot.Count > 0)
                {
                    foreach (DocumentSnapshot document in snapshot.Documents)
                    {
                        // Extraer los datos de cada grupo (nombre, descripcion, juegoEscena)
                        string groupName = document.GetValue<string>("nombre");
                        string groupDescription = document.GetValue<string>("descripcion");
                        string gameScene = document.GetValue<string>("juegoEscena");

                        // Imprimir los datos en consola para verificar que se están cargando correctamente
                        Debug.Log($"Grupo: {groupName}, Descripción: {groupDescription}, Escena: {gameScene}");

                        // Crear un botón para cada grupo
                        CreateGroupButton(groupName, groupDescription, gameScene);
                    }
                }
                else
                {
                    Debug.LogError("No hay documentos en la colección 'grupos'.");
                }
            }
            else
            {
                Debug.LogError("Error al cargar los grupos desde Firebase: " + task.Exception);
            }
        });
    }

    // Crear un botón para cada grupo
    void CreateGroupButton(string groupName, string groupDescription, string gameScene)
    {
        // Instanciar un nuevo botón a partir del prefab
        GameObject newButton = Instantiate(buttonPrefab, content);

        // Verificar que el prefab tenga el componente Text para el nombre del grupo
        TextMeshProUGUI[] buttonTexts = newButton.GetComponentsInChildren<TextMeshProUGUI>();

        if (buttonTexts.Length >= 2) // Verifica que haya al menos dos componentes Text
        {
            // Asignar el texto del primer componente Text (para el nombre)
            buttonTexts[0].text = groupName;

            // Asignar el texto del segundo componente Text (para la descripción)
            buttonTexts[1].text = groupDescription;

            Debug.Log($"Texto del botón asignado: {buttonTexts[0].text} - {buttonTexts[1].text}");
        }
        else
        {
            Debug.LogError("El prefab de botón no tiene suficientes componentes Text.");
        }

        // Configurar el evento para cuando se haga clic en el botón
        Button button = newButton.GetComponent<Button>();
        button.onClick.AddListener(() => OnGroupSelected(gameScene));
    }


    // Acción cuando se selecciona un grupo
    void OnGroupSelected(string gameScene)
    {
        // Cargar la escena relacionada con el grupo
        Debug.Log($"Grupo seleccionado. Cargando la escena: {gameScene}");
        SceneManager.LoadScene(gameScene);
    }
}
