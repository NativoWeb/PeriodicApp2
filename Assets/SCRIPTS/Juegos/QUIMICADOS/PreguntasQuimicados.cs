using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using static ControladorEncuesta;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using DG.Tweening.Core.Easing;

// Usa un namespace para evitar colisiones de nombres
namespace QuizGame
{
    // JSON root y clases de datos
    [Serializable]
    public class CuestionarioRoot
    {
        public string titulo;
        public string descripcion;
        public List<CategoriaData> categorias;
    }

    [Serializable]
    public class CategoriaData
    {
        public string nombre;
        public List<PreguntaData> preguntas;
    }

    [Serializable]
    public class PreguntaData
    {
        public string pregunta;
        public List<string> opciones;
        public int respuestaCorrecta;
    }

    public class PreguntasQuimicados : MonoBehaviour
    {
        [Header("UI")]
        public Text TextTimer;  // Referencia al componente Text de la UI
        public TextMeshProUGUI txtPregunta;
        public TextMeshProUGUI txtFeedBack;
        public GameObject panelFeedBack;
        public Toggle[] opciones;

        [Header("Colores de Respuesta")]
        public Color colorCorrecto = Color.green;
        public Color colorIncorrecto = Color.red;
        public Color colorNormal = Color.white; // Color por defecto
        public Color colorFondoCorrecto = new Color(0.66f, 0.81f, 0.30f); // Verde claro
        public Color colorFondoIncorrecto = new Color(0.89f, 0.31f, 0.31f); // Rojo claro

        [Header("JSON")]
        public string nombreArchivoResources = "Quimicados";
        private List<Pregunta> preguntasFiltradas;

        // Temporizador variables
        public float tiempoRestante;  // Tiempo inicial del temporizador en segundos (10 segundos)
        private bool preguntaFinalizada = false;  // Flag para saber si la pregunta ha sido finalizada (cuando se pasa a la siguiente pregunta)

        // Datos de la pregunta cargada
        private PreguntaData preguntaCargada;
        private bool eventosToggleHabilitados = false;
        // FIREBASE
        private FirebaseFirestore db;
        private FirebaseAuth auth;

        private string categoriaSel;
        private int preguntaActual = 0;

        private string partidaId;

        void Start()
        {
            // Inicializar Firebase
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;

            // Cargar preguntas
            CargarPreguntaDesdeJSON();
            MostrarPregunta();

            ActualizarTextoTiempo();
            partidaId = PlayerPrefs.GetString("partidaIdQuimicados");
            // Configurar progreso
            if (preguntasFiltradas == null)
                preguntasFiltradas = new List<Pregunta>();
        }
        // M�todo para manejar el temporizador
        void Update()
        {
            ActualizarTextoTiempo();

            // Solo actualizar el temporizador si la pregunta no ha sido finalizada
            if (!preguntaFinalizada)
            {

                if (tiempoRestante > 0)
                {
                    tiempoRestante -= Time.deltaTime; // Reduce el tiempo
                }
                else  // Verifica que la pregunta a�n no se ha respondido
                {
                    preguntaFinalizada = true; // Evita que el c�digo se ejecute varias veces en un solo frame
                    RevealAnswers();
                }
            }

        }
        void ActualizarTextoTiempo()
        {
            TextTimer.text = tiempoRestante.ToString("00") + " Segundos";
        }
        void CargarPreguntaDesdeJSON()
        {
            // Cargar archivo JSON desde Resources
            TextAsset archivoJSON = Resources.Load<TextAsset>(nombreArchivoResources);
            if (archivoJSON == null)
            {
                return;
            }

            // Deserializar al objeto raíz
            CuestionarioRoot root = JsonUtility.FromJson<CuestionarioRoot>(archivoJSON.text);
            if (root == null || root.categorias == null)
            {
                return;
            }

            // Obtener categoría seleccionada
            categoriaSel = PlayerPrefs.GetString("CategoriaRuleta");
            CategoriaData categoria = root.categorias
                .FirstOrDefault(c => c.nombre.Equals(categoriaSel, StringComparison.OrdinalIgnoreCase));

            if (categoria == null)
            {
                return;
            }

            if (categoria.preguntas == null || categoria.preguntas.Count == 0)
            {
                return;
            }

            // Seleccionar una pregunta aleatoria
            System.Random rnd = new System.Random();
            preguntaCargada = categoria.preguntas[rnd.Next(categoria.preguntas.Count)];
        }

        void MostrarPregunta()
        {
            // 1) Limpio y configuro listeners
            ConfigurarToggleListeners();

            // 2) Desmarco y habilito interacción
            desmarcarToggle();

            // 3) Ya permito que los toggles disparen eventos
            eventosToggleHabilitados = true;

            ActivarInteractividadOpciones();

            if (preguntaCargada == null)
            {
                return;
            }

            // Mostrar texto de la pregunta
            txtPregunta.text = preguntaCargada.pregunta;

            // Preparar y mezclar opciones
            var listaOpciones = preguntaCargada.opciones
                .Select((texto, idx) => (texto, idx))
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            // Calcular nuevo índice de la respuesta correcta
            int nuevoIndiceCorrecto = listaOpciones.FindIndex(x => x.idx == preguntaCargada.respuestaCorrecta);
            preguntaCargada.respuestaCorrecta = nuevoIndiceCorrecto;

            // Asignar a toggles
            for (int i = 0; i < opciones.Length; i++)
            {
                if (i < listaOpciones.Count)
                {
                    opciones[i].gameObject.SetActive(true);
                    opciones[i].isOn = false;
                    opciones[i].GetComponentInChildren<TextMeshProUGUI>().text = listaOpciones[i].texto;
                }
                else
                {
                    opciones[i].gameObject.SetActive(false);
                }
            }
        }
        // Nuevo método para revelar respuestas
        async Task RevealAnswers()
        {
            // deshabilita futuros eventos
            eventosToggleHabilitados = false;

            // resalta la opción correcta en verde, las demás en rojo
            for (int i = 0; i < opciones.Length; i++)
            {
                var img = opciones[i].image;
                if (i == preguntaCargada.respuestaCorrecta)
                    img.color = colorCorrecto;
                else
                    img.color = colorIncorrecto;

                // opcional: desactiva la interactividad
                opciones[i].interactable = false;
            }

            // muestra el panel de feedback como “Tiempo agotado”
            if (panelFeedBack != null)
            {
                panelFeedBack.SetActive(true);
                panelFeedBack.GetComponent<Image>().color = colorFondoIncorrecto;
            }
            if (txtFeedBack != null)
                txtFeedBack.text = "Tiempo agotado";

            CambiarTurno(partidaId);
            await Task.Delay(2000);
            SceneManager.LoadScene("QuimicadosGame");
        }

        async Task ValidarRespuesta(int indiceSeleccionado)
        {
            bool correcto = (indiceSeleccionado == preguntaCargada.respuestaCorrecta);
            // Visual
            if (correcto)
            {
                bool logro = PlayerPrefs.GetInt("CompletarLogro") == 1;

                if (logro)
                {
                    cambiarLogro();
                    PlayerPrefs.SetInt("CompletarLogro", 0);
                }
                // Mostrar solo la correcta
                for (int i = 0; i < opciones.Length; i++)
                {
                    if (i == preguntaCargada.respuestaCorrecta)
                        opciones[i].image.color = colorCorrecto;
                    else
                        opciones[i].image.color = colorNormal;
                }

                // Activar feedback visual
                panelFeedBack.SetActive(true);
                panelFeedBack.GetComponent<Image>().color = colorFondoCorrecto;
                txtFeedBack.text = "Correcto";
                PlayerPrefs.SetInt("wasCorrect", 1);
            }
            else
            {
                // Mostrar todas las opciones y resaltar correcta/incorrecta
                for (int i = 0; i < opciones.Length; i++)
                {
                    opciones[i].gameObject.SetActive(true);

                    if (i == indiceSeleccionado)
                        opciones[i].image.color = colorIncorrecto;
                    else if (i == preguntaCargada.respuestaCorrecta)
                        opciones[i].image.color = colorCorrecto;
                    else
                        opciones[i].image.color = colorNormal;
                }

                // Activar feedback visual
                panelFeedBack.SetActive(true);
                panelFeedBack.GetComponent<Image>().color = colorFondoIncorrecto;
                txtFeedBack.text = "Incorrecto";
                PlayerPrefs.SetInt("wasIncorrect", 1);
                CambiarTurno(partidaId);
            }
            DesactivarInteractividadOpciones();
            await Task.Delay(2000);
            SceneManager.LoadScene("QuimicadosGame");
        }
        void CambiarTurno(string partidaId)
        {
            var docRef = db.Collection("partidasQuimicados").Document(partidaId);

            docRef.GetSnapshotAsync()
                  .ContinueWithOnMainThread(task =>
                  {
                      if (task.IsFaulted)
                      {
                          Debug.LogError($"Error al leer partida: {task.Exception}");
                          return;
                      }

                      DocumentSnapshot snapshot = task.Result;
                      if (!snapshot.Exists)
                      {
                          Debug.LogError("La partida no existe.");
                          return;
                      }

                      // Leer campos
                      string jugadorA = snapshot.GetValue<string>("jugadorA");
                      string jugadorB = snapshot.GetValue<string>("jugadorB");
                      string turnoActual = snapshot.GetValue<string>("turnoActual");

                      // Obtener UID del usuario conectado
                      string yo = auth.CurrentUser.UserId;

                      // Calcular siguiente turno
                      string siguienteTurno;
                      if (yo == jugadorA)
                          siguienteTurno = jugadorB;
                      else if (yo == jugadorB)
                          siguienteTurno = jugadorA;
                      else
                      {
                          Debug.LogWarning("Este usuario no es ni A ni B en la partida.");
                          return;
                      }

                      // Si ya es el turno de ese mismo, no actualices
                      if (turnoActual == siguienteTurno)
                      {
                          Debug.Log("El turno ya está asignado correctamente.");
                          return;
                      }

                      // Actualizar campo "turno"
                      docRef.UpdateAsync("turnoActual", siguienteTurno)
                    .ContinueWithOnMainThread(updateTask =>
                      {
                          if (updateTask.IsFaulted)
                              Debug.LogError($"Error al actualizar turno: {updateTask.Exception}");
                          else
                          {
                              Debug.Log($"✅ Turno cambiado a {(siguienteTurno == jugadorA ? "JugadorA" : "JugadorB")}");
                              //FindObjectOfType<NotificacionManager>().EnviarNotificacionTurno(siguienteTurno, auth.CurrentUser.UserId);
                          }

                      });
                  });
        }
        void cambiarLogro()
        {
            string miUid = auth.CurrentUser.UserId;
            string campoCategorias = (miUid == PlayerPrefs.GetString("uidJugadorAQuimicados") ? "CategoriasJugadorA" : "CategoriasJugadorB");

            db.Collection("partidasQuimicados").Document(partidaId).UpdateAsync($"{campoCategorias}.{categoriaSel}", true)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                        Debug.Log("✅ Categoría marcada como completada.");
                    else
                        Debug.LogError("❌ Error actualizando categoría: " + task.Exception);
                });
        }

        private void ConfigurarToggleListeners()
        {
            foreach (Toggle t in opciones)
                t.onValueChanged.RemoveAllListeners();

            for (int i = 0; i < opciones.Length; i++)
            {
                int idx = i;
                opciones[idx].onValueChanged.AddListener(isOn =>
                {
                    if (isOn && eventosToggleHabilitados)
                    {
                        preguntaFinalizada = true;      // evita el timeout luego
                        eventosToggleHabilitados = false;
                        ValidarRespuesta(idx);
                    }
                });
            }
        }

        void desmarcarToggle()
        {
            foreach (Toggle toggle in opciones)
            {
                toggle.isOn = false;
                toggle.interactable = true;
            }
        }
        void ActivarInteractividadOpciones()
        {
            foreach (Toggle toggle in opciones)
            {
                toggle.interactable = true; // Reactiva la interactividad de cada Toggle de opci�n
            }
        }

        void DesactivarInteractividadOpciones()
        {
            foreach (Toggle toggle in opciones)
            {
                toggle.interactable = false; // Desactiva la interactividad de cada Toggle de opci�n
            }
        }
    }
}