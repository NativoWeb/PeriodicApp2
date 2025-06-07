//using Firebase;
//using Firebase.Messaging;
//using Firebase.Extensions;
//using Firebase.Firestore;
//using UnityEngine;
//using Firebase.Auth;

//public class FCMTokenManager : MonoBehaviour
//{
//    private void Start()
//    {
//        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
//            FirebaseApp app = FirebaseApp.DefaultInstance;
//            FirebaseMessaging.TokenReceived += OnTokenReceived;
//            FirebaseMessaging.MessageReceived += OnMessageReceived;

//            // Aquí obtienes el FCM token
//            ObtenerFCMToken();
//        });
//    }

//    private void ObtenerFCMToken()
//    {
//        FirebaseMessaging.GetTokenAsync().ContinueWithOnMainThread(task => {
//            if (task.IsCompleted && !string.IsNullOrEmpty(task.Result))
//            {
//                string token = task.Result;
//                Debug.Log("FCM Token: " + token);
//                // Aquí puedes guardar el token en Firebase Firestore, asociado al usuario
//                GuardarTokenEnFirestore(token);
//            }
//            else
//            {
//                Debug.LogError("No se pudo obtener el token FCM.");
//            }
//        });
//    }

//    private void GuardarTokenEnFirestore(string token)
//    {
//        // Suponiendo que tienes el UID del usuario de Firebase Auth
//        string uid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

//        // Guardar el token en la base de datos
//        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
//        DocumentReference userRef = db.Collection("users").Document(uid);

//        // Usamos UpdateAsync para actualizar solo el campo fcmToken sin sobrescribir otros datos
//        userRef.UpdateAsync("fcmToken", token).ContinueWithOnMainThread(task => {
//            if (task.IsCompleted)
//            {
//                Debug.Log("Token FCM guardado correctamente.");
//            }
//            else
//            {
//                Debug.LogError("Error al guardar el token FCM: " + task.Exception);
//            }
//        });
//    }


//    private void OnTokenReceived(object sender, TokenReceivedEventArgs tokenArgs)
//    {
//        Debug.Log("Nuevo token FCM recibido: " + tokenArgs.Token);
//    }

//    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
//    {
//        Debug.Log("Mensaje recibido: " + e.Message);
//    }
//}
