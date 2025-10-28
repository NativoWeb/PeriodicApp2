using UnityEngine;
using UnityEngine.Audio;
using Vuforia;

public class ImageRecognition : MonoBehaviour
{
    private bool logroDesbloqueado = false;
    private ObserverBehaviour trackable;
    private ImageTargetSpawner spawner;
    private string ruta;

    public GameObject imageTargetPrefab;

    [Header("Audio")]
    public bool autoPlayAudio = true; // Activa/desactiva reproducci�n autom�tica
    private AudioSource audioSource;

    void Start()
    {
        spawner = FindObjectOfType<ImageTargetSpawner>();

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
        if (status.Status == Status.TRACKED)
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

        // Habilitar el botón de completar misión
        if (spawner != null && spawner.botonCompletarMision != null)
        {
            spawner.botonCompletarMision.interactable = true;
            Debug.Log("✅ [ImageRecognition] Botón de completar misión habilitado");
        }

        // Mostrar el panel de botón de completar misión
        if (spawner != null && spawner.PanelBotonCompletarUI != null)
        {
            spawner.PanelBotonCompletarUI.SetActive(true);
            Debug.Log("✅ [ImageRecognition] Panel de completar misión mostrado");
        }
        else
        {
            Debug.LogWarning("⚠️ [ImageRecognition] PanelBotonCompletarUI no está asignado en ImageTargetSpawner");
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
