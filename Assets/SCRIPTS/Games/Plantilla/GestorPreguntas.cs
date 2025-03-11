using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using static GestorOraciones;

public class GestorPreguntas : MonoBehaviour
{
    public TextMeshProUGUI txtPregunta;
    public Toggle[] opciones;
    public Text txtTiempo;
    public Text txtRacha;
    public BarraProgreso barraProgreso;
    public GameObject panelFinal;
    public TextMeshProUGUI txtResultado;

    private Dictionary<int, List<PreguntaConOpciones>> preguntasPorNivel = new Dictionary<int, List<PreguntaConOpciones>>();

    private List<PreguntaConOpciones> preguntas = new List<PreguntaConOpciones>();

    private int indicePreguntaActual = 0;
    private int racha = 0;
    private int respuestasCorrectas = 0;
    private float tiempoPorPregunta = 10f;
    private float tiempoRestante;
    private bool preguntaEnCurso = true;
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    private int nivelSeleccionado;

    int nivelActual = 2;

    void Start()
    {

        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        nivelSeleccionado = ControladorNiveles.nivelSeleccionado;
        // Cargar preguntas por nivel
        CargarPreguntas();

        // Obtener nivel del usuario desde Firebase
        CargarPreguntasNivel(nivelSeleccionado);
        StartCoroutine(Temporizador());
    }

    void CargarPreguntasNivel(int nivelSeleccionado)
    {
        if (preguntasPorNivel.ContainsKey(nivelSeleccionado))
        {
            preguntas = preguntasPorNivel[nivelSeleccionado];
            indicePreguntaActual = 0;
            barraProgreso.InicializarBarra(preguntas.Count);
            MostrarPregunta();
        }
        else
        {
            Debug.LogWarning($"No hay preguntas definidas para el nivel {nivelSeleccionado}.");
        }
    }

    public void MostrarPregunta()
    {
        if (indicePreguntaActual >= preguntas.Count)
        {
            MostrarResultadosFinales();
            return;
        }
        PreguntaConOpciones preguntaActual = preguntas[indicePreguntaActual];
        txtPregunta.text = preguntaActual.pregunta;
        for (int i = 0; i < opciones.Length; i++)
        {
            opciones[i].GetComponentInChildren<TextMeshProUGUI>().text = preguntaActual.opciones[i];
            opciones[i].isOn = false;
            opciones[i].GetComponentInChildren<Image>().color = Color.white;
            int index = i;
            opciones[i].onValueChanged.RemoveAllListeners();
            opciones[i].onValueChanged.AddListener(delegate { ValidarRespuesta(index); });
        }
        preguntaEnCurso = true;
        StopCoroutine("ActualizarTimer");
        StartCoroutine("Temporizador");
    }
    public void ValidarRespuesta(int indiceSeleccionado)
    {
        if (!preguntaEnCurso) return;
        preguntaEnCurso = false;
        StopCoroutine("ActualizarTimer");
        PreguntaConOpciones preguntaActual = preguntas[indicePreguntaActual];
        Color verdeCorrecto = new Color(0xAA / 255f, 0xC4 / 255f, 0x3D / 255f);
        Color rojoIncorrecto = new Color(0xC4 / 255f, 0x3E / 255f, 0x3B / 255f);
        Color color = (indiceSeleccionado == preguntaActual.indiceCorrecto) ? verdeCorrecto : rojoIncorrecto;
        opciones[indiceSeleccionado].GetComponentInChildren<Image>().color = color;
        if (indiceSeleccionado == preguntaActual.indiceCorrecto)
        {
            racha++;
            respuestasCorrectas++;
        }
        else
        {
            racha = 0;
        }
        txtRacha.text = "" + racha;
        if (indicePreguntaActual == preguntas.Count - 1)
        {
            MostrarResultadosFinales();
            return;
        }
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
            txtRacha.text = "" + racha;
            yield return new WaitForSeconds(1.5f);
            if (indicePreguntaActual == preguntas.Count - 1) yield break;
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
        GuardarProgreso(nivelActual, respuestasCorrectas);
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


    void CargarPreguntas()
    {
        preguntasPorNivel[1] = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Cuál es el símbolo químico del oxígeno?", new string[] { "O", "Ox", "Om", "X" }, 0),
            new PreguntaConOpciones("¿Qué gas necesitamos para respirar?", new string[] { "Nitrógeno", "Oxígeno", "Dióxido de carbono", "Helio" }, 1),
            new PreguntaConOpciones("¿Cuál es el número atómico del hidrógeno?", new string[] { "1", "2", "8", "16" }, 0),
            new PreguntaConOpciones("¿Cuál es el estado físico del agua a 100°C?", new string[] { "Sólido", "Líquido", "Gaseoso", "Plasma" }, 2),
            new PreguntaConOpciones("¿Cuál es el metal más liviano?", new string[] { "Hierro", "Aluminio", "Litio", "Plomo" }, 2),
            new PreguntaConOpciones("¿Qué gas se usa para llenar globos aerostáticos?", new string[] { "Oxígeno", "Helio", "Hidrógeno", "Nitrógeno" }, 1),
            new PreguntaConOpciones("¿Qué líquido es conocido como 'disolvente universal'?", new string[] { "Alcohol", "Agua", "Gasolina", "Ácido sulfúrico" }, 1),
            new PreguntaConOpciones("¿Cómo se llama el proceso en el que el agua pasa de sólido a gas sin ser líquido?", new string[] { "Sublimación", "Condensación", "Fusión", "Evaporación" }, 0),
            new PreguntaConOpciones("¿Qué tipo de cambio ocurre cuando un hielo se derrite?", new string[] { "Físico", "Químico", "Radiactivo", "Ninguno" }, 0),
            new PreguntaConOpciones("¿Qué componente del aire es el más abundante?", new string[] { "Oxígeno", "Nitrógeno", "Dióxido de carbono", "Argón" }, 1)
        };

        preguntasPorNivel[4] = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Cuál es el número atómico del carbono?", new string[] { "6", "12", "8", "14" }, 0),
            new PreguntaConOpciones("¿Qué tipo de enlace une los átomos en una molécula de agua?", new string[] { "Iónico", "Metálico", "Covalente", "Péptido" }, 2),
            new PreguntaConOpciones("¿Cuál es el gas noble más ligero?", new string[] { "Neón", "Argón", "Helio", "Radón" }, 2),
            new PreguntaConOpciones("¿Qué elemento tiene el símbolo 'Fe'?", new string[] { "Fósforo", "Flúor", "Hierro", "Francio" }, 2),
            new PreguntaConOpciones("¿Cómo se llama la tabla donde están ordenados los elementos químicos?", new string[] { "Tabla periódica", "Tabla química", "Tabla de Mendeleev", "Tabla elemental" }, 0),
            new PreguntaConOpciones("¿Qué partícula tiene carga negativa?", new string[] { "Protón", "Neutrón", "Electrón", "Quark" }, 2),
            new PreguntaConOpciones("¿Cuál es el elemento más abundante en el universo?", new string[] { "Oxígeno", "Carbono", "Hidrógeno", "Helio" }, 2),
            new PreguntaConOpciones("¿Qué compuesto es conocido como 'sal de mesa'?", new string[] { "H₂O", "NaCl", "KCl", "NaOH" }, 1),
            new PreguntaConOpciones("¿Cuál es el pH del agua pura?", new string[] { "5", "7", "9", "11" }, 1),
            new PreguntaConOpciones("¿Cómo se llama la reacción química que libera energía en forma de luz o calor?", new string[] { "Exotérmica", "Endotérmica", "Neutra", "Explosiva" }, 0)
        };

        preguntasPorNivel[7] = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Cuál es la fórmula química del dióxido de carbono?", new string[] { "CO", "CO₂", "C₂O", "C₃O₂" }, 1),
            new PreguntaConOpciones("¿Qué elemento es el principal componente de los huesos humanos?", new string[] { "Hierro", "Calcio", "Fósforo", "Sodio" }, 1),
            new PreguntaConOpciones("¿Qué sustancia se usa comúnmente para neutralizar ácidos en el estómago?", new string[] { "Ácido sulfúrico", "Hidróxido de sodio", "Bicarbonato de sodio", "Ácido acético" }, 2),
            new PreguntaConOpciones("¿Cómo se llaman las sustancias que aceleran las reacciones químicas en los seres vivos?", new string[] { "Hormonas", "Catalizadores", "Enzimas", "Isótopos" }, 2),
            new PreguntaConOpciones("¿Qué propiedad del agua permite que los insectos caminen sobre su superficie?", new string[] { "Adhesión", "Cohesión", "Tensión superficial", "Viscosidad" }, 2),
            new PreguntaConOpciones("¿Qué elemento químico se encuentra en los lápices de grafito?", new string[] { "Carbón", "Carbono", "Plomo", "Estaño" }, 1),
            new PreguntaConOpciones("¿Cuál es la capa más externa de un átomo?", new string[] { "Núcleo", "Electrón", "Órbita de valencia", "Neutrón" }, 2),
            new PreguntaConOpciones("¿Qué compuesto químico se usa en la fabricación del vidrio?", new string[] { "Sílice", "Calcio", "Fósforo", "Cobre" }, 0),
            new PreguntaConOpciones("¿Qué gas se produce en la fotosíntesis?", new string[] { "Oxígeno", "Dióxido de carbono", "Hidrógeno", "Nitrógeno" }, 0),
            new PreguntaConOpciones("¿Qué elemento se encuentra en los dientes y huesos y es importante para su fortaleza?", new string[] { "Sodio", "Magnesio", "Fósforo", "Litio" }, 2)
        };

        preguntasPorNivel[10] = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Qué tipo de enlace se da entre un metal y un no metal?", new string[] { "Covalente", "Iónico", "Metálico", "Radiactivo" }, 1),
            new PreguntaConOpciones("¿Cuál es el único metal líquido a temperatura ambiente?", new string[] { "Plomo", "Mercurio", "Oro", "Sodio" }, 1),
            new PreguntaConOpciones("¿Cuál es el gas que causa el efecto invernadero en mayor cantidad?", new string[] { "Oxígeno", "Metano", "Dióxido de carbono", "Óxidos de nitrógeno" }, 2),
            new PreguntaConOpciones("¿Cuál es la unidad de medida de la cantidad de sustancia en el SI?", new string[] { "Gramo", "Mol", "Litro", "Átomo" }, 1),
            new PreguntaConOpciones("¿Cuál es la partícula subatómica con carga positiva?", new string[] { "Neutrón", "Protón", "Electrón", "Quark" }, 1),
            new PreguntaConOpciones("¿Qué gas inflamable se usaba en los dirigibles de antes?", new string[] { "Helio", "Hidrógeno", "Oxígeno", "Nitrógeno" }, 1),
            new PreguntaConOpciones("¿Qué ácido se encuentra en el estómago humano?", new string[] { "Ácido sulfúrico", "Ácido clorhídrico", "Ácido acético", "Ácido fosfórico" }, 1),
            new PreguntaConOpciones("¿Cómo se llama el proceso en el que una sustancia pasa de gas a líquido?", new string[] { "Evaporación", "Condensación", "Sublimación", "Fusión" }, 1),
            new PreguntaConOpciones("¿Cuál de los siguientes NO es un metal alcalino?", new string[] { "Sodio", "Potasio", "Calcio", "Litio" }, 2),
            new PreguntaConOpciones("¿Qué elemento tiene el mayor número atómico en la tabla periódica?", new string[] { "Uranio", "Oganesón", "Plutonio", "Radón" }, 1)
        };

        preguntasPorNivel[13] = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Cómo se llama la reacción donde un compuesto se descompone en otros más simples?", new string[] { "Síntesis", "Descomposición", "Reducción", "Óxido-reducción" }, 1),
            new PreguntaConOpciones("¿Qué es un isótopo?", new string[] { "Un ion", "Un elemento con diferente número de neutrones", "Un enlace químico", "Un gas noble" }, 1),
            new PreguntaConOpciones("¿Qué se forma cuando un ácido y una base reaccionan?", new string[] { "Agua", "Gas", "Sal y agua", "Óxido" }, 2),
            new PreguntaConOpciones("¿Cómo se llama el fenómeno en el que un sólido pasa directamente a gas?", new string[] { "Evaporación", "Sublimación", "Condensación", "Fusión" }, 1),
            new PreguntaConOpciones("¿Qué ley dice que la materia no se crea ni se destruye, solo se transforma?", new string[] { "Ley de Boyle", "Ley de Lavoisier", "Ley de Dalton", "Ley de Avogadro" }, 1),
            new PreguntaConOpciones("¿Qué compuesto se usa para neutralizar la acidez en el estómago?", new string[] { "NaCl", "HCl", "NaHCO₃", "H₂O" }, 2),
            new PreguntaConOpciones("¿Cuál es la base química del ADN?", new string[] { "Azúcares", "Nucleótidos", "Aminoácidos", "Proteínas" }, 1),
            new PreguntaConOpciones("¿Qué tipo de enlace es el más fuerte?", new string[] { "Iónico", "Metálico", "Covalente", "Dipolo-dipolo" }, 2),
            new PreguntaConOpciones("¿Cuál es el pH de una solución ácida?", new string[] { "Mayor a 7", "Igual a 7", "Menor a 7", "Igual a 14" }, 2),
            new PreguntaConOpciones("¿Qué metal es más reactivo con el agua?", new string[] { "Litio", "Sodio", "Potasio", "Cesio" }, 3)
        };

        preguntasPorNivel[16] = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Qué es un catión?", new string[] { "Un átomo con carga negativa", "Un átomo con carga positiva", "Un enlace químico", "Un tipo de molécula" }, 1),
            new PreguntaConOpciones("¿Cuál es la unidad de medida de la energía en el SI?", new string[] { "Watt", "Julio", "Newton", "Caloría" }, 1),
            new PreguntaConOpciones("¿Qué compuesto se conoce como cal apagada?", new string[] { "CaO", "Ca(OH)₂", "NaOH", "CaCO₃" }, 1),
            new PreguntaConOpciones("¿Cuál es el gas más abundante en la atmósfera terrestre?", new string[] { "Oxígeno", "Nitrógeno", "Dióxido de carbono", "Argón" }, 1),
            new PreguntaConOpciones("¿Cómo se llama la reacción donde un metal se combina con oxígeno?", new string[] { "Reducción", "Oxidación", "Síntesis", "Electrólisis" }, 1),
            new PreguntaConOpciones("¿Cuál de estos elementos es un gas noble?", new string[] { "Flúor", "Neón", "Bromo", "Cloro" }, 1),
            new PreguntaConOpciones("¿Qué propiedad de los metales permite que se estiren en hilos?", new string[] { "Maleabilidad", "Ductilidad", "Conductividad", "Tenacidad" }, 1),
            new PreguntaConOpciones("¿Qué partícula es responsable de la radiactividad?", new string[] { "Electrón", "Protón", "Neutrón", "Fotón" }, 2),
            new PreguntaConOpciones("¿Cuál de estos compuestos es un ácido fuerte?", new string[] { "H₂CO₃", "HCl", "H₂SO₄", "NaOH" }, 2),
            new PreguntaConOpciones("¿Qué nombre recibe la constante de Avogadro?", new string[] { "6.022×10²³", "9.8 m/s²", "3.1416", "1.602×10⁻¹⁹" }, 0)
        };

        preguntasPorNivel[19] = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Cuál de estos elementos es un metaloide?", new string[] { "Aluminio", "Boro", "Calcio", "Potasio" }, 1),
            new PreguntaConOpciones("¿Qué ley de los gases establece que el volumen es inversamente proporcional a la presión?", new string[] { "Ley de Charles", "Ley de Boyle", "Ley de Avogadro", "Ley de Dalton" }, 1),
            new PreguntaConOpciones("¿Qué compuesto se usa en la fabricación de fertilizantes?", new string[] { "Ácido sulfúrico", "Amoníaco", "Metano", "Ozono" }, 1),
            new PreguntaConOpciones("¿Cuál es la fórmula del ácido sulfúrico?", new string[] { "HCl", "H₂SO₄", "HNO₃", "NaOH" }, 1),
            new PreguntaConOpciones("¿Qué gas se libera cuando el metal reacciona con ácido?", new string[] { "Oxígeno", "Dióxido de carbono", "Hidrógeno", "Nitrógeno" }, 2),
            new PreguntaConOpciones("¿Qué científico desarrolló la teoría atómica moderna?", new string[] { "Lavoisier", "Dalton", "Rutherford", "Bohr" }, 1),
            new PreguntaConOpciones("¿Qué propiedad química define la acidez o alcalinidad de una sustancia?", new string[] { "Solubilidad", "pH", "Densidad", "Conductividad" }, 1),
            new PreguntaConOpciones("¿Cómo se llama el proceso de convertir agua en hidrógeno y oxígeno usando electricidad?", new string[] { "Reducción", "Oxidación", "Electrólisis", "Fusión" }, 2),
            new PreguntaConOpciones("¿Cuál es el metal más abundante en la corteza terrestre?", new string[] { "Hierro", "Aluminio", "Cobre", "Plomo" }, 1),
            new PreguntaConOpciones("¿Qué gas se usa en la soldadura de arco?", new string[] { "Oxígeno", "Hidrógeno", "Argón", "Dióxido de carbono" }, 2)
        };

        preguntasPorNivel[22] = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Qué tipo de enlace hay en las moléculas de agua?", new string[] { "Covalente", "Iónico", "Metálico", "Dipolo-dipolo" }, 0),
            new PreguntaConOpciones("¿Qué elemento se usa en las baterías recargables?", new string[] { "Plomo", "Litio", "Cadmio", "Zinc" }, 1),
            new PreguntaConOpciones("¿Cuál de los siguientes compuestos es un óxido?", new string[] { "NaCl", "CO₂", "H₂SO₄", "C₆H₁₂O₆" }, 1),
            new PreguntaConOpciones("¿Qué tipo de reacción química libera calor?", new string[] { "Endotérmica", "Exotérmica", "Reducción", "Electrólisis" }, 1),
            new PreguntaConOpciones("¿Cómo se llama el proceso en el que un líquido pasa directamente a gas sin hervir?", new string[] { "Sublimación", "Evaporación", "Condensación", "Fusión" }, 1),
            new PreguntaConOpciones("¿Qué compuesto es el principal componente del vinagre?", new string[] { "Ácido fórmico", "Ácido cítrico", "Ácido acético", "Ácido clorhídrico" }, 2),
            new PreguntaConOpciones("¿Qué compuesto se encuentra en los fósforos y se usa para encender fuego?", new string[] { "Azufre", "Fósforo", "Nitrato de potasio", "Magnesio" }, 1),
            new PreguntaConOpciones("¿Cuál de estos elementos es un halógeno?", new string[] { "Oxígeno", "Cloro", "Nitrógeno", "Hidrógeno" }, 1),
            new PreguntaConOpciones("¿Qué propiedad de los metales permite que conduzcan electricidad?", new string[] { "Maleabilidad", "Conductividad", "Dureza", "Punto de fusión" }, 1),
            new PreguntaConOpciones("¿Qué metal es líquido a temperatura ambiente además del mercurio?", new string[] { "Galio", "Plomo", "Oro", "Cromo" }, 0)
        };

        preguntasPorNivel[25] = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Cuál es el proceso químico por el cual las plantas producen su propio alimento?", new string[] { "Fermentación", "Fotosíntesis", "Respiración celular", "Electrólisis" }, 1),
            new PreguntaConOpciones("¿Qué material es más resistente al calor?", new string[] { "Hierro", "Carburo de silicio", "Aluminio", "Vidrio templado" }, 1),
            new PreguntaConOpciones("¿Cuál de los siguientes es un compuesto orgánico?", new string[] { "CO₂", "CH₄", "H₂SO₄", "NaCl" }, 1),
            new PreguntaConOpciones("¿Qué compuesto es la base del ADN?", new string[] { "Glucosa", "Nucleótidos", "Lípidos", "Aminoácidos" }, 1),
            new PreguntaConOpciones("¿Qué gas es el principal responsable del efecto invernadero?", new string[] { "Dióxido de carbono", "Oxígeno", "Metano", "Argón" }, 2),
            new PreguntaConOpciones("¿Cuál es el único metal que no se oxida fácilmente?", new string[] { "Hierro", "Oro", "Cobre", "Plata" }, 1),
            new PreguntaConOpciones("¿Qué compuesto se usa en las bolsas biodegradables?", new string[] { "Polietileno", "Ácido poliláctico", "PVC", "Polipropileno" }, 1),
            new PreguntaConOpciones("¿Qué metal es más denso?", new string[] { "Plomo", "Osmio", "Cobre", "Mercurio" }, 1),
            new PreguntaConOpciones("¿Qué material se usa para las ventanas a prueba de balas?", new string[] { "Vidrio templado", "Policarbonato", "Cuarzo", "Acrílico" }, 1),
            new PreguntaConOpciones("¿Cuál es el metal más caro del mundo?", new string[] { "Oro", "Rodio", "Platino", "Paladio" }, 1)
        };
    }
}


//void GenerarPreguntasDesdeIA()
//{
//    FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
//    db.Collection("preguntas").GetSnapshotAsync().ContinueWithOnMainThread(task =>
//    {
//        if (task.IsCompleted)
//        {
//            preguntas = new List<string>();
//            foreach (DocumentSnapshot doc in task.Result.Documents)
//            {
//                preguntas.Add(doc.GetString("pregunta"));
//            }
//            barraProgreso.InicializarBarra(preguntas.Count);
//            MostrarPregunta();
//        }
//    });
//}