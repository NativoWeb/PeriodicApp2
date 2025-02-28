//using Firebase;
//using Firebase.Firestore;
//using Firebase.Extensions;
//using UnityEngine;
//using UnityEngine.UI;

//public class ocupacion : MonoBehaviour
//{
//    public Toggle studentToggle;
//    public Toggle teacherToggle;
//    public Toggle otherToggle;
//    public Button saveButton;
//    private FirebaseFirestore db;

//    void Start()
//    {
//         Inicializar Firestore
//        db = FirebaseFirestore.DefaultInstance;

//         Asegurarse de que el bot�n de guardar est� correctamente asociado
//        if (saveButton != null)
//        {
//            saveButton.onClick.AddListener(SaveOccupation);
//        }
//        else
//        {
//            Debug.LogError("El bot�n de guardar no est� asignado.");
//        }
//    }

//    void SaveOccupation()
//    {
//        string occupation = "Otro"; // Por defecto

//         Determinar ocupaci�n seg�n el toggle seleccionado
//        if (studentToggle.isOn) occupation = "Estudiante";
//        else if (teacherToggle.isOn) occupation = "Profesor";

//         Verificar si el UID del usuario est� disponible
//        if (!string.IsNullOrEmpty(RegisterController.currentUserID))
//        {
//            DocumentReference docRef = db.Collection("users").Document(RegisterController.currentUserID);

//             Intentar guardar la ocupaci�n en Firestore
//            docRef.SetAsync(new { occupation = occupation }, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
//            {
//                if (task.IsCompleted && !task.IsFaulted)
//                {
//                     La ocupaci�n se guard� correctamente
//                    Debug.Log("Ocupaci�n guardada correctamente.");
//                }
//                else
//                {
//                     Si hubo un error, mostrar detalles
//                    if (task.Exception != null)
//                    {
//                        foreach (var e in task.Exception.InnerExceptions)
//                        {
//                            Debug.LogError($"Error al guardar la ocupaci�n: {e.Message}");
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogError("Error desconocido al guardar la ocupaci�n.");
//                    }
//                }
//            });
//        }
//        else
//        {
//            Debug.LogError("No se encontr� el UID del usuario. �El usuario est� correctamente autenticado?");
//        }
//    }
//}
