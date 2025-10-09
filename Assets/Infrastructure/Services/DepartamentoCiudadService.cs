using System.Collections.Generic;
using System.Linq;
using PeriodicApp.Core.Domain.Entities;

namespace PeriodicApp.Infrastructure.Services
{
    public class DepartamentoCiudadService
    {
        private static List<DepartamentoCiudad> departamentosCiudades;

        public static List<DepartamentoCiudad> ObtenerDepartamentosCiudades()
        {
            if (departamentosCiudades == null)
            {
                InicializarDatos();
            }
            return departamentosCiudades;
        }

        public static List<string> ObtenerDepartamentos()
        {
            if (departamentosCiudades == null)
            {
                InicializarDatos();
            }
            return departamentosCiudades.Select(d => d.Departamento).ToList();
        }

        public static List<string> ObtenerCiudadesPorDepartamento(string departamento)
        {
            if (departamentosCiudades == null)
            {
                InicializarDatos();
            }

            var dept = departamentosCiudades.FirstOrDefault(d => d.Departamento == departamento);
            return dept?.Ciudades ?? new List<string>();
        }

        private static void InicializarDatos()
        {
            departamentosCiudades = new List<DepartamentoCiudad>
            {
                new DepartamentoCiudad("Amazonas", new List<string> { "Leticia", "Puerto Nariño" }),

                new DepartamentoCiudad("Antioquia", new List<string>
                {
                    "Medellín", "Bello", "Itagüí", "Envigado", "Apartadó", "Turbo",
                    "Rionegro", "Caucasia", "Carmen de Viboral", "La Ceja", "Sabaneta",
                    "Yarumal", "Andes", "Marinilla", "Santa Rosa de Osos"
                }),

                new DepartamentoCiudad("Arauca", new List<string>
                {
                    "Arauca", "Tame", "Arauquita", "Saravena", "Fortul"
                }),

                new DepartamentoCiudad("Atlántico", new List<string>
                {
                    "Barranquilla", "Soledad", "Malambo", "Sabanalarga", "Puerto Colombia",
                    "Galapa", "Baranoa", "Santo Tomás", "Palmar de Varela", "Juan de Acosta"
                }),

                new DepartamentoCiudad("Bolívar", new List<string>
                {
                    "Cartagena", "Magangué", "Turbaco", "Arjona", "El Carmen de Bolívar",
                    "Mompós", "Simití", "San Pablo", "Santa Rosa del Sur", "Mahates"
                }),

                new DepartamentoCiudad("Boyacá", new List<string>
                {
                    "Tunja", "Duitama", "Sogamoso", "Chiquinquirá", "Paipa", "Villa de Leyva",
                    "Puerto Boyacá", "Moniquirá", "Nobsa", "Samacá", "Tibasosa"
                }),

                new DepartamentoCiudad("Caldas", new List<string>
                {
                    "Manizales", "Villamaría", "La Dorada", "Chinchiná", "Riosucio",
                    "Anserma", "Palestina", "Salamina", "Supía", "Aguadas"
                }),

                new DepartamentoCiudad("Caquetá", new List<string>
                {
                    "Florencia", "San Vicente del Caguán", "Puerto Rico", "El Doncello",
                    "La Montañita", "Belén de los Andaquíes", "Cartagena del Chairá"
                }),

                new DepartamentoCiudad("Casanare", new List<string>
                {
                    "Yopal", "Aguazul", "Villanueva", "Paz de Ariporo", "Monterrey",
                    "Tauramena", "Maní", "Trinidad", "Hato Corozal"
                }),

                new DepartamentoCiudad("Cauca", new List<string>
                {
                    "Popayán", "Santander de Quilichao", "Puerto Tejada", "Patía",
                    "Miranda", "Corinto", "Guapi", "Piendamó", "Timbío", "Silvia"
                }),

                new DepartamentoCiudad("Cesar", new List<string>
                {
                    "Valledupar", "Aguachica", "Bosconia", "Codazzi", "Chimichagua",
                    "La Paz", "Curumaní", "Agustín Codazzi", "San Diego", "Pelaya"
                }),

                new DepartamentoCiudad("Chocó", new List<string>
                {
                    "Quibdó", "Istmina", "Condoto", "Tadó", "Acandí",
                    "El Carmen de Atrato", "Río Sucio", "Nuquí", "Bahía Solano"
                }),

                new DepartamentoCiudad("Córdoba", new List<string>
                {
                    "Montería", "Cereté", "Lorica", "Sahagún", "Planeta Rica",
                    "Montelíbano", "Tierralta", "Ayapel", "Chinú", "San Antero"
                }),

                new DepartamentoCiudad("Cundinamarca", new List<string>
                {
                    "Bogotá", "Soacha", "Facatativá", "Zipaquirá", "Fusagasugá", "Chía",
                    "Madrid", "Mosquera", "Funza", "Girardot", "Cajicá", "La Calera",
                    "Cota", "Sopó", "Tenjo", "Tabio", "Tocancipá", "Gachancipá"
                }),

                new DepartamentoCiudad("Guainía", new List<string>
                {
                    "Inírida", "Barranco Minas", "Cacahual"
                }),

                new DepartamentoCiudad("Guaviare", new List<string>
                {
                    "San José del Guaviare", "Calamar", "El Retorno", "Miraflores"
                }),

                new DepartamentoCiudad("Huila", new List<string>
                {
                    "Neiva", "Pitalito", "Garzón", "La Plata", "Campoalegre",
                    "San Agustín", "Isnos", "Gigante", "Rivera", "Aipe"
                }),

                new DepartamentoCiudad("La Guajira", new List<string>
                {
                    "Riohacha", "Maicao", "Uribia", "Manaure", "Fonseca",
                    "San Juan del Cesar", "Villanueva", "Dibulla", "Barrancas"
                }),

                new DepartamentoCiudad("Magdalena", new List<string>
                {
                    "Santa Marta", "Ciénaga", "El Banco", "Fundación", "Plato",
                    "Zona Bananera", "Aracataca", "Santa Ana", "Sitionuevo"
                }),

                new DepartamentoCiudad("Meta", new List<string>
                {
                    "Villavicencio", "Acacías", "Granada", "Puerto López", "San Martín",
                    "Cumaral", "Restrepo", "Puerto Gaitán", "La Macarena", "Guamal"
                }),

                new DepartamentoCiudad("Nariño", new List<string>
                {
                    "Pasto", "Tumaco", "Ipiales", "Túquerres", "Barbacoas",
                    "La Cruz", "Samaniego", "Cumbal", "El Charco", "Ricaurte"
                }),

                new DepartamentoCiudad("Norte de Santander", new List<string>
                {
                    "Cúcuta", "Ocaña", "Pamplona", "Villa del Rosario", "Los Patios",
                    "Tibú", "El Zulia", "Chinácota", "Sardinata", "Convención"
                }),

                new DepartamentoCiudad("Putumayo", new List<string>
                {
                    "Mocoa", "Puerto Asís", "Valle del Guamuez", "Orito",
                    "San Miguel", "Puerto Guzmán", "Villagarzón", "Sibundoy"
                }),

                new DepartamentoCiudad("Quindío", new List<string>
                {
                    "Armenia", "Calarcá", "La Tebaida", "Montenegro", "Quimbaya",
                    "Circasia", "Salento", "Filandia", "Buenavista", "Córdoba"
                }),

                new DepartamentoCiudad("Risaralda", new List<string>
                {
                    "Pereira", "Dosquebradas", "Santa Rosa de Cabal", "La Virginia",
                    "Marsella", "Belén de Umbría", "Apía", "Pueblo Rico", "Santuario"
                }),

                new DepartamentoCiudad("San Andrés y Providencia", new List<string>
                {
                    "San Andrés", "Providencia"
                }),

                new DepartamentoCiudad("Santander", new List<string>
                {
                    "Bucaramanga", "Floridablanca", "Girón", "Piedecuesta", "Barrancabermeja",
                    "San Gil", "Socorro", "Málaga", "Barbosa", "Vélez", "Sabana de Torres"
                }),

                new DepartamentoCiudad("Sucre", new List<string>
                {
                    "Sincelejo", "Corozal", "Sampués", "Tolú", "Coveñas",
                    "Majagual", "San Marcos", "San Onofre", "Ovejas", "Morroa"
                }),

                new DepartamentoCiudad("Tolima", new List<string>
                {
                    "Ibagué", "Espinal", "Melgar", "Honda", "Líbano",
                    "Chaparral", "Mariquita", "Purificación", "Guamo", "Flandes"
                }),

                new DepartamentoCiudad("Valle del Cauca", new List<string>
                {
                    "Cali", "Palmira", "Buenaventura", "Tuluá", "Cartago", "Buga",
                    "Jamundí", "Yumbo", "Sevilla", "Pradera", "Florida", "Candelaria"
                }),

                new DepartamentoCiudad("Vaupés", new List<string>
                {
                    "Mitú", "Carurú", "Taraira"
                }),

                new DepartamentoCiudad("Vichada", new List<string>
                {
                    "Puerto Carreño", "La Primavera", "Santa Rosalía", "Cumaribo"
                })
            };
        }
    }
}
