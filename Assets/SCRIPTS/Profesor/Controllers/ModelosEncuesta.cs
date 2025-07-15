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

    // NUEVOS CAMPOS
    [SerializeField] private int tipopregunta;             // 0 = VerdaderoFalso, 1 = OpcionMultiple
    [SerializeField] private int tiempoSegundos;   // 15, 30, 45, 60

    [FirestoreProperty("textoPregunta")]
    public string TextoPregunta
    {
        get => textoPregunta;
        set => textoPregunta = value;
    }

    [FirestoreProperty("opciones")]
    public List<OpcionModelo> Opciones
    {
        get => opciones;
        set => opciones = value;
    }

    [FirestoreProperty("tipo")]
    public int Tipo
    {
        get => tipopregunta;
        set => tipopregunta = value;
    }

    [FirestoreProperty("tiempoSegundos")]
    public int TiempoSegundos
    {
        get => tiempoSegundos;
        set => tiempoSegundos = value;
    }
}


// --- MODELO PRINCIPAL PARA LA ENCUESTA (VERSIÓN CORREGIDA) ---
[FirestoreData]
[System.Serializable]
public class EncuestaModelo
{
    // --- CAMPOS PRIVADOS PARA JsonUtility (GUARDADO LOCAL) ---
    [SerializeField] private string id;
    [SerializeField] private string idcreador;
    [SerializeField] private string titulo;
    [SerializeField] private string descripcion;
    // [SerializeField] private bool activa; // Cambiaremos esto para que coincida con Firebase
    [SerializeField] private List<PreguntaModelo> preguntas = new List<PreguntaModelo>();
    [SerializeField] private string tipoEncuesta;
    [SerializeField] private string categoriaMision;
    [SerializeField] private string elementoMision;
    [SerializeField] private string fechaCreacionString;

    // --- NUEVOS CAMPOS PRIVADOS REQUERIDOS POR FIREBASE ---
    [SerializeField] private bool publicada;
    // No necesitamos un campo privado para la fecha si solo la vamos a leer.

    // --- PROPIEDADES PÚBLICAS PARA FIREBASE (Y PARA EL RESTO DEL CÓDIGO) ---
    [FirestoreProperty("id")]
    public string Id { get { return id; } set { id = value; } }

    [FirestoreProperty("idcreador")]
    public string IdCreador { get { return idcreador; } set { idcreador = value; } }

    [FirestoreProperty("titulo")]
    public string Titulo { get { return titulo; } set { titulo = value; } }

    [FirestoreProperty("descripcion")]
    public string Descripcion { get { return descripcion; } set { descripcion = value; } }

    [FirestoreProperty("preguntas")]
    public List<PreguntaModelo> Preguntas { get { return preguntas; } set { preguntas = value; } }

    [FirestoreProperty("tipoEncuesta")]
    public string TipoEncuesta { get { return tipoEncuesta; } set { tipoEncuesta = value; } }

    [FirestoreProperty("categoriaMision")]
    public string CategoriaMision { get { return categoriaMision; } set { categoriaMision = value; } }

    [FirestoreProperty("elementoMision")]
    public string ElementoMision { get { return elementoMision; } set { elementoMision = value; } }

    [FirestoreProperty("publicada")]
    public bool Publicada { get { return publicada; } set { publicada = value; } }

    [FirestoreProperty, ServerTimestamp]
    public Timestamp FechaCreacion { get; set; }

    public string FechaCreacionString
    {
        get { return fechaCreacionString; }
        set { fechaCreacionString = value; }
    }

    // Constructor vacío (ya lo tienes, está bien)
    public EncuestaModelo()
    {
        preguntas = new List<PreguntaModelo>();
    }

    // Constructor completo (ajustado para usar 'publicada')
    public EncuestaModelo(string id, string IdCreador, string titulo, string desc, List<PreguntaModelo> preguntas, bool pub, string tipo, string cat, string elem, string fechaCreacion)
    {
        this.Id = id;
        this.IdCreador = IdCreador;
        this.Titulo = titulo;
        this.Descripcion = desc;
        this.Preguntas = preguntas;
        this.Publicada = pub; // Usamos la nueva propiedad
        this.TipoEncuesta = tipo;
        this.CategoriaMision = (tipo == "Mision") ? cat : null;
        this.ElementoMision = (tipo == "Mision") ? elem : null;
        this.FechaCreacionString = fechaCreacion;
    }

    // --- MÉTODO ToDictionary() REINTEGRADO Y CORREGIDO ---
    public Dictionary<string, object> ToDictionary()
    {
        // ... (tu lógica interna de ToDictionary está bien) ...

        var dict = new Dictionary<string, object>
        {
            { "id", Id },
            { "idcreador", IdCreador },
            { "titulo", Titulo },
            { "descripcion", Descripcion },
            { "publicada", Publicada }, // Usar "publicada" para consistencia
            { "preguntas", /* preguntasList */ Preguntas }, // Firestore puede manejar la lista de modelos directamente
            { "tipoEncuesta", TipoEncuesta },
            { "categoriaMision", CategoriaMision },
            { "elementoMision", ElementoMision }
        };
        // Nota: No necesitas añadir fechaCreacion aquí, porque el atributo [ServerTimestamp]
        // le dice a Firebase que lo añada automáticamente en el servidor al guardar.

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