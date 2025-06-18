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

        // Carga modelo de aplicaci�n desde Resources/ModelosAplicacion/
        GameObject prefab = Resources.Load<GameObject>("Modelos3DBlender/" + nombreElemento);
        if (prefab != null)
        {
            modeloAplicacion = Instantiate(prefab, parent.transform);
            modeloAplicacion.transform.localPosition = Vector3.zero;
            modeloAplicacion.transform.localScale = Vector3.one * 1f; // ajustar tama�o
            modeloAplicacion.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No se encontr� modelo de aplicaci�n para: " + nombreElemento);
        }

        // Carga textura desde Resources/Textures/[nombreElemento].png
        Texture2D textura = Resources.Load<Texture2D>("Modelos3DBlender/texturas/" + nombreElemento);
        if (textura != null && modeloAplicacion != null)
        {
            Renderer renderer = modeloAplicacion.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.mainTexture = textura;
                renderer.material = material;
            }
        }

        modeloAplicacion.AddComponent<Rotador>();

        // Mostrar el bot�n solo si ambos modelos est�n disponibles
        botonCambiarModelo.SetActive(modeloAtomico != null && modeloAplicacion != null);
        mostrandoAtomico = true;
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
}