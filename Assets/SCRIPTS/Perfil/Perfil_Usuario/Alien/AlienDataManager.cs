
using Firebase.Auth;
using Firebase.Database;
using Firebase.Firestore;
using System;
using System.Collections;
using UnityEngine;
using System.Net;
using UnityEngine.Networking;
using System.Threading.Tasks;
using TMPro;



public class AlienDataManager : MonoBehaviour
{

    // instanciamos variables database 
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;
    public TMP_Text xptotalTxt;

    [SerializeField] private AlienSwipeController swipeController;

    [SerializeField] private Material lockedMaterial;    // gris, sin textura

    private readonly string[] ordenRangos = {
    "Novato de laboratorio",
    "Aprendiz Atomico",
    "Promesa quimica",
    "Cientifico en Formacion",
    "Experto Molecular",
    "Maestro de Laboratorio",
    "Sabio de la tabla",
    "Leyenda química"
};
    [System.Serializable]
    public class RangoXP
    {
        public string nombre;
        public int xpMinimo;
        public int xpMaximo;
    }

    [SerializeField]
    private RangoXP[] rangosXP = new RangoXP[]
{
    new RangoXP { nombre = "Aprendiz Atomico", xpMinimo = 0, xpMaximo = 300 },
    new RangoXP { nombre = "Explorador de Elementos", xpMinimo = 300, xpMaximo = 900 },
    new RangoXP { nombre = "Científico en Formación", xpMinimo = 900, xpMaximo = 2000 },
    new RangoXP { nombre = "Experto Molecular", xpMinimo = 2000, xpMaximo = 4000 },
    new RangoXP { nombre = "Maestro de Laboratorio", xpMinimo = 4000, xpMaximo = 7500 },
    new RangoXP { nombre = "Sabio de la tabla", xpMinimo = 7500, xpMaximo = 13000 },
    new RangoXP { nombre = "Leyenda química", xpMinimo = 13000, xpMaximo = 25000 },
    new RangoXP { nombre = "Alquimista Supremo", xpMinimo = 25000, xpMaximo = 50000 /*Mathf.Infinity*/ }
};

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        yield return StartCoroutine(HayInternetCoroutine((conexion) =>
        {
            if (conexion)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;

                if (FirebaseAuth.DefaultInstance.CurrentUser != null)
                {
                    userId = auth.CurrentUser.UserId;
                    Debug.Log("Conectado a Firebase con usuario: " + userId);
                    IniciarFirebase();
                }
                else
                {
                    Debug.LogWarning("Usuario no autenticado.");
                }
            }
            else
            {
                ModoSinInternet();
            }
        }));
    }

    private async void IniciarFirebase()
    {
        // verificamos el que el usuario no sea null
        if (string.IsNullOrEmpty(userId))
        {
            Debug.Log("Usuario no autenticado, no se puede acceder a firestore");
            return;

        }

        DocumentReference docRef = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                int xp = snapshot.GetValue<int>("xp");
                xptotalTxt.text = xp.ToString();
                Debug.Log($"XP obtenido: {xp}");
                swipeController.ActualizarSliderXP(xp, rangosXP);

                // Calcular el índice de rango real basado en el XP
                int indiceRango = CalcularIndiceRangoPorXP(xp);
                Debug.Log($"Índice de rango desbloqueado: {indiceRango}");

                // 3. Crear máscara de desbloqueo
                int totalAliens = swipeController.alienRotators.Length;
                bool[] desbloqueado = new bool[totalAliens];

                for (int i = 0; i < totalAliens; i++)
                    desbloqueado[i] = i <= indiceRango;   // desbloquea de 0 hasta su rango

                // 4. Pasar la máscara al SwipeController
                swipeController.SetUnlockMask(desbloqueado, lockedMaterial);

                // 5. Mostrar directamente el alien correspondiente al rango
                swipeController.IrAlAlien(indiceRango);
            }
            else
            {
                Debug.Log("fallo al intentar traer datos desde firebase");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"No se pudo acceder a firestore{e.Message}");
        }
        
    }
    private int CalcularIndiceRangoPorXP(int xp)
    {
        for (int i = rangosXP.Length - 1; i >= 0; i--)
        {
            if (xp >= rangosXP[i].xpMinimo)
                return i;
        }
        return 0;
    }

    private void ModoSinInternet()
    {
        Debug.Log("no tienes conexión a internet");
        swipeController.IrAlAlien(0);
        return;
    }

    IEnumerator HayInternetCoroutine(System.Action<bool> callback)
    {
        UnityWebRequest req = new UnityWebRequest("https://www.google.com");
        req.method = UnityWebRequest.kHttpVerbGET;
        req.downloadHandler = new DownloadHandlerBuffer();

        yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool exito = req.result == UnityWebRequest.Result.Success;
#else
    bool exito = !req.isNetworkError && !req.isHttpError;
#endif

        callback(exito);
    }


}
