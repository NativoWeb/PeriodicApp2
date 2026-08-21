
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
    "Leyenda química",
    "Alquimista Supremo"
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
    new RangoXP { nombre = "Novato de laboratorio", xpMinimo = 0, xpMaximo = 200 },
    new RangoXP { nombre = "Aprendiz Atomico", xpMinimo = 200, xpMaximo = 600 },
    new RangoXP { nombre = "Promesa quimica", xpMinimo = 600, xpMaximo = 1200 },
    new RangoXP { nombre = "Cientifico en Formacion", xpMinimo = 1200, xpMaximo = 2300 },
    new RangoXP { nombre = "Experto Molecular", xpMinimo = 2300, xpMaximo = 3500 },
    new RangoXP { nombre = "Maestro de Laboratorio", xpMinimo = 3500, xpMaximo = 6000 },
    new RangoXP { nombre = "Sabio de la tabla", xpMinimo = 6000, xpMaximo = 10000 },
    new RangoXP { nombre = "Leyenda química", xpMinimo = 10000, xpMaximo = 50000 }
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
        Debug.Log("[AlienDataManager] Sin internet. Usando datos locales.");

        int xpLocal = PlayerPrefs.GetInt("xp", 0);
        int indiceRango = CalcularIndiceRangoPorXP(xpLocal);

        // Crear mascara de desbloqueo basada en XP local
        int totalAliens = swipeController.alienRotators.Length;
        bool[] desbloqueado = new bool[totalAliens];
        for (int i = 0; i < totalAliens; i++)
            desbloqueado[i] = i <= indiceRango;

        swipeController.SetUnlockMask(desbloqueado, lockedMaterial);
        swipeController.ActualizarSliderXP(xpLocal, rangosXP);
        swipeController.IrAlAlien(indiceRango);

        if (xptotalTxt != null)
            xptotalTxt.text = xpLocal.ToString();
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
