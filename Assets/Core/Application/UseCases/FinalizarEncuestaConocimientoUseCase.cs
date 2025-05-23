using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalizarEncuestaConocimientoUseCase
{
    private readonly IServicioFirestore firestore;
    private readonly IServicioAutenticacion auth;

    public FinalizarEncuestaConocimientoUseCase(IServicioFirestore firestore, IServicioAutenticacion auth)
    {
        this.firestore = firestore;
        this.auth = auth;
    }

    public async Task Ejecutar()
    {
        PlayerPrefs.SetInt("EstadoEncuestaConocimiento", 1);
        PlayerPrefs.Save();

        bool hayInternet = Application.internetReachability != NetworkReachability.NotReachable;
        bool estadoAprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool estadoConocimiento = true;

        string userId = auth.CurrentUser?.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("No hay usuario autenticado.");
            return;
        }

        if (hayInternet)
        {
            await firestore.GuardarEstadoEncuestaConocimientoAsync(userId, true);
            var userData = await firestore.ObtenerUsuarioAsync(userId);

            estadoAprendizaje = userData.ContainsKey("EstadoEncuestaAprendizaje") && (bool)userData["EstadoEncuestaAprendizaje"];
            estadoConocimiento = userData.ContainsKey("EstadoEncuestaConocimiento") && (bool)userData["EstadoEncuestaConocimiento"];
        }

        if (estadoAprendizaje && estadoConocimiento)
            SceneManager.LoadScene("Categorías");
        else
            SceneManager.LoadScene("SeleccionarEncuesta");
    }
}
