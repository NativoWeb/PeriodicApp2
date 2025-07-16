using System;
using TMPro;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.UI;

public class RachaManager : MonoBehaviour
{
    public TMP_Text RachaTexto;
    public Button BtnAbrir_panelRacha;
    [SerializeField] public GameObject panelRacha;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser user;
    private string userId;

    private int rachaActualLocal;
    private DateTime ultimaFechaLocal;

    private void Start()
    {
        // Forzar orientación vertical
        Screen.orientation = ScreenOrientation.Portrait;

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        user = auth.CurrentUser;
        userId = user?.UserId;

        VerificarRacha();
        BtnAbrir_panelRacha.onClick.AddListener(AbrirPanelRacha);
    }
    void AbrirPanelRacha()
    {
        panelRacha.SetActive(true);
    }
    private void VerificarRacha()
    {
        DateTime hoy = DateTime.UtcNow.Date;

        // Leer de PlayerPrefs
        string fechaGuardadaStr = PlayerPrefs.GetString("ultimaFecha", "");
        rachaActualLocal = PlayerPrefs.GetInt("rachaActual", 0);

        if (DateTime.TryParse(fechaGuardadaStr, out ultimaFechaLocal))
        {
            int diferencia = (hoy - ultimaFechaLocal).Days;

            if (diferencia == 1)
            {
                rachaActualLocal++;
                GuardarRachaLocal(hoy);
            }
            else if (diferencia > 1)
            {
                rachaActualLocal = 1;
                GuardarRachaLocal(hoy);
            }
            // diferencia == 0 → ya se sumó racha hoy, no hacemos nada
        }
        else
        {
            // Primera vez o error al leer fecha
            rachaActualLocal = 1;
            GuardarRachaLocal(hoy);
        }

        ActualizarUIRacha();

        // Solo si hay conexión a Internet
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            StartCoroutine(SincronizarConFirebase(hoy));
        }

        VerificarYSumarXP(hoy);
    }

    private void GuardarRachaLocal(DateTime fecha)
    {
        PlayerPrefs.SetString("ultimaFecha", fecha.ToString("yyyy-MM-dd"));
        PlayerPrefs.SetInt("rachaActual", rachaActualLocal);
    }

    private void VerificarYSumarXP(DateTime hoy)
    {
        string fechaUltimoXP = PlayerPrefs.GetString("fechaUltimoXP", "");

        if (fechaUltimoXP == hoy.ToString("yyyy-MM-dd"))
        {
            Debug.Log("XP ya otorgado hoy");
            return;
        }

        int xp = CalcularXPSegunRacha(rachaActualLocal);
        SumarXPTemporario(xp);
        PlayerPrefs.SetString("fechaUltimoXP", hoy.ToString("yyyy-MM-dd"));

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            StartCoroutine(SumarXPFirebase(xp));
        }
    }

    private int CalcularXPSegunRacha(int racha)
    {
        if (racha >= 200) return 5;
        if (racha >= 100) return 4;
        if (racha >= 30) return 3;
        if (racha >= 7) return 2;
        return 1;
    }

    private void SumarXPTemporario(int xp)
    {
        int xpTemp = PlayerPrefs.GetInt("TempXP", 0);
        xpTemp += xp;
        PlayerPrefs.SetInt("TempXP", xpTemp);
        Debug.Log($"XP temporal actualizado: {xpTemp}");
    }

    private IEnumerator SumarXPFirebase(int xp)
    {
        if (string.IsNullOrEmpty(userId)) yield break;

        DocumentReference docRef = db.Collection("users").Document(userId);
        Task<DocumentSnapshot> task = docRef.GetSnapshotAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result.Exists && task.Result.TryGetValue<int>("xp", out int xpActual))
        {
            int nuevoXP = xpActual + xp;
            docRef.UpdateAsync("xp", nuevoXP);
        }
    }

    private IEnumerator SincronizarConFirebase(DateTime hoy)
    {
        if (string.IsNullOrEmpty(userId)) yield break;

        DocumentReference docRef = db.Collection("users").Document(userId);
        Task<DocumentSnapshot> task = docRef.GetSnapshotAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result.Exists)
        {
            DocumentSnapshot snapshot = task.Result;

            DateTime? fechaFirestore = null;
            int rachaFirestore = 1;

            if (snapshot.TryGetValue("ultimaFecha", out Timestamp ts))
                fechaFirestore = ts.ToDateTime().Date;

            if (snapshot.TryGetValue("rachaActual", out int racha))
                rachaFirestore = racha;

            int diasDiferencia = (hoy - fechaFirestore.GetValueOrDefault(hoy)).Days;

            if (diasDiferencia == 1)
            {
                rachaFirestore++;
            }
            else if (diasDiferencia > 1)
            {
                rachaFirestore = 1;
            }

            docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "rachaActual", rachaFirestore },
                { "ultimaFecha", Timestamp.FromDateTime(DateTime.UtcNow.Date.ToUniversalTime()) }
            });

            // También actualizamos local con datos de Firestore para mantener sincronía
            rachaActualLocal = rachaFirestore;
            GuardarRachaLocal(hoy);
            ActualizarUIRacha();
        }
    }

    private void ActualizarUIRacha()
    {
        if (RachaTexto != null)
            RachaTexto.text = rachaActualLocal.ToString();
    }
}
