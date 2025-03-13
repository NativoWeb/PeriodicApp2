//verificaicon controller
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VerificacionController : MonoBehaviour
{
    public TMP_InputField verificationCodeInput;
    public Button verifyButton;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            if (app != null)
            {
                auth = FirebaseAuth.DefaultInstance;
                firestore = FirebaseFirestore.DefaultInstance;
                verifyButton.onClick.AddListener(OnVerifyButtonClick);
            }
            else
            {
                Debug.LogError("Firebase no se ha podido inicializar.");
            }
        });
    }

    public void OnVerifyButtonClick()
    {
        string enteredCode = verificationCodeInput.text;

        // Obtener el código de verificación almacenado en Firestore
        FirebaseUser currentUser = auth.CurrentUser;
        if (currentUser != null)
        {
            VerifyCode(currentUser.UserId, enteredCode);
        }
        else
        {
            Debug.LogError("No se ha encontrado un usuario autenticado.");
        }
    }

    private void VerifyCode(string userId, string enteredCode)
    {
        DocumentReference docRef = firestore.Collection("users").Document(userId);
        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("La solicitud fue cancelada.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Error al obtener el código de verificación: " + task.Exception?.Message);
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists)
            {
                string storedCode = snapshot.GetValue<string>("VerificationCode");
                if (enteredCode == storedCode)
                {
                    Debug.Log("✅ Código verificado correctamente.");

                    // Guardar que el usuario verificó su correo en PlayerPrefs
                    PlayerPrefs.SetInt("EmailVerified", 1);
                    PlayerPrefs.Save(); // Guarda la preferencia

                    // Cambiar a la escena de completar perfil
                    SceneManager.LoadScene("Registrar");
                }
                else
                {
                    Debug.LogError("El código ingresado es incorrecto.");
                }
            }
            else
            {
                Debug.LogError("No se ha encontrado un código de verificación para este usuario.");
            }
        });
    }

}
