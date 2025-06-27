using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

// --- MODELO PARA LAS OPCIONES ---
[FirestoreData]
[System.Serializable]
public class OpcionModelo
{
    // Campo para JsonUtility (local)
    [SerializeField] private string texto;
    [SerializeField] private bool esCorrecta;

    // Propiedad para Firebase (online)
    [FirestoreProperty("texto")]
    public string Texto { get { return texto; } set { texto = value; } }

    [FirestoreProperty("esCorrecta")]
    public bool EsCorrecta { get { return esCorrecta; } set { esCorrecta = value; } }
}

// --- MODELO PARA LAS PREGUNTAS ---
[FirestoreData]
[System.Serializable]
public class PreguntaModelo
{
    [SerializeField] private string textoPregunta;
    [SerializeField] private List<OpcionModelo> opciones = new List<OpcionModelo>();

    [FirestoreProperty("textoPregunta")]
    public string TextoPregunta { get { return textoPregunta; } set { textoPregunta = value; } }

    [FirestoreProperty("opciones")]
    public List<OpcionModelo> Opciones { get { return opciones; } set { opciones = value; } }

    public PreguntaModelo()
    {
        opciones = new List<OpcionModelo>();
    }
}

// --- MODELO PRINCIPAL PARA LA ENCUESTA (VERSI�N CON ToDictionary CORREGIDO) ---
[FirestoreData]
[System.Serializable]
public class EncuestaModelo
{
    // --- CAMPOS PRIVADOS PARA JsonUtility (GUARDADO LOCAL) ---
    [SerializeField] private string id;
    [SerializeField] private string titulo;
    [SerializeField] private string descripcion;
    [SerializeField] private bool publicada;
    [SerializeField] private List<PreguntaModelo> preguntas = new List<PreguntaModelo>();
    [SerializeField] private string tipoEncuesta;
    [SerializeField] private string categoriaMision;
    [SerializeField] private string elementoMision; 
    [SerializeField] private string codigoUnion;

    // --- PROPIEDADES P�BLICAS PARA FIREBASE (Y PARA EL RESTO DEL C�DIGO) ---
    [FirestoreProperty("id")]
    public string Id { get { return id; } set { id = value; } }

    [FirestoreProperty("codigoUnion")]
    public string CodigoUnion { get { return codigoUnion; } set { codigoUnion = value; } }

    [FirestoreProperty("titulo")]
    public string Titulo { get { return titulo; } set { titulo = value; } }

    [FirestoreProperty("descripcion")]
    public string Descripcion { get { return descripcion; } set { descripcion = value; } }

    [FirestoreProperty("publicada")]
    public bool Publicada { get { return publicada; } set { publicada = value; } }

    [FirestoreProperty("preguntas")]
    public List<PreguntaModelo> Preguntas { get { return preguntas; } set { preguntas = value; } }

    [FirestoreProperty("tipoEncuesta")]
    public string TipoEncuesta { get { return tipoEncuesta; } set { tipoEncuesta = value; } }

    [FirestoreProperty("categoriaMision")]
    public string CategoriaMision { get { return categoriaMision; } set { categoriaMision = value; } }

    [FirestoreProperty("elementoMision")]
    public string ElementoMision { get { return elementoMision; } set { elementoMision = value; } }

    // Constructor vac�o
    public EncuestaModelo()
    {
        preguntas = new List<PreguntaModelo>();
    }

    // Constructor completo
    public EncuestaModelo(string id, string titulo, string desc, List<PreguntaModelo> preguntas, bool pub, string tipo, string cat, string elem)
    {
        this.Id = id;
        this.Titulo = titulo;
        this.Descripcion = desc;
        this.Preguntas = preguntas;
        this.Publicada = pub;
        this.TipoEncuesta = tipo;
        this.CategoriaMision = (tipo == "Mision") ? cat : null;
        this.ElementoMision = (tipo == "Mision") ? elem : null;
    }

    // --- M�TODO ToDictionary() REINTEGRADO Y CORREGIDO ---
    public Dictionary<string, object> ToDictionary()
    {
        // 1. Convertir la lista de PreguntaModelo a una lista de diccionarios
        var preguntasList = new List<object>();
        foreach (var pregunta in Preguntas)
        {
            var opcionesList = new List<object>();
            foreach (var opcion in pregunta.Opciones)
            {
                opcionesList.Add(new Dictionary<string, object>
                {
                    { "texto", opcion.Texto },
                    { "esCorrecta", opcion.EsCorrecta }
                });
            }

            preguntasList.Add(new Dictionary<string, object>
            {
                { "textoPregunta", pregunta.TextoPregunta },
                { "opciones", opcionesList }
            });
        }

        // 2. Crear el diccionario principal
        var dict = new Dictionary<string, object>
        {
            { "id", Id },
            { "titulo", Titulo },
            { "descripcion", Descripcion },
            { "publicada", Publicada },
            { "preguntas", preguntasList }, // A�adimos la lista de diccionarios de preguntas
            { "tipoEncuesta", TipoEncuesta },
            { "categoriaMision", CategoriaMision },
            { "elementoMision", ElementoMision }
        };

        return dict;
    }
}

// La clase ListaSimple no necesita cambios
[System.Serializable]
public class ListaSimple
{
    public List<string> ids;
    public ListaSimple(List<string> lista) { ids = lista; }
}