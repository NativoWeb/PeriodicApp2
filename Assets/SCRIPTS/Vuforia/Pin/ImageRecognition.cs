using UnityEngine;
using UnityEngine.Audio;
using Vuforia;

public class ImageRecognition : MonoBehaviour
{
    private bool logroDesbloqueado = false;
    private ObserverBehaviour trackable;
    private GuardarMisionCompletada controlador;
    private string ruta;

    public GameObject imageTargetPrefab;

    [Header("Audio")]
    public bool autoPlayAudio = true; // Activa/desactiva reproducción automática
    private AudioSource audioSource;

    void Start()
    {
        // Buscar el GuardarMisionCompletada en la escena
        controlador = GuardarMisionCompletada.instancia;
        if (controlador == null)
        {
            controlador = FindAnyObjectByType<GuardarMisionCompletada>();
        }

        string numeroAtomico = PlayerPrefs.GetString("NumeroAtomico", "").Trim();

        trackable = GetComponent<ObserverBehaviour>();

        ruta = PlayerPrefs.GetString("CargarVuforia", "");
        // Si este ImageTarget no es el elemento de la misión, se desactiva
        // Comparamos por número atómico para evitar problemas con acentos o variantes de nombre
        if (string.IsNullOrEmpty(numeroAtomico) || !trackable.TargetName.Trim().ToLower().StartsWith(numeroAtomico + "_"))
        {
            gameObject.SetActive(false);
        }

        if (trackable)
        {
            trackable.OnTargetStatusChanged += OnImageDetected;
        }
    }

    private void OnImageDetected(ObserverBehaviour observer, TargetStatus status)
    {
        if (status.Status == Status.TRACKED && !logroDesbloqueado)
        {
            Debug.Log($"¡Imagen detectada! {trackable.TargetName} desbloqueado.");
            // Usar ElementoSeleccionado del JSON (normalizado) para evitar discrepancias con el nombre del target Vuforia
            string audioName = NormalizarNombre(PlayerPrefs.GetString("ElementoSeleccionado", "").Trim());
            Debug.Log($"🔊 Buscando audio: AudiosPines/{audioName}");
            CargarAudio(audioName, imageTargetPrefab);
            logroDesbloqueado = true;
            DesbloquearLogro(trackable.TargetName);
        }
    }

    private string NormalizarNombre(string nombre)
    {
        return nombre
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("ü", "u").Replace("ñ", "n")
            .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
            .Replace("Ü", "U").Replace("Ñ", "N")
            .ToUpper();
    }

    void DesbloquearLogro(string elemento)
    {
        Debug.Log($"🏆 [ImageRecognition] Logro desbloqueado: {elemento}");

        // Activar el panel de botón y habilitar el botón de completar misión
        if (controlador != null)
        {
            if (controlador.PanelBotonUI != null)
            {
                controlador.PanelBotonUI.SetActive(true);
                Debug.Log("✅ [ImageRecognition] PanelBotonUI activado");
            }

            if (controlador.botonCompletarMision != null)
            {
                controlador.botonCompletarMision.interactable = true;
                Debug.Log("✅ [ImageRecognition] Botón de completar misión habilitado");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ [ImageRecognition] GuardarMisionCompletada no encontrado en la escena");
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
        AudioClip clip = Resources.Load<AudioClip>("AudiosPines/" + nombreElemento);
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
}
