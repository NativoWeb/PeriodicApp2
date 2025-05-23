using Firebase.Auth;
using Firebase.Firestore;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using System.Linq;
using UnityEngine;

public class ContadorNotificacion : MonoBehaviour
{
    private FirebaseFirestore db;

    public GameObject panelNotificacion; // asignalo desde el editor
    public TMP_Text txtCantidadPartidas;     // UI text con 
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        StartCoroutine(RevisarCadaXSegundos());
    }

    public async Task<int> ObtenerCantidadPartidasActivas()
    {
        string miUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        var partidasRef = db.Collection("partidasQuimicados");

        var qA = await partidasRef
            .WhereEqualTo("estado", "jugando")
            .WhereEqualTo("jugadorA", miUid)
            .GetSnapshotAsync();

        var qB = await partidasRef
            .WhereEqualTo("estado", "jugando")
            .WhereEqualTo("jugadorB", miUid)
            .GetSnapshotAsync();

        var docs = qA.Documents
            .Concat(qB.Documents)
            .GroupBy(d => d.Id)
            .Select(g => g.First())
            .ToList();

        return docs.Count;
    }
    public void OcultarNotificacionYReiniciarContador()
    {
        panelNotificacion.SetActive(false);
        txtCantidadPartidas.text = ""; // o "0" si preferís mostrar el número
    }
    private async void RevisarNotificaciones()
    {
        int cantidad = await ObtenerCantidadPartidasActivas();

        if (cantidad > 0)
        {
            panelNotificacion.SetActive(true);
            txtCantidadPartidas.text = cantidad.ToString();
        }
        else
        {
            panelNotificacion.SetActive(false);
        }
    }
    IEnumerator RevisarCadaXSegundos()
    {
        while (true)
        {
            RevisarNotificaciones();
            yield return new WaitForSeconds(10f);
        }
    }

}
