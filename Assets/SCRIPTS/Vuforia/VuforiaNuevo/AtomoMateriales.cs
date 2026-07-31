using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye y reutiliza los materiales del modelo atomico.
///
/// Los shaders viven en Resources/Shaders/ para que queden garantizados en el
/// build de Android sin depender de la lista de "Always Included Shaders".
/// Cada combinacion de shader y color se crea una sola vez y se comparte entre
/// todas las particulas, que es lo que permite que Unity agrupe los cientos de
/// esferas de un atomo pesado en muy pocas draw calls.
/// </summary>
public static class AtomoMateriales
{
    private const string RutaNucleon = "Shaders/AtomoNucleon";
    private const string RutaElectron = "Shaders/AtomoElectron";
    private const string RutaOrbita = "Shaders/AtomoOrbita";

    private static readonly Dictionary<string, Material> Cache =
        new Dictionary<string, Material>();

    private static readonly Dictionary<string, Shader> CacheShaders =
        new Dictionary<string, Shader>();

    /// <summary>
    /// Material para protones y neutrones: solido, con borde luminoso.
    /// </summary>
    public static Material Nucleon(Color color, float emision, float fuerzaBorde)
    {
        string clave = Clave("nucleon", color, emision, fuerzaBorde);

        Material material;
        if (TomarDeCache(clave, out material))
        {
            return material;
        }

        material = NuevoMaterial(RutaNucleon, "Standard");
        material.color = color;
        AsignarSiExiste(material, "_Color", color);
        AsignarSiExiste(material, "_ColorBorde", Aclarar(color, 0.55f));
        AsignarFloatSiExiste(material, "_Emision", emision);
        AsignarFloatSiExiste(material, "_FuerzaBorde", fuerzaBorde);
        material.enableInstancing = true;

        Cache[clave] = material;
        return material;
    }

    /// <summary>
    /// Material para electrones: nucleo emisivo con halo Fresnel.
    /// </summary>
    public static Material Electron(Color color, float emision, float fuerzaHalo)
    {
        string clave = Clave("electron", color, emision, fuerzaHalo);

        Material material;
        if (TomarDeCache(clave, out material))
        {
            return material;
        }

        material = NuevoMaterial(RutaElectron, "Unlit/Color");
        material.color = color;
        AsignarSiExiste(material, "_Color", color);
        AsignarSiExiste(material, "_ColorHalo", Aclarar(color, 0.7f));
        AsignarFloatSiExiste(material, "_Emision", emision);
        AsignarFloatSiExiste(material, "_FuerzaHalo", fuerzaHalo);
        material.enableInstancing = true;

        Cache[clave] = material;
        return material;
    }

    /// <summary>
    /// Material para el anillo de orbita: aditivo, con pulso de energia.
    /// </summary>
    public static Material Orbita(Color color, float brilloBase, float fuerzaPulso)
    {
        string clave = Clave("orbita", color, brilloBase, fuerzaPulso);

        Material material;
        if (TomarDeCache(clave, out material))
        {
            return material;
        }

        material = NuevoMaterial(RutaOrbita, "Sprites/Default");
        material.color = color;
        AsignarSiExiste(material, "_Color", color);
        AsignarSiExiste(material, "_ColorPulso", Aclarar(color, 0.85f));
        AsignarFloatSiExiste(material, "_Base", brilloBase);
        AsignarFloatSiExiste(material, "_FuerzaPulso", fuerzaPulso);

        Cache[clave] = material;
        return material;
    }

    /// <summary>
    /// Material aditivo simple para las estelas de los electrones.
    /// </summary>
    public static Material Estela(Color color)
    {
        string clave = Clave("estela", color, 0f, 0f);

        Material material;
        if (TomarDeCache(clave, out material))
        {
            return material;
        }

        material = NuevoMaterial(RutaOrbita, "Sprites/Default");
        material.color = color;
        AsignarSiExiste(material, "_Color", color);
        AsignarSiExiste(material, "_ColorPulso", Aclarar(color, 0.8f));
        // Sin pulso: la estela ya transmite el movimiento por si misma
        AsignarFloatSiExiste(material, "_Base", 1f);
        AsignarFloatSiExiste(material, "_FuerzaPulso", 0f);

        Cache[clave] = material;
        return material;
    }

    /// <summary>
    /// Convierte el color hexadecimal del JSON (por ejemplo "#FFB6C1") en Color.
    /// Devuelve <paramref name="porDefecto"/> si el texto no es valido.
    /// </summary>
    public static Color DesdeHex(string hex, Color porDefecto)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return porDefecto;
        }

        Color resultado;
        if (ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out resultado))
        {
            return resultado;
        }

        return porDefecto;
    }

    /// <summary>
    /// Libera los materiales generados. Conviene llamarlo al salir de la escena
    /// de AR para no dejar materiales huerfanos en memoria.
    /// </summary>
    public static void Limpiar()
    {
        foreach (KeyValuePair<string, Material> par in Cache)
        {
            if (par.Value != null)
            {
                Object.Destroy(par.Value);
            }
        }
        Cache.Clear();
    }

    // ------------------------------------------------------------------ utilidades

    private static bool TomarDeCache(string clave, out Material material)
    {
        if (Cache.TryGetValue(clave, out material) && material != null)
        {
            return true;
        }

        // Unity pudo destruir el material al descargar la escena anterior
        if (material == null)
        {
            Cache.Remove(clave);
        }
        return false;
    }

    private static Material NuevoMaterial(string rutaShader, string shaderRespaldo)
    {
        Shader shader;
        if (!CacheShaders.TryGetValue(rutaShader, out shader) || shader == null)
        {
            shader = Resources.Load<Shader>(rutaShader);

            if (shader == null)
            {
                Debug.LogWarning($"⚠️ [AtomoMateriales] No se encontró el shader '{rutaShader}' " +
                                 $"en Resources. Se usa '{shaderRespaldo}'.");
                shader = Shader.Find(shaderRespaldo);
            }

            CacheShaders[rutaShader] = shader;
        }

        return new Material(shader);
    }

    private static void AsignarSiExiste(Material material, string propiedad, Color valor)
    {
        if (material.HasProperty(propiedad))
        {
            material.SetColor(propiedad, valor);
        }
    }

    private static void AsignarFloatSiExiste(Material material, string propiedad, float valor)
    {
        if (material.HasProperty(propiedad))
        {
            material.SetFloat(propiedad, valor);
        }
    }

    /// <summary>Versión más clara de un color, para halos y bordes.</summary>
    private static Color Aclarar(Color color, float cantidad)
    {
        return new Color(
            Mathf.Lerp(color.r, 1f, cantidad),
            Mathf.Lerp(color.g, 1f, cantidad),
            Mathf.Lerp(color.b, 1f, cantidad),
            color.a);
    }

    private static string Clave(string tipo, Color color, float a, float b)
    {
        return string.Concat(
            tipo, "_",
            ColorUtility.ToHtmlStringRGBA(color), "_",
            a.ToString("F2"), "_",
            b.ToString("F2"));
    }
}