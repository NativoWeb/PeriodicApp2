using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using System;
using UnityEngine.UI;

public class ListaComunidadesManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject tarjetaPrefab;
    public Transform contenedor;
    public TMP_InputField inputBusqueda;
    public Button botonBuscar;
    public TMP_Text textoEstado; // Nuevo: Texto para mostrar mensajes de estado
    public GameObject panelEstado; // Nuevo: Panel contenedor del texto de estado

    [Header("Configuración de Mensajes")]
    public string mensajeCargando = "Cargando comunidades...";
    public string mensajeNoResultados = "No se encontraron coincidencias";
    public string mensajeError = "Error al cargar los datos";
    public string mensajeListo = "{0} comunidades encontradas";

    [Header("Componentes de Tarjeta")]
    public string formatoMiembros = "{0} Miembros";

    private string usuarioActualId;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private List<DocumentSnapshot> todasComunidades = new List<DocumentSnapshot>();

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Inicializar panel de estado
        if (panelEstado != null) panelEstado.SetActive(false);

        if (auth.CurrentUser != null)
        {
            usuarioActualId = auth.CurrentUser.UserId;
            MostrarMensajeEstado(mensajeCargando, true);
            CargarComunidadesDelUsuario();

            if (botonBuscar != null)
            {
                botonBuscar.onClick.AddListener(BuscarComunidades);
            }

            if (inputBusqueda != null)
            {
                inputBusqueda.onSubmit.AddListener(delegate { BuscarComunidades(); });
            }
        }
        else
        {
            MostrarMensajeEstado("No hay usuario autenticado", true);
            Debug.LogWarning("No hay usuario autenticado");
        }
    }
   

    // Nuevo método: Muestra mensajes de estado al usuario
    void MostrarMensajeEstado(string mensaje, bool mostrar = true)
    {
        if (textoEstado != null)
        {
            textoEstado.text = mensaje;
        }

        if (panelEstado != null)
        {
            panelEstado.SetActive(mostrar);
        }

        // Oculta automáticamente mensajes de éxito después de 3 segundos
        if (mostrar && mensaje == string.Format(mensajeListo, todasComunidades.Count))
        {
            Invoke("OcultarPanelEstado", 3f);
        }
    }

    void OcultarPanelEstado()
    {
        if (panelEstado != null && textoEstado.text != mensajeError && textoEstado.text != mensajeCargando)
        {
            panelEstado.SetActive(false);
        }
    }

    void CargarComunidadesDelUsuario()
    {
        Query query = db.Collection("comunidades");

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                MostrarMensajeEstado(mensajeError, true);
                Debug.LogError("Error al cargar comunidades");
                return;
            }

            todasComunidades.Clear();

            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                todasComunidades.Add(doc);
            }

            MostrarTodasComunidades();
            MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
        });
    }

    void BuscarComunidades()
    {
        if (inputBusqueda == null || string.IsNullOrWhiteSpace(inputBusqueda.text))
        {
            MostrarTodasComunidades();
            return;
        }

        string terminoBusqueda = inputBusqueda.text.Trim().ToLower();
        int resultadosEncontrados = 0;

        foreach (Transform child in contenedor)
        {
            Destroy(child.gameObject);
        }

        foreach (DocumentSnapshot comunidad in todasComunidades)
        {
            string nombre = comunidad.ContainsField("nombre") ? comunidad.GetValue<string>("nombre").ToLower() : "";
            string descripcion = comunidad.ContainsField("descripcion") ? comunidad.GetValue<string>("descripcion").ToLower() : "";

            if (nombre.Contains(terminoBusqueda) || descripcion.Contains(terminoBusqueda))
            {
                CrearTarjetaComunidad(comunidad); // Pasamos el snapshot directamente
                resultadosEncontrados++;
            }
        }

        if (resultadosEncontrados > 0)
        {
            MostrarMensajeEstado($"Se encontraron {resultadosEncontrados} resultados", true);
        }
        else
        {
            MostrarMensajeEstado(mensajeNoResultados, true);
        }
    }




    void MostrarTodasComunidades()
    {
        foreach (Transform child in contenedor)
        {
            Destroy(child.gameObject);
        }

        Query query = db.Collection("comunidades");
        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                MostrarMensajeEstado(mensajeError, true);
                Debug.LogError("Error al cargar comunidades");
                return;
            }

            todasComunidades.Clear(); // Limpiar la lista de comunidades

            // Iterar sobre los documentos de la colección
            foreach (DocumentSnapshot snapshot in task.Result.Documents)
            {
                // Convertir el DocumentSnapshot a un Dictionary<string, object>
                CrearTarjetaComunidad(snapshot);
            }

            MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
        });
    }

    void CrearTarjetaComunidad(DocumentSnapshot snapshot)
    {
        Dictionary<string, object> dataComunidad = snapshot.ToDictionary();

        GameObject tarjeta = Instantiate(tarjetaPrefab, contenedor);

        TMP_Text[] textos = tarjeta.GetComponentsInChildren<TMP_Text>();
        GameObject iconoPrivado = FindChildByName(tarjeta, "IconoPrivado");
        GameObject iconoPublico = FindChildByName(tarjeta, "IconoPublico");

        string nombre = dataComunidad.GetValueOrDefault("nombre", "Sin nombre").ToString();
        string descripcion = dataComunidad.GetValueOrDefault("descripcion", "Sin descripción").ToString();
        string tipo = dataComunidad.GetValueOrDefault("tipo", "publica").ToString().ToLower();
        string idComunidad = snapshot.Id; // ✅ Lo extraemos del snapshot

        string fechaFormateada = "Fecha desconocida";
        if (dataComunidad.TryGetValue("fechaCreacion", out object fechaObj))
        {
            if (fechaObj is Timestamp timestamp)
                fechaFormateada = timestamp.ToDateTime().ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
            else if (fechaObj is string fechaString)
                fechaFormateada = fechaString;
        }

        int cantidadMiembros = 0;
        List<object> miembros = new List<object>();
        if (dataComunidad.TryGetValue("miembros", out object miembrosObj) && miembrosObj is List<object> listaMiembros)
        {
            miembros = listaMiembros;
            cantidadMiembros = miembros.Count;
        }

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

        if (iconoPrivado != null && iconoPublico != null)
        {
            iconoPrivado.SetActive(tipo == "privada");
            iconoPublico.SetActive(tipo != "privada");
        }

        Button botonSolicitar = FindChildByName(tarjeta, "BotonSolicitar")?.GetComponent<Button>();
        TMP_Text textoBoton = botonSolicitar?.GetComponentInChildren<TMP_Text>();

        if (botonSolicitar != null && auth.CurrentUser != null)
        {
            string uidUsuarioActual = auth.CurrentUser.UserId;
            bool esMiembro = miembros.Contains(uidUsuarioActual);

            if (esMiembro)
            {
                botonSolicitar.interactable = false;
                botonSolicitar.GetComponent<Image>().color = new Color32(0, 200, 0, 255); // Verde
                if (textoBoton != null) textoBoton.text = "Miembro";
            }
            else
            {
                // Buscar si ya envió solicitud
                db.Collection("solicitudes_comunidad")
                    .WhereEqualTo("idUsuario", uidUsuarioActual)
                    .WhereEqualTo("idComunidad", idComunidad)
                    .WhereEqualTo("estado", "pendiente")
                    .GetSnapshotAsync()
                    .ContinueWithOnMainThread(task =>
                    {
                        if (task.IsCompleted && task.Result.Count > 0)
                        {
                            // Ya hay una solicitud enviada
                            botonSolicitar.interactable = false;
                            botonSolicitar.GetComponent<Image>().color = new Color32(55, 189, 247, 255); // Azul claro
                            if (textoBoton != null) textoBoton.text = "Solicitud enviada";
                        }
                        else
                        {
                            // No hay solicitud, permitir enviar
                            botonSolicitar.interactable = true;
                            botonSolicitar.GetComponent<Image>().color = new Color32(55, 189, 247, 255); // Azul claro
                            if (textoBoton != null) textoBoton.text = "Solicitar unirse";

                            botonSolicitar.onClick.AddListener(() =>
                            {
                                CrearSolicitudUnirse(nombre, idComunidad);
                                botonSolicitar.interactable = false;
                                if (textoBoton != null) textoBoton.text = "Solicitud enviada";
                            });
                        }
                    });
            }

        }


        void CrearSolicitudUnirse(string nombreComunidad, string idComunidad)
        {
            if (auth.CurrentUser == null)
            {
                MostrarMensajeEstado("Usuario no autenticado", true);
                return;
            }

            string usuarioId = auth.CurrentUser.UserId;
            string usuarioNombre = auth.CurrentUser.DisplayName ?? "Anónimo";

            Dictionary<string, object> solicitud = new Dictionary<string, object>
    {
        { "idUsuario", usuarioId },
        { "nombreUsuario", usuarioNombre },
        { "idComunidad", idComunidad },
        { "nombreComunidad", nombreComunidad },
        { "estado", "pendiente" },
        { "fechaSolicitud", Timestamp.GetCurrentTimestamp() }
    };

            db.Collection("solicitudes_comunidad").AddAsync(solicitud).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                {
                    MostrarMensajeEstado("Solicitud enviada con éxito", true);
                }
                else
                {
                    MostrarMensajeEstado("Error al enviar la solicitud", true);
                    Debug.LogError("Error al crear solicitud: " + task.Exception?.Message);
                }
            });
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
}