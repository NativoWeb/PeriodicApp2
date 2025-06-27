using UnityEngine;
using UnityEngine.UI;
using TMPro; // Aseg�rate de tener este using para los Dropdowns de TextMeshPro
using System.Collections.Generic;

public class ControladorSeleccionMision : MonoBehaviour
{
    // 1. REFERENCIAS A LOS ELEMENTOS DE LA UI
    // Arr�stralos desde la jerarqu�a al Inspector de este script
    [SerializeField] private TMP_Dropdown categoriaDropdown;
    [SerializeField] private TMP_Dropdown elementoDropdown;
    [SerializeField] private Button btnContinuarMision;

    // 2. ESTRUCTURA DE DATOS PARA CATEGOR�AS Y ELEMENTOS
    private Dictionary<string, List<string>> datosElementos;

    void Awake()
    {
        // 3. INICIALIZAR LOS DATOS (en una app real, esto podr�a venir de Firebase o un archivo)
        InicializarDatos();
    }

    // Puedes llamar a esta funci�n cuando se abre el panel
    public void IniciarPanel()
    {
        // 4. CONFIGURAR EL ESTADO INICIAL DEL PANEL
        btnContinuarMision.interactable = false; // El bot�n empezar deshabilitado
        elementoDropdown.interactable = false; // El dropdown de elementos tambi�n

        // 5. POBLAR EL DROPDOWN DE CATEGOR�AS
        PoblarCategorias();

        // 6. A�ADIR LISTENERS PARA DETECTAR CAMBIOS
        // Limpiamos listeners anteriores para evitar llamadas m�ltiples si el panel se abre y cierra
        categoriaDropdown.onValueChanged.RemoveAllListeners();
        elementoDropdown.onValueChanged.RemoveAllListeners();

        categoriaDropdown.onValueChanged.AddListener(OnCategoriaSeleccionada);
        elementoDropdown.onValueChanged.AddListener(OnElementoSeleccionado);
    }

    private void InicializarDatos()
    {
        datosElementos = new Dictionary<string, List<string>>
    {
        {
            "Metales Alcalinos", new List<string>
            {
                "Litio (Li)",
                "Sodio (Na)",
                "Potasio (K)",
                "Rubidio (Rb)",
                "Cesio (Cs)",
                "Francio (Fr)"
            }
        },
        {
            "Metales Alcalinot�rreos", new List<string>
            {
                "Berilio (Be)",
                "Magnesio (Mg)",
                "Calcio (Ca)",
                "Estroncio (Sr)",
                "Bario (Ba)",
                "Radio (Ra)"
            }
        },
        {
            "Lant�nidos", new List<string>
            {
                "Lantano (La)",
                "Cerio (Ce)",
                "Praseodimio (Pr)",
                "Neodimio (Nd)",
                "Prometio (Pm)",
                "Samario (Sm)",
                "Europio (Eu)",
                "Gadolinio (Gd)",
                "Terbio (Tb)",
                "Disprosio (Dy)",
                "Holmio (Ho)",
                "Erbio (Er)",
                "Tulio (Tm)",
                "Iterbio (Yb)",
                "Lutecio (Lu)"
            }
        },
        {
            "Act�nidos", new List<string>
            {
                "Actinio (Ac)",
                "Torio (Th)",
                "Protactinio (Pa)",
                "Uranio (U)",
                "Neptunio (Np)",
                "Plutonio (Pu)",
                "Americio (Am)",
                "Curio (Cm)",
                "Berkelio (Bk)",
                "Californio (Cf)",
                "Einstenio (Es)",
                "Fermio (Fm)",
                "Mendelevio (Md)",
                "Nobelio (No)",
                "Lawrencio (Lr)"
            }
        },
        {
            "Metales de Transici�n", new List<string>
            {
                "Escandio (Sc)", "Titanio (Ti)", "Vanadio (V)", "Cromo (Cr)", "Manganeso (Mn)",
                "Hierro (Fe)", "Cobalto (Co)", "N�quel (Ni)", "Cobre (Cu)", "Zinc (Zn)",
                "Itrio (Y)", "Circonio (Zr)", "Niobio (Nb)", "Molibdeno (Mo)", "Tecnecio (Tc)",
                "Rutenio (Ru)", "Rodio (Rh)", "Paladio (Pd)", "Plata (Ag)", "Cadmio (Cd)",
                "Hafnio (Hf)", "Tantalio (Ta)", "Wolframio (W)", "Renio (Re)", "Osmio (Os)",
                "Iridio (Ir)", "Platino (Pt)", "Oro (Au)", "Mercurio (Hg)", "Rutherfordio (Rf)",
                "Dubnio (Db)", "Seaborgio (Sg)", "Bohrio (Bh)", "Hasio (Hs)", "Meitnerio (Mt)",
                "Darmstatio (Ds)", "Roentgenio (Rg)", "Copernicio (Cn)"
            }
        },
        {
            "Metales del bloque p", new List<string>
            {
                "Aluminio (Al)",
                "Galio (Ga)",
                "Indio (In)",
                "Esta�o (Sn)",
                "Talio (Tl)",
                "Plomo (Pb)",
                "Bismuto (Bi)",
                "Nihonio (Nh)",
                "Flerovio (Fl)",
                "Moscovio (Mc)",
                "Livermorio (Lv)"
            }
        },
        {
            "Metaloides", new List<string>
            {
                "Boro (B)",
                "Silicio (Si)",
                "Germanio (Ge)",
                "Ars�nico (As)",
                "Antimonio (Sb)",
                "Telurio (Te)",
                "Polonio (Po)",
                "Astato (At)"
            }
        },
        {
            "No metales", new List<string>
            {
                "Hidr�geno (H)",
                "Carbono (C)",
                "Nitr�geno (N)",
                "Ox�geno (O)",
                "F�sforo (P)",
                "Azufre (S)",
                "Selenio (Se)"
            }
        },
        {
            "Hal�genos", new List<string>
            {
                "Fl�or (F)",
                "Cloro (Cl)",
                "Bromo (Br)",
                "Yodo (I)",
                "Teneso (Ts)"
            }
        },
        {
            "Gases Nobles", new List<string>
            {
                "Helio (He)",
                "Ne�n (Ne)",
                "Arg�n (Ar)",
                "Kript�n (Kr)",
                "Xen�n (Xe)",
                "Rad�n (Rn)",
                "Oganes�n (Og)"
            }
        }
    };
    }

    private void PoblarCategorias()
    {
        categoriaDropdown.ClearOptions();

        // A�adimos una opci�n por defecto que no sea seleccionable
        categoriaDropdown.options.Add(new TMP_Dropdown.OptionData("Seleccione una categor�a..."));

        // A�adimos las categor�as reales
        categoriaDropdown.AddOptions(new List<string>(datosElementos.Keys));

        // Reseteamos el valor para que muestre la opci�n por defecto
        categoriaDropdown.value = 0;
        categoriaDropdown.RefreshShownValue();
    }

    // 7. ESTA FUNCI�N SE EJECUTA CUANDO CAMBIA LA CATEGOR�A
    private void OnCategoriaSeleccionada(int index)
    {
        // Si el �ndice es 0, es la opci�n "Seleccione...", as� que reseteamos todo
        if (index == 0)
        {
            elementoDropdown.interactable = false;
            elementoDropdown.ClearOptions();
            elementoDropdown.options.Add(new TMP_Dropdown.OptionData("Seleccione un elemento..."));
            elementoDropdown.value = 0;
            elementoDropdown.RefreshShownValue();
        }
        else
        {
            // Obtenemos el nombre de la categor�a seleccionada (restando 1 por la opci�n por defecto)
            string categoriaSeleccionada = categoriaDropdown.options[index].text;
            List<string> elementos = datosElementos[categoriaSeleccionada];

            // Poblamos el dropdown de elementos
            PoblarElementos(elementos);
            elementoDropdown.interactable = true;
        }

        // Despu�s de cada cambio, validamos si el bot�n Continuar debe activarse
        ValidarSeleccion();
    }

    private void PoblarElementos(List<string> elementos)
    {
        elementoDropdown.ClearOptions();
        elementoDropdown.options.Add(new TMP_Dropdown.OptionData("Seleccione un elemento..."));
        elementoDropdown.AddOptions(elementos);
        elementoDropdown.value = 0;
        elementoDropdown.RefreshShownValue();
    }

    // 8. ESTA FUNCI�N SE EJECUTA CUANDO CAMBIA EL ELEMENTO
    private void OnElementoSeleccionado(int index)
    {
        ValidarSeleccion();
    }

    // 9. VALIDA SI AMBAS SELECCIONES SON V�LIDAS PARA HABILITAR EL BOT�N
    private void ValidarSeleccion()
    {
        bool categoriaValida = categoriaDropdown.value > 0; // > 0 ignora la opci�n por defecto
        bool elementoValido = elementoDropdown.interactable && elementoDropdown.value > 0;

        btnContinuarMision.interactable = categoriaValida && elementoValido;
    }

    // 10. M�TODO P�BLICO PARA OBTENER LOS DATOS SELECCIONADOS
    public (string categoria, string elemento) ObtenerSeleccion()
    {
        if (btnContinuarMision.interactable)
        {
            string cat = categoriaDropdown.options[categoriaDropdown.value].text;
            string elem = elementoDropdown.options[elementoDropdown.value].text;
            return (cat, elem);
        }
        return (null, null); // O lanzar un error
    }
}