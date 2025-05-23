using System.Threading.Tasks;

public class LoginUsuario
{
    private readonly IServicioAutenticacion servicioAutenticacion;
    private readonly IServicioLocalStorage servicioLocalStorage;

    public LoginUsuario(IServicioAutenticacion servicioAutenticacion, IServicioLocalStorage servicioLocalStorage)
    {
        this.servicioAutenticacion = servicioAutenticacion;
        this.servicioLocalStorage = servicioLocalStorage;
    }

        public async Task<ResultadoLogin> Ejecutar(string email, string password)
        {
            try
            {
                var usuario = await servicioAutenticacion.LoginAsync(email, password);

                servicioLocalStorage.Guardar("userId", usuario.UserId);
                servicioLocalStorage.Guardar("DisplayName", usuario.DisplayName);
                servicioLocalStorage.Guardar("Estadouser", "nube");

                var resultado = new ResultadoLogin
                {
                    Exito = true,
                    UsuarioId = usuario.UserId
                };

                return resultado;
            }
            catch (System.Exception ex)
            {
                return new ResultadoLogin
                {
                    Exito = false,
                    MensajeError = ex.Message
                };
            }
        }


    public class ResultadoLogin
    {
        public bool Exito { get; set; }
        public string UsuarioId { get; set; }
        public string MensajeError { get; set; }
    }
}
    
