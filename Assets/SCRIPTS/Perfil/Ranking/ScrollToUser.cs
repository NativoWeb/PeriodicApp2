using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;
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
        Debug.Log("N�mero de elementos en content: " + content.childCount);

        foreach (Transform child in content)
        {
            TMP_Text nombre = child.GetComponentInChildren<TMP_Text>(true); // Busca el nombre dentro del prefab
            string nombreTexto = nombre != null ? nombre.text.Trim().ToLower() : "N/A";

            Debug.Log("Buscando en: " + child.name + " - Texto encontrado: " + nombreTexto);

            if (nombre != null && nombreTexto == usuarioActual)
            {
                Debug.Log("Usuario encontrado en: " + child.name);

                // Asegura que el contenido est� correctamente actualizado antes de calcular la posici�n
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

                // Obtiene la posici�n del usuario dentro del ranking
                RectTransform targetRect = child.GetComponent<RectTransform>();
                RectTransform contentRect = (RectTransform)content;

                // Calcula la posici�n relativa dentro del ScrollRect
                float contentHeight = contentRect.rect.height;
                float targetY = Mathf.Abs(targetRect.anchoredPosition.y);
                float normalizedPosition = 1 - (targetY / contentHeight);

                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
                
               
                Debug.Log("Scroll ajustado a posici�n: " + scrollRect.verticalNormalizedPosition);
                break;
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
