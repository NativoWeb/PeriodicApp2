using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using System;
using UnityEngine.UI;
using System.Linq;
using System.Globalization;
using System.Text;

public class ListaComunidadesManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject tarjetaPrefab;
    public Transform contenedor;
    public TMP_InputField inputBusqueda;
    public Button botonBuscar;
    public TMP_Text textoEstado; // Texto para mostrar mensajes de estado
    public GameObject panelEstado; // Panel contenedor del texto de estado

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
    private Dictionary<string, GameObject> tarjetasPorId = new Dictionary<string, GameObject>();

    // MODIFICADO: Variable para el idioma
    private string appIdioma;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // MODIFICADO: Obtener idioma y configurar textos
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");
        InicializarTextosUI();

        if (panelEstado != null) panelEstado.SetActive(false);

        if (auth.CurrentUser != null)
        {
            usuarioActualId = auth.CurrentUser.UserId;
            MostrarMensajeEstado(mensajeCargando, true);
            CargarComunidades();

            if (botonBuscar != null)
            {
                botonBuscar.onClick.AddListener(BuscarComunidades);
            }

            if (inputBusqueda != null)
            {
                inputBusqueda.onSubmit.AddListener(delegate { BuscarComunidades(); });
                inputBusqueda.onValueChanged.AddListener(delegate { BuscarComunidades(); });
            }
        }
        else
        {
            // MODIFICADO: Texto de error traducido
            string errorAuth = (appIdioma == "ingles") ? "No authenticated user" : "No hay usuario autenticado";
            MostrarMensajeEstado(errorAuth, true);
            Debug.LogWarning(errorAuth);
        }
    }

    // MODIFICADO: Nuevo método para centralizar la traducción de textos de la UI
    void InicializarTextosUI()
    {
        if (appIdioma == "ingles")
        {
            mensajeCargando = "Loading communities...";
            mensajeNoResultados = "No matches found";
            mensajeError = "Error loading data";
            mensajeListo = "{0} communities found";
            formatoMiembros = "{0} Members";
        }
        // Si no es "en", se mantienen los valores por defecto en español del inspector.
    }

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

        if (mostrar && (mensaje == string.Format(mensajeListo, todasComunidades.Count) || mensaje.Contains("éxito") || mensaje.Contains("success")))
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

    public void CargarComunidades()
    {
        foreach (Transform child in contenedor)
        {
            Destroy(child.gameObject);
        }
        tarjetasPorId.Clear();
        todasComunidades.Clear();

        MostrarMensajeEstado(mensajeCargando, true);

        Query query = db.Collection("comunidades");

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                MostrarMensajeEstado(mensajeError, true);
                Debug.LogError("Error al cargar comunidades: " + task.Exception);
                return;
            }

            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                todasComunidades.Add(doc);
                CrearTarjetaComunidad(doc);
            }

            MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
        });
    }

    private string ultimaBusqueda = "";
    private float tiempoEsperaFiltrado = 0.2f;
    private float ultimoTiempoFiltrado = 0;

    void BuscarComunidades()
    {
        if (todasComunidades.Count == 0)
        {
            // MODIFICADO: Texto traducido
            string noComunidadesMsg = (appIdioma == "ingles") ? "No communities loaded yet" : "No hay comunidades cargadas";
            MostrarMensajeEstado(noComunidadesMsg, true);
            return;
        }

        if (Time.time - ultimoTiempoFiltrado < tiempoEsperaFiltrado)
        {
            CancelInvoke("EjecutarBusqueda");
            Invoke("EjecutarBusqueda", tiempoEsperaFiltrado);
            return;
        }

        ultimoTiempoFiltrado = Time.time;
        EjecutarBusqueda();
    }

    void EjecutarBusqueda()
    {
        string terminoBusqueda = inputBusqueda?.text != null ? NormalizarTexto(inputBusqueda.text.Trim()) : "";

        if (terminoBusqueda == ultimaBusqueda) return;

        ultimaBusqueda = terminoBusqueda;

        foreach (var tarjeta in tarjetasPorId.Values)
        {
            tarjeta.SetActive(false);
        }

        if (string.IsNullOrWhiteSpace(terminoBusqueda))
        {
            foreach (var tarjeta in tarjetasPorId.Values)
            {
                tarjeta.SetActive(true);
            }
            MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
            return;
        }

        int resultadosEncontrados = 0;
        foreach (DocumentSnapshot comunidad in todasComunidades)
        {
            if (!comunidad.Exists || !comunidad.ContainsField("nombre")) continue;

            string nombre = NormalizarTexto(comunidad.GetValue<string>("nombre"));
            string descripcion = comunidad.ContainsField("descripcion") ?
                NormalizarTexto(comunidad.GetValue<string>("descripcion")) : "";

            if (nombre.Contains(terminoBusqueda) || descripcion.Contains(terminoBusqueda))
            {
                if (tarjetasPorId.TryGetValue(comunidad.Id, out GameObject tarjeta))
                {
                    tarjeta.SetActive(true);
                    resultadosEncontrados++;
                }
            }
        }

        // MODIFICADO: Texto de resultados traducido
        string msgResultados = (appIdioma == "ingles")
            ? $"{resultadosEncontrados} communities found"
            : $"Se encontraron {resultadosEncontrados} comunidades";

        MostrarMensajeEstado(resultadosEncontrados > 0 ? msgResultados : mensajeNoResultados, true);
    }

    string NormalizarTexto(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();
        foreach (char c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    void CrearTarjetaComunidad(DocumentSnapshot snapshot)
    {
        Dictionary<string, object> dataComunidad = snapshot.ToDictionary();
        GameObject tarjeta = Instantiate(tarjetaPrefab, contenedor);
        tarjeta.name = snapshot.Id;
        tarjetasPorId[snapshot.Id] = tarjeta;

        // Referencias a componentes de la tarjeta
        TMP_Text[] textos = tarjeta.GetComponentsInChildren<TMP_Text>();
        GameObject iconoPrivado = FindChildByName(tarjeta, "IconoPrivado");
        GameObject iconoPublico = FindChildByName(tarjeta, "IconoPublico");
        Image ImageComunidad = FindChildByName(tarjeta, "ImageComunidad")?.GetComponent<Image>();
        Button botonSolicitar = FindChildByName(tarjeta, "BotonSolicitar")?.GetComponent<Button>();
        TMP_Text textoBoton = botonSolicitar?.GetComponentInChildren<TMP_Text>();

        // MODIFICADO: Textos por defecto traducidos
        string nombre = dataComunidad.GetValueOrDefault("nombre", (appIdioma == "ingles") ? "No name" : "Sin nombre").ToString();
        string descripcion = dataComunidad.GetValueOrDefault("descripcion", (appIdioma == "ingles") ? "No description" : "Sin descripción").ToString();
        string tipo = dataComunidad.GetValueOrDefault("tipo", "publica").ToString().ToLower();
        string idComunidad = snapshot.Id;
        string ComunidadPath = dataComunidad.GetValueOrDefault("imagenRuta", "").ToString();

        Sprite ComunidadSprite = Resources.Load<Sprite>(ComunidadPath) ?? Resources.Load<Sprite>("Comunidades/ImagenComunidades/default");
        ImageComunidad.sprite = ComunidadSprite;

        // MODIFICADO: Formato de fecha y texto por defecto traducidos
        string fechaFormateada = (appIdioma == "ingles") ? "Unknown date" : "Fecha desconocida";
        if (dataComunidad.TryGetValue("fechaCreacion", out object fechaObj) && fechaObj is Timestamp timestamp)
        {
            CultureInfo culture = new CultureInfo(appIdioma == "ingles" ? "en-US" : "es-ES");
            fechaFormateada = timestamp.ToDateTime().ToString("dd MMMM yyyy", culture);
        }

        int cantidadMiembros = 0;
        if (dataComunidad.TryGetValue("miembros", out object miembrosObj) && miembrosObj is List<object> listaMiembros)
        {
            cantidadMiembros = listaMiembros.Count;
        }

        foreach (TMP_Text texto in textos)
        {
            switch (texto.gameObject.name)
            {
                case "TextoNombre": texto.text = nombre; break;
                case "TextoDescripcion": texto.text = descripcion; break;
                case "TextoFecha": texto.text = fechaFormateada; break;
                case "TextoMiembros": texto.text = string.Format(formatoMiembros, cantidadMiembros); break;
            }
        }

        if (iconoPrivado != null && iconoPublico != null)
        {
            iconoPrivado.SetActive(tipo == "privada");
            iconoPublico.SetActive(tipo != "privada");
        }

        if (botonSolicitar != null && auth.CurrentUser != null)
        {
            string uidUsuarioActual = auth.CurrentUser.UserId;
            bool esMiembro = (miembrosObj as List<object>)?.Contains(uidUsuarioActual) ?? false;

            botonSolicitar.onClick.RemoveAllListeners();

            if (esMiembro)
            {
                botonSolicitar.interactable = false;
                if (textoBoton != null) textoBoton.text = (appIdioma == "ingles") ? "Member" : "Miembro";
            }
            else if (tipo == "publica")
            {
                botonSolicitar.interactable = true;
                if (textoBoton != null) textoBoton.text = (appIdioma == "ingles") ? "Join" : "Unirme";
                botonSolicitar.onClick.AddListener(() => {
                    if (textoBoton != null) textoBoton.text = (appIdioma == "ingles") ? "Joining..." : "Uniendo...";
                    UnirseAComunidadDirectamente(idComunidad, uidUsuarioActual, botonSolicitar, textoBoton);
                });
            }
            else // Comunidad privada
            {
                db.Collection("solicitudes_comunidad")
                    .WhereEqualTo("idUsuario", uidUsuarioActual)
                    .WhereEqualTo("idComunidad", idComunidad)
                    .WhereEqualTo("estado", "pendiente")
                    .GetSnapshotAsync()
                    .ContinueWithOnMainThread(task =>
                    {
                        if (task.IsCompleted && !task.IsFaulted && task.Result.Count > 0)
                        {
                            botonSolicitar.interactable = false;
                            if (textoBoton != null) textoBoton.text = (appIdioma == "ingles") ? "Request sent" : "Solicitud enviada";
                        }
                        else
                        {
                            botonSolicitar.interactable = true;
                            if (textoBoton != null) textoBoton.text = (appIdioma == "ingles") ? "Request to join" : "Solicitar unirse";
                            botonSolicitar.onClick.AddListener(() =>
                            {
                                CrearSolicitudUnirse(nombre, idComunidad);
                                botonSolicitar.interactable = false;
                                if (textoBoton != null) textoBoton.text = (appIdioma == "ingles") ? "Waiting..." : "Esperando respuesta";
                            });
                        }
                    });
            }
        }
    }

    void UnirseAComunidadDirectamente(string comunidadId, string usuarioId, Button boton, TMP_Text textoBoton)
    {
        DocumentReference comunidadRef = db.Collection("comunidades").Document(comunidadId);
        comunidadRef.UpdateAsync("miembros", FieldValue.ArrayUnion(usuarioId))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    boton.interactable = false;
                    if (textoBoton != null) textoBoton.text = (appIdioma == "ingles") ? "Member" : "Miembro";
                    // MODIFICADO: Texto de éxito traducido
                    MostrarMensajeEstado((appIdioma == "ingles") ? "You have joined the community!" : "¡Te has unido a la comunidad!", true);
                    // Actualizar la lista local
                    comunidadRef.GetSnapshotAsync().ContinueWithOnMainThread(snapTask =>
                    {
                        if (snapTask.IsCompleted)
                        {
                            int index = todasComunidades.FindIndex(c => c.Id == comunidadId);
                            if (index >= 0) todasComunidades[index] = snapTask.Result;
                        }
                    });
                }
                else
                {
                    // MODIFICADO: Texto de error traducido
                    MostrarMensajeEstado((appIdioma == "ingles") ? "Error joining" : "Error al unirse", true);
                    Debug.LogError("Error al unirse: " + task.Exception?.Message);
                }
            });
    }

    void CrearSolicitudUnirse(string nombreComunidad, string idComunidad)
    {
        if (auth.CurrentUser == null)
        {
            MostrarMensajeEstado((appIdioma == "ingles") ? "User not authenticated" : "Usuario no autenticado", true);
            return;
        }

        string usuarioId = auth.CurrentUser.UserId;
        // MODIFICADO: Texto por defecto traducido
        string usuarioNombre = auth.CurrentUser.DisplayName ?? ((appIdioma == "ingles") ? "Anonymous" : "Anónimo");

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
            if (task.IsCompleted && !task.IsFaulted)
            {
                // MODIFICADO: Texto de éxito traducido
                MostrarMensajeEstado((appIdioma == "ingles") ? "Request sent successfully" : "Solicitud enviada con éxito", true);
            }
            else
            {
                // MODIFICADO: Texto de error traducido
                MostrarMensajeEstado((appIdioma == "ingles") ? "Error sending request" : "Error al enviar la solicitud", true);
                Debug.LogError("Error al crear solicitud: " + task.Exception?.Message);
            }
        });
    }

    GameObject FindChildByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.name == name) return child.gameObject;
            GameObject found = FindChildByName(child.gameObject, name);
            if (found != null) return found;
        }
        return null;
    }
}