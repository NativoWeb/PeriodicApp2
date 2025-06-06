using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;


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
    public Button btnVolver;

    private Dictionary<string, ElementoQuimico> elementos;

    private string ultimoElementoActivo = "";
    private string ultimaIntencion = "";


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
        new Intencion { nombre = "general", ejemplo = "Cuéntame del elemento. ¿Qué sabes sobre él? Quiero una descripción general." },
    };

    private HashSet<string> affirmativeWords = new HashSet<string>()
    {
        "si", "sí", "claro", "ok", "adelante", "dime", "cuentame", "mas", "más", "quiero saber", "continua", "continúa", "sigue"
    };




    void Start()
    {
        CrearBurbujaIA("¡Hola! Soy tu tutor virtual de química. Pregúntame sobre cualquier elemento de la tabla periódica.");
        CargarElementosDesdeJSONL();
        loader.CargarEmbeddings();
        btnVolver.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Inicio");
        });
        if (embedder == null)
        {
            Debug.LogError("El embedder no está asignado en el Start().");
        }
        else
        {
            Debug.Log("Embedder activo en tiempo de ejecución.");
        }


    }



    void CargarElementosDesdeJSONL()
    {
        elementos = new Dictionary<string, ElementoQuimico>();
        string ruta = Path.Combine(Application.streamingAssetsPath, "periodic_master_118.jsonl");

        foreach (string linea in File.ReadAllLines(ruta))
        {
            ElementoQuimico el = JsonUtility.FromJson<ElementoQuimico>(linea);

            if (!elementos.ContainsKey(el.simbolo.ToLower()))
                elementos[el.simbolo.ToLower()] = el;
        }

        Debug.Log("Elementos cargados: " + elementos.Count);
        Debug.Log("Ejemplo clave: " + elementos.Keys.First());

    }

    public void EnviarPregunta()
    {
        string pregunta = inputPregunta.text.ToLower();
        if (!string.IsNullOrEmpty(pregunta))
        {
            CrearBurbujaUsuario(pregunta);
            ProcesarPregunta(pregunta);
            inputPregunta.text = "";
        }
    }

    public void ProcesarPregunta(string pregunta)
    {
        Debug.Log("Pregunta Original: " + pregunta);
        string preguntaNormalizada = pregunta.Trim().ToLower();
        Debug.Log("Pregunta Normalizada: " + preguntaNormalizada);

        // --- PASO 1: Manejar respuestas afirmativas en contexto ---
        // (Esta sección queda igual a la versión modificada que te di antes)
        if (!string.IsNullOrEmpty(ultimoElementoActivo))
        {
            bool isAffirmative = false;
            string normalizedLower = preguntaNormalizada.ToLower();
            foreach (string affirmative in affirmativeWords)
            {
                if (normalizedLower == affirmative || normalizedLower.EndsWith(" " + affirmative))
                {
                    isAffirmative = true;
                    break;
                }
            }

            if (isAffirmative)
            {
                Debug.Log($"Respuesta afirmativa detectada para el elemento '{ultimoElementoActivo}' con intención '{ultimaIntencion}'.");
                string respuestaExtendida = GenerarRespuestaExtendida(elementos[ultimoElementoActivo], ultimaIntencion);
                CrearBurbujaIA(respuestaExtendida);
                ultimoElementoActivo = "";
                ultimaIntencion = "";
                Debug.Log("Contexto limpiado después de respuesta extendida.");
                return;
            }
        }

        // --- PASO 2: Intentar identificar un elemento en la nueva pregunta (recopilar TODOS los matches) ---
        ElementoQuimico elementoEncontrado = null; // Este será el "mejor" elemento encontrado
        string simboloEncontrado = "";             // Símbolo del mejor elemento

        Debug.Log("Recopilando todos los potenciales matches de elementos en la pregunta normalizada: " + preguntaNormalizada);

        List<ElementoQuimico> potencialesMatches = new List<ElementoQuimico>(); // Lista para guardar todos los elementos encontrados

        foreach (var par in elementos)
        {
            string simbolo = par.Value.simbolo.ToLower();
            string nombre = par.Value.nombre.ToLower();

            // Usar Regex con \b para buscar la palabra completa (símbolo o nombre)
            bool foundBySymbol = false;
            // Opcional: Evitar símbolos de 1 o 2 letras que son muy comunes (como 'se', 'la', 'el', 'un', 'y', 'o', 'si')
            // Esto es una heurística. \b ya ayuda mucho, pero puedes añadir más control si los símbolos cortos dan problemas.
            // Por ahora, mantengamos la detección \b simple. Si \b alone doesn't work, check if simbolo.Length > 2 might help.
            // Para "se" (2 letras), \bse\b sí coincide en "como se llama". Necesitamos el desempate posterior.
            foundBySymbol = Regex.IsMatch(preguntaNormalizada, $@"\b{Regex.Escape(simbolo)}\b");

            bool foundByName = Regex.IsMatch(preguntaNormalizada, $@"\b{Regex.Escape(nombre)}\b");


            if (foundBySymbol || foundByName)
            {
                // ¡Encontramos una coincidencia! La añadimos a la lista en lugar de hacer break.
                potencialesMatches.Add(par.Value);
                Debug.Log($"Potencial match encontrado: '{par.Value.nombre}' (Símbolo: '{par.Value.simbolo}'). Matched by {(foundBySymbol ? "Symbol" : "Name")}.");
            }
        }

        // --- PASO 2b: Decidir cuál es el MEJOR elemento si se encontraron múltiples matches ---
        if (potencialesMatches.Count == 1)
        {
            elementoEncontrado = potencialesMatches[0];
            simboloEncontrado = elementoEncontrado.simbolo.ToLower();
            Debug.Log($"Se encontró 1 match claro: '{elementoEncontrado.nombre}'. Usándolo.");
        }
        else if (potencialesMatches.Count > 1)
        {
            Debug.Log($"Se encontraron {potencialesMatches.Count} matches potenciales. Decidiendo el mejor.");

            // Criterio de desempate:
            // 1. Priorizar nombres sobre símbolos (un nombre es más probable que sea la intención que un símbolo corto que es palabra común).
            // 2. Si ambos son nombres o símbolos, priorizar el match más largo.
            // 3. (Opcional pero mejor) Si hay empate, priorizar el que aparece más tarde en la frase (requiere encontrar la posición del match).
            // Implementaremos 1 y 2 que son más sencillos con la lista actual.

            ElementoQuimico mejorMatch = null;
            int mejorLongitudMatch = -1;
            bool mejorEsNombre = false;

            foreach (var match in potencialesMatches)
            {
                bool matchedByName = Regex.IsMatch(preguntaNormalizada, $@"\b{Regex.Escape(match.nombre.ToLower())}\b");
                bool matchedBySymbol = Regex.IsMatch(preguntaNormalizada, $@"\b{Regex.Escape(match.simbolo.ToLower())}\b"); // Re-check just in case

                int currentMatchLength = 0;
                if (matchedByName) currentMatchLength = match.nombre.Length;
                else if (matchedBySymbol) currentMatchLength = match.simbolo.Length; // Usar la longitud del símbolo/nombre que coincidió

                // Lógica de prioridad:
                // - Si el match actual es un nombre y el mejor match actual no es un nombre, el actual es mejor.
                // - Si ambos son nombres O ambos son símbolos, el mejor es el que tiene mayor longitud de match.
                // - Si el match actual es un símbolo y el mejor match actual es un nombre, el actual no es mejor.

                if (mejorMatch == null)
                {
                    mejorMatch = match;
                    mejorLongitudMatch = currentMatchLength;
                    mejorEsNombre = matchedByName;
                }
                else
                {
                    if (matchedByName && !mejorEsNombre) // Match actual es nombre, mejor actual es símbolo -> actual es mejor
                    {
                        mejorMatch = match;
                        mejorLongitudMatch = currentMatchLength;
                        mejorEsNombre = true;
                    }
                    else if ((matchedByName == mejorEsNombre) && currentMatchLength > mejorLongitudMatch) // Ambos son nombres o ambos símbolos, el actual es más largo -> actual es mejor
                    {
                        mejorMatch = match;
                        mejorLongitudMatch = currentMatchLength;
                        mejorEsNombre = matchedByName; // (aunque sería el mismo valor si el primer caso no se cumplió)
                    }
                    // Si el match actual es símbolo y el mejor es nombre (matchedByName == false && mejorEsNombre == true), no actualizamos.
                    // Si ambos son igual de nombre/símbolo y el actual no es más largo, no actualizamos.
                }
            }

            // Después de revisar todos los potenciales matches, asignamos el mejor encontrado
            if (mejorMatch != null)
            {
                elementoEncontrado = mejorMatch;
                simboloEncontrado = elementoEncontrado.simbolo.ToLower();
                Debug.Log($"Mejor match decidido: '{elementoEncontrado.nombre}' (Símbolo: '{elementoEncontrado.simbolo}', Tipo match: {(mejorEsNombre ? "Nombre" : "Símbolo")}, Longitud: {mejorLongitudMatch}).");
            }
            else
            {
                // Esto no debería pasar si potencialesMatches.Count > 0, pero es un fallback seguro
                Debug.Log("Error al decidir el mejor match a pesar de haber múltiples. Tratando como no encontrado.");
            }
        }
        else // potencialesMatches.Count == 0
        {
            Debug.Log("No se encontraron matches directos de elementos en la pregunta actual.");
            // elementoEncontrado ya es null
        }


        // --- PASO 3: Procesar según si se encontró un elemento NUEVO (el mejor match) o se debe usar el contexto ---
        // (Esta sección queda igual a la versión modificada que te di antes)
        if (elementoEncontrado != null)
        {
            Debug.Log($"Procesando pregunta sobre el elemento encontrado: '{elementoEncontrado.nombre}'.");
            string intencionDetectadaEnPregunta = DetectarIntencionPorEmbedding(preguntaNormalizada);
            Debug.Log($"Intención detectada para '{elementoEncontrado.nombre}': {intencionDetectadaEnPregunta}");

            string respuestaInicial = GenerarRespuestaConversacional(elementoEncontrado, intencionDetectadaEnPregunta);
            CrearBurbujaIA(respuestaInicial);

            if (intencionDetectadaEnPregunta == "general" || intencionDetectadaEnPregunta == "uso")
            {
                ultimoElementoActivo = simboloEncontrado;
                ultimaIntencion = intencionDetectadaEnPregunta;
                Debug.Log($"Contexto establecido: Elemento='{ultimoElementoActivo}', Intención='{ultimaIntencion}'");
            }
            else
            {
                ultimoElementoActivo = "";
                ultimaIntencion = "";
                Debug.Log("Contexto limpiado (respuesta inicial específica).");
            }

        }
        else // No se encontró un elemento específico claro en la pregunta actual
        {
            Debug.Log("No se encontró un elemento específico en la pregunta actual.");

            if (!string.IsNullOrEmpty(ultimoElementoActivo))
            {
                // Usar contexto
                Debug.Log($"Usando contexto del elemento '{ultimoElementoActivo}'.");
                ElementoQuimico elContextual = elementos[ultimoElementoActivo];

                string intencionDetectadaEnContexto = DetectarIntencionPorEmbedding(preguntaNormalizada);
                Debug.Log($"Intención detectada para el elemento contextual '{elContextual.nombre}': {intencionDetectadaEnContexto}");

                string respuestaContextual = GenerarRespuestaConversacional(elContextual, intencionDetectadaEnContexto);
                CrearBurbujaIA(respuestaContextual);

                if (intencionDetectadaEnContexto == "general" || intencionDetectadaEnContexto == "uso")
                {
                    ultimaIntencion = intencionDetectadaEnContexto;
                    Debug.Log($"Contexto actualizado: Elemento='{ultimoElementoActivo}', Intención='{ultimaIntencion}'");
                }
                else
                {
                    ultimoElementoActivo = "";
                    ultimaIntencion = "";
                    Debug.Log("Contexto limpiado (respuesta específica sobre elemento contextual).");
                }
            }
            else
            {
                // No hay elemento ni contexto
                Debug.Log("No hay elemento encontrado y no hay contexto activo. Pidiendo especificación.");
                CrearBurbujaIA("No entendí muy bien tu pregunta. ¿Puedes indicar a qué elemento químico te refieres?");
            }
        }
    }


    // Evita ejecutar embeddings para palabras de afirmación o sin sentido semántico
    bool palabraInútil(string p)
    {
        return p.Length < 4 || p == "sí" || p == "ok" || p == "claro" || p == "vale" || p == "gracias";
    }


    int BuscarElementoMasParecido(float[] vector)
    {
        float maxSim = float.MinValue;
        int mejorIndex = 0;

        for (int i = 0; i < loader.embeddings.Count; i++)
        {
            float sim = CalcularSimilitudCoseno(vector, loader.embeddings[i]);
            if (sim > maxSim)
            {
                maxSim = sim;
                mejorIndex = i;
            }
        }


        return mejorIndex;
    }

    float CalcularSimilitudCoseno(float[] a, float[] b)
    {
        float dot = 0f, magA = 0f, magB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (Mathf.Sqrt(magA) * Mathf.Sqrt(magB) + 1e-6f);
    }

    string DetectarIntencionPorTexto(string pregunta)
    {
        string lower = pregunta.ToLower();

        if (lower.Contains("para qué") || lower.Contains("uso") || lower.Contains("utiliza")) return "uso";
        if (lower.Contains("grupo") || lower.Contains("periodo") || lower.Contains("fila") || lower.Contains("columna")) return "ubicacion";
        if (lower.Contains("masa") || lower.Contains("peso")) return "masa";
        if (lower.Contains("estado") || lower.Contains("sólido") || lower.Contains("líquido") || lower.Contains("gaseoso")) return "estado";
        if (lower.Contains("tipo") || lower.Contains("metal") || lower.Contains("no metal") || lower.Contains("metaloide")) return "tipo";
        if (lower.Contains("familia") || lower.Contains("categoría") || lower.Contains("bloque")) return "categoria";

        return null;
    }


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
        string categoria = PlayerPrefs.GetString("CategoriaSeleccionada", "");

        if (elemento == "" || idMision == -1)
        {
            CrearBurbujaIA("No hay ninguna misión activa. Pídele al tutor que te asigne una.");
            return;
        }

        // Aquí puedes usar tu lógica para validar si la respuesta es correcta (ideal si tienes campo 'respuestaEsperada')
        if (respuesta.ToLower().Contains("metaloide"))  // ← temporal, puedes hacerlo dinámico
        {
            gestorMisiones.MarcarMisionComoCompletada(); // activa todo tu flujo: XP, JSON, Firebase
            CrearBurbujaIA("¡Excelente! Completaste la misión correctamente.");
        }
        else
        {
            CrearBurbujaIA("Esa no es la respuesta esperada. Intenta de nuevo.");
        }
    }



    string DetectarIntencionPorEmbedding(string pregunta)
    {
        string directa = DetectarIntencionPorTexto(pregunta);
        if (directa != null) return directa;

        // fallback semántico
        float[] embPregunta = embedder.ObtenerEmbedding(pregunta);
        float maxSim = float.MinValue;
        string mejor = "general";
        foreach (var intencion in intenciones)
        {
            float[] embEjemplo = embedder.ObtenerEmbedding(intencion.ejemplo);
            float sim = CalcularSimilitudCoseno(embPregunta, embEjemplo);
            if (sim > maxSim)
            {
                maxSim = sim;
                mejor = intencion.nombre;
            }
        }
        return mejor;
    }


    string GenerarRespuestaConversacional(ElementoQuimico el, string intencion)
    {
        switch (intencion)
        {
            case "uso":
                return $"¡Gran pregunta! El {el.nombre} se utiliza frecuentemente en ciencia, industria o medicina. Por ejemplo: {el.descripcion.Split('.')[0]}. ¿Te interesa saber más usos?";

            case "ubicacion":
                return $"Claro, el {el.nombre} está ubicado en el grupo {el.grupo} y el periodo {el.periodo} de la tabla periódica. ¡Eso nos dice mucho sobre su comportamiento!";

            case "masa":
                return $"La masa atómica del {el.nombre} es aproximadamente {el.masa_atomica} u. Es un dato útil cuando estudias reacciones químicas.";

            case "estado":
                return $"En condiciones normales, el {el.nombre} se encuentra en estado {el.estado.ToLower()}. Esto influye en cómo lo puedes manejar o almacenar.";

            case "tipo":
                return $"El {el.nombre} es un {el.tipo.ToLower()}. Eso significa que comparte propiedades con otros elementos del mismo tipo.";

            case "categoria":
                return $"El {el.nombre} pertenece a la categoría de los {el.tipo.ToLower()}s. Esta categoría agrupa elementos con propiedades similares.";
            default:
                return $"El {el.nombre} ({el.simbolo}) tiene número atómico {el.numero_atomico}. Es un elemento fascinante. ¿Quieres saber para qué se usa o cómo se comporta?";
        }
    }

    string GenerarRespuestaExtendida(ElementoQuimico el, string intencion)
    {
        switch (intencion)
        {
            case "uso":
                return $"Además de su uso común, el {el.nombre} también juega un rol importante en muchos procesos industriales y naturales. Por ejemplo, {el.descripcion}";

            case "ubicacion":
                return $"El {el.nombre} está en el grupo {el.grupo}, lo que indica su número de electrones de valencia, y en el periodo {el.periodo}, que indica cuántos niveles tiene su configuración electrónica.";

            case "masa":
                return $"Su masa atómica precisa es de {el.masa_atomica}. Este valor se usa en cálculos estequiométricos para determinar proporciones en reacciones.";

            case "estado":
                return $"Saber que el {el.nombre} es un {el.estado.ToLower()} nos ayuda a entender cómo almacenarlo y manipularlo, especialmente si trabajas en laboratorios.";

            case "tipo":
                return $"Como {el.tipo.ToLower()}, el {el.nombre} comparte propiedades con otros elementos similares, como su conductividad, brillo o reactividad química.";

            default:
                return $"El {el.nombre} tiene muchas otras propiedades interesantes. Por ejemplo: {el.descripcion}";
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
