using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditarPerfilManager : MonoBehaviour
{

    [Header("Panel Editar Perfil y componentes")]
    [SerializeField] public GameObject panelEditar;
    [SerializeField] private TMP_Dropdown edadDropdown;
    [SerializeField] private TMP_Dropdown departamentoDropdown;
    [SerializeField] private TMP_Dropdown ciudadDropdown;

    private Dictionary<string, List<string>> ciudadesPorDepartamento = new Dictionary<string, List<string>>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CargarTotalementeDropDowns();
    }
    void LlenarDropdowns()
    {
        // Lista de edades (1 a 100 a�os)
        List<string> edades = new List<string>();
        for (int i = 10; i <= 100; i++)
        {
            edades.Add(i.ToString());
        }
        ActualizarDropdown(edadDropdown, edades);

        // Lista de departamentos de Colombia
        List<string> departamentos = new List<string>(ciudadesPorDepartamento.Keys);
        ActualizarDropdown(departamentoDropdown, departamentos);

        // Inicializar ciudades con el primer departamento
        ActualizarCiudades();
    }

    void LlenarCiudadesPorDepartamento()
    {
        ciudadesPorDepartamento["Seleccionar"] = new List<string> { "Seleccione un departamento" };
        ciudadesPorDepartamento["Amazonas"] = new List<string> { "Leticia", "Puerto Nari�o" };
        ciudadesPorDepartamento["Antioquia"] = new List<string> { "Medell�n", "Bello", "Envigado", "Itag��", "Rionegro", "Apartad�", "Turbo", "Sabaneta" };
        ciudadesPorDepartamento["Arauca"] = new List<string> { "Arauca", "Saravena", "Tame", "Arauquita" };
        ciudadesPorDepartamento["Atl�ntico"] = new List<string> { "Barranquilla", "Soledad", "Malambo", "Sabanalarga", "Puerto Colombia" };
        ciudadesPorDepartamento["Bol�var"] = new List<string> { "Cartagena", "Magangu�", "Turbaco", "Arjona", "El Carmen de Bol�var" };
        ciudadesPorDepartamento["Boyac�"] = new List<string> { "Tunja", "Duitama", "Sogamoso", "Chiquinquir�", "Paipa" };
        ciudadesPorDepartamento["Caldas"] = new List<string> { "Manizales", "La Dorada", "Chinchin�", "Villamar�a", "Riosucio" };
        ciudadesPorDepartamento["Caquet�"] = new List<string> { "Florencia", "San Vicente del Cagu�n", "Puerto Rico", "Doncello" };
        ciudadesPorDepartamento["Casanare"] = new List<string> { "Yopal", "Aguazul", "Villanueva", "Tauramena" };
        ciudadesPorDepartamento["Cauca"] = new List<string> { "Popay�n", "Santander de Quilichao", "Pat�a", "Puerto Tejada" };
        ciudadesPorDepartamento["Cesar"] = new List<string> { "Valledupar", "Aguachica", "Codazzi", "La Jagua de Ibirico" };
        ciudadesPorDepartamento["Choc�"] = new List<string> { "Quibd�", "Istmina", "Condoto", "Bah�a Solano" };
        ciudadesPorDepartamento["C�rdoba"] = new List<string> { "Monter�a", "Ceret�", "Sahag�n", "Lorica", "Montel�bano" };
        ciudadesPorDepartamento["Cundinamarca"] = new List<string> { "Bogot�", "Soacha", "Zipaquir�", "Girardot", "Facatativ�", "Ch�a", "Fusagasug�" };
        ciudadesPorDepartamento["Guain�a"] = new List<string> { "In�rida" };
        ciudadesPorDepartamento["Guaviare"] = new List<string> { "San Jos� del Guaviare", "Calamar", "Miraflores" };
        ciudadesPorDepartamento["Huila"] = new List<string> { "Neiva", "Pitalito", "Garz�n", "La Plata" };
        ciudadesPorDepartamento["La Guajira"] = new List<string> { "Riohacha", "Maicao", "Uribia", "Fonseca" };
        ciudadesPorDepartamento["Magdalena"] = new List<string> { "Santa Marta", "Ci�naga", "Fundaci�n", "El Banco" };
        ciudadesPorDepartamento["Meta"] = new List<string> { "Villavicencio", "Acac�as", "Granada", "Puerto Gait�n" };
        ciudadesPorDepartamento["Nari�o"] = new List<string> { "Pasto", "Ipiales", "Tumaco", "T�querres" };
        ciudadesPorDepartamento["Norte de Santander"] = new List<string> { "C�cuta", "Oca�a", "Pamplona", "Villa del Rosario" };
        ciudadesPorDepartamento["Putumayo"] = new List<string> { "Mocoa", "Puerto As�s", "Orito", "Valle del Guamuez" };
        ciudadesPorDepartamento["Quind�o"] = new List<string> { "Armenia", "Circasia", "Montenegro", "Calarc�" };
        ciudadesPorDepartamento["Risaralda"] = new List<string> { "Pereira", "Dosquebradas", "Santa Rosa de Cabal", "La Virginia" };
        ciudadesPorDepartamento["San Andr�s y Providencia"] = new List<string> { "San Andr�s", "Providencia" };
        ciudadesPorDepartamento["Santander"] = new List<string> { "Bucaramanga", "Floridablanca", "Gir�n", "Piedecuesta", "Barrancabermeja" };
        ciudadesPorDepartamento["Sucre"] = new List<string> { "Sincelejo", "Corozal", "Sampu�s", "San Marcos" };
        ciudadesPorDepartamento["Tolima"] = new List<string> { "Ibagu�", "Espinal", "Melgar", "Honda" };
        ciudadesPorDepartamento["Valle del Cauca"] = new List<string> { "Cali", "Palmira", "Buenaventura", "Tulu�", "Cartago", "Buga" };
        ciudadesPorDepartamento["Vaup�s"] = new List<string> { "Mit�" };
        ciudadesPorDepartamento["Vichada"] = new List<string> { "Puerto Carre�o", "La Primavera" };
    }


    void ActualizarCiudades()
    {
        string departamentoSeleccionado = departamentoDropdown.options[departamentoDropdown.value].text;
        if (ciudadesPorDepartamento.ContainsKey(departamentoSeleccionado))
        {
            ActualizarDropdown(ciudadDropdown, ciudadesPorDepartamento[departamentoSeleccionado]);
        }
    }

    void ActualizarDropdown(TMP_Dropdown dropdown, List<string> opciones)
    {
        dropdown.ClearOptions(); // Limpiar opciones anteriores
        dropdown.AddOptions(opciones); // Agregar nuevas opciones
    }

    private void CargarTotalementeDropDowns()
    {
        LlenarCiudadesPorDepartamento();
        LlenarDropdowns();
        departamentoDropdown.onValueChanged.AddListener(delegate { ActualizarCiudades(); });
    }
    
    public void activarPanelEditar()
    {
     
            panelEditar.SetActive(true);
    }
    public void desactivarPanelEditar()
    {
        if (panelEditar != null )
        panelEditar.SetActive(false);
    }


}
