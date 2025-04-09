using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using Firebase.Extensions;
using TMPro;
using System.Threading.Tasks;


public class RachaManager : MonoBehaviour
{
    int rachaActualLocal;
    DateTime ultimaFechaLocal;

    public TMP_Text Racha;
    private FirebaseFirestore db;
    private string userId;
    private FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        var user = auth.CurrentUser;
        if (user != null)
        {
            userId = user.UserId;
        }
        VerificarRachaLocal();
    }

    void VerificarRachaLocal()
    {
        DateTime hoy = DateTime.UtcNow.Date;

        string fechaStr = PlayerPrefs.GetString("ultimaFecha", "");
        rachaActualLocal = PlayerPrefs.GetInt("rachaActual", 0);

        if (!string.IsNullOrEmpty(fechaStr))
        {
            ultimaFechaLocal = DateTime.Parse(fechaStr);
            TimeSpan diferencia = hoy - ultimaFechaLocal;

            if (diferencia.Days == 1)
                rachaActualLocal++;
            else if (diferencia.Days > 1)
                rachaActualLocal = 1;
            // Si es el mismo día, no cambia
        }
        else
        {
            rachaActualLocal = 1;
        }

        // Guardar nueva fecha y racha local
        PlayerPrefs.SetString("ultimaFecha", hoy.ToString("yyyy-MM-dd"));
        PlayerPrefs.SetInt("rachaActual", rachaActualLocal);
        PlayerPrefs.Save();

        Debug.Log($"[Offline] Racha: {rachaActualLocal}");
        ActualizarUIRacha();
        // Verificar con Firebase si hay conexión
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            SincronizarConFirebase(hoy);
        }
    }

    async void SincronizarConFirebase(DateTime hoy)
    {
        if (userId == null) return;

        DocumentReference docRef = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snap = await docRef.GetSnapshotAsync();
            Debug.Log("📄 Snapshot obtenido de Firebase");

            if (snap.Exists)
            {
                Debug.Log("📌 Documento existe en Firebase, verificando campos...");

                if (snap.ContainsField("rachaActual") && snap.ContainsField("ultimaFecha"))
                {
                    DateTime ultimaFechaFirebase = snap.GetValue<Timestamp>("ultimaFecha").ToDateTime().Date;
                    int rachaFirebase = snap.GetValue<int>("rachaActual");

                    int diasDiferencia = (hoy - ultimaFechaFirebase).Days;

                    // Evaluar la racha según la diferencia de días
                    if (diasDiferencia == 1)
                    {
                        Debug.Log("❄️ Racha congelada por inactividad de 1 día.");
                        // Racha congelada, no aumenta
                    }
                    else if (diasDiferencia >= 2)
                    {
                        Debug.Log("💥 Racha perdida por inactividad de más de 1 día.");
                        rachaActualLocal = 0;
                    }

                    // Determinar XP según la duración de la racha
                    int xpDiario = 1;
                    if (rachaActualLocal >= 30)
                        xpDiario = 5;
                    else if (rachaActualLocal >= 15)
                        xpDiario = 4;
                    else if (rachaActualLocal >= 7)
                        xpDiario = 3;
                    else if (rachaActualLocal >= 1)
                        xpDiario = 2;

                    // Guardar local
                    PlayerPrefs.SetString("ultimaFecha", hoy.ToString("yyyy-MM-dd"));
                    PlayerPrefs.SetInt("rachaActual", rachaActualLocal);
                    PlayerPrefs.SetInt("xpGanadaHoy", xpDiario);
                    PlayerPrefs.Save();

                    // XP Local y Firebase
                    SumarXPTemporario(xpDiario);
                    await Task.Delay(500);
                    SumarXPFirebase(xpDiario);
                    PlayerPrefs.SetInt("TempXP", 0);
                    PlayerPrefs.Save();

                    // Actualizar en Firebase
                    await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "rachaActual", rachaActualLocal },
                    { "ultimaFecha", hoy }
                });

                    Debug.Log($"✅ Racha y XP actualizados en Firebase. XP Ganado hoy: {xpDiario}");
                    ActualizarUIRacha();
                }
                else
                {
                    Debug.Log("⚠️ Campos no existen, creando datos en Firebase...");

                    await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "rachaActual", rachaActualLocal },
                    { "ultimaFecha", hoy }
                });

                    SumarXPTemporario(1); // primera vez: 1 XP
                    await Task.Delay(500);
                     SumarXPFirebase(1);
                    PlayerPrefs.SetInt("TempXP", 0);
                    PlayerPrefs.Save();

                    Debug.Log("✅ Campos creados correctamente en Firebase.");
                    ActualizarUIRacha();
                }
            }
            else
            {
                Debug.Log("📁 Documento no existe, creándolo...");

                await docRef.SetAsync(new Dictionary<string, object>
            {
                { "rachaActual", rachaActualLocal },
                { "ultimaFecha", hoy }
            });

                SumarXPTemporario(1); // primera vez: 1 XP
                await Task.Delay(500);
                SumarXPFirebase(1);
                PlayerPrefs.SetInt("TempXP", 0);
                PlayerPrefs.Save();

                Debug.Log("✅ Documento creado correctamente en Firebase.");
                ActualizarUIRacha();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Error durante sincronización: " + e.Message);
        }
    }

    void ActualizarUIRacha()
    {
        if (Racha != null)
            Racha.text = rachaActualLocal.ToString();
    }

    void SumarXPTemporario(int xp)
    {
        int xpTemp = PlayerPrefs.GetInt("TempXP", 0);
        xpTemp += xp;
        PlayerPrefs.SetInt("TempXP", xpTemp);
        PlayerPrefs.Save();
        Debug.Log($"🔄 XP {xp} sumado temporalmente. Total TempXP: {xpTemp}");
    }

    async void SumarXPFirebase(int xp)
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No hay usuario.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(user.UserId);
        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            int xpActual = snapshot.Exists && snapshot.TryGetValue("xp", out int valor) ? valor : 0;
            int nuevoXP = xpActual + xp;
            await userRef.UpdateAsync("xp", nuevoXP);
            Debug.Log($"✅ XP actualizado: {nuevoXP}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al subir XP: {e.Message}");
        }
    }
}
