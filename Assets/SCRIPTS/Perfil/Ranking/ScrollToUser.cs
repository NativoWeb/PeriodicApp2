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

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private ListenerRegistration listener;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        ObtenerInformacionUsuario();

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

        string usuarioActual = auth.CurrentUser.DisplayName;
        Debug.Log("Usuario actual: " + usuarioActual);

        Task<QuerySnapshot> queryTask = db.Collection("users")
            .OrderByDescending("xp")
            .GetSnapshotAsync();

        yield return new WaitUntil(() => queryTask.IsCompleted);

        if (queryTask.Exception != null)
        {
            Debug.LogError("Error al obtener la información del usuario: " + queryTask.Exception.Message);
            yield break;
        }

        QuerySnapshot snapshot = queryTask.Result;

        if (!snapshot.Documents.Any())
        {
            Debug.LogWarning("No hay usuarios en la base de datos.");
            yield break;
        }

        List<DocumentSnapshot> documentos = snapshot.Documents.ToList();
        DocumentSnapshot usuarioDoc = documentos.FirstOrDefault(doc => doc.GetValue<string>("DisplayName") == usuarioActual);

        if (usuarioDoc != null)
        {
            int xp = usuarioDoc.GetValue<int>("xp");
            int posicion = documentos.IndexOf(usuarioDoc) + 1;

            Debug.Log($"Usuario encontrado: {usuarioActual}, XP: {xp}, Posición: {posicion}");

            StartCoroutine(UpdateUI(usuarioActual, xp, posicion));
        }
        else
        {
            Debug.LogWarning("Usuario no encontrado en la base de datos.");
        }
    }
    IEnumerator UpdateUI(string usuario, int xp, int posicion)
    {
        nombreUsuarioText.text = usuario;
        xpUsuarioText.text = "XP: " + xp.ToString();
        posicionUsuarioText.text = "#" + posicion.ToString();
        yield return null;
    }

    public void ScrollToUserPosition()
    {
        string usuarioActual = PlayerPrefs.GetString("DisplayName", "").Trim().ToLower();
        Debug.Log("Usuario actual: " + usuarioActual);
        Debug.Log("Número de elementos en content: " + content.childCount);

        foreach (Transform child in content)
        {
            TMP_Text nombre = child.GetComponentInChildren<TMP_Text>(true);
            string nombreTexto = nombre != null ? nombre.text.Trim().ToLower() : "N/A";


            if (nombre != null && nombreTexto == usuarioActual)
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
