using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using System;

public class ComunidadManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject tarjetaPrefab;
    public Transform contenedor;

    [Header("Componentes de Tarjeta")]
    public string formatoMiembros = "{0} Miembros";

    private string usuarioActualId;
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            usuarioActualId = auth.CurrentUser.UserId;
            CargarComunidadesDelUsuario();
        }
        else
        {
            Debug.LogWarning("No hay usuario autenticado");
        }
    }

    void CargarComunidadesDelUsuario()
    {
        Query query = db.Collection("comunidades")
                      .WhereArrayContains("miembros", usuarioActualId);

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error al cargar comunidades");
                return;
            }

            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                Dictionary<string, object> data = doc.ToDictionary();
                CrearTarjetaComunidad(data);
            }
        });
    }
    void CrearTarjetaComunidad(Dictionary<string, object> dataComunidad)
    {
        // Instanciar la tarjeta
        GameObject tarjeta = Instantiate(tarjetaPrefab, contenedor);

        // Obtener referencias a los componentes de UI
        TMP_Text[] textos = tarjeta.GetComponentsInChildren<TMP_Text>();
        GameObject iconoPrivado = FindChildByName(tarjeta, "IconoPrivado");
        GameObject iconoPublico = FindChildByName(tarjeta, "IconoPublico");

        // Extraer datos (con valores por defecto)
        string nombre = dataComunidad.GetValueOrDefault("nombre", "Sin nombre").ToString();
        string descripcion = dataComunidad.GetValueOrDefault("descripcion", "Sin descripción").ToString();
        string tipo = dataComunidad.GetValueOrDefault("tipo", "publica").ToString().ToLower();

        // Manejo de la fecha
        string fechaFormateada = "Fecha desconocida";
        if (dataComunidad.TryGetValue("fechaCreacion", out object fechaObj))
        {
            if (fechaObj is Timestamp timestamp)
            {
                DateTime fecha = timestamp.ToDateTime();
                // Formatear la fecha en español (ejemplo: "15 enero 2023")
                fechaFormateada = fecha.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
            }
            else if (fechaObj is string fechaString)
            {
                fechaFormateada = fechaString; // Si ya es un string, lo usamos tal cual
            }
        }

        int cantidadMiembros = 0;
        if (dataComunidad.TryGetValue("miembros", out object miembrosObj) && miembrosObj is List<object> miembros)
        {
            cantidadMiembros = miembros.Count;
        }

        // Configurar UI
        foreach (TMP_Text texto in textos)
        {
            switch (texto.gameObject.name)
            {
                case "TextoNombre":
                    texto.text = nombre;
                    break;
                case "TextoDescripcion":
                    texto.text = descripcion;
                    break;
                case "TextoFecha":
                    texto.text = fechaFormateada;
                    break;
                case "TextoMiembros":
                    texto.text = string.Format(formatoMiembros, cantidadMiembros);
                    break;
                case "TextoTipo":
                    texto.text = tipo == "privada" ? "Privada" : "Pública";
                    break;
            }
        }

        // Configurar iconos
        if (iconoPrivado != null && iconoPublico != null)
        {
            iconoPrivado.SetActive(tipo == "privada");
            iconoPublico.SetActive(tipo != "privada");
        }
    }
    // Método auxiliar para encontrar hijos por nombre
    GameObject FindChildByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.name == name)
                return child.gameObject;

            GameObject found = FindChildByName(child.gameObject, name);
            if (found != null)
                return found;
        }
        return null;
    }
}