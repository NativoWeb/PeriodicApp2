using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using System.Collections;

public class ScrollToUser : MonoBehaviour
{
    public ScrollRect scrollRect; // Asigna el ScrollRect del ranking
    public Transform content; // Asigna el Content del ScrollView

    public TMP_Text nombreUsuarioText;
    public TMP_Text xpUsuarioText;
    public TMP_Text posicionUsuarioText;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        ObtenerInformacionUsuario();
    }

    public void ScrollToUserPosition()
    {
        string usuarioActual = PlayerPrefs.GetString("DisplayName", "").Trim().ToLower();
        Debug.Log("Usuario actual: " + usuarioActual);
        Debug.Log("Número de elementos en content: " + content.childCount);

        foreach (Transform child in content)
        {
            TMP_Text nombre = child.GetComponentInChildren<TMP_Text>(true); // Busca el nombre dentro del prefab
            string nombreTexto = nombre != null ? nombre.text.Trim().ToLower() : "N/A";

            Debug.Log("Buscando en: " + child.name + " - Texto encontrado: " + nombreTexto);

            if (nombre != null && nombreTexto == usuarioActual)
            {
                Debug.Log("Usuario encontrado en: " + child.name);

                // Asegura que el contenido esté correctamente actualizado antes de calcular la posición
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

                // Obtiene la posición del usuario dentro del ranking
                RectTransform targetRect = child.GetComponent<RectTransform>();
                RectTransform contentRect = (RectTransform)content;

                // Calcula la posición relativa dentro del ScrollRect
                float contentHeight = contentRect.rect.height;
                float targetY = Mathf.Abs(targetRect.anchoredPosition.y);
                float normalizedPosition = 1 - (targetY / contentHeight);

                // Inicia la corrutina para desplazamiento suave
                StartCoroutine(SmoothScrollToPosition(normalizedPosition));

                // Inicia la animación de la caja del usuario encontrado
                StartCoroutine(AnimateUserBox(child));

                break;
            }
        }
    }

    IEnumerator SmoothScrollToPosition(float targetPosition)
    {
        float startPos = scrollRect.verticalNormalizedPosition;
        float time = 0f;
        float duration = 0.3f; // Duración del desplazamiento

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
            Color highlightColor = new Color(1f, 1f, 1f, 0.5f); // Blanco transparente

            // Parpadeo 3 veces
            for (int i = 0; i < 3; i++)
            {
                boxImage.color = highlightColor;
                yield return new WaitForSeconds(0.3f);
                boxImage.color = originalColor;
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    public void ObtenerInformacionUsuario()
    {
        string usuarioActual = auth.CurrentUser.DisplayName;

        db.Collection("users")
          .OrderByDescending("xp")
          .GetSnapshotAsync()
          .ContinueWith(task =>
          {
              if (task.IsCompleted)
              {
                  int posicion = 1;
                  foreach (DocumentSnapshot document in task.Result.Documents)
                  {
                      string nombre = document.GetValue<string>("DisplayName");
                      int xp = document.GetValue<int>("xp");

                      if (nombre == usuarioActual)
                      {
                          nombreUsuarioText.text = nombre;
                          xpUsuarioText.text = "XP: " + xp.ToString();
                          posicionUsuarioText.text = "#" + posicion.ToString();
                          break;
                      }
                      posicion++;
                  }
              }
          });
    }
}
