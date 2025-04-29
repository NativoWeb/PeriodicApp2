using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class PanelInvitacionController : MonoBehaviour
{
    public TMP_Text txtInfo;
    public Button btnAceptar;
    public Button btnRechazar; 
    
    private DatabaseReference realtime;
    private string miUID;
    private string partidaId;

    void Start()
    {
        miUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        btnAceptar.onClick.AddListener(() =>
        {
            InvitacionManager.instancia.AceptarInvitacion();
            gameObject.SetActive(false);
        });

        btnRechazar.onClick.AddListener(() =>
        {
            InvitacionManager.instancia.RechazarInvitacion();
            gameObject.SetActive(false);
        });
    }
    public void Mostrar(string from, string juego, string _partidaId)
    {
        partidaId = _partidaId;
        txtInfo.text = $"Has sido invitado por {from} a jugar: {juego}";
        gameObject.SetActive(true);

        Invoke("OcultarPanel", 5f);
    }
    void OcultarPanel()
    {
        if (gameObject != null && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    IEnumerator AnimarEntrada()
    {
        CanvasGroup canvas = GetComponent<CanvasGroup>();
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -Screen.height); // inicia abajo
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * 2;
            canvas.alpha = Mathf.Lerp(0, 1, t);
            rt.anchoredPosition = Vector2.Lerp(new Vector2(0, -Screen.height), Vector2.zero, t);
            yield return null;
        }
    }
}
