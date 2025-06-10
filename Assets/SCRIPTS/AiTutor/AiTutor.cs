using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine.Networking;

[System.Serializable]
public class ElementoQuimico
{
    public string id;
    public string nombre;
    public string simbolo;
    public int numero_atomico;
    public int grupo;
    public int periodo;
    public float masa_atomica;
    public string estado;
    public string tipo;
    public string descripcion;
}

public class AiTutor : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_InputField inputPregunta;
    public GameObject bubbleUserPrefab;
    public GameObject bubbleAiPrefab;
    public Transform contentChat;
    public EmbeddingsLoader loader;
    public MiniLMEmbedder embedder;
    public GuardarMisionCompletada gestorMisiones;

    private Dictionary<string, ElementoQuimico> elementos;
    private string ultimoElementoActivo = "";
    private string ultimaIntencion = "";
    private bool elementosCargados = false;

    public class Intencion
    {
        public string nombre;
        public string ejemplo;
    }

    private List<Intencion> intenciones = new List<Intencion>
    {
        new Intencion { nombre = "uso", ejemplo = "¿Para qué se usa el oxígeno? ¿Cuál es su aplicación principal? ¿En qué industrias se utiliza este elemento?" },
        new Intencion { nombre = "ubicacion", ejemplo = "¿En qué grupo y periodo está el sodio? ¿Dónde se encuentra ubicado en la tabla periódica?" },
        new Intencion { nombre = "masa", ejemplo = "¿Cuál es la masa atómica del helio? ¿Qué peso tiene un átomo de este elemento?" },
        new Intencion { nombre = "estado", ejemplo = "¿Está en estado sólido, líquido o gaseoso? ¿Cuál es su estado físico a temperatura ambiente?" },
        new Intencion { nombre = "tipo", ejemplo = "¿Qué tipo de elemento es el torio? ¿Pertenece a los metales alcalinos, alcalinotérreos o es un actínido?" },
        new Intencion { nombre = "categoria", ejemplo = "¿Cuál es la categoría del torio? ¿A qué familia pertenece este elemento en la tabla periódica? ¿Qué bloque ocupa? ¿Qué tipo de familia es?"},
        new Intencion { nombre = "numeroAtomico", ejemplo = "¿Cuál es el número atómico del hidrógeno? ¿Qué número tiene en la tabla periódica?" },
        new Intencion { nombre = "general", ejemplo = "Cuéntame del elemento. ¿Qué sabes sobre él? Quiero una descripción general." },
    };

    // Palabras que NO deben considerarse como elementos químicos
    private HashSet<string> palabrasExcluidas = new HashSet<string>()
    {
        "si", "sí", "se", "la", "el", "un", "una", "de", "del", "en", "con", "por", "para", "que", "qué",
        "es", "son", "como", "cómo", "mas", "más", "y", "o", "pero", "no", "me", "te", "le", "lo", "los",
        "las", "al", "del", "esta", "está", "este", "esto", "eso", "esa", "su", "sus", "mi", "mis", "tu", "tus"
    };

    private HashSet<string> affirmativeWords = new HashSet<string>()
    {
        "si", "sí", "claro", "ok", "adelante", "dime", "cuentame", "cuéntame", "mas", "más",
        "quiero saber", "continua", "continúa", "sigue", "dale", "venga", "perfecto", "genial"
    };

    void Start()
    {
        CrearBurbujaIA("¡Hola! Soy tu tutor virtual de química. Cargando elementos...");
        StartCoroutine(CargarElementosDesdeJSONL());
        loader.CargarEmbeddings();

        if (embedder == null)
        {
            Debug.LogError("El embedder no está asignado en el Start().");
        }
        else
        {
            Debug.Log("Embedder activo en tiempo de ejecución.");
        }
    }

    // MÉTODO CORREGIDO: Carga compatible con Android
    IEnumerator CargarElementosDesdeJSONL()
    {
        elementos = new Dictionary<string, ElementoQuimico>();

        // Construir la ruta correcta para StreamingAssets
        string ruta;
        if (Application.platform == RuntimePlatform.Android)
        {
            ruta = Path.Combine(Application.streamingAssetsPath, "periodic_master_118.jsonl");
        }
        else
        {
            ruta = "file://" + Path.Combine(Application.streamingAssetsPath, "periodic_master_118.jsonl");
        }

        Debug.Log("Intentando cargar desde: " + ruta);

        using (UnityWebRequest request = UnityWebRequest.Get(ruta))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string contenido = request.downloadHandler.text;
                Debug.Log("Archivo cargado exitosamente. Tamaño: " + contenido.Length + " caracteres");

                // Procesar el contenido línea por línea
                string[] lineas = contenido.Split('\n');
                int elementosCargadosCount = 0;

                foreach (string linea in lineas)
                {
                    if (!string.IsNullOrEmpty(linea.Trim()))
                    {
                        try
                        {
                            ElementoQuimico el = JsonUtility.FromJson<ElementoQuimico>(linea.Trim());

                            if (el != null && !string.IsNullOrEmpty(el.simbolo))
                            {
                                string simboloKey = el.simbolo.ToLower();
                                if (!elementos.ContainsKey(simboloKey))
                                {
                                    elementos[simboloKey] = el;
                                    elementosCargadosCount++;
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning("Error al procesar línea: " + linea + ". Error: " + e.Message);
                        }
                    }
                }

                elementosCargados = true;
                Debug.Log("Elementos cargados exitosamente: " + elementosCargadosCount);

                // Actualizar el mensaje de bienvenida
                CrearBurbujaIA("¡Perfecto! Elementos cargados. Pregúntame sobre cualquier elemento de la tabla periódica.");

                if (elementosCargadosCount > 0)
                {
                    Debug.Log("Ejemplo de elemento cargado: " + elementos.Values.First().nombre);
                }
            }
            else
            {
                Debug.LogError("Error al cargar el archivo JSONL: " + request.error);
                Debug.LogError("Código de respuesta: " + request.responseCode);

                // Intentar cargar desde Resources como fallback
                yield return StartCoroutine(CargarDesdeResources());
            }
        }
    }

    // MÉTODO ALTERNATIVO: Cargar desde Resources como fallback
    IEnumerator CargarDesdeResources()
    {
        Debug.Log("Intentando cargar desde Resources como fallback...");

        // Nota: Para esto necesitas mover el archivo a Assets/Resources/
        TextAsset jsonFile = Resources.Load<TextAsset>("periodic_master_118");

        if (jsonFile != null)
        {
            Debug.Log("Archivo encontrado en Resources");

            string[] lineas = jsonFile.text.Split('\n');
            int elementosCargadosCount = 0;

            foreach (string linea in lineas)
            {
                if (!string.IsNullOrEmpty(linea.Trim()))
                {
                    try
                    {
                        ElementoQuimico el = JsonUtility.FromJson<ElementoQuimico>(linea.Trim());

                        if (el != null && !string.IsNullOrEmpty(el.simbolo))
                        {
                            string simboloKey = el.simbolo.ToLower();
                            if (!elementos.ContainsKey(simboloKey))
                            {
                                elementos[simboloKey] = el;
                                elementosCargadosCount++;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("Error al procesar línea desde Resources: " + linea + ". Error: " + e.Message);
                    }
                }
            }

            elementosCargados = true;
            Debug.Log("Elementos cargados desde Resources: " + elementosCargadosCount);
            CrearBurbujaIA("¡Elementos cargados! Pregúntame sobre cualquier elemento de la tabla periódica.");
        }
        else
        {
            Debug.LogError("No se pudo cargar el archivo ni desde StreamingAssets ni desde Resources");
            CrearBurbujaIA("Error al cargar los elementos químicos. Por favor, verifica la instalación.");
        }

        yield return null;
    }

    public void EnviarPregunta()
    {
        if (!elementosCargados)
        {
            CrearBurbujaIA("Los elementos aún se están cargando. Por favor, espera un momento.");
            return;
        }

        string pregunta = inputPregunta.text;
        if (!string.IsNullOrEmpty(pregunta))
        {
            CrearBurbujaUsuario(pregunta);
            ProcesarPregunta(pregunta);
            inputPregunta.text = "";
        }
    }

    public void ProcesarPregunta(string pregunta)
    {
        if (!elementosCargados)
        {
            CrearBurbujaIA("Los elementos aún se están cargando. Por favor, espera un momento.");
            return;
        }

        Debug.Log("Pregunta Original: " + pregunta);
        string preguntaNormalizada = pregunta.Trim().ToLower();
        Debug.Log("Pregunta Normalizada: " + preguntaNormalizada);

        // PASO 1: Verificar si es una respuesta afirmativa en contexto
        if (!string.IsNullOrEmpty(ultimoElementoActivo) && EsRespuestaAfirmativa(preguntaNormalizada))
        {
            Debug.Log($"Respuesta afirmativa detectada para el elemento '{ultimoElementoActivo}' con intención '{ultimaIntencion}'.");
            string respuestaExtendida = GenerarRespuestaExtendida(elementos[ultimoElementoActivo], ultimaIntencion);
            CrearBurbujaIA(respuestaExtendida);
            ultimoElementoActivo = "";
            ultimaIntencion = "";
            Debug.Log("Contexto limpiado después de respuesta extendida.");
            return;
        }

        // PASO 2: Buscar elemento químico en la pregunta
        ElementoQuimico elementoEncontrado = BuscarElementoEnPregunta(preguntaNormalizada);

        if (elementoEncontrado != null)
        {
            // Se encontró un elemento específico
            Debug.Log($"Elemento encontrado: {elementoEncontrado.nombre}");
            ProcesarPreguntaConElemento(elementoEncontrado, preguntaNormalizada);
        }
        else if (!string.IsNullOrEmpty(ultimoElementoActivo))
        {
            // No se encontró elemento pero hay contexto activo
            Debug.Log($"Usando contexto del elemento '{ultimoElementoActivo}'.");
            ElementoQuimico elContextual = elementos[ultimoElementoActivo];
            ProcesarPreguntaConElemento(elContextual, preguntaNormalizada);
        }
        else
        {
            // No hay elemento ni contexto
            Debug.Log("No hay elemento encontrado y no hay contexto activo.");
            CrearBurbujaIA("No entendí muy bien tu pregunta. ¿Puedes indicar sobre qué elemento químico quieres saber?");
        }
    }

    private bool EsRespuestaAfirmativa(string pregunta)
    {
        // Verificar palabras afirmativas exactas o al final de la frase
        foreach (string affirmative in affirmativeWords)
        {
            if (pregunta == affirmative || pregunta.EndsWith(" " + affirmative))
            {
                return true;
            }
        }
        return false;
    }

    private ElementoQuimico BuscarElementoEnPregunta(string pregunta)
    {
        List<ElementoQuimico> candidatos = new List<ElementoQuimico>();

        foreach (var par in elementos)
        {
            string simbolo = par.Value.simbolo.ToLower();
            string nombre = par.Value.nombre.ToLower();

            // Excluir palabras comunes que podrían confundirse con símbolos
            if (palabrasExcluidas.Contains(simbolo))
                continue;

            bool foundByName = Regex.IsMatch(pregunta, $@"\b{Regex.Escape(nombre)}\b");
            bool foundBySymbol = false;

            // Para símbolos, ser más estrictos si son muy cortos
            if (simbolo.Length > 2)
            {
                foundBySymbol = Regex.IsMatch(pregunta, $@"\b{Regex.Escape(simbolo)}\b");
            }
            else
            {
                // Para símbolos cortos, verificar que no estén en contexto de palabras comunes
                foundBySymbol = Regex.IsMatch(pregunta, $@"\b{Regex.Escape(simbolo)}\b") &&
                               !EsSimboloEnContextoComun(pregunta, simbolo);
            }

            if (foundByName || foundBySymbol)
            {
                candidatos.Add(par.Value);
                Debug.Log($"Candidato encontrado: {par.Value.nombre} ({par.Value.simbolo}) - Match por {(foundByName ? "nombre" : "símbolo")}");
            }
        }

        if (candidatos.Count == 0)
            return null;

        if (candidatos.Count == 1)
            return candidatos[0];

        // Si hay múltiples candidatos, priorizar por nombre completo y luego por longitud
        return candidatos.OrderByDescending(c =>
        {
            bool byName = Regex.IsMatch(pregunta, $@"\b{Regex.Escape(c.nombre.ToLower())}\b");
            return byName ? c.nombre.Length + 1000 : c.simbolo.Length; // Bonus para nombres
        }).First();
    }

    private bool EsSimboloEnContextoComun(string pregunta, string simbolo)
    {
        // Verificar contextos donde el símbolo probablemente no es un elemento
        string[] contextosComunes = {
            "como se", "que se", "si se", "se usa", "se encuentra", "se llama",
            "la masa", "la categoria", "el tipo", "el estado"
        };

        foreach (string contexto in contextosComunes)
        {
            if (pregunta.Contains(contexto))
                return true;
        }

        return false;
    }

    private void ProcesarPreguntaConElemento(ElementoQuimico elemento, string pregunta)
    {
        string intencionDetectada = DetectarIntencionPorEmbedding(pregunta);
        Debug.Log($"Intención detectada: {intencionDetectada}");

        string respuesta = GenerarRespuestaConversacional(elemento, intencionDetectada);
        CrearBurbujaIA(respuesta);

        // Establecer contexto solo para intenciones que pueden tener seguimiento
        if (intencionDetectada == "general" || intencionDetectada == "uso")
        {
            ultimoElementoActivo = elemento.simbolo.ToLower();
            ultimaIntencion = intencionDetectada;
            Debug.Log($"Contexto establecido: Elemento='{ultimoElementoActivo}', Intención='{ultimaIntencion}'");
        }
        else
        {
            ultimoElementoActivo = "";
            ultimaIntencion = "";
            Debug.Log("Contexto limpiado (respuesta específica).");
        }
    }

    string DetectarIntencionPorTexto(string pregunta)
    {
        string lower = pregunta.ToLower();

        // Detección más específica de intenciones
        if (lower.Contains("número atómico") || lower.Contains("numero atomico"))
            return "numeroAtomico";

        if (lower.Contains("para qué") || lower.Contains("para que") ||
            lower.Contains("uso") || lower.Contains("utiliza") || lower.Contains("sirve"))
            return "uso";

        if (lower.Contains("grupo") || lower.Contains("periodo") ||
            lower.Contains("fila") || lower.Contains("columna") || lower.Contains("ubicado"))
            return "ubicacion";

        if (lower.Contains("masa") || lower.Contains("peso"))
            return "masa";

        if (lower.Contains("estado") || lower.Contains("sólido") ||
            lower.Contains("líquido") || lower.Contains("gaseoso"))
            return "estado";

        if (lower.Contains("tipo") || lower.Contains("metal") ||
            lower.Contains("no metal") || lower.Contains("metaloide"))
            return "tipo";

        if (lower.Contains("categoria") || lower.Contains("categoría") ||
            lower.Contains("familia") || lower.Contains("bloque"))
            return "categoria";

        return null;
    }

    string DetectarIntencionPorEmbedding(string pregunta)
    {
        string directa = DetectarIntencionPorTexto(pregunta);
        if (directa != null) return directa;

        // Fallback semántico
        if (embedder == null) return "general";

        float[] embPregunta = embedder.ObtenerEmbedding(pregunta);
        if (embPregunta == null) return "general";

        float maxSim = float.MinValue;
        string mejor = "general";

        foreach (var intencion in intenciones)
        {
            float[] embEjemplo = embedder.ObtenerEmbedding(intencion.ejemplo);
            if (embEjemplo != null)
            {
                float sim = CalcularSimilitudCoseno(embPregunta, embEjemplo);
                if (sim > maxSim)
                {
                    maxSim = sim;
                    mejor = intencion.nombre;
                }
            }
        }
        return mejor;
    }

    string GenerarRespuestaConversacional(ElementoQuimico el, string intencion)
    {
        switch (intencion)
        {
            case "numeroAtomico":
                return $"El número atómico del {el.nombre} es {el.numero_atomico}. Esto significa que tiene {el.numero_atomico} protones en su núcleo.";

            case "uso":
                string usoBasico = el.descripcion.Split('.')[0];
                return $"¡Gran pregunta! el {el.nombre} ({el.simbolo}) se utiliza principalmente en: {usoBasico}. ¿Te interesa saber más usos específicos?";

            case "ubicacion":
                return $"El {el.nombre} ({el.simbolo}) está ubicado en el grupo {el.grupo} y el periodo {el.periodo} de la tabla periódica. ¡Eso nos dice mucho sobre su comportamiento químico!";

            case "masa":
                return $"La masa atómica del {el.nombre} ({el.simbolo}) es aproximadamente {el.masa_atomica} u (unidades de masa atómica). Este valor es importante para cálculos químicos.";

            case "estado":
                return $"En condiciones normales de temperatura y presión, el {el.nombre} ({el.simbolo}) se encuentra en estado {el.estado.ToLower()}. Esto influye en cómo se maneja y almacena.";

            case "tipo":
                return $"El {el.nombre} ({el.simbolo}) es clasificado como un {el.tipo.ToLower()}. Esta clasificación nos indica sus propiedades químicas generales.";

            case "categoria":
                return $"El {el.nombre} ({el.simbolo}) pertenece a la categoría de los {el.tipo.ToLower()}s en la tabla periódica. Los elementos de esta categoría comparten propiedades similares.";

            default:
                return $"El {el.nombre} ({el.simbolo}) es el elemento con número atómico {el.numero_atomico}. Es un {el.tipo.ToLower()} fascinante. ¿Quieres saber sobre sus usos, propiedades o ubicación en la tabla periódica?";
        }
    }

    string GenerarRespuestaExtendida(ElementoQuimico el, string intencion)
    {
        switch (intencion)
        {
            case "uso":
                return $"Te cuento más sobre los usos del {el.nombre}: {el.descripcion} Su versatilidad lo hace muy valioso en diferentes industrias.";

            case "general":
                return $"Información adicional sobre el {el.nombre}: {el.descripcion} Como puedes ver, es un elemento con características muy interesantes.";

            default:
                return $"Información adicional sobre el {el.nombre}: {el.descripcion}";
        }
    }

    float CalcularSimilitudCoseno(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return 0f;

        float dot = 0f, magA = 0f, magB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (Mathf.Sqrt(magA) * Mathf.Sqrt(magB) + 1e-6f);
    }

    // Métodos de UI y misiones (sin cambios)
    public void GuiarMisionDesdeTutor(string elemento, int idMision)
    {
        PlayerPrefs.SetString("ElementoSeleccionado", elemento);
        PlayerPrefs.SetInt("MisionActual", idMision);
        PlayerPrefs.Save();

        CrearBurbujaIA($"Misión del elemento {elemento}:\n¿Listo para completarla?");
    }

    public void EvaluarRespuesta(string respuesta)
    {
        string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
        int idMision = PlayerPrefs.GetInt("MisionActual", -1);

        if (elemento == "" || idMision == -1)
        {
            CrearBurbujaIA("No hay ninguna misión activa. Pídele al tutor que te asigne una.");
            return;
        }

        if (respuesta.ToLower().Contains("metaloide"))
        {
            gestorMisiones.MarcarMisionComoCompletada();
            CrearBurbujaIA("¡Excelente! Completaste la misión correctamente.");
        }
        else
        {
            CrearBurbujaIA("Esa no es la respuesta esperada. Intenta de nuevo.");
        }
    }

    void CrearBurbujaUsuario(string texto)
    {
        GameObject burbuja = Instantiate(bubbleUserPrefab, contentChat);
        burbuja.GetComponentInChildren<TextMeshProUGUI>().text = texto;

        RectTransform rt = burbuja.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-10, rt.anchoredPosition.y);
    }

    void CrearBurbujaIA(string texto)
    {
        GameObject burbuja = Instantiate(bubbleAiPrefab, contentChat);
        burbuja.GetComponentInChildren<TextMeshProUGUI>().text = texto;

        RectTransform rt = burbuja.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, rt.anchoredPosition.y);
    }
}