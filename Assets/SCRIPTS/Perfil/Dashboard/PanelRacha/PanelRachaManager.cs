using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;

public class PanelRachaManager : MonoBehaviour
{
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    public Image avatarImage;
    public TMP_Text Txtdias_racha;
    public TMP_Text TxtRango;
    public TMP_Text TxtXp;
    public TMP_Text Txt_Motivacion;
    private string userId;
    private string rango;

    private readonly string[] mensajesMotivacionales = new string[]
 {
        "¡No dejes que la racha se rompa!",
        "¡Estás construyendo algo grande!",
        "¡Sigue así, tu constancia vale oro!",
        "¡Un día más, un paso más cerca del éxito!",
        "¡El conocimiento es poder, no te detengas!",
        "¡Tu esfuerzo de hoy, es tu victoria de mañana!",
        "¡Que tu racha sea tan fuerte como tus sueños!",
        "¡Cada día cuenta, sigue experimentando!",
        "¡Vamos, científico en ascenso!",
        "¡No pares ahora, tu futuro químico te espera!"
 };
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();
        ActualizarDatosUsuario();
    }
    void ActualizarDatosUsuario()
    {
        string mensaje = mensajesMotivacionales[Random.Range(0, mensajesMotivacionales.Length)];
        Txt_Motivacion.text = mensaje;

        bool hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (!hayInternet)
        {
            // 📴 Sin conexión: mostrar desde PlayerPrefs
            int xpLocal = PlayerPrefs.GetInt("TempXP", 0);
            string nombreLocal = PlayerPrefs.GetString("DisplayName", "Sin nombre");

            // Obtener rango por XP
            string rangoLocal = ObtenerRangoSegunXP(xpLocal);
            PlayerPrefs.SetString("Rango", rangoLocal); // actualiza el rango local

            // 🖼 Avatar offline
            string rutaAvatar = ObtenerAvatarPorRango(rangoLocal);
            Sprite avatar = Resources.Load<Sprite>(rutaAvatar);
            if (avatar != null)
            {
                avatarImage.sprite = avatar;
                avatarImage.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("⚠ Avatar no encontrado en ruta: " + rutaAvatar);
            }
            return;
        }

        // 🌐 Con internet: cargar desde Firestore
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(user.UserId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                var snapshot = task.Result;
                // 

                // días de racha
                int dias_racha = snapshot.ContainsField("rachaActual") ? snapshot.GetValue<int>("rachaActual") : 0;
                Txtdias_racha.text = dias_racha.ToString();
                // XP
                int xp = snapshot.ContainsField("xp") ? snapshot.GetValue<int>("xp") : 0;
                TxtXp.text = xp.ToString();
                PlayerPrefs.SetInt("TempXP", xp);
             
                //Actualiza rango en firebase
                ActualizarRangoSegunXP(xp);

                // Rango 
                string rango = snapshot.ContainsField("Rango") ? snapshot.GetValue<string>("Rango") : "" ;
                TxtRango.text = rango.ToString();
                // 🖼 Avatar online
                string rutaAvatar = ObtenerAvatarPorRango(rango);
                Sprite avatar = Resources.Load<Sprite>(rutaAvatar);
                if (avatar != null) avatarImage.sprite = avatar;
                else Debug.LogWarning("⚠ Avatar no encontrado en ruta: " + rutaAvatar);

                PlayerPrefs.Save();

                Debug.Log("✅ Datos cargados correctamente desde Firestore.");
            }
            else
            {
                Debug.LogWarning("⚠ No se encontró el documento del usuario en Firestore.");
            }
        });
    }
    public async void ActualizarRangoSegunXP(int xp)
    {
        string nuevoRango = ObtenerRangoSegunXP(xp);
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("Rango", nuevoRango);
        rango = nuevoRango;
    }

    private string ObtenerRangoSegunXP(int xp)
    {
        if (xp >= 25000) return "Alquimista Supremo";
        if (xp >= 10000) return "Leyenda química";
        if (xp >= 6000) return "Sabio de la tabla";
        if (xp >= 3500) return "Maestro de Laboratorio";
        if (xp >= 2300) return "Experto Molecular";
        if (xp >= 1200) return "Cientifico en Formacion";
        if (xp >= 600) return "Promesa quimica";
        if (xp >= 200) return "Aprendiz Atomico";
        return "Novato de laboratorio";
    }

    private string ObtenerAvatarPorRango(string rango)
    {
        switch (rango)
        {
            case "Novato de laboratorio": return "Avatares/Rango1";
            case "Aprendiz Atomico": return "Avatares/Rango2";
            case "Promesa quimica": return "Avatares/Rango3";
            case "Cientifico en Formacion": return "Avatares/Rango4";
            case "Experto Molecular": return "Avatares/Rango5";
            case "Maestro de Laboratorio": return "Avatares/Rango6";
            case "Sabio de la tabla": return "Avatares/Rango7";
            case "Leyenda química": return "Avatares/Rango8";
            case "Alquimista Supremo": return "Avatares/Rango8";
            default: return "Avatares/Rango1";
        }
    }


}
