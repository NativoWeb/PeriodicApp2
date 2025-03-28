using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class CategoriaBotones : MonoBehaviour
{
    public Transform contenedorBotones; // Contenedor en el Scroll View
    public GameObject prefabBoton; // Prefab del botón
    public TextMeshProUGUI NombreUsuarioTMP;


    public TextMeshProUGUI tituloTMP;
    public TextMeshProUGUI nombreTMP;
    public TextMeshProUGUI descripcionTMP;
    public Button botonCambiarEscena; // Botón para cambiar de escena

    public string juegoEscenaActual;
    private Button botonSeleccionado;
    private Color colorNormal = Color.gray;
    private Color colorSeleccionado = new Color(81f / 255f, 178f / 255f, 124f / 255f); // #51B27C

    List<Categoria> categorias = new List<Categoria>
{
    new Categoria("Metales Alcalinos", "¡Prepárate para la reactividad extrema! ¿Podrás dominar estos metales explosivos?", "Escena_Alcalinos"),
    new Categoria("Metales Alcalinotérreos", "¡Más estables, pero igual de sorprendentes! Descubre su papel esencial en la química.", "Escena_Alcalinos"),
    new Categoria("Metales de Transición", "¡Los maestros del cambio! Explora los metales que forman los colores más vibrantes.", "Escena_Transicionales"),
    new Categoria("Metales Postransicionales", "¡Menos famosos, pero igual de útiles! ¿Cuánto sabes de estos metales versátiles?", "Escena_Postransicionales"),
    new Categoria("Metaloides", "¡Ni metal ni no metal! Atrévete a jugar con los elementos más enigmáticos.", "Escena_Metaloides"),
    new Categoria("No Metales Reactivos", "¡Elementos esenciales para la vida! Descubre su impacto en nuestro mundo.", "Escena_NoMetales"),
    new Categoria("Gases Nobles", "¡Silenciosos pero poderosos! ¿Podrás jugar con los elementos más estables?", "Escena_GasesNobles"),
    new Categoria("Lantánidos", "¡Los metales raros que hacen posible la tecnología moderna! ¿Aceptas el reto?", "Escena_Lantanidos"),
    new Categoria("Actínidos", "¡La energía del futuro! Juega con los elementos más radioactivos y misteriosos.", "Escena_Actinidos"),
    new Categoria("Elementos Desconocidos", "¡Aventúrate en lo desconocido! ¿Cuánto sabes de estos elementos misteriosos?", "Escena_Desconocidos")
};


    void Start()
    {
        
        Debug.Log("📌 Cargando categorías...");
        botonCambiarEscena.interactable = false; // Desactivar botón hasta que se seleccione una categoría
        CargarCategorias();
        string username = PlayerPrefs.GetString("DisplayName", "");
        NombreUsuarioTMP.text = username;

    }

    void CargarCategorias()
    {
        bool primerBotonSeleccionado = false;

        for (int i = 0; i < categorias.Count; i++)
        {
            Categoria categoria = categorias[i];
            GameObject nuevoBoton = CrearBoton(i + 1, categoria);

            if (!primerBotonSeleccionado)
            {
                SeleccionarNivel(nuevoBoton.GetComponent<Button>(), categoria);
                primerBotonSeleccionado = true;
            }
        }

        Debug.Log("✅ Categorías cargadas correctamente.");
    }

    GameObject CrearBoton(int numero, Categoria categoria)
    {
        GameObject nuevoBoton = Instantiate(prefabBoton, contenedorBotones);
        nuevoBoton.SetActive(true);

        TextMeshProUGUI textoBoton = nuevoBoton.GetComponentInChildren<TextMeshProUGUI>();
        Button boton = nuevoBoton.GetComponent<Button>();

        if (textoBoton != null)
            textoBoton.text = numero.ToString();

        boton.onClick.AddListener(() => SeleccionarNivel(boton, categoria));

        return nuevoBoton;
    }

    void SeleccionarNivel(Button boton, Categoria categoria)
    {
        if (botonSeleccionado != null)
            botonSeleccionado.GetComponent<Image>().color = colorNormal;

        botonSeleccionado = boton;
        botonSeleccionado.GetComponent<Image>().color = colorSeleccionado;

        tituloTMP.text = "Categoría " + (categorias.IndexOf(categoria) + 1 + ":");
        nombreTMP.text = categoria.Titulo;
        descripcionTMP.text = categoria.Descripcion;
        juegoEscenaActual = categoria.Escena;
        PlayerPrefs.SetString("juegoEscenaActual", categoria.Escena);

        botonCambiarEscena.interactable = true;
        botonCambiarEscena.onClick.RemoveAllListeners();
        botonCambiarEscena.onClick.AddListener(CambiarEscena);
    }


    void CambiarEscena()
    {
        Debug.Log($"Cambiando a la escena: {juegoEscenaActual}");

        if (!string.IsNullOrEmpty(juegoEscenaActual))
        {
            PlayerPrefs.SetString("CategoriaSeleccionada", nombreTMP.text);
            SceneManager.LoadScene(juegoEscenaActual);
        }
        else
        {
            Debug.LogWarning("No hay una escena asignada para esta categoría.");
        }
    }
}

[System.Serializable]
public class Categoria
{
    public string Titulo;
    public string Descripcion;
    public string Escena;

    public Categoria(string titulo, string descripcion, string escena)
    {
        Titulo = titulo;
        Descripcion = descripcion;
        Escena = escena;
    }
}
