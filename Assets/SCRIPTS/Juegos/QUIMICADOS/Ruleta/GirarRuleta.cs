using Firebase.Auth;
using Firebase.Database;
using Firebase.Firestore;
using System.Collections;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GirarRuleta : MonoBehaviour
{
    public Button botonGirar; // Asignar desde el Inspector
    public RectTransform ruleta;
    public TMP_Text textoCategoria;
    public RectTransform flecha; // ← Asigna esto en el Inspector
    public GameObject PopUpJugar;

    private FirebaseFirestore db;
    private string uidActual;
    private string partidaId;

    private string PartidaIdQuimicados;
    private bool girando = false;
    private string[] Categorias = new string[]
    {
        "Metales Alcalinos", "Metales Alcalinotérreos", "Metales de Transición",
        "Metales Postransicionales", "Metaloides", "No Metales Reactivos", "Gases Nobles",
        "Lantánidos", "Actínoides", "Propiedades Desconocidas"
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
        if (!girando && botonGirar.interactable)
            StartCoroutine(GirarAnimacion());
    }

    IEnumerator GirarAnimacion()
    {
        girando = true;

        int vueltas = UnityEngine.Random.Range(3, 6);
        int sectorFinal = UnityEngine.Random.Range(0, Categorias.Length); // índice exacto
        float anguloSector = 360f / Categorias.Length;
        float anguloFinal = vueltas * 360f + (sectorFinal * anguloSector);

        float velocidadGradosPorSegundo = 180f; // 2 vueltas por segundo
        float anguloRecorrido = 0f;

        float anguloInicial = ruleta.eulerAngles.z;
        float anguloDestino = anguloInicial + anguloFinal;

        float ultimoAnguloTrigger = anguloInicial;

        while (anguloRecorrido < anguloFinal)
        {
            float delta = velocidadGradosPorSegundo * Time.deltaTime;
            anguloRecorrido += delta;
            float anguloActual = anguloInicial + anguloRecorrido;

            ruleta.eulerAngles = new Vector3(0, 0, anguloActual);

            // Detecta paso por secciones de 36°
            float anguloActualZ = 360f - (anguloActual % 360f); // sentido horario
            if (Mathf.FloorToInt(anguloActualZ / anguloSector) != Mathf.FloorToInt(ultimoAnguloTrigger / anguloSector))
            {
                StartCoroutine(AnimarFlecha());
                ultimoAnguloTrigger = anguloActualZ;
            }

            yield return null;
        }

        // Asegura ángulo exacto final
        ruleta.eulerAngles = new Vector3(0, 0, anguloDestino);

        // Selecciona categoría final
        float anguloFinalZ = ruleta.eulerAngles.z % 360f;
        int indice = Mathf.FloorToInt((360f - anguloFinalZ + (anguloSector / 2)) % 360f / anguloSector);
        string categoriaSeleccionada = Categorias[indice];
        textoCategoria.text = categoriaSeleccionada;

        PlayerPrefs.SetString("CategoriaRuleta", categoriaSeleccionada);
        girando = false;

        PopUpJugar.SetActive(true);
    }

    IEnumerator AnimarFlecha()
    {
        Vector3 rotacionOriginal = new Vector3(0,0,0);
        Vector3 rotacionLeve = rotacionOriginal + new Vector3(0, 0, -10f); // se inclina un poco

        flecha.localEulerAngles = rotacionLeve;
        yield return new WaitForSeconds(0.05f); // pequeña pausa

        flecha.localEulerAngles = rotacionOriginal; // vuelve exactamente a la rotación original
    }



}
