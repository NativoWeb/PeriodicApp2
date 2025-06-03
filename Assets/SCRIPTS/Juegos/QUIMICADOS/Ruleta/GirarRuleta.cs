using Firebase.Auth;
using Firebase.Database;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GirarRuleta : MonoBehaviour
{
    public RectTransform ruletaTransform; // La imagen de la ruleta
    public Button botonGirar; // Botón invisible o evento

    private bool girando = false;
    private int totalCategorias = 10;
    private float anguloPorCategoria;

    public AudioSource audioSelector;     // Este es el componente que reproduce
    public AudioClip ticClip;             // Este es el sonido "tic" (tu .mp3)

    public TMP_Text textoCategoria;
    public RectTransform flecha; // ← Asigna esto en el Inspector
    public GameObject PopUpJugar;
    public Image PanelColor;
    public Image imgCat;

    private FirebaseFirestore db;
    private string uidActual;
    private string partidaId;

    private string PartidaIdQuimicados;
    string[] categorias = new string[]
    {
        "No Metales Reactivos",
        "Actínoides",             // 0° (arriba)
        "Metales Alcalinotérreos",     // 36°
        "Metales de Transición",                   // 72°
        "Gases Nobles",    // 108°
        "Lantánidos",                   // 144°
        "Metales Postransicionales",                 // 180°
        "Metaloides",        // 216°
        "Propiedades Desconocidas",      // 252°
        "Metales Alcalinos",                   // 288°
    };

    void Start()
    {
        partidaId = PlayerPrefs.GetString("partidaIdQuimicados");
        db = FirebaseFirestore.DefaultInstance;
        uidActual = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        botonGirar.onClick.AddListener(Girar);
        EscucharTurno(); // ← esta es nueva
    }
    void EscucharTurno()
    {
        db.Collection("partidasQuimicados").Document(partidaId).Listen(snapshot =>
        {
            if (snapshot.Exists && snapshot.TryGetValue("turnoActual", out string turnoActual))
            {
                if (turnoActual == uidActual)
                {
                    botonGirar.interactable = true;
                }
                else
                {
                    botonGirar.interactable = false;
                }
            }
        });
    }

    public void Girar()
    {
        if (!girando)
            StartCoroutine(GirarRuletaCoroutine());
    }

    private IEnumerator GirarRuletaCoroutine()
    {
        girando = true;

        int totalCategorias = categorias.Length;
        float anguloPorCategoria = 360f / totalCategorias;

        // Elegir una categoría aleatoria
        int indiceCategoria = Random.Range(0, totalCategorias);
        float anguloFinal = indiceCategoria * anguloPorCategoria;

        // Rotación total con varias vueltas antes de frenar
        float rotacionTotal = (360f * Random.Range(5, 8)) + anguloFinal;

        float duracion = 4f;
        float tiempo = 0f;

        float anguloInicial = ruletaTransform.eulerAngles.z;
        float anguloObjetivo = anguloInicial + rotacionTotal;

        float prevAngle = anguloInicial;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;
            float angle = Mathf.Lerp(anguloInicial, anguloObjetivo, EaseOutCubic(t));
            ruletaTransform.rotation = Quaternion.Euler(0, 0, angle);

            // Puedes agregar aquí animación de "click" si pasa una categoría
            float delta = Mathf.Abs(angle - prevAngle);
            if (delta >= anguloPorCategoria)
            {
                StartCoroutine(AnimarFlecha());

                if (audioSelector != null && ticClip != null)
                    audioSelector.PlayOneShot(ticClip);

                prevAngle = angle;
            }

            yield return null;
        }

        // Ajustar al ángulo final exacto
        ruletaTransform.rotation = Quaternion.Euler(0, 0, anguloObjetivo);

        // Calcular el índice real de la categoría
        float anguloZ = ruletaTransform.eulerAngles.z % 360f;
        int indiceFinal = Mathf.RoundToInt(anguloZ / anguloPorCategoria) % totalCategorias;

        string categoriaElegida = categorias[indiceFinal];
        PlayerPrefs.SetString("CategoriaRuleta", categoriaElegida);
        textoCategoria.text = categoriaElegida;

        Dictionary<string, Color32> coloresCategoria = new Dictionary<string, Color32>
        {
            { "Gases Nobles", new Color32(0x00, 0xA2, 0x93, 255) },              // #00A293
            { "Actínoides", new Color32(0x33, 0x37, 0x8E, 255) },                // #33378E
            { "Metales Alcalinos", new Color32(0x41, 0xB9, 0xDE, 255) },         // #41B9DE
            { "Metales Postransicionales", new Color32(0x72, 0x65, 0xAA, 255) }, // #7265AA
            { "Metaloides", new Color32(0xB4, 0xBC, 0xBE, 255) },                // #B4BCBE
            { "Lantánidos", new Color32(0xC0, 0x20, 0x3C, 255) },                // #C0203C
            { "Metales de Transición", new Color32(0xED, 0x6D, 0x9D, 255) },     // #ED6D9D
            { "Metales Alcalinotérreos", new Color32(0xF0, 0x81, 0x2F, 255) },   // #F0812F
            { "No Metales Reactivos", new Color32(0xFF, 0xD4, 0x4B, 255) },      // #FFD44B
            { "Propiedades Desconocidas", new Color32(0x7A, 0xB9, 0x50, 255) }   // #7AB950
        };

        if (coloresCategoria.TryGetValue(categoriaElegida, out Color32 colorFinal))
        {
            PanelColor.color = colorFinal;
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró color para la categoría: " + categoriaElegida);
        }
        if (categoriaElegida == "Metales de Transición")
        {
            categoriaElegida = "MetalesTransicion";
        }

        string nombreArchivo = FormatearNombreArchivo(categoriaElegida);

        Sprite sprite = Resources.Load<Sprite>($"images/CategoriasQuimicados/{nombreArchivo}");
        imgCat.sprite = sprite;

        PopUpJugar.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("Cuestionario");

        girando = false;
    }

    string FormatearNombreArchivo(string original)
    {
        string sinTildes = original
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ñ", "n");

        string sinEspacios = sinTildes.Replace(" ", ""); // Quitar espacios internos

        return sinEspacios.Trim(); // Por seguridad
    }
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3);
    }
    IEnumerator AnimarFlecha()
    {
        Vector3 rotacionOriginal = new Vector3(0,0,0);
        Vector3 rotacionLeve = rotacionOriginal + new Vector3(0, 0, 10f); // se inclina un poco

        flecha.localEulerAngles = rotacionLeve;
        yield return new WaitForSeconds(0.05f); // pequeña pausa

        flecha.localEulerAngles = rotacionOriginal; // vuelve exactamente a la rotación original
    }
}
