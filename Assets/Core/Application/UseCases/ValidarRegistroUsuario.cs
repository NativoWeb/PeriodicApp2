using System;
using System.Linq;
using System.Text.RegularExpressions;

///
/// Caso de uso que valida las reglas del formulario de registro.
/// Evalúa: formato de correo, dominio permitido, coincidencia de contraseñas y requisitos de seguridad.
/// 
public class ValidarRegistroUsuario
{
    private readonly string[] dominiosPermitidos = {
        "gmail.com", "hotmail.com", "yahoo.com", "outlook.com", "icloud.com"
    };

    public ResultadoValidacionRegistro Ejecutar(string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            return ResultadoValidacionRegistro.Error("Completa todos los campos.");
        }

        if (!EsCorreoValido(email))
        {
            return ResultadoValidacionRegistro.Error("Correo con formato inválido.");
        }

        if (!EsDominioPermitido(email))
        {
            return ResultadoValidacionRegistro.Error("Dominio del correo no está permitido.");
        }

        if (password != confirmPassword)
        {
            return ResultadoValidacionRegistro.Error("Las contraseñas no coinciden.");
        }

        if (!CumpleRequisitosContrasena(password))
        {
            return ResultadoValidacionRegistro.Error("La contraseña no cumple con los requisitos de seguridad.");
        }

        return ResultadoValidacionRegistro.Exito();
    }

    private bool EsCorreoValido(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private bool EsDominioPermitido(string email)
    {
        string dominio = email.Split('@').Last();
        return dominiosPermitidos.Any(d => d.Equals(dominio, StringComparison.OrdinalIgnoreCase));
    }

    private bool CumpleRequisitosContrasena(string password)
    {
        return password.Length >= 6 &&
               Regex.IsMatch(password, "[A-Z]") &&          // al menos una mayúscula
               Regex.IsMatch(password, "[a-z]") &&          // al menos una minúscula
               Regex.IsMatch(password, @"[\W_]");           // al menos un carácter especial
    }
}
