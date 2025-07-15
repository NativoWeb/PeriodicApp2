using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

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
        public string elemento;
        public List<PreguntaData> preguntas;
    }

    [Serializable]
    public class PreguntaData
    {
        public string pregunta;
        public string pregunta_en; // Campo para la pregunta en inglés
        public List<string> opciones;
        public List<string> opciones_en; // Campo para las opciones en inglés
        public int respuestaCorrecta;
    }

    public class PreguntasQuimicados : MonoBehaviour
    {
        [Header("UI")]
        public Text TextTimer;
        public TextMeshProUGUI txtPregunta;
        public TextMeshProUGUI txtFeedBack;
        public GameObject panelFeedBack;
        public Toggle[] opciones;

        [Header("Colores de Respuesta")]
        public Color colorCorrecto = Color.green;
        public Color colorIncorrecto = Color.red;
        public Color colorNormal = Color.white;
        public Color colorFondoCorrecto = new Color(0.66f, 0.81f, 0.30f);
        public Color colorFondoIncorrecto = new Color(0.89f, 0.31f, 0.31f);

        [Header("JSON")]
        public string nombreArchivoResources = "Quimicados";

        // Variables de estado del juego
        private PreguntaData preguntaCargada;
        private bool eventosToggleHabilitados = false;
        public float tiempoRestante; // Hecho público para ajustar en el inspector si es necesario
        private bool preguntaFinalizada = false;

        // Firebase
        private FirebaseFirestore db;
        private FirebaseAuth auth;

        // Datos de la partida
        private string categoriaSel;
        private string partidaId;
        private string appIdioma;

        void Start()
        {
            // Inicializar Firebase
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;

            // Obtener el idioma de la aplicación desde PlayerPrefs.
            // Si no está definido, se usará "es" (español) por defecto.
            appIdioma = PlayerPrefs.GetString("appIdioma", "español");

            // Cargar datos de la partida
            partidaId = PlayerPrefs.GetString("partidaIdQuimicados");
            categoriaSel = PlayerPrefs.GetString("CategoriaRuleta");

            // Configurar e iniciar el juego
            CargarPreguntaDesdeJSON();
            MostrarPregunta();
            ActualizarTextoTiempo();
        }

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
                else
                {
                    preguntaFinalizada = true; // Evita que el código se ejecute varias veces
                    RevealAnswers(); // Llama a revelar respuestas cuando el tiempo se agota
                }
            }
        }

        void ActualizarTextoTiempo()
        {
            TextTimer.text = tiempoRestante.ToString("00") + (appIdioma == "ingles" ? " Seconds" : " Segundos");
        }

        void CargarPreguntaDesdeJSON()
        {
            TextAsset archivoJSON = Resources.Load<TextAsset>(nombreArchivoResources);
            if (archivoJSON == null)
            {
                Debug.LogError($"Error: No se pudo encontrar el archivo JSON en Resources: {nombreArchivoResources}");
                return;
            }

            CuestionarioRoot root = JsonUtility.FromJson<CuestionarioRoot>(archivoJSON.text);
            if (root == null || root.categorias == null)
            {
                Debug.LogError("Error: El JSON está mal formado o no contiene la lista de categorías.");
                return;
            }

            CategoriaData categoria = root.categorias
                .FirstOrDefault(c => c.nombre.Equals(categoriaSel, StringComparison.OrdinalIgnoreCase));

            if (categoria == null)
            {
                Debug.LogError($"Error: No se encontró la categoría seleccionada: {categoriaSel}");
                return;
            }

            if (categoria.preguntas == null || categoria.preguntas.Count == 0)
            {
                Debug.LogError($"Error: La categoría '{categoriaSel}' no tiene preguntas.");
                return;
            }

            // Seleccionar una pregunta aleatoria de la categoría
            System.Random rnd = new System.Random();
            preguntaCargada = categoria.preguntas[rnd.Next(categoria.preguntas.Count)];
        }

        void MostrarPregunta()
        {
            ConfigurarToggleListeners();
            desmarcarToggle();
            eventosToggleHabilitados = true;
            ActivarInteractividadOpciones();

            if (preguntaCargada == null)
            {
                Debug.LogError("Error: No hay ninguna pregunta cargada para mostrar.");
                return;
            }

            // 1. Seleccionar el texto de la pregunta según el idioma
            txtPregunta.text = (appIdioma == "ingles" && !string.IsNullOrEmpty(preguntaCargada.pregunta_en))
                ? preguntaCargada.pregunta_en
                : preguntaCargada.pregunta;

            // 2. Seleccionar la lista de opciones correcta según el idioma
            List<string> opcionesSource = (appIdioma == "ingles" && preguntaCargada.opciones_en != null && preguntaCargada.opciones_en.Count > 0)
                ? preguntaCargada.opciones_en
                : preguntaCargada.opciones;

            // 3. Mezclar opciones para que no aparezcan siempre en el mismo orden
            var listaOpciones = opcionesSource
                .Select((texto, idx) => (texto, idx)) // Guarda el índice original
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            // 4. Actualizar el índice de la respuesta correcta después de mezclar
            int nuevoIndiceCorrecto = listaOpciones.FindIndex(x => x.idx == preguntaCargada.respuestaCorrecta);
            preguntaCargada.respuestaCorrecta = nuevoIndiceCorrecto;

            // 5. Asignar textos a los toggles de la UI
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

        async Task RevealAnswers()
        {
            eventosToggleHabilitados = false;

            // Resalta la opción correcta en verde y las demás en rojo
            for (int i = 0; i < opciones.Length; i++)
            {
                if (i < preguntaCargada.opciones.Count) // Evitar errores si hay más toggles que opciones
                {
                    var img = opciones[i].image;
                    if (i == preguntaCargada.respuestaCorrecta)
                        img.color = colorCorrecto;
                    else
                        img.color = colorIncorrecto;
                }
                opciones[i].interactable = false;
            }

            // Muestra el panel de feedback indicando que el tiempo se agotó
            if (panelFeedBack != null)
            {
                panelFeedBack.SetActive(true);
                panelFeedBack.GetComponent<Image>().color = colorFondoIncorrecto;
            }
            if (txtFeedBack != null)
                txtFeedBack.text = (appIdioma == "ingles") ? "Time's up!" : "¡Tiempo agotado!";

            CambiarTurno(partidaId);
            await Task.Delay(2000); // Espera 2 segundos antes de cambiar de escena
            SceneManager.LoadScene("QuimicadosGame");
        }

        async Task ValidarRespuesta(int indiceSeleccionado)
        {
            eventosToggleHabilitados = false; // Deshabilita futuros eventos de toggle
            preguntaFinalizada = true; // Marca la pregunta como finalizada para detener el temporizador
            bool correcto = (indiceSeleccionado == preguntaCargada.respuestaCorrecta);

            // Visualización de la respuesta
            if (correcto)
            {
                // Comprobar si se debe actualizar un logro
                if (PlayerPrefs.GetInt("CompletarLogro") == 1)
                {
                    PlayerPrefs.SetInt("CompletarLogro", 0);
                    cambiarLogro();
                }

                // Resaltar solo la opción correcta
                opciones[indiceSeleccionado].image.color = colorCorrecto;

                // Activar feedback visual positivo
                panelFeedBack.SetActive(true);
                panelFeedBack.GetComponent<Image>().color = colorFondoCorrecto;
                txtFeedBack.text = (appIdioma == "ingles") ? "Correct!" : "¡Correcto!";
                PlayerPrefs.SetInt("wasCorrect", 1);
            }
            else
            {
                // Resaltar la opción incorrecta seleccionada y la correcta
                for (int i = 0; i < opciones.Length; i++)
                {
                    if (i < preguntaCargada.opciones.Count)
                    {
                        if (i == indiceSeleccionado)
                            opciones[i].image.color = colorIncorrecto;
                        else if (i == preguntaCargada.respuestaCorrecta)
                            opciones[i].image.color = colorCorrecto;
                    }
                }

                // Activar feedback visual negativo
                panelFeedBack.SetActive(true);
                panelFeedBack.GetComponent<Image>().color = colorFondoIncorrecto;
                txtFeedBack.text = (appIdioma == "ingles") ? "Incorrect!" : "¡Incorrecto!";
                PlayerPrefs.SetInt("wasIncorrect", 1);
                CambiarTurno(partidaId);
            }

            DesactivarInteractividadOpciones();
            await Task.Delay(2000);
            SceneManager.LoadScene("QuimicadosGame");
        }

        void CambiarTurno(string partidaId)
        {
            if (string.IsNullOrEmpty(partidaId)) return;

            var docRef = db.Collection("partidasQuimicados").Document(partidaId);

            docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
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

                string jugadorA = snapshot.GetValue<string>("jugadorA");
                string jugadorB = snapshot.GetValue<string>("jugadorB");
                string turnoActual = snapshot.GetValue<string>("turnoActual");
                string yo = auth.CurrentUser.UserId;

                string siguienteTurno = (yo == jugadorA) ? jugadorB : jugadorA;

                if (turnoActual == siguienteTurno)
                {
                    Debug.Log("El turno ya está asignado al otro jugador, no se requiere cambio.");
                    return;
                }

                docRef.UpdateAsync("turnoActual", siguienteTurno)
                    .ContinueWithOnMainThread(updateTask => {
                        if (updateTask.IsFaulted)
                            Debug.LogError($"Error al actualizar turno: {updateTask.Exception}");
                        else
                            Debug.Log($"✅ Turno cambiado a: {siguienteTurno}");
                    });
            });
        }

        void cambiarLogro()
        {
            if (string.IsNullOrEmpty(partidaId)) return;

            string miUid = auth.CurrentUser.UserId;
            string campoCategorias = (miUid == PlayerPrefs.GetString("uidJugadorAQuimicados") ? "CategoriasJugadorA" : "CategoriasJugadorB");

            db.Collection("partidasQuimicados").Document(partidaId).UpdateAsync($"{campoCategorias}.{categoriaSel}", true)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted && !task.IsFaulted)
                        Debug.Log("✅ Categoría marcada como completada en Firebase.");
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
                toggle.image.color = colorNormal; // Reinicia el color a normal
                toggle.interactable = true;
            }
        }

        void ActivarInteractividadOpciones()
        {
            foreach (Toggle toggle in opciones)
            {
                toggle.interactable = true;
            }
        }

        void DesactivarInteractividadOpciones()
        {
            foreach (Toggle toggle in opciones)
            {
                toggle.interactable = false;
            }
        }
        
    }

}