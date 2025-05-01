using System;
using System.Linq;
using System.Text.RegularExpressions;


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

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return ResultadoValidacionRegistro.Error("Correo con formato inválido.");
        }

        string dominio = email.Split('@').Last();
        if (!dominiosPermitidos.Any(d => d.Equals(dominio, StringComparison.OrdinalIgnoreCase)))
        {
            return ResultadoValidacionRegistro.Error("Dominio del correo no permitido.");
        }

        if (password != confirmPassword)
        {
            return ResultadoValidacionRegistro.Error("Las contraseñas no coinciden.");
        }

        if (password.Length < 6 ||
            !Regex.IsMatch(password, "[A-Z]") ||
            !Regex.IsMatch(password, "[a-z]") ||
            !Regex.IsMatch(password, @"[\^\$\*\.\[\]\{\}\(\)\?\""!@#%&/\\,><':;|_~`]"))
        {
            return ResultadoValidacionRegistro.Error("La contraseña no cumple con los requisitos de seguridad.");
        }

        return ResultadoValidacionRegistro.Exito();
    }

}
