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
    private Dictionary<string, GameObject> tarjetasPorId = new Dictionary<string, GameObject>(); // Diccionario para rastrear tarjetas por ID

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
            CargarComunidades();

            // Podemos mantener el botón de búsqueda como alternativa
            if (botonBuscar != null)
            {
                botonBuscar.onClick.AddListener(BuscarComunidades);
            }

            // Configurar búsqueda en tiempo real mientras se escribe
            if (inputBusqueda != null)
            {
                // Mantener onSubmit para compatibilidad
                inputBusqueda.onSubmit.AddListener(delegate { BuscarComunidades(); });

                // Agregar evento que se dispara con cada cambio del texto
                inputBusqueda.onValueChanged.AddListener(delegate { BuscarComunidades(); });
            }
        }
        else
        {
            MostrarMensajeEstado("No hay usuario autenticado", true);
            Debug.LogWarning("No hay usuario autenticado");
        }
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

    // Método simplificado para cargar las comunidades una sola vez
    public void CargarComunidades()
    {
        // Limpiar la UI
        foreach (Transform child in contenedor)
        {
            Destroy(child.gameObject);
        }
        tarjetasPorId.Clear();
        todasComunidades.Clear();

        // Mostrar mensaje de carga
        MostrarMensajeEstado(mensajeCargando, true);

        // Realizar consulta única a Firestore
        Query query = db.Collection("comunidades");

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                MostrarMensajeEstado(mensajeError, true);
                Debug.LogError("Error al cargar comunidades: " + task.Exception);
                return;
            }

            // Almacenar los datos
            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                todasComunidades.Add(doc);
                CrearTarjetaComunidad(doc);
            }

            // Mostrar mensaje de éxito
            MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
        });
    }

    // Variables para optimizar la búsqueda en tiempo real
    private string ultimaBusqueda = "";
    private float tiempoEsperaFiltrado = 0.2f;
    private float ultimoTiempoFiltrado = 0;

    // Método optimizado para búsqueda en tiempo real
    void BuscarComunidades()
    {
        // Validar que haya comunidades cargadas
        if (todasComunidades.Count == 0)
        {
            MostrarMensajeEstado("No hay comunidades cargadas", true);
            return;
        }

        // Evitar filtrados excesivos cuando se escribe rápido
        if (Time.time - ultimoTiempoFiltrado < tiempoEsperaFiltrado)
        {
            CancelInvoke("EjecutarBusqueda");
            Invoke("EjecutarBusqueda", tiempoEsperaFiltrado);
            return;
        }

        ultimoTiempoFiltrado = Time.time;
        EjecutarBusqueda();
    }

    // Ejecuta la búsqueda efectiva
    void EjecutarBusqueda()
    {
        // Obtener el término de búsqueda
        string terminoBusqueda = inputBusqueda?.text != null ? NormalizarTexto(inputBusqueda.text.Trim()) : "";

        // Si el término de búsqueda no ha cambiado, no hacer nada
        if (terminoBusqueda == ultimaBusqueda)
        {
            return;
        }

        ultimaBusqueda = terminoBusqueda;

        // Ocultar todas las tarjetas primero
        foreach (var tarjeta in tarjetasPorId.Values)
        {
            tarjeta.SetActive(false);
        }

        // Si el campo está vacío, mostrar todas
        if (string.IsNullOrWhiteSpace(terminoBusqueda))
        {
            foreach (var tarjeta in tarjetasPorId.Values)
            {
                tarjeta.SetActive(true);
            }
            MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
            return;
        }

        // Buscar por coincidencia de texto
        int resultadosEncontrados = 0;

        foreach (DocumentSnapshot comunidad in todasComunidades)
        {
            // Verificar que el documento existe y tiene campo nombre
            if (!comunidad.Exists || !comunidad.ContainsField("nombre"))
            {
                continue;
            }

            string nombre = NormalizarTexto(comunidad.GetValue<string>("nombre"));
            string descripcion = comunidad.ContainsField("descripcion") ?
                NormalizarTexto(comunidad.GetValue<string>("descripcion")) : "";

            // Buscar por nombre o descripción
            if (nombre.Contains(terminoBusqueda) || descripcion.Contains(terminoBusqueda))
            {
                // Mostrar la tarjeta si existe
                if (tarjetasPorId.TryGetValue(comunidad.Id, out GameObject tarjeta))
                {
                    tarjeta.SetActive(true);
                    resultadosEncontrados++;
                }
            }
        }

        // Mostrar mensaje de resultados
        MostrarMensajeEstado(
            resultadosEncontrados > 0
                ? $"Se encontraron {resultadosEncontrados} comunidades"
                : mensajeNoResultados,
            true);
    }

    // Método para normalizar texto (quitar acentos y convertir a minúsculas)
    string NormalizarTexto(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

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

        return stringBuilder.ToString()
                           .Normalize(NormalizationForm.FormC)
                           .ToLowerInvariant();
    }

    void CrearTarjetaComunidad(DocumentSnapshot snapshot)
    {
        Dictionary<string, object> dataComunidad = snapshot.ToDictionary();

        GameObject tarjeta = Instantiate(tarjetaPrefab, contenedor);
        tarjeta.name = snapshot.Id;

        // Registrar la tarjeta en el diccionario
        tarjetasPorId[snapshot.Id] = tarjeta;

        TMP_Text[] textos = tarjeta.GetComponentsInChildren<TMP_Text>();
        GameObject iconoPrivado = FindChildByName(tarjeta, "IconoPrivado");
        GameObject iconoPublico = FindChildByName(tarjeta, "IconoPublico");
        Image ImageComunidad = FindChildByName(tarjeta, "ImageComunidad")?.GetComponent<Image>();

        string nombre = dataComunidad.GetValueOrDefault("nombre", "Sin nombre").ToString();
        string descripcion = dataComunidad.GetValueOrDefault("descripcion", "Sin descripción").ToString();
        string tipo = dataComunidad.GetValueOrDefault("tipo", "publica").ToString().ToLower();
        string idComunidad = snapshot.Id;
        string ComunidadPath = dataComunidad.GetValueOrDefault("imagenRuta", "").ToString();// consigo la ruta de la imagen 

        Sprite ComunidadSprite = Resources.Load<Sprite>(ComunidadPath) ?? Resources.Load<Sprite>("Comunidades/ImagenComunidades/default");// cargo la imagen desde resources

        ImageComunidad.sprite = ComunidadSprite; // la pongo en la IU

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

            botonSolicitar.onClick.RemoveAllListeners(); // Limpiar listeners previos

            if (esMiembro)
            {
                // Configurar botón como "Miembro"
                botonSolicitar.interactable = false;
                if (textoBoton != null) textoBoton.text = "Miembro";
            }
            else if (tipo == "publica")
            {
                // Lógica para comunidades públicas
                botonSolicitar.interactable = true;
                if (textoBoton != null) textoBoton.text = "Unirme";

                botonSolicitar.onClick.AddListener(() => {
                    botonSolicitar.interactable = false;
                    if (textoBoton != null) textoBoton.text = "Uniendo...";
                    UnirseAComunidadDirectamente(idComunidad, uidUsuarioActual, botonSolicitar, textoBoton);
                });
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
                            if (textoBoton != null) textoBoton.text = "Solicitud enviada";
                        }
                        else
                        {
                            // No hay solicitud, permitir enviar
                            botonSolicitar.interactable = true;
                            if (textoBoton != null) textoBoton.text = "Solicitar unirse";

                            botonSolicitar.onClick.AddListener(() =>
                            {
                                CrearSolicitudUnirse(nombre, idComunidad);
                                botonSolicitar.interactable = false;
                                if (textoBoton != null) textoBoton.text = "Esperando respuesta";
                            });
                        }
                    });
            }
        }
    }

    void UnirseAComunidadDirectamente(string comunidadId, string usuarioId, Button boton, TMP_Text textoBoton)
    {
        DocumentReference comunidadRef = db.Collection("comunidades").Document(comunidadId);

        // Usamos FieldValue.ArrayUnion para agregar el usuario sin duplicados
        comunidadRef.UpdateAsync("miembros", FieldValue.ArrayUnion(usuarioId))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    // Actualización UI inmediata
                    boton.interactable = false;
                    if (textoBoton != null) textoBoton.text = "Miembro";

                    MostrarMensajeEstado("¡Te has unido a la comunidad!", true);

                    // Actualizar la lista local para reflejar el cambio
                    comunidadRef.GetSnapshotAsync().ContinueWithOnMainThread(snapTask =>
                    {
                        if (snapTask.IsCompleted)
                        {
                            // Actualizar la copia local
                            int index = todasComunidades.FindIndex(c => c.Id == comunidadId);
                            if (index >= 0)
                            {
                                todasComunidades[index] = snapTask.Result;
                            }
                        }
                    });
                }
                else
                {
                    MostrarMensajeEstado("Error al unirse", true);
                    Debug.LogError("Error al unirse: " + task.Exception?.Message);
                }
            });
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