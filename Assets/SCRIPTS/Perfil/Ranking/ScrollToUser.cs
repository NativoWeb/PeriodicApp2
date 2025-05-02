using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ScrollToUser : MonoBehaviour
{
    public ScrollRect scrollRect; // Asigna el ScrollRect del ranking
    public Transform content; // Asigna el Content del ScrollView

    public TMP_Text nombreUsuarioText;
    public TMP_Text xpUsuarioText;
    public TMP_Text posicionUsuarioText;

    // Referencias a los botones de cambio de ranking
    public Button btnRankingGeneral;
    public Button btnRankingAmigos;

    // Referencia a la lista de contenido a actualizar
    public Transform rankingContentGeneral;
    public Transform rankingContentAmigos;

    // Variables para almacenar la información del usuario
    private string usuarioActualID;
    private string usuarioActualNombre;
    private int usuarioActualXP;
    private int posicionGeneral = 0;
    private int posicionAmigos = 0;

    // Modo de visualización actual
    private enum ModoRanking { General, Amigos }
    private ModoRanking modoActual = ModoRanking.General;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Guardar información del usuario actual
        if (auth.CurrentUser != null)
        {
            usuarioActualID = auth.CurrentUser.UserId;
            usuarioActualNombre = auth.CurrentUser.DisplayName;
        }

        // Agregar listeners a los botones
        if (btnRankingGeneral != null)
        {
            btnRankingGeneral.onClick.AddListener(() => CambiarModoRanking(ModoRanking.General));
        }

        if (btnRankingAmigos != null)
        {
            btnRankingAmigos.onClick.AddListener(() => CambiarModoRanking(ModoRanking.Amigos));
        }

        // Inicializar con información del usuario
        ObtenerInformacionUsuario();
    }

    private void CambiarModoRanking(ModoRanking nuevoModo)
    {
        if (modoActual != nuevoModo)
        {
            modoActual = nuevoModo;

            // Actualizar visibilidad de los contenidos
            if (rankingContentGeneral != null && rankingContentAmigos != null)
            {
                rankingContentGeneral.gameObject.SetActive(nuevoModo == ModoRanking.General);
                rankingContentAmigos.gameObject.SetActive(nuevoModo == ModoRanking.Amigos);
            }

            ActualizarUISegunModo();

            // Actualizar el contenido según el modo
            if (nuevoModo == ModoRanking.General)
            {
                ActualizarContenidoRankingGeneral();
            }
            else
            {
                ActualizarContenidoRankingAmigos();
            }
        }
    }

    private void ActualizarUISegunModo()
    {
        if (modoActual == ModoRanking.General)
        {
            // Actualizar la UI con la posición general
            UpdateUIDirectly(usuarioActualNombre, usuarioActualXP, posicionGeneral);

            // Cambiar el content del ScrollRect si es necesario
            if (rankingContentGeneral != null)
            {
                scrollRect.content = rankingContentGeneral.GetComponent<RectTransform>();
                content = rankingContentGeneral;
            }
        }
        else // ModoRanking.Amigos
        {
            // Actualizar la UI con la posición entre amigos
            UpdateUIDirectly(usuarioActualNombre, usuarioActualXP, posicionAmigos);

            // Cambiar el content del ScrollRect si es necesario
            if (rankingContentAmigos != null)
            {
                scrollRect.content = rankingContentAmigos.GetComponent<RectTransform>();
                content = rankingContentAmigos;
            }

            // Si aún no tenemos la posición entre amigos, calcularla
            if (posicionAmigos == 0)
            {
                ObtenerPosicionEntreAmigos();
            }
        }
    }

    private void ObtenerInformacionUsuario()
    {
        StartCoroutine(ObtenerInformacionUsuarioCoroutine());
    }

    private IEnumerator ObtenerInformacionUsuarioCoroutine()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("Usuario no autenticado.");
            yield break;
        }

        // Obtener datos del usuario actual
        Task<DocumentSnapshot> userTask = db.Collection("users").Document(usuarioActualID).GetSnapshotAsync();
        yield return new WaitUntil(() => userTask.IsCompleted);

        if (userTask.Exception != null)
        {
            Debug.LogError("Error al obtener datos del usuario: " + userTask.Exception.Message);
            yield break;
        }

        if (userTask.Result.Exists)
        {
            // Guardar el nombre del usuario si no lo tenemos
            if (string.IsNullOrEmpty(usuarioActualNombre))
            {
                usuarioActualNombre = userTask.Result.GetValue<string>("DisplayName");
            }

            // Guardar el XP del usuario
            if (userTask.Result.TryGetValue<int>("xp", out int xpValue))
            {
                usuarioActualXP = xpValue;
            }

            // Obtener ranking general
            yield return StartCoroutine(ObtenerPosicionGeneral());

            // Obtener ranking entre amigos
            yield return StartCoroutine(ObtenerPosicionEntreAmigosCoroutine());

            // Actualizar UI según el modo actual
            ActualizarUISegunModo();

            // Inicializar el contenido según el modo actual
            if (modoActual == ModoRanking.General)
            {
                ActualizarContenidoRankingGeneral();
            }
            else
            {
                ActualizarContenidoRankingAmigos();
            }
        }
    }

    private IEnumerator ObtenerPosicionGeneral()
    {
        Task<QuerySnapshot> queryTask = db.Collection("users")
            .OrderByDescending("xp")
            .GetSnapshotAsync();

        yield return new WaitUntil(() => queryTask.IsCompleted);

        if (queryTask.Exception != null)
        {
            Debug.LogError("Error al obtener el ranking general: " + queryTask.Exception.Message);
            yield break;
        }

        List<DocumentSnapshot> documentos = queryTask.Result.Documents.ToList();
        int posicion = 1;

        foreach (var doc in documentos)
        {
            if (doc.Id == usuarioActualID)
            {
                posicionGeneral = posicion;
                break;
            }
            posicion++;
        }

        Debug.Log($"Posición general del usuario: {posicionGeneral}");

        // Si estamos en modo general, actualizar inmediatamente la UI
        if (modoActual == ModoRanking.General)
        {
            UpdateUIDirectly(usuarioActualNombre, usuarioActualXP, posicionGeneral);
        }
    }

    private void ObtenerPosicionEntreAmigos()
    {
        StartCoroutine(ObtenerPosicionEntreAmigosCoroutine());
    }

    private IEnumerator ObtenerPosicionEntreAmigosCoroutine()
    {
        // Lista para almacenar información de amigos
        List<(string id, string nombre, int xp)> listaAmigos = new List<(string, string, int)>();

        // Agregar el usuario actual a la lista
        listaAmigos.Add((usuarioActualID, usuarioActualNombre, usuarioActualXP));

        // Obtener solicitudes de amistad donde el usuario es remitente
        Task<QuerySnapshot> task1 = db.Collection("SolicitudesAmistad")
            .WhereEqualTo("estado", "aceptada")
            .WhereEqualTo("idRemitente", usuarioActualID)
            .GetSnapshotAsync();

        yield return new WaitUntil(() => task1.IsCompleted);

        if (task1.Exception != null)
        {
            Debug.LogError("Error al obtener solicitudes como remitente: " + task1.Exception.Message);
            yield break;
        }

        List<string> idsAmigos = new List<string>();

        foreach (DocumentSnapshot doc in task1.Result.Documents)
        {
            string idAmigo = doc.GetValue<string>("idDestinatario");
            idsAmigos.Add(idAmigo);
        }

        // Obtener solicitudes de amistad donde el usuario es destinatario
        Task<QuerySnapshot> task2 = db.Collection("SolicitudesAmistad")
            .WhereEqualTo("estado", "aceptada")
            .WhereEqualTo("idDestinatario", usuarioActualID)
            .GetSnapshotAsync();

        yield return new WaitUntil(() => task2.IsCompleted);

        if (task2.Exception != null)
        {
            Debug.LogError("Error al obtener solicitudes como destinatario: " + task2.Exception.Message);
            yield break;
        }

        foreach (DocumentSnapshot doc in task2.Result.Documents)
        {
            string idAmigo = doc.GetValue<string>("idRemitente");
            idsAmigos.Add(idAmigo);
        }

        // Si no hay amigos, solo mostramos al usuario actual
        if (idsAmigos.Count == 0)
        {
            posicionAmigos = 1;

            // Si estamos en modo amigos, actualizar la UI
            if (modoActual == ModoRanking.Amigos)
            {
                UpdateUIDirectly(usuarioActualNombre, usuarioActualXP, posicionAmigos);
            }
            yield break;
        }

        // Obtener información de cada amigo
        int amigosObtenidos = 0;
        foreach (string idAmigo in idsAmigos)
        {
            Task<DocumentSnapshot> taskAmigo = db.Collection("users").Document(idAmigo).GetSnapshotAsync();
            yield return new WaitUntil(() => taskAmigo.IsCompleted);

            amigosObtenidos++;

            if (taskAmigo.Exception == null && taskAmigo.Result.Exists)
            {
                string nombre = taskAmigo.Result.GetValue<string>("DisplayName");
                int xp = 0;
                if (taskAmigo.Result.TryGetValue<int>("xp", out int xpValue))
                {
                    xp = xpValue;
                }

                listaAmigos.Add((idAmigo, nombre, xp));
            }

            // Si ya procesamos a todos los amigos
            if (amigosObtenidos >= idsAmigos.Count)
            {
                // Ordenar por XP de mayor a menor
                var listaOrdenada = listaAmigos.OrderByDescending(j => j.xp).ToList();

                // Encontrar la posición del usuario actual
                posicionAmigos = listaOrdenada.FindIndex(j => j.id == usuarioActualID) + 1;

                Debug.Log($"Posición entre amigos: {posicionAmigos}");

                // Si estamos en modo amigos, actualizar la UI
                if (modoActual == ModoRanking.Amigos)
                {
                    UpdateUIDirectly(usuarioActualNombre, usuarioActualXP, posicionAmigos);
                }

                // Actualizar contenido de ranking de amigos si es necesario
                if (modoActual == ModoRanking.Amigos && rankingContentAmigos != null)
                {
                    LlenarContenidoAmigos(listaOrdenada);
                }
            }
        }
    }

    private void UpdateUIDirectly(string usuario, int xp, int posicion)
    {
        if (nombreUsuarioText != null)
            nombreUsuarioText.text = usuario;

        if (xpUsuarioText != null)
            xpUsuarioText.text = "XP: " + xp.ToString();

        if (posicionUsuarioText != null)
            posicionUsuarioText.text = "#" + posicion.ToString();
    }

    // Método para actualizar el contenido del ranking general
    private void ActualizarContenidoRankingGeneral()
    {
        StartCoroutine(ActualizarContenidoRankingGeneralCoroutine());
    }

    private IEnumerator ActualizarContenidoRankingGeneralCoroutine()
    {
        // Solo continuar si tenemos el contenedor para el ranking general
        if (rankingContentGeneral == null) yield break;

        Task<QuerySnapshot> queryTask = db.Collection("users")
            .OrderByDescending("xp")
            .Limit(100) // Limitar a 100 usuarios para mejor rendimiento
            .GetSnapshotAsync();

        yield return new WaitUntil(() => queryTask.IsCompleted);

        if (queryTask.Exception != null)
        {
            Debug.LogError("Error al obtener usuarios para ranking general: " + queryTask.Exception.Message);
            yield break;
        }

 

        // Forzar actualización del layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)rankingContentGeneral);

        // Si estamos en este modo, hacer scroll a la posición del usuario
        if (modoActual == ModoRanking.General)
        {
            ScrollToUserPosition();
        }
    }

    // Método para actualizar el contenido del ranking de amigos
    private void ActualizarContenidoRankingAmigos()
    {
        StartCoroutine(ActualizarContenidoRankingAmigosCoroutine());
    }

    private IEnumerator ActualizarContenidoRankingAmigosCoroutine()
    {
        // Obtener la lista de amigos y actualizar el contenido
        yield return StartCoroutine(ObtenerPosicionEntreAmigosCoroutine());
    }

    // Método para llenar el contenido de amigos con la lista ordenada
    private void LlenarContenidoAmigos(List<(string id, string nombre, int xp)> listaOrdenada)
    {
        // Solo continuar si tenemos el contenedor para el ranking de amigos
        if (rankingContentAmigos == null) return;

        
        // Forzar actualización del layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)rankingContentAmigos);

        // Si estamos en este modo, hacer scroll a la posición del usuario
        if (modoActual == ModoRanking.Amigos)
        {
            ScrollToUserPosition();
        }
    }

    // Método auxiliar para limpiar un contenedor
    private void LimpiarContenido(Transform container)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    public void ScrollToUserPosition()
    {
        string usuarioActual = usuarioActualNombre.Trim().ToLower();
        Debug.Log("Usuario actual: " + usuarioActual);
        Debug.Log("Número de elementos en content: " + content.childCount);

        foreach (Transform child in content)
        {
            TMP_Text nombre = child.GetComponentInChildren<TMP_Text>(true);
            if (nombre == null) continue;

            string nombreTexto = nombre.text.Trim().ToLower();

            if (nombreTexto == usuarioActual)
            {
                Debug.Log("Usuario encontrado en: " + child.name);

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

                RectTransform targetRect = child.GetComponent<RectTransform>();
                RectTransform contentRect = (RectTransform)content;

                float contentHeight = contentRect.rect.height;
                float targetY = Mathf.Abs(targetRect.anchoredPosition.y);
                float normalizedPosition = 1 - (targetY / contentHeight);

                StartCoroutine(SmoothScrollToPosition(normalizedPosition));
                StartCoroutine(AnimateUserBox(child));
                break;
            }
        }
    }

    IEnumerator SmoothScrollToPosition(float targetPosition)
    {
        float startPos = scrollRect.verticalNormalizedPosition;
        float time = 0f;
        float duration = 0.3f;

        while (time < duration)
        {
            time += Time.deltaTime;
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, Mathf.Clamp01(targetPosition), time / duration);
            yield return null;
        }
    }

    IEnumerator AnimateUserBox(Transform userBox)
    {
        Image boxImage = userBox.GetComponent<Image>();

        if (boxImage != null)
        {
            Color originalColor = boxImage.color;
            Color highlightColor = new Color(1f, 1f, 1f, 0.5f);

            for (int i = 0; i < 3; i++)
            {
                boxImage.color = highlightColor;
                yield return new WaitForSeconds(0.3f);
                boxImage.color = originalColor;
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}