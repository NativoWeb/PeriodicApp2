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

//         Asegurarse de que el botón de guardar esté correctamente asociado
//        if (saveButton != null)
//        {
//            saveButton.onClick.AddListener(SaveOccupation);
//        }
//        else
//        {
//            Debug.LogError("El botón de guardar no está asignado.");
//        }
//    }

//    void SaveOccupation()
//    {
//        string occupation = "Otro"; // Por defecto

//         Determinar ocupación según el toggle seleccionado
//        if (studentToggle.isOn) occupation = "Estudiante";
//        else if (teacherToggle.isOn) occupation = "Profesor";

//         Verificar si el UID del usuario está disponible
//        if (!string.IsNullOrEmpty(RegisterController.currentUserID))
//        {
//            DocumentReference docRef = db.Collection("users").Document(RegisterController.currentUserID);

//             Intentar guardar la ocupación en Firestore
//            docRef.SetAsync(new { occupation = occupation }, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
//            {
//                if (task.IsCompleted && !task.IsFaulted)
//                {
//                     La ocupación se guardó correctamente
//                    Debug.Log("Ocupación guardada correctamente.");
//                }
//                else
//                {
//                     Si hubo un error, mostrar detalles
//                    if (task.Exception != null)
//                    {
//                        foreach (var e in task.Exception.InnerExceptions)
//                        {
//                            Debug.LogError($"Error al guardar la ocupación: {e.Message}");
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogError("Error desconocido al guardar la ocupación.");
//                    }
//                }
//            });
//        }
//        else
//        {
//            Debug.LogError("No se encontró el UID del usuario. ¿El usuario está correctamente autenticado?");
//        }
//    }
//}
