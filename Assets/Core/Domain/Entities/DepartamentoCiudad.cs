using System.Collections.Generic;

namespace PeriodicApp.Core.Domain.Entities
{
    public class DepartamentoCiudad
    {
        public string Departamento { get; set; }
        public List<string> Ciudades { get; set; }

        public DepartamentoCiudad(string departamento, List<string> ciudades)
        {
            Departamento = departamento;
            Ciudades = ciudades;
        }
    }
}
