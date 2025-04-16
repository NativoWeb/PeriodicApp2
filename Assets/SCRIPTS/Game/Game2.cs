using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;

[System.Serializable]
public class PreguntaJuego
{
    public int id;
    public string elemento;
    public string imagen;
    public string pregunta;
    public List<string> opciones;
    public string respuesta_correcta;
}

[System.Serializable]
public class PreguntaData
{
    public List<PreguntaJuego> niveles;
}

public class Game2 : MonoBehaviour
{
    public static Game2 Instancia;

    public TextMeshProUGUI txtPregunta;
    public Image imgElemento;
    public Button[] botonesRespuestas;
    public Text txtRacha;
    public Text txtTemporizador;

    public GameObject panelPerdiste;
    public TextMeshProUGUI txtResumen;

    private List<PreguntaJuego> preguntas;
    private int indiceActual = 0;
    private int racha = 0;
    private float tiempoRestante = 10f;
    private bool tiempoActivo = true;
    private int xpTotalGanado = 0;
    private bool juegoTerminado = false;

    FirebaseAuth auth;
    FirebaseFirestore db;

    void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        CargarPreguntas();
    }

    void Update()
    {
        if (juegoTerminado || !tiempoActivo) return;

        tiempoRestante -= Time.deltaTime;
        txtTemporizador.text = Mathf.Ceil(tiempoRestante).ToString();

        if (tiempoRestante <= 0f)
        {
            tiempoActivo = false;
            PerderRacha();
            // Ya no llamamos a SiguientePregunta() aquí
        }
    }
    

    void CargarPreguntas()
    {
        TextAsset json = Resources.Load<TextAsset>("juego_tabla_periodica_preguntas");
        if (json != null)
        {
            PreguntaData data = JsonUtility.FromJson<PreguntaData>(json.text);
            preguntas = data.niveles.OrderBy(x => Random.value).ToList();
            MostrarPregunta();
        }
        else
        {
            Debug.LogError("No se pudo cargar el archivo JSON de preguntas.");
        }
    }

    public void MostrarPregunta()
    {
        if (preguntas == null || preguntas.Count == 0) return;

        PreguntaJuego preguntaActual = preguntas[indiceActual];
        txtPregunta.text = preguntaActual.pregunta;

        List<string> opciones = preguntaActual.opciones.OrderBy(x => Random.value).ToList();
        for (int i = 0; i < botonesRespuestas.Length; i++)
        {
            if (i < opciones.Count)
            {
                string opcion = opciones[i];
                botonesRespuestas[i].GetComponentInChildren<TextMeshProUGUI>().text = opcion;
                botonesRespuestas[i].onClick.RemoveAllListeners();
                botonesRespuestas[i].onClick.AddListener(() => ComprobarRespuesta(opcion));
                botonesRespuestas[i].gameObject.SetActive(true);
            }
            else
            {
                botonesRespuestas[i].gameObject.SetActive(false);
            }
        }

        tiempoRestante = 10f;
        tiempoActivo = true;
    }

    public void ComprobarRespuesta(string respuestaUsuario)
    {
        if (!tiempoActivo) return;

        tiempoActivo = false;

        if (VerificarRespuesta(respuestaUsuario))
        {
            AumentarRacha();
        }
        else
        {
            PerderRacha();
        }

        SiguientePregunta();
    }

    public void AumentarRacha()
    {
        racha++;
        txtRacha.text = racha.ToString();

        int xp = CalcularXPporRacha(racha);
        xpTotalGanado += xp;

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            SumarXPTemporario(xp);
        }
        else
        {
            _ = SumarXPFirebase(xp);
        }
    }

    public void PerderRacha()
    {
        juegoTerminado = true;
        tiempoActivo = false;

        MostrarPanelPerdiste();
        racha = 0;
        txtTemporizador.text = "0";
        txtRacha.text = racha.ToString();
    }

    public bool VerificarRespuesta(string respuestaUsuario)
    {
        return preguntas[indiceActual].respuesta_correcta == respuestaUsuario;
    }

    public void SiguientePregunta()
    {
        if (juegoTerminado) return;

        if (indiceActual < preguntas.Count - 1)
        {
            indiceActual++;
            MostrarPregunta();
        }
        else
        {
            indiceActual = 0;
            MostrarPregunta();
        }
    }

    public void FinalizarJuego()
    {
        Debug.Log("Juego finalizado!");
    }

    int CalcularXPporRacha(int racha)
    {
        if (racha >= 50)
            return 5;
        else if (racha >= 10)
            return 3;
        else if (racha >= 5)
            return 2;
        else
            return 1;
    }

    void MostrarPanelPerdiste()
    {
        panelPerdiste.SetActive(true);
        txtResumen.text = $"¡Perdiste la racha!\n\n Racha alcanzada: {racha}\n XP ganado: {xpTotalGanado}";
        xpTotalGanado = 0; // Reiniciar XP tras mostrar
    }

    void SumarXPTemporario(int xp)
    {
        int xpTemporal = PlayerPrefs.GetInt("TempXP", 0);
        xpTemporal += xp;
        PlayerPrefs.SetInt("TempXP", xpTemporal);
        PlayerPrefs.Save();
        Debug.Log($"🔄 No hay conexión. XP {xp} guardado en TempXP. Total: {xpTemporal}");
    }

    async Task SumarXPFirebase(int xp)
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(user.UserId);

        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            int xpActual = 0;

            if (snapshot.Exists && snapshot.TryGetValue<int>("xp", out int valorXP))
            {
                xpActual = valorXP;
            }

            int xpNuevo = xpActual + xp;

            await userRef.UpdateAsync("xp", xpNuevo);
            Debug.Log($"✅ XP actualizado en Firebase: {xpNuevo}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al actualizar XP en Firebase: {e.Message}");
        }
    }
}
