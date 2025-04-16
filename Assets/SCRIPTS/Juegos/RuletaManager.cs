//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Threading.Tasks;
//using Firebase.Database;
//using Firebase.Auth;
//using Firebase.Extensions;

//public class RuletaManager : MonoBehaviour
//{
//    public GameObject combate;
//    public GameObject PanelRuleta;
//    public RectTransform ruleta;
//    public TextMeshProUGUI textoCategoria;
//    public string[] Categorias = new string[]
//    {
//        "Metales Alcalinos", "Metales Alcalinotérreos", "Metales de Transición",
//        "Metales Postransicionales", "Metaloides", "No Metales Reactivos", "Gases Nobles",
//        "Lantánidos", "Actínoides", "Propiedades Desconocidas"
//    };

//    private bool girando = false;

//    void Start()
//    {
//        PanelRuleta.SetActive(true);

//        // Si soy el creador de la partida, giro la ruleta
//        string miUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
//        string partidaId = PlayerPrefs.GetString("PartidaId");

//        FirebaseDatabase.DefaultInstance.GetReference("partidas").Child(partidaId)
//            .Child("jugadorA").GetValueAsync().ContinueWith(task =>
//            {
//                if (task.IsCompleted && task.Result.Exists)
//                {
//                    string jugadorA = task.Result.Value.ToString();

//                    if (miUID == jugadorA)
//                    {
//                        // Yo soy el jugador A, giro la ruleta
//                        GirarRuleta();
//                    }
//                    else
//                    {
//                        EmpezarEscuchaCategoriaDesdeFirebase(); // Yo soy jugador B, espero la categoría desde Firebase
//                    }
//                }
//            });
//    }

//    void EmpezarEscuchaCategoriaDesdeFirebase()
//    {
//        string partidaId = PlayerPrefs.GetString("PartidaId");
//        var categoriaRef = FirebaseDatabase.DefaultInstance
//            .GetReference("partidas")
//            .Child(partidaId)
//            .Child("categoriaSeleccionada");

//        textoCategoria.text = "Esperando selección de categoría...";

//        // 1. Intentar obtener el valor una sola vez primero
//        categoriaRef.GetValueAsync().ContinueWith(task =>
//        {
//            if (task.IsCompleted && task.Result.Exists)
//            {
//                string categoria = task.Result.Value.ToString();
//                Debug.Log("✅ Categoría recibida directamente: " + categoria);
//                textoCategoria.text = "Categoría: " + categoria;
//                PlayerPrefs.SetString("CategoriaRuleta", categoria);
//                GameManager.instancia.CargarJsons();
//                QuitarPanel();
//            }
//            else
//            {
//                // 2. Si aún no existe, escuchar cambios
//                categoriaRef.ValueChanged += (object sender, ValueChangedEventArgs args) =>
//                {
//                    if (args.Snapshot.Exists)
//                    {
//                        string categoria = args.Snapshot.Value.ToString();
//                        Debug.Log("✅ Categoría recibida desde evento: " + categoria);
//                        textoCategoria.text = "Categoría: " + categoria;
//                        PlayerPrefs.SetString("CategoriaRuleta", categoria);
//                        GameManager.instancia.CargarJsons();
//                        QuitarPanel();
//                    }
//                    else
//                    {
//                        Debug.Log("⚠️ Aún no se ha asignado categoría.");
//                    }
//                };
//            }
//        });
//    }

//    public void GirarRuleta()
//    {
//        if (!girando)
//            StartCoroutine(GirarAnimacion());
//    }

//    IEnumerator GirarAnimacion()
//    {
//        girando = true;

//        float tiempo = 4f;
//        float velocidad = 500f;
//        float deceleracion = 200f;

//        float anguloTotal = Random.Range(3, 6) * 360 + Random.Range(0, 360); // vueltas + aleatorio
//        float anguloInicial = ruleta.eulerAngles.z;
//        float anguloFinal = anguloInicial + anguloTotal;

//        float tiempoActual = 0f;

//        while (tiempoActual < tiempo)
//        {
//            float t = tiempoActual / tiempo;
//            float rotacion = Mathf.Lerp(anguloInicial, anguloFinal, t);
//            ruleta.eulerAngles = new Vector3(0, 0, rotacion);
//            tiempoActual += Time.deltaTime;
//            yield return null;
//        }

//        ruleta.eulerAngles = new Vector3(0, 0, anguloFinal);

//        // Determinar categoría
//        float anguloFinalZ = ruleta.eulerAngles.z % 360f;
//        float anguloSector = 360f / Categorias.Length;
//        int indice = Mathf.FloorToInt((360f - anguloFinalZ + (anguloSector / 2)) % 360f / anguloSector);
//        string partidaId = PlayerPrefs.GetString("PartidaId");
//        string categoriaSeleccionada = Categorias[indice];
//        textoCategoria.text = $"Categoría: {categoriaSeleccionada}";

//        // GUARDAR EN FIREBASE
//        FirebaseDatabase.DefaultInstance
//        .GetReference("partidas")
//        .Child(partidaId)
//        .Child("categoriaSeleccionada")
//        .SetValueAsync(categoriaSeleccionada);
//        PlayerPrefs.SetString("CategoriaRuleta", categoriaSeleccionada);
//        girando = false;
//        GameManager.instancia.CargarJsons();
//        QuitarPanel();
//    }

//    private async void QuitarPanel()
//    {
//        await Task.Delay(3000);
//        PanelRuleta.SetActive(false);
//        combate.SetActive(true);
//        await Task.Delay(3000);
//        combate.SetActive(false);
//    }
//}
