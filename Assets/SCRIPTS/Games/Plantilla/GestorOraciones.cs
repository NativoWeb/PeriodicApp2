using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class GestorOraciones : MonoBehaviour
{
    [System.Serializable]
    public class Pregunta
    {
        public string oracion;
        public string respuestaCorrecta;
        public List<string> opciones;
    }

    public TextMeshProUGUI txtOracion;
    public Transform contenedorOpciones;
    public GameObject botonPrefab;
    public Text txtTiempo;
    public Text txtRacha;
    public GameObject panelFinal;
    public TextMeshProUGUI txtResultado;
    public BarraProgreso barraProgreso;

    private Dictionary<int, List<OracionConPalabras>> preguntasPorNivel = new Dictionary<int, List<OracionConPalabras>>();
    private List<OracionConPalabras> preguntas = new List<OracionConPalabras>();

    private int indicePreguntaActual = 0;
    private int racha = 0;
    private int respuestasCorrectas = 0;
    private float tiempoPorPregunta = 10f;
    private float tiempoRestante;
    private bool preguntaEnCurso = true;
    private int nivelActual;
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    private int nivelSeleccionado;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        nivelSeleccionado = ControladorNiveles.nivelSeleccionado;
        // Cargar preguntas por nivel
        CargarPreguntas();

        // Obtener nivel del usuario desde Firebase
        CargarPreguntasNivel(2);
    }


    void CargarPreguntas()
    {
        preguntasPorNivel[2] = new List<OracionConPalabras>
        {
            new OracionConPalabras("El agua está compuesta por _____ y oxígeno.", new string[] { "hidrógeno", "carbono", "helio", "nitrógeno" }, 0),
            new OracionConPalabras("La tabla periódica organiza los _____.", new string[] { "elementos", "moléculas", "átomos", "compuestos" }, 0),
            new OracionConPalabras("El símbolo químico del oxígeno es _____.", new string[] { "O", "Ox", "Og", "O2" }, 0),
            new OracionConPalabras("El agua hierve a _____ grados Celsius.", new string[] { "100", "0", "50", "200" }, 0),
            new OracionConPalabras("El pH mide el nivel de _____.", new string[] { "acidez", "temperatura", "densidad", "viscosidad" }, 0),
            new OracionConPalabras("El gas que respiramos principalmente es _____.", new string[] { "nitrógeno", "oxígeno", "dióxido de carbono", "helio" }, 0),
            new OracionConPalabras("El carbono es un elemento _____.", new string[] { "no metálico", "metálico", "radiactivo", "gaseoso" }, 0),
            new OracionConPalabras("El oro tiene el símbolo _____.", new string[] { "Au", "Ag", "O", "Go" }, 0),
            new OracionConPalabras("El cloro se usa para _____.", new string[] { "desinfectar", "oxidar", "fundir metales", "enfriar" }, 0),
            new OracionConPalabras("Los líquidos toman la forma de su _____.", new string[] { "recipiente", "estado", "temperatura", "masa" }, 0)
        };


        preguntasPorNivel[5] = new List<OracionConPalabras>
                    {
                        new OracionConPalabras("Los estados de la materia son sólido, líquido y _____.", new string[] { "gas", "plasma", "energía", "fluido" }, 0),
                        new OracionConPalabras("La materia está compuesta por _____.", new string[] { "átomos", "moléculas", "iones", "electrones" }, 0),
                        new OracionConPalabras("Cuando el agua se congela, pasa de estado líquido a _____.", new string[] { "sólido", "gas", "plasma", "vapor" }, 0),
                        new OracionConPalabras("El cambio de estado de sólido a gas se llama _____.", new string[] { "sublimación", "fusión", "evaporación", "condensación" }, 0),
                        new OracionConPalabras("La densidad se calcula dividiendo la masa entre _____.", new string[] { "volumen", "peso", "presión", "temperatura" }, 0),
                        new OracionConPalabras("El punto de fusión del hielo es _____.", new string[] { "0°C", "100°C", "-50°C", "200°C" }, 0),
                        new OracionConPalabras("El aire es una mezcla de _____.", new string[] { "gases", "líquidos", "sólidos", "iones" }, 0),
                        new OracionConPalabras("El gas más ligero es _____.", new string[] { "hidrógeno", "helio", "oxígeno", "neón" }, 0),
                        new OracionConPalabras("Los metales suelen ser buenos conductores de _____.", new string[] { "electricidad", "luz", "radiación", "sonido" }, 0),
                        new OracionConPalabras("Cuando el agua se evapora, pasa de estado líquido a _____.", new string[] { "gas", "sólido", "plasma", "cristal" }, 0)
                    };

        preguntasPorNivel[8] = new List<OracionConPalabras>
                        {
                            new OracionConPalabras("Los protones tienen carga _____.", new string[] { "positiva", "negativa", "neutra", "variable" }, 0),
                            new OracionConPalabras("Los electrones orbitan alrededor del _____.", new string[] { "núcleo", "protón", "neutrón", "átomo" }, 0),
                            new OracionConPalabras("El número atómico indica la cantidad de _____.", new string[] { "protones", "electrones", "neutrones", "átomos" }, 0),
                            new OracionConPalabras("Los metales alcalinos están en el grupo _____.", new string[] { "1", "2", "17", "18" }, 0),
                            new OracionConPalabras("Los gases nobles son elementos muy _____.", new string[] { "estables", "reactivos", "metálicos", "densos" }, 0),
                            new OracionConPalabras("El símbolo químico del sodio es _____.", new string[] { "Na", "S", "So", "N" }, 0),
                            new OracionConPalabras("El carbono tiene un número atómico de _____.", new string[] { "6", "12", "8", "14" }, 0),
                            new OracionConPalabras("El elemento más abundante en el universo es _____.", new string[] { "hidrógeno", "oxígeno", "helio", "carbono" }, 0),
                            new OracionConPalabras("Los elementos con propiedades similares se agrupan en _____.", new string[] { "familias", "filas", "grupos", "secciones" }, 0),
                            new OracionConPalabras("El flúor es un elemento muy _____.", new string[] { "reactivo", "pesado", "inestable", "metálico" }, 0)
                        };

        preguntasPorNivel[11] = new List<OracionConPalabras>
                        {
                            new OracionConPalabras("Los electrones de valencia son responsables de los _____.", new string[] { "enlaces químicos", "protones", "neutrones", "fotones" }, 0),
                            new OracionConPalabras("Un enlace covalente implica la _____.", new string[] { "compartición de electrones", "transferencia de electrones", "pérdida de protones", "adición de neutrones" }, 0),
                            new OracionConPalabras("El enlace iónico se forma entre un metal y un _____.", new string[] { "no metal", "metal", "gas noble", "metaloide" }, 0),
                            new OracionConPalabras("Las sustancias que aumentan la velocidad de una reacción se llaman _____.", new string[] { "catalizadores", "reactivos", "productos", "enzimas" }, 0),
                            new OracionConPalabras("Una reacción exotérmica _____.", new string[] { "libera energía", "absorbe energía", "requiere calor", "es endotérmica" }, 0),
                            new OracionConPalabras("Un ejemplo de reacción química es la _____.", new string[] { "combustión", "evaporación", "condensación", "fusión" }, 0),
                            new OracionConPalabras("El agua es un ejemplo de compuesto _____.", new string[] { "covalente", "iónico", "metálico", "radiactivo" }, 0),
                            new OracionConPalabras("Los productos en una reacción química están en el lado _____.", new string[] { "derecho", "izquierdo", "superior", "inferior" }, 0),
                            new OracionConPalabras("El dióxido de carbono es un _____.", new string[] { "compuesto", "elemento", "mezcla", "metal" }, 0),
                            new OracionConPalabras("La ley de conservación de la materia dice que la materia no se _____.", new string[] { "crea ni destruye", "transforma", "multiplica", "fusiona" }, 0)
                        };

        preguntasPorNivel[14] = new List<OracionConPalabras>
                        {
                            new OracionConPalabras("El mol es una unidad de _____.", new string[] { "cantidad de sustancia", "masa", "volumen", "densidad" }, 0),
                            new OracionConPalabras("La masa molar del agua (H₂O) es aproximadamente _____.", new string[] { "18 g/mol", "32 g/mol", "44 g/mol", "2 g/mol" }, 0),
                            new OracionConPalabras("El reactivo limitante en una reacción es el que _____.", new string[] { "se consume primero", "queda en exceso", "es el más pesado", "tiene más átomos" }, 0),
                            new OracionConPalabras("La ecuación química balanceada respeta la ley de _____.", new string[] { "conservación de la masa", "reacciones químicas", "dinámica molecular", "constantes químicas" }, 0),
                            new OracionConPalabras("La constante de Avogadro es _____.", new string[] { "6.022 × 10²³", "3.1416", "9.81", "1.602 × 10⁻¹⁹" }, 0),
                            new OracionConPalabras("El volumen molar de un gas ideal es aproximadamente _____.", new string[] { "22.4 L", "10 L", "1 L", "100 L" }, 0),
                            new OracionConPalabras("La ecuación PV=nRT es conocida como la ecuación del _____.", new string[] { "gas ideal", "estado líquido", "pH", "reactivo limitante" }, 0),
                            new OracionConPalabras("En una reacción química, la cantidad de reactivos y productos se mide en _____.", new string[] { "moles", "gramos", "mililitros", "litros" }, 0),
                            new OracionConPalabras("Un mol de cualquier gas ocupa el mismo volumen a _____.", new string[] { "condiciones normales", "altas temperaturas", "baja presión", "en un sólido" }, 0),
                            new OracionConPalabras("Si se duplican los reactivos, los productos también _____.", new string[] { "se duplican", "se dividen", "disminuyen", "se eliminan" }, 0)
                        };

        preguntasPorNivel[17] = new List<OracionConPalabras>
                        {
                            new OracionConPalabras("El pH de una solución ácida es menor a _____.", new string[] { "7", "14", "10", "1" }, 0),
                            new OracionConPalabras("El ácido clorhídrico (HCl) es un ácido _____.", new string[] { "fuerte", "débil", "neutro", "básico" }, 0),
                            new OracionConPalabras("El hidróxido de sodio (NaOH) es una _____.", new string[] { "base fuerte", "base débil", "sal", "ácido" }, 0),
                            new OracionConPalabras("El agua pura tiene un pH de _____.", new string[] { "7", "0", "14", "5" }, 0),
                            new OracionConPalabras("Los ácidos liberan iones _____.", new string[] { "H+", "OH-", "Na+", "Cl-" }, 0),
                            new OracionConPalabras("Las bases liberan iones _____.", new string[] { "OH-", "H+", "Na+", "Cl-" }, 0),
                            new OracionConPalabras("Cuando un ácido y una base reaccionan, forman _____.", new string[] { "sal y agua", "dióxido de carbono", "hidrógeno", "etanol" }, 0),
                            new OracionConPalabras("Un ejemplo de ácido débil es _____.", new string[] { "ácido acético", "ácido sulfúrico", "ácido nítrico", "ácido clorhídrico" }, 0),
                            new OracionConPalabras("El bicarbonato de sodio actúa como un _____.", new string[] { "amortiguador de pH", "ácido fuerte", "sal insoluble", "metal pesado" }, 0),
                            new OracionConPalabras("El jugo de limón tiene un pH _____.", new string[] { "ácido", "básico", "neutro", "radiactivo" }, 0)
                        };

        preguntasPorNivel[20] = new List<OracionConPalabras>
                        {
                            new OracionConPalabras("La energía absorbida o liberada en una reacción química se llama _____.", new string[] { "entalpía", "entropía", "energía cinética", "caloría" }, 0),
                            new OracionConPalabras("Una reacción endotérmica _____.", new string[] { "absorbe calor", "libera calor", "ocurre sin energía", "no cambia la temperatura" }, 0),
                            new OracionConPalabras("La unidad de energía en el sistema internacional es el _____.", new string[] { "joule", "caloría", "ergio", "vatio" }, 0),
                            new OracionConPalabras("La combustión es una reacción _____.", new string[] { "exotérmica", "endotérmica", "reversible", "nuclear" }, 0),
                            new OracionConPalabras("El calor específico del agua es _____.", new string[] { "4.18 J/g°C", "1.00 J/g°C", "2.22 J/g°C", "9.81 J/g°C" }, 0),
                            new OracionConPalabras("La energía en los enlaces químicos se denomina _____.", new string[] { "energía potencial química", "energía cinética", "energía térmica", "entropía" }, 0),
                            new OracionConPalabras("La entropía es una medida del _____.", new string[] { "desorden", "calor", "trabajo", "reacción" }, 0),
                            new OracionConPalabras("Las reacciones espontáneas ocurren cuando la energía libre de Gibbs es _____.", new string[] { "negativa", "positiva", "cero", "alta" }, 0),
                            new OracionConPalabras("La ecuación de Gibbs es ΔG = ΔH - TΔS, donde ΔH representa la _____.", new string[] { "entalpía", "entropía", "temperatura", "energía cinética" }, 0),
                            new OracionConPalabras("Un catalizador _____.", new string[] { "reduce la energía de activación", "consume reactivos", "aumenta la entalpía", "cambia los productos" }, 0)
                        };

        preguntasPorNivel[23] = new List<OracionConPalabras>
                        {
                            new OracionConPalabras("La reacción de oxidación implica la _____.", new string[] { "pérdida de electrones", "ganancia de electrones", "pérdida de protones", "ganancia de neutrones" }, 0),
                            new OracionConPalabras("La reducción implica la _____.", new string[] { "ganancia de electrones", "pérdida de electrones", "ganancia de protones", "pérdida de neutrones" }, 0),
                            new OracionConPalabras("En una celda galvánica, el ánodo es el electrodo donde ocurre la _____.", new string[] { "oxidación", "reducción", "reacción neutra", "neutralización" }, 0),
                            new OracionConPalabras("En una celda galvánica, el cátodo es el electrodo donde ocurre la _____.", new string[] { "reducción", "oxidación", "fusión", "sublimación" }, 0),
                            new OracionConPalabras("La electrólisis se usa para _____.", new string[] { "descomponer compuestos", "crear enlaces covalentes", "neutralizar soluciones", "medir la presión" }, 0),
                            new OracionConPalabras("El flujo de electrones en un circuito eléctrico se llama _____.", new string[] { "corriente eléctrica", "voltaje", "resistencia", "capacitancia" }, 0),
                            new OracionConPalabras("La batería es un ejemplo de una _____.", new string[] { "celda galvánica", "celda electrolítica", "reacción endotérmica", "fusión nuclear" }, 0),
                            new OracionConPalabras("El voltaje en una celda galvánica se calcula con la ecuación de _____.", new string[] { "Nernst", "Boyle", "Gibbs", "Arrhenius" }, 0),
                            new OracionConPalabras("En la ecuación electroquímica, el agente oxidante es la especie que _____.", new string[] { "se reduce", "se oxida", "pierde protones", "gana neutrones" }, 0),
                            new OracionConPalabras("El número de oxidación del oxígeno en la mayoría de los compuestos es _____.", new string[] { "-2", "+2", "0", "-1" }, 0)
                        };

        preguntasPorNivel[26] = new List<OracionConPalabras>
                        {
                            new OracionConPalabras("El átomo central en la química orgánica es el _____.", new string[] { "carbono", "oxígeno", "hidrógeno", "nitrógeno" }, 0),
                            new OracionConPalabras("Los hidrocarburos saturados se llaman _____.", new string[] { "alcanos", "alquenos", "alquinos", "aromáticos" }, 0),
                            new OracionConPalabras("Los hidrocarburos con dobles enlaces se llaman _____.", new string[] { "alquenos", "alcanos", "alquinos", "éteres" }, 0),
                            new OracionConPalabras("Los alcoholes contienen el grupo funcional _____.", new string[] { "-OH", "-COOH", "-NH2", "-SH" }, 0),
                            new OracionConPalabras("Los ésteres se forman a partir de un ácido y un _____.", new string[] { "alcohol", "hidrocarburo", "éter", "nitrilo" }, 0),
                            new OracionConPalabras("El benceno es un ejemplo de un compuesto _____.", new string[] { "aromático", "alifático", "cíclico", "heterocíclico" }, 0),
                            new OracionConPalabras("Los aminoácidos contienen los grupos funcionales _____.", new string[] { "amina y carboxilo", "carbonilo y éter", "éter y alquino", "hidroxilo y fenol" }, 0),
                            new OracionConPalabras("El ADN contiene bases nitrogenadas como _____.", new string[] { "adenina", "metano", "benceno", "etano" }, 0),
                            new OracionConPalabras("Los polímeros son macromoléculas formadas por _____.", new string[] { "monómeros", "átomos", "iones", "ácidos" }, 0),
                            new OracionConPalabras("El PET es un polímero usado en la fabricación de _____.", new string[] { "botellas plásticas", "vidrio", "acero", "cartón" }, 0)
                        };

    }

    void CargarPreguntasNivel(int nivel)
    {
        if (preguntasPorNivel.ContainsKey(nivel))
        {
            preguntas = preguntasPorNivel[nivel];
            indicePreguntaActual = 0;
            barraProgreso.InicializarBarra(preguntas.Count);
            MostrarPregunta();
        }
        else
        {
            Debug.LogWarning($"No hay preguntas definidas para el nivel {nivel}.");
        }
    }

    void MostrarPregunta()
    {
        if (indicePreguntaActual >= preguntas.Count)
        {
            MostrarResultadosFinales();
            return;
        }
        OracionConPalabras preguntaActual = preguntas[indicePreguntaActual];
        txtOracion.text = preguntaActual.oracion;

        foreach (Transform child in contenedorOpciones)
            Destroy(child.gameObject);

        for (int i = 0; i < preguntaActual.opciones.Length; i++)
        {
            GameObject btn = Instantiate(botonPrefab, contenedorOpciones);
            TextMeshProUGUI txtBtn = btn.GetComponentInChildren<TextMeshProUGUI>();
            txtBtn.text = preguntaActual.opciones[i];
            int index = i;
            btn.GetComponent<Button>().onClick.AddListener(() => SeleccionarPalabra(index, btn));
        }

        preguntaEnCurso = true;
        StopCoroutine("Temporizador");
        StartCoroutine("Temporizador");
    }

    void SeleccionarPalabra(int indiceSeleccionado, GameObject boton)
    {
        if (!preguntaEnCurso) return;
        preguntaEnCurso = false;
        StopCoroutine("Temporizador");

        OracionConPalabras preguntaActual = preguntas[indicePreguntaActual];

        bool esCorrecto = (indiceSeleccionado == preguntaActual.indiceCorrecto);
        string colorCorrecto = "<color=#A2C94D>";
        string colorIncorrecto = "<color=#C43E3B>";
        string colorFin = "</color>";

        string palabraSeleccionada = preguntaActual.opciones[indiceSeleccionado];
        string palabraColoreada = esCorrecto ? $"{colorCorrecto}{palabraSeleccionada}{colorFin}" : $"{colorIncorrecto}{palabraSeleccionada}{colorFin}";

        txtOracion.text = preguntaActual.oracion.Replace("_____", palabraColoreada);
        boton.GetComponent<Image>().color = esCorrecto ? new Color(0.64f, 0.79f, 0.30f) : new Color(0.77f, 0.24f, 0.23f);

        if (esCorrecto)
        {
            racha++;
            respuestasCorrectas++;
        }
        else
        {
            racha = 0;
        }

        txtRacha.text = racha.ToString();
        StartCoroutine(EsperarYSiguientePregunta());
    }

    IEnumerator Temporizador()
    {
        tiempoRestante = tiempoPorPregunta;
        while (tiempoRestante > 0)
        {
            if (!preguntaEnCurso) yield break;
            tiempoRestante -= Time.deltaTime;
            txtTiempo.text = tiempoRestante.ToString("F1");
            yield return null;
        }
        if (preguntaEnCurso)
        {
            preguntaEnCurso = false;
            racha = 0;
            txtRacha.text = racha.ToString();
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(EsperarYSiguientePregunta());
        }
    }
    IEnumerator EsperarYSiguientePregunta()
    {
        yield return new WaitForSeconds(1.5f);
        indicePreguntaActual++;
        barraProgreso.InicializarBarra(preguntas.Count);
        MostrarPregunta();
    }
    void MostrarResultadosFinales()
    {
        panelFinal.SetActive(true);
        int experiencia = (respuestasCorrectas * 100) / preguntas.Count;
        txtResultado.text = $"Respuestas correctas: {respuestasCorrectas}/{preguntas.Count}\nExperiencia ganada: {experiencia}XP\nBonificación de racha: {racha * 10}";
    }


    public void GuardarYSalir()
    {
        SceneManager.LoadScene("Grupos");
        GuardarProgreso(nivelSeleccionado, respuestasCorrectas);
    }

    public async void GuardarProgreso(int nivelActualJugado, int correctas)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ Usuario no autenticado.");
            return;
        }
        string userId = auth.CurrentUser.UserId;
        DocumentReference docGrupo = db.Collection("users").Document(userId).Collection("grupos").Document("grupo 1");
        DocumentReference docUsuario = db.Collection("users").Document(userId);
        try
        {
            // Obtener datos actuales
            DocumentSnapshot snapshotGrupo = await docGrupo.GetSnapshotAsync();
            DocumentSnapshot snapshotUsuario = await docUsuario.GetSnapshotAsync();
            int nivelAlmacenado = snapshotGrupo.Exists && snapshotGrupo.TryGetValue<int>("nivel", out int nivel) ? nivel : 1;
            int xpActual = snapshotUsuario.Exists && snapshotUsuario.TryGetValue<int>("xp", out int xp) ? xp : 0;

            int xpGanado = correctas * 100;

            // 🔹 Si el usuario juega un nivel menor al suyo, gana la mitad de XP y NO sube de nivel
            if (nivelActualJugado <= nivelAlmacenado)
            {
                xpGanado /= 2;
                Debug.Log("🔻 Jugaste un nivel menor, XP reducida a la mitad.");
            }

            bool subirNivel = nivelActualJugado >= nivelAlmacenado;
            int nuevoNivel = subirNivel ? nivelActualJugado : nivelAlmacenado;
            int nuevoXp = xpActual + xpGanado;

            // Guardar XP
            await docUsuario.SetAsync(new Dictionary<string, object> { { "xp", nuevoXp } }, SetOptions.MergeAll);
            // Guardar Nivel si sube
            if (subirNivel)
            {
                await docGrupo.SetAsync(new Dictionary<string, object> { { "nivel", nuevoNivel } }, SetOptions.MergeAll);
            }
            Debug.Log($"✅ Progreso guardado: Nivel {nuevoNivel}, XP Total {nuevoXp}");
            // Guardar localmente en PlayerPrefs
            PlayerPrefs.SetInt("nivelCompletado", nuevoNivel);
            PlayerPrefs.SetInt("xp", nuevoXp);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al guardar el progreso: {e.Message}");
        }
    }

    [System.Serializable]
    public class OracionConPalabras
    {
        public string oracion;
        public string[] opciones;
        public int indiceCorrecto;

        public OracionConPalabras(string oracion, string[] opciones, int indiceCorrecto)
        {
            this.oracion = oracion;
            this.opciones = opciones;
            this.indiceCorrecto = indiceCorrecto;
        }
    }

}
