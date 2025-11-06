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

        string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "").Trim().ToLower();

        trackable = GetComponent<ObserverBehaviour>();

        ruta = PlayerPrefs.GetString("CargarVuforia", "");
        // Si este ImageTarget no es el elemento de la misión, se desactiva
        if (trackable.TargetName.Trim().ToLower() != elemento.Trim().ToLower())
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
            // Cargar y reproducir audio
            CargarAudio(trackable.TargetName.Trim().ToLower(), imageTargetPrefab);
            logroDesbloqueado = true;
            DesbloquearLogro(trackable.TargetName);
        }
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
