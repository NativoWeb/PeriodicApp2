using UnityEngine;

public class ModeloLoader : MonoBehaviour
{
    public GameObject botonCambiarModelo; // Asigna en el Inspector

    private GameObject modeloAtomico;
    private GameObject modeloAplicacion;
    private bool mostrandoAtomico = true;

    public void InicializarCambioVisual(string nombreElemento, GameObject parent)
    {
        // Destruye anteriores si existieran
        if (modeloAtomico != null) Destroy(modeloAtomico);
        if (modeloAplicacion != null) Destroy(modeloAplicacion);

        // Encuentra el contenedor atómico generado
        Transform atomo = parent.transform.Find("AtomContainer");
        if (atomo != null)
        {
            modeloAtomico = atomo.gameObject;
        }

        // Carga modelo de aplicación desde Resources/ModelosAplicacion/
        GameObject prefab = Resources.Load<GameObject>("Modelos3DBlender/" + nombreElemento);
        if (prefab != null)
        {
            modeloAplicacion = Instantiate(prefab, parent.transform);
            modeloAplicacion.transform.localPosition = Vector3.zero;
            modeloAplicacion.transform.localScale = Vector3.one * 50f; // ajustar tamaño
            modeloAplicacion.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No se encontró modelo de aplicación para: " + nombreElemento);
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

        // Mostrar el botón solo si ambos modelos están disponibles
        botonCambiarModelo.SetActive(modeloAtomico != null && modeloAplicacion != null);
        mostrandoAtomico = true;
    }

    public void CambiarModelo()
    {
        if (modeloAtomico == null || modeloAplicacion == null) return;

        mostrandoAtomico = !mostrandoAtomico;
        modeloAtomico.SetActive(mostrandoAtomico);
        modeloAplicacion.SetActive(!mostrandoAtomico);
    }
}
