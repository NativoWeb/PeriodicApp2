using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.IO;
using System.Collections;

public class GestorMisiones : MonoBehaviour
{
    [Header("Panel Principal del Elemento")]
    public GameObject PanelMisiones;
    public GameObject PanelDatosElemento;
    public TextMeshProUGUI txtSimbolo;
    public TextMeshProUGUI txtNombre;
    public TextMeshProUGUI txtNumeroAtomico;
    public Image ImgElemento;

    [Header("Botones de Cambio")]
    public Button btnMisiones;
    public Button btnInformacion;

    [Header("UI Misiones")]
    public GameObject prefabMision;
    public Transform contenedorMisiones;


    [Header("Botón de Regreso a categorias")]
    public Button BtnCategorias;
    public GameObject PanelCategorias;
    public GameObject PanelElemento;
    public GameObject PanelInformacion;

    private string JsonIdioma;
    string appIdioma;
    string categoriaSeleccionada;
    private bool estaCargando = false;

    JSONNode jsonDataInformacion;
    JSONNode jsonDataMisiones;

    // Mapea cada categoría a un Color32 único
    private static readonly Dictionary<string, Color32> ColoresPorCategoria = new Dictionary<string, Color32>
{
    { "Metales Alcalinos",        new Color32(0x41, 0xB9, 0xDE, 0xFF) },
    { "Metales Alcalinotérreos",  new Color32(0xF0, 0x81, 0x2F, 0xFF) },
    { "Metales de Transición",     new Color32(0xED, 0x6D, 0x9D, 0xFF) },
    { "Metales postransicionales", new Color32(0x72, 0x65, 0xAA, 0xFF) },
    { "Metaloides",                new Color32(0xCD, 0xCB, 0xCC, 0xFF) },
    { "No Metales",      new Color32(0x79, 0xBB, 0x51, 0xFF) },
    { "Gases Nobles",              new Color32(0x00, 0xA2, 0x93, 0xFF) },
    { "Lantánidos",                new Color32(0xC0, 0x20, 0x3C, 0xFF) },
    { "Actinoides",                new Color32(0x33, 0x37, 0x8E, 0xFF) },
    { "Propiedades desconocidas",  new Color32(0xC2, 0x89, 0x58, 0xFF) },
};
    private bool datosCargados = false;

    void Awake()
    {
        // Setup de listeners que no dependen de JSON
        btnInformacion.onClick.AddListener(IrAIformacion);
        BtnCategorias.onClick.AddListener(RegresaraCategorias);
    }

    void OnEnable()
    {
        StartCoroutine(IniciarCargaYRefresco());
    }

    void Start()
    {
        // Start sirve como un respaldo en caso de que OnEnable se ejecute
        // antes de que todo esté listo. La bandera "estaCargando" evitará la doble ejecución.
        StartCoroutine(IniciarCargaYRefresco());
    }

    public IEnumerator CargarJSONYContinuar()
    {
        yield return CargarJSON( JsonIdioma, nodo => jsonDataInformacion = nodo);
        yield return CargarJSON("Json_Misiones.json", nodo => jsonDataMisiones = nodo);
    }

    IEnumerator CargarJSON(string nombreArchivo, System.Action<JSONNode> callback)
    {
        string rutaPersistente = Path.Combine(Application.persistentDataPath, nombreArchivo);

        // Intentar cargar desde persistentDataPath
        if (File.Exists(rutaPersistente))
        {
            string contenido = File.ReadAllText(rutaPersistente);
            JSONNode jsonCargado = JSON.Parse(contenido);

            // Verificar si tiene la estructura correcta (para Json_Misiones.json)
            if (nombreArchivo == "Json_Misiones.json")
            {
                // Si tiene la clave vieja "Misiones_Categorias", borramos el archivo
                if (jsonCargado != null && jsonCargado.HasKey("Misiones_Categorias"))
                {
                    Debug.LogWarning($"⚠️ Archivo {nombreArchivo} en persistentDataPath tiene estructura antigua. Eliminando...");
                    File.Delete(rutaPersistente);
                    // Continuar para cargar desde Resources
                }
                else if (jsonCargado != null && jsonCargado.HasKey("Misiones"))
                {
                    // Estructura correcta
                    callback(jsonCargado);
                    yield break;
                }
            }
            else
            {
                // Para otros archivos, cargar normalmente
                callback(jsonCargado);
                yield break;
            }
        }

        // Si no existe o fue eliminado, lo cargo de Resources
        string recurso = $"Plantillas_Json/{Path.GetFileNameWithoutExtension(nombreArchivo)}";
        TextAsset txt = Resources.Load<TextAsset>(recurso);

        // espero un frame para evitar race conditions
        yield return null;

        if (txt != null)
        {
            callback(JSON.Parse(txt.text));
            Debug.Log($"✅ {nombreArchivo} cargado desde Resources");
        }
        else
        {
            Debug.LogError($"❌ No se encontró {nombreArchivo} en Resources/{recurso}");
            callback(null);
        }
    }

    private IEnumerator IniciarCargaYRefresco()
    {
        // 1. Prevenir doble ejecución
        if (estaCargando) yield break;
        estaCargando = true;

        // 2. *** LÓGICA DE PREPARACIÓN (MOVIDA AQUÍ) ***
        //    Primero, determinamos el contexto y preparamos las variables.
        if (PlayerPrefs.HasKey("VolverAElemento"))
        {
            Debug.Log("✅ [GestorMisiones] Se detectó un retorno. Estableciendo contexto...");
            string elementoDeRetorno = PlayerPrefs.GetString("VolverAElemento");
            string categoriaDeRetorno = PlayerPrefs.GetString("VolverACategoria");

            PlayerPrefs.SetString("ElementoSeleccionado", elementoDeRetorno);
            PlayerPrefs.SetString("CategoriaSeleccionada", categoriaDeRetorno);

            PlayerPrefs.DeleteKey("VolverAElemento");
            PlayerPrefs.DeleteKey("VolverACategoria");
            PlayerPrefs.Save();
        }

        // Asignamos las variables de clase AHORA que el PlayerPrefs es correcto.
        categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");

        if (appIdioma == "español")
        {
            JsonIdioma = "Json_Informacion.json";
        }
        else
        {
            JsonIdioma = "Json_Informacion_en.json";
            categoriaSeleccionada = devolverCatTrad(categoriaSeleccionada);
        }

        // 3. Ahora que las variables están listas, cargamos los JSON
        yield return StartCoroutine(CargarJSONYContinuar());
        datosCargados = true;

        // 4. Refrescamos la UI
        RefrescarUI();

        // 5. Verificación final
        VerificarRetornoDeMision();

        estaCargando = false;
    }

    public void RefrescarUI()
    {
        CargarInfoElementoSeleccionado();
        CargarDatosElementoSeleccionado();
    }


    void CargarInfoElementoSeleccionado()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");
        categoriaSeleccionada = devolverCatTrad(categoriaSeleccionada);
        string elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado");

        // Cambiar color del panel según categoría
        if (PanelDatosElemento != null)
        {
            var imgPanel = PanelDatosElemento.GetComponent<Image>();
            if (imgPanel != null)
            {
                Color32 colorCat = ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var c) ? c : new Color32(255, 255, 255, 255);
                imgPanel.color = colorCat;
            }
        }

        // Asignar color según el elemento

        if (elementoSeleccionado == "Astato" || elementoSeleccionado == "Téneso")
        {
            ImgElemento.sprite = Resources.Load<Sprite>("ImagenesElementos/desconocidoVerde");
        }
        else if (elementoSeleccionado == "Proactinio" || elementoSeleccionado == "Neptunio" || elementoSeleccionado == "Berkelio" ||
                 elementoSeleccionado == "Einstenio" || elementoSeleccionado == "Fermio" || elementoSeleccionado == "Mendelevio" ||
                 elementoSeleccionado == "Nobelio" || elementoSeleccionado == "Lawrencio" || elementoSeleccionado == "Prometio")
        {
            ImgElemento.sprite = Resources.Load<Sprite>("ImagenesElementos/desconocidoAzul");
        }
        else if (elementoSeleccionado == "Rutherfordio" || elementoSeleccionado == "Dubnio" || elementoSeleccionado == "Seaborgio" ||
                 elementoSeleccionado == "Bohrio" || elementoSeleccionado == "Hassio" || elementoSeleccionado == "Meitnerio" ||
                 elementoSeleccionado == "Darmstatio" || elementoSeleccionado == "Roentgenio" || elementoSeleccionado == "Copernicio" ||
                 elementoSeleccionado == "Nihonio" || elementoSeleccionado == "Flerovio" || elementoSeleccionado == "Moscovio" || elementoSeleccionado == "Livermorio")
        {
            ImgElemento.sprite = Resources.Load<Sprite>("ImagenesElementos/desconocidoRosado");
        }
        else
        {
            // Intentar cargar la imagen del elemento
            Sprite spriteElemento = Resources.Load<Sprite>("ImagenesElementos/" + elementoSeleccionado);
            ImgElemento.sprite = spriteElemento;
        }


        // Cambiar color del boton de misiones
        if (btnMisiones != null)
        {
            var ImgMisiones = btnMisiones.GetComponent<Image>();
            if (ImgMisiones != null)
            {
                Color32 colorCat = ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var c) ? c : new Color32(255, 255, 255, 255);
                ImgMisiones.color = colorCat;
            }
        }

        // Verificar si el JSON cargado existe
        if (jsonDataInformacion == null ||
            !jsonDataInformacion.HasKey("Informacion") ||
            !jsonDataInformacion["Informacion"].HasKey("Categorias"))
        {
            Debug.LogError("El JSON de información cargado desde archivo es inválido.");
            return;
        }

        var categorias = jsonDataInformacion["Informacion"]["Categorias"];

        if (!categorias.HasKey(categoriaSeleccionada) ||
            !categorias[categoriaSeleccionada].HasKey(elementoSeleccionado))
        {
            Debug.LogError("No se encontró el elemento seleccionado.");
            return;
        }

        var elementoJson = categorias[categoriaSeleccionada][elementoSeleccionado];

        txtSimbolo.text = elementoJson["simbolo"];
        txtNombre.text = elementoJson["nombre"];
        txtNumeroAtomico.text = elementoJson["numero_atomico"].Value;
    }

    void CargarDatosElementoSeleccionado()
    {

        string elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado");

        Debug.Log($"🔍 Cargando misiones para: Categoría='{categoriaSeleccionada}', Elemento='{elementoSeleccionado}'");

        // Color del panel según categoría
        if (PanelDatosElemento != null)
        {
            var imgPanel = PanelDatosElemento.GetComponent<Image>();
            if (imgPanel != null && ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var colorCat))
            {
                imgPanel.color = colorCat;
            }
        }

        // Verificaciones paso a paso con logs
        if (jsonDataMisiones == null)
        {
            Debug.LogError("❌ jsonDataMisiones es NULL");
            return;
        }

        if (!jsonDataMisiones.HasKey("Misiones"))
        {
            Debug.LogError("❌ El JSON no tiene la clave 'Misiones'");
            Debug.Log($"Claves disponibles: {string.Join(", ", jsonDataMisiones.Keys)}");
            return;
        }

        var misionesNode = jsonDataMisiones["Misiones"];

        if (!misionesNode.HasKey("Categorias"))
        {
            Debug.LogError("❌ El JSON no tiene la clave 'Categorias' dentro de 'Misiones'");
            return;
        }

        var categoriasNode = misionesNode["Categorias"];

        if (!categoriasNode.HasKey(categoriaSeleccionada))
        {
            Debug.LogError($"❌ No se encontró la categoría '{categoriaSeleccionada}'");
            Debug.Log($"Categorías disponibles: {string.Join(", ", categoriasNode.Keys)}");
            return;
        }

        var categoriaNode = categoriasNode[categoriaSeleccionada];

        if (!categoriaNode.HasKey("Elementos"))
        {
            Debug.LogError($"❌ La categoría '{categoriaSeleccionada}' no tiene 'Elementos'");
            return;
        }

        var elementosNode = categoriaNode["Elementos"];

        if (!elementosNode.HasKey(elementoSeleccionado))
        {
            Debug.LogError($"❌ No se encontró el elemento '{elementoSeleccionado}' en la categoría '{categoriaSeleccionada}'");
            Debug.Log($"Elementos disponibles: {string.Join(", ", elementosNode.Keys)}");
            return;
        }

        Debug.Log($"✅ Elemento '{elementoSeleccionado}' encontrado en la categoría '{categoriaSeleccionada}'");

        var misionesArray = jsonDataMisiones["Misiones"]["Categorias"][categoriaSeleccionada]["Elementos"][elementoSeleccionado]["misiones"].AsArray;

        // Desactivar el contenedor para evitar destello visual durante la actualización
        contenedorMisiones.gameObject.SetActive(false);

        LimpiarMisiones(); // Limpia el contenido previo

        if (appIdioma == "español")
        {
            foreach (JSONNode misionJson in misionesArray)
            {
                Mision mision = new Mision
                {
                    id = misionJson["id"].AsInt,
                    titulo = misionJson["titulo"],
                    descripcion = misionJson["descripcion"],
                    tipo = misionJson["tipo"],
                    rutaEscena = misionJson["rutaescena"],
                    completada = misionJson["completada"].AsBool
                };

                switch (mision.tipo)
                {
                    case "AR":
                        mision.xp = 10;
                        mision.logoMision = "logosMision/ar";
                        break;
                    case "QR":
                        mision.xp = 10;
                        mision.logoMision = "logosMision/qr";
                        break;
                    case "Juego":
                        mision.xp = 12;
                        mision.logoMision = "logosMision/juego";
                        break;
                    case "Quiz":
                        mision.xp = 12;
                        mision.logoMision = "logosMision/quiz";
                        break;
                    case "Evaluacion":
                        mision.xp = 12;
                        mision.logoMision = "logosMision/evaluacion";
                        break;
                    default:
                        mision.xp = 0;
                        mision.logoMision = "logosMision/default";
                        break;
                }

                PlayerPrefs.SetInt("xp_mision", mision.xp); // Puedes quitarlo si no es necesario

                CrearPrefabMision(mision);
            }
        }
        else
        {
            foreach (JSONNode misionJson in misionesArray)
            {
                Mision mision = new Mision
                {
                    id = misionJson["id"].AsInt,
                    titulo = misionJson["titulo_en"],
                    descripcion = misionJson["descripcion_en"],
                    tipo = misionJson["tipo"],
                    rutaEscena = misionJson["rutaescena"],
                    completada = misionJson["completada"].AsBool
                };

                switch (mision.tipo)
                {
                    case "AR":
                        mision.xp = 10;
                        mision.logoMision = "logosMision/ar";
                        break;
                    case "QR":
                        mision.xp = 10;
                        mision.logoMision = "logosMision/qr";
                        break;
                    case "Juego":
                        mision.xp = 12;
                        mision.logoMision = "logosMision/juego";
                        break;
                    case "Quiz":
                        mision.xp = 12;
                        mision.logoMision = "logosMision/quiz";
                        break;
                    case "Evaluacion":
                        mision.xp = 12;
                        mision.logoMision = "logosMision/evaluacion";
                        break;
                    default:
                        mision.xp = 0;
                        mision.logoMision = "logosMision/default";
                        break;
                }

                PlayerPrefs.SetInt("xp_mision", mision.xp); // Puedes quitarlo si no es necesario

                CrearPrefabMision(mision);
            }
        }

        // Reactivar el contenedor después de cargar todas las misiones
        contenedorMisiones.gameObject.SetActive(true);
    }

    public string devolverCatTrad(string categoriaSeleccionada)
    {
        switch (categoriaSeleccionada)
        {
            case "Alkali Metals":
                return "Metales Alcalinos";

            case "Alkaline Earth Metals":
                return "Metales Alcalinotérreos";

            case "Transition Metals":
                return "Metales de Transición";

            case "Post-transition Metals":
                return "Metales postransicionales";

            case "Metalloids":
                return "Metaloides";

            case "Nonmetals":
                return "No Metales";

            case "Noble Gases":
                return "Gases Nobles";

            case "Lanthanides":
                return "Lantánidos";

            case "Actinides":
                return "Actinoides";

            case "Unknown Properties":
                return "Propiedades desconocidas";

            default:
                return categoriaSeleccionada;
        }
    }

    void CrearPrefabMision(Mision mision)
    {
        string elementoseleccionado = PlayerPrefs.GetString("ElementoSeleccionado");
        GameObject nuevaMision = Instantiate(prefabMision, contenedorMisiones);
        UI_Mision uiMision = nuevaMision.GetComponent<UI_Mision>();
        uiMision.ConfigurarMision(mision);

        Button botonMision = nuevaMision.GetComponentInChildren<Button>();

        // Asignar evento para cambiar de escena
        botonMision.onClick.AddListener(() => CargarEscenaMision(mision.rutaEscena, elementoseleccionado, mision.id));
    }

    void CargarEscenaMision(string nombreEscena, string elemento, int idMision)
    {
        if (string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogError("No se encontró una escena válida para esta misión.");
            return;
        }

        // Guardar el estado de la misión antes de cambiar de escena
        PlayerPrefs.SetString("ElementoSeleccionado", elemento);
        PlayerPrefs.SetString("SimboloElemento", txtSimbolo.text);
        PlayerPrefs.SetInt("MisionActual", idMision);
        if (idMision == 1)
        {
            PlayerPrefs.SetString("CargarVuforia", "Misiones");
        }
        PlayerPrefs.Save();

        // Cargar la escena de la misión
        SceneManager.LoadScene(nombreEscena);
    }

    void LimpiarMisiones()
    {
        // Usar una lista temporal para evitar modificar la colección mientras iteramos
        var children = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in contenedorMisiones)
        {
            children.Add(child.gameObject);
        }

        // Destruir todos los objetos inmediatamente
        foreach (var child in children)
        {
            DestroyImmediate(child);
        }
    }

    private void RegresaraCategorias()
    {
        PanelElemento.SetActive(true);
        PanelInformacion.SetActive(false);
        PanelMisiones.SetActive(false);
    }

    public void IrAIformacion()
    {
        PanelInformacion.SetActive(true);
        PanelMisiones.SetActive(false);
        PanelElemento.SetActive(false);
        PanelCategorias.SetActive(false);
    }

    void VerificarRetornoDeMision()
    {
        // Verificar si tenemos datos de una misión completada
        string elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado", "");
        int misionActual = PlayerPrefs.GetInt("MisionActual", -1);

        // Si hay un elemento seleccionado y una misión actual, significa que venimos de una misión
        if (!string.IsNullOrEmpty(elementoSeleccionado) && misionActual != -1)
        {
            Debug.Log($"Retornando de misión {misionActual} del elemento {elementoSeleccionado}");

            // Activar el panel de misiones del elemento
            PanelElemento.SetActive(false);
            PanelCategorias.SetActive(false);
            PanelMisiones.SetActive(true);
            PanelInformacion.SetActive(false);
        }
    }
}
