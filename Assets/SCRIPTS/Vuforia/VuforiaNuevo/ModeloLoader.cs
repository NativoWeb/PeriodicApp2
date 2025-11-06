using UnityEngine;

public class ModeloLoader : MonoBehaviour
{
    public GameObject botonCambiarModelo; // Asigna en el Inspector

    private GameObject modeloAtomico;
    private GameObject modeloAplicacion;
    private bool mostrandoAtomico = true;

    [Header("Audio")]
    public bool autoPlayAudio = true; // Activa/desactiva reproducci�n autom�tica
    private AudioSource audioSource;

    public void InicializarCambioVisual(string nombreElemento, GameObject parent)
    {
        // Destruye anteriores si existieran
        if (modeloAtomico != null) Destroy(modeloAtomico);
        if (modeloAplicacion != null) Destroy(modeloAplicacion);

        // Cargar y reproducir audio
        CargarAudio(nombreElemento, parent);

        // Encuentra el contenedor at�mico generado
        Transform atomo = parent.transform.Find("AtomContainer");
        if (atomo != null)
        {
            modeloAtomico = atomo.gameObject;
        }

        // Carga modelo de aplicación usando búsqueda inteligente
        GameObject prefab = CargarModeloInteligente(nombreElemento);
        if (prefab != null)
        {
            modeloAplicacion = Instantiate(prefab, parent.transform);
            modeloAplicacion.transform.localPosition = Vector3.zero;
            modeloAplicacion.transform.localScale = Vector3.one * 1f; // ajustar tama�o
            modeloAplicacion.SetActive(false);
            Debug.Log($"✅ [ModeloLoader] Modelo 3D cargado correctamente para: {nombreElemento}");
        }
        else
        {
            Debug.LogError($"❌ [ModeloLoader] No se encontró modelo de aplicación para: {nombreElemento}");
        }

        // Carga textura usando búsqueda inteligente
        Texture2D textura = CargarTexturaInteligente(nombreElemento);
        if (textura != null && modeloAplicacion != null)
        {
            Renderer renderer = modeloAplicacion.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.mainTexture = textura;
                renderer.material = material;
                Debug.Log($"✅ [ModeloLoader] Textura '{textura.name}' aplicada correctamente a {nombreElemento}");
            }
        }
        else if (modeloAplicacion != null)
        {
            Debug.LogWarning($"⚠️ [ModeloLoader] No se encontró textura para: {nombreElemento}");
        }

        if (modeloAplicacion != null)
        {
            modeloAplicacion.AddComponent<Rotador>();
        }

        // Mostrar el botón solo si ambos modelos están disponibles
        if (botonCambiarModelo != null)
        {
            bool mostrarBoton = modeloAtomico != null && modeloAplicacion != null;
            botonCambiarModelo.SetActive(mostrarBoton);

            Debug.Log($"🔘 [ModeloLoader] Botón cambiar modelo: {(mostrarBoton ? "VISIBLE" : "OCULTO")} " +
                      $"(Modelo atómico: {(modeloAtomico != null ? "✓" : "✗")}, " +
                      $"Modelo 3D: {(modeloAplicacion != null ? "✓" : "✗")})");
        }
        else
        {
            Debug.LogError($"❌ [ModeloLoader] botonCambiarModelo NO está asignado en el Inspector!");
        }

        mostrandoAtomico = true;
    }

    // Método mejorado para cargar texturas con múltiples intentos
    private Texture2D CargarTexturaInteligente(string nombreElemento)
    {
        string basePath = "Modelos3DBlender/texturas/";
        Texture2D textura = null;

        // Lista de variaciones de nombres a probar (orden optimizado)
        string[] variaciones = new string[]
        {
            nombreElemento,                                          // Nombre original
            CorregirOrtografia(nombreElemento),                     // Correcciones ortográficas PRIMERO
            CapitalizarPrimeraLetra(nombreElemento),                // Primera letra mayúscula
            CapitalizarPrimeraLetra(CorregirOrtografia(nombreElemento)), // Corrección + mayúscula
            nombreElemento.ToLower(),                               // Todo minúsculas
            nombreElemento.ToUpper(),                               // Todo mayúsculas
            "e" + nombreElemento,                                   // Con 'e' al inicio (ej: scandio -> escandio)
            "e" + CorregirOrtografia(nombreElemento)                // e + corrección
        };

        // Intentar cargar con cada variación
        foreach (string variacion in variaciones)
        {
            if (string.IsNullOrEmpty(variacion)) continue; // Saltar variaciones vacías

            textura = Resources.Load<Texture2D>(basePath + variacion);
            if (textura != null)
            {
                if (variacion != nombreElemento)
                {
                    Debug.Log($"🔍 [ModeloLoader] Textura encontrada con variación: '{variacion}' para elemento '{nombreElemento}'");
                }
                return textura;
            }
        }

        Debug.LogError($"❌ [ModeloLoader] No se pudo encontrar textura para '{nombreElemento}'. Intentos: {string.Join(", ", variaciones)}");
        return null;
    }

    // Capitaliza la primera letra
    private string CapitalizarPrimeraLetra(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        return char.ToUpper(texto[0]) + texto.Substring(1).ToLower();
    }

    // Correcciones ortográficas conocidas
    private string CorregirOrtografia(string nombreElemento)
    {
        // Mapa de correcciones conocidas
        switch (nombreElemento.ToLower())
        {
            case "einsteinio": return "einstenio";     // Error ortográfico común
            case "scandio": return "escandio";         // Nombre alternativo
            case "eurupio": return "europio";          // Error ortográfico
            case "inidio": return "indio";             // Error ortográfico
            default: return nombreElemento;
        }
    }

    // Método mejorado para cargar modelos 3D con múltiples intentos
    private GameObject CargarModeloInteligente(string nombreElemento)
    {
        string basePath = "Modelos3DBlender/";
        GameObject modelo = null;

        // Lista de variaciones de nombres a probar (orden optimizado)
        string[] variaciones = new string[]
        {
            nombreElemento,                                          // Nombre original
            CorregirOrtografiaModelo(nombreElemento),               // Correcciones ortográficas PRIMERO
            CapitalizarPrimeraLetra(nombreElemento),                // Primera letra mayúscula
            CapitalizarPrimeraLetra(CorregirOrtografiaModelo(nombreElemento)), // Corrección + mayúscula
            nombreElemento.ToLower(),                               // Todo minúsculas
            nombreElemento.ToUpper(),                               // Todo mayúsculas
            "e" + nombreElemento,                                   // Con 'e' al inicio (ej: scandio -> escandio)
            nombreElemento + "_antes"                               // Con sufijo _antes
        };

        // Intentar cargar con cada variación
        foreach (string variacion in variaciones)
        {
            if (string.IsNullOrEmpty(variacion)) continue; // Saltar variaciones vacías

            modelo = Resources.Load<GameObject>(basePath + variacion);
            if (modelo != null)
            {
                if (variacion != nombreElemento)
                {
                    Debug.Log($"🔍 [ModeloLoader] Modelo 3D encontrado con variación: '{variacion}' para elemento '{nombreElemento}'");
                }
                return modelo;
            }
        }

        Debug.LogError($"❌ [ModeloLoader] No se pudo encontrar modelo 3D para '{nombreElemento}'. Intentos: {string.Join(", ", variaciones)}");
        return null;
    }

    // Correcciones ortográficas específicas para modelos (pueden ser diferentes a las texturas)
    private string CorregirOrtografiaModelo(string nombreElemento)
    {
        // Para modelos, einsteinio se escribe correctamente (no como einstenio)
        switch (nombreElemento.ToLower())
        {
            case "einstenio": return "einsteinio";     // La textura es einstenio, pero el modelo es einsteinio
            case "scandio": return "escandio";         // Nombre alternativo
            case "eurupio": return "europio";          // Error ortográfico
            case "inidio": return "indio";             // Error ortográfico
            default: return nombreElemento;
        }
    }

    private void CargarAudio(string nombreElemento, GameObject parent)
    {
        // Obtener o crear el componente AudioSource
        audioSource = parent.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = parent.AddComponent<AudioSource>();
        }

        // Cargar el archivo de audio desde Resources/Audios/
        AudioClip clip = Resources.Load<AudioClip>("Modelos3DBlender/audios/" + nombreElemento);
        if (clip == null)
        {
            Debug.LogError("No se encontro el audio: " + nombreElemento);
        }
        else
        {
            Debug.Log("Audio cargado: " + clip.name);
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    public void CambiarModelo()
    {
        if (modeloAtomico == null || modeloAplicacion == null) return;

        mostrandoAtomico = !mostrandoAtomico;
        modeloAtomico.SetActive(mostrandoAtomico);
        modeloAplicacion.SetActive(!mostrandoAtomico);
    }

    // Método público para detener el audio cuando se pierde el tracking
    public void DetenerAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("🔇 [ModeloLoader] Audio detenido");
        }
    }

    // Método público para limpiar los modelos
    public void LimpiarModelos()
    {
        if (modeloAtomico != null)
        {
            Destroy(modeloAtomico);
            modeloAtomico = null;
        }
        if (modeloAplicacion != null)
        {
            Destroy(modeloAplicacion);
            modeloAplicacion = null;
        }
        Debug.Log("🧹 [ModeloLoader] Modelos limpiados");
    }

    // Método público para ocultar el botón de cambiar modelo
    public void OcultarBotonCambiarModelo()
    {
        if (botonCambiarModelo != null)
        {
            botonCambiarModelo.SetActive(false);
            Debug.Log("🔘 [ModeloLoader] Botón cambiar modelo ocultado");
        }
    }
}