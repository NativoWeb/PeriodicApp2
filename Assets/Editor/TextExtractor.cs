#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Necesario para el JSON de Oraciones

// ==============================================================================
// 1. DEFINICIÓN DE ESTRUCTURAS PARA CADA JSON
// ==============================================================================

#region JSON Structures
// --- Para juego_tabla_periodica_preguntas.json ---
[System.Serializable]
public class NivelPregunta
{
    public int id; // <--- ¡AÑADE ESTA LÍNEA!
    public string elemento;
    public string pregunta;
    public List<string> opciones;
    public string respuesta_correcta;
}

[System.Serializable]
public class JuegoTablaPeriodicaRoot
{
    public List<NivelPregunta> niveles;
}

// --- Para preguntas_estilo_aprendizaje.json ---
[System.Serializable]
public class PreguntaEstilo
{
    public string textoPregunta;
}

[System.Serializable]
public class EstilosAprendizaje
{
    public List<PreguntaEstilo> Metodologia_Tradicional;
    public List<PreguntaEstilo> Aprendizaje_Basado_en_Proyectos;
    public List<PreguntaEstilo> Aprendizaje_Basado_en_Problemas;
    public List<PreguntaEstilo> Aprendizaje_Cooperativo;
    public List<PreguntaEstilo> Gamificacion;
}

[System.Serializable]
public class PreguntasEstiloRoot
{
    public EstilosAprendizaje preguntasEstilo;
}

// --- Para preguntas_estilo_aprendizaje_2.json ---
[System.Serializable]
public class AfirmacionEstilo
{
    public string textoAfirmacion;
}

[System.Serializable]
public class EstilosAprendizajeBinario
{
    public List<AfirmacionEstilo> Metodologia_Tradicional;
    public List<AfirmacionEstilo> Aprendizaje_Basado_en_Proyectos;
    public List<AfirmacionEstilo> Aprendizaje_Basado_en_Problemas;
    public List<AfirmacionEstilo> Aprendizaje_Cooperativo;
    public List<AfirmacionEstilo> Gamificacion;
}

[System.Serializable]
public class PreguntasEstiloBinarioRoot
{
    public EstilosAprendizajeBinario preguntasEstiloBinario;
}

// --- Para preguntas_tabla_periodica.json / _act.json / _mejores.json / _categorias1.json ---
[System.Serializable]
public class PreguntaGrupo
{
    public string textoPregunta;
    public List<string> opcionesRespuesta;
}

[System.Serializable]
public class ElementoGrupo
{
    public string elemento;
    public List<PreguntaGrupo> preguntas;
}

[System.Serializable]
public class GrupoPreguntas
{
    public string grupo;
    public List<ElementoGrupo> elementos;
}

[System.Serializable]
public class PreguntasTablaPeriodicaRoot
{
    public List<GrupoPreguntas> gruposPreguntas;
}

// --- Para Quimicados.json ---
[System.Serializable]
public class PreguntaQuimica
{
    public string pregunta;
    public List<string> opciones;
    public string explicacion;
}
[System.Serializable]
public class CategoriaQuimica
{
    public string nombre;
    public string elemento;
    public List<PreguntaQuimica> preguntas;
}
[System.Serializable]
public class QuimicadosRoot
{
    public List<CategoriaQuimica> categorias;
}

// --- Para Json_Informacion.json ---
[System.Serializable]
public class PropiedadInfo
{
    public string info;
}
[System.Serializable]
public class PropiedadesElemento
{
    public PropiedadInfo masa_atomica;
    public PropiedadInfo punto_fusion;
    public PropiedadInfo punto_ebullicion;
    public PropiedadInfo estado;
    public PropiedadInfo electronegatividad;
}
[System.Serializable]
public class InfoElemento
{
    public string nombre;
    public string simbolo;
    public string descripcion;
    public PropiedadesElemento propiedades;
}
[System.Serializable]
public class InfoCategoria
{
    // Usamos Dictionary porque las claves (nombres de elementos) son dinámicas
    [JsonExtensionData]
    public Dictionary<string, JToken> Elementos { get; set; }
}
[System.Serializable]
public class InfoCategorias
{
    [JsonProperty("Metales Alcalinos")] public InfoCategoria MetalesAlcalinos;
    // ... Añadir más categorías si es necesario
}
[System.Serializable]
public class InformacionRoot
{
    public InfoCategorias Categorias;
}

// --- Para Json_Logros.json ---
[System.Serializable]
public class LogroElemento
{
    public string nombre;
}
[System.Serializable]
public class LogroCategoria
{
    public string nombre;
}
[System.Serializable]
public class LogrosCategoria
{
    public Dictionary<string, LogroElemento> logros_elementos;
    public LogroCategoria logro_categoria;
}
[System.Serializable]
public class LogrosCategorias
{
    [JsonExtensionData]
    public Dictionary<string, JToken> Categorias { get; set; }
}
[System.Serializable]
public class LogrosRoot
{
    public LogrosCategorias Logros;
}

// --- Para Json_Misiones.json ---
[System.Serializable]
public class Mision
{
    public string titulo;
    public string descripcion;
}
[System.Serializable]
public class MisionesElemento
{
    public List<Mision> misiones;
}
[System.Serializable]
public class MisionesCategoria
{
    public Dictionary<string, MisionesElemento> Elementos;
    public Dictionary<string, Mision> MisionFinal;
}
[System.Serializable]
public class MisionesCategorias
{
    [JsonExtensionData]
    public Dictionary<string, JToken> Categorias { get; set; }
}
[System.Serializable]
public class MisionesRoot
{
    public MisionesCategorias Misiones;
}

#endregion

public class TextExtractor
{
    private const string CsvPath = "Assets/Localization/MasterLocalizationTable.csv";

    [MenuItem("Tools/Localization/ULTIMATE/Extract ALL Dynamic Data Texts")]
    private static void ExtractAllDynamicTexts()
    {
        Debug.Log("Iniciando extracción de textos de TODAS las fuentes de datos dinámicas...");
        Dictionary<string, string> keys = LoadExistingKeys();

        // Especifica la carpeta donde están tus JSON
        string dataPath = "Assets/Resources/Plantillas_Json"; // <-- ¡¡CAMBIA ESTO A TU CARPETA!!

        // Llama a una función de extracción para cada tipo de JSON
        ProcessJuegoTablaPeriodica(Path.Combine(dataPath, "juego_tabla_periodica_preguntas.json"), ref keys);
        ProcessPreguntasEstilo(Path.Combine(dataPath, "preguntas_estilo_aprendizaje.json"), ref keys);
        ProcessPreguntasEstiloBinario(Path.Combine(dataPath, "preguntas_estilo_aprendizaje_2.json"), ref keys);

        // Procesar todos los JSON de preguntas de la tabla periódica
        ProcessPreguntasTabla(Path.Combine(dataPath, "preguntas_tabla_periodica.json"), ref keys);
        ProcessPreguntasTabla(Path.Combine(dataPath, "preguntas_tabla_periodica_act.json"), ref keys);
        ProcessPreguntasTabla(Path.Combine(dataPath, "preguntas_tabla_periodica_mejores.json"), ref keys);
        ProcessPreguntasTabla(Path.Combine(dataPath, "preguntas_tabla_periodica_categorias1.json"), ref keys);
        ProcessPreguntasTabla(Path.Combine(dataPath, "preguntas_tabla_periodica_categorias.json"), ref keys);

        ProcessOraciones(Path.Combine(dataPath, "Oraciones.json"), ref keys);
        ProcessQuimicados(Path.Combine(dataPath, "Quimicados.json"), ref keys);
        ProcessInformacion(Path.Combine(dataPath, "Json_Informacion.json"), ref keys);
        ProcessLogros(Path.Combine(dataPath, "Json_Logros.json"), ref keys);
        ProcessMisiones(Path.Combine(dataPath, "Json_Misiones.json"), ref keys);

        SaveKeysToFile(keys);
        Debug.Log($"Extracción de datos dinámicos completada. Ahora hay {keys.Count} claves en total.");
    }

    // ==============================================================================
    // 2. FUNCIONES DE PROCESADO PARA CADA JSON
    // ==============================================================================

    #region JSON Processors

    private static void ProcessJuegoTablaPeriodica(string jsonPath, ref Dictionary<string, string> keys)
    {
        if (!File.Exists(jsonPath)) return;
        Debug.Log($"Procesando: {jsonPath}");

        var root = JsonConvert.DeserializeObject<JuegoTablaPeriodicaRoot>(File.ReadAllText(jsonPath));
        if (root?.niveles == null) return;

        foreach (var nivel in root.niveles)
        {
            AddKey(ref keys, $"jtp_q_{nivel.id}_pregunta", nivel.pregunta);
            for (int i = 0; i < nivel.opciones.Count; i++)
            {
                AddKey(ref keys, $"jtp_q_{nivel.id}_opcion_{i}", nivel.opciones[i]);
            }
        }
    }

    private static void ProcessPreguntasEstilo(string jsonPath, ref Dictionary<string, string> keys)
    {
        if (!File.Exists(jsonPath)) return;
        Debug.Log($"Procesando: {jsonPath}");

        var root = JsonConvert.DeserializeObject<PreguntasEstiloRoot>(File.ReadAllText(jsonPath));
        if (root?.preguntasEstilo == null) return;

        ProcessEstilo(root.preguntasEstilo.Metodologia_Tradicional, "tradicional", ref keys);
        ProcessEstilo(root.preguntasEstilo.Aprendizaje_Basado_en_Proyectos, "proyectos", ref keys);
        ProcessEstilo(root.preguntasEstilo.Aprendizaje_Basado_en_Problemas, "problemas", ref keys);
        ProcessEstilo(root.preguntasEstilo.Aprendizaje_Cooperativo, "cooperativo", ref keys);
        ProcessEstilo(root.preguntasEstilo.Gamificacion, "gamificacion", ref keys);
    }

    private static void ProcessPreguntasEstiloBinario(string jsonPath, ref Dictionary<string, string> keys)
    {
        if (!File.Exists(jsonPath)) return;
        Debug.Log($"Procesando: {jsonPath}");

        var root = JsonConvert.DeserializeObject<PreguntasEstiloBinarioRoot>(File.ReadAllText(jsonPath));
        if (root?.preguntasEstiloBinario == null) return;

        ProcessEstiloBinario(root.preguntasEstiloBinario.Metodologia_Tradicional, "tradicional_bin", ref keys);
        ProcessEstiloBinario(root.preguntasEstiloBinario.Aprendizaje_Basado_en_Proyectos, "proyectos_bin", ref keys);
        ProcessEstiloBinario(root.preguntasEstiloBinario.Aprendizaje_Basado_en_Problemas, "problemas_bin", ref keys);
        ProcessEstiloBinario(root.preguntasEstiloBinario.Aprendizaje_Cooperativo, "cooperativo_bin", ref keys);
        ProcessEstiloBinario(root.preguntasEstiloBinario.Gamificacion, "gamificacion_bin", ref keys);
    }

    private static void ProcessPreguntasTabla(string jsonPath, ref Dictionary<string, string> keys)
    {
        if (!File.Exists(jsonPath)) return;
        Debug.Log($"Procesando: {jsonPath}");

        PreguntasTablaPeriodicaRoot root;
        try
        {
            root = JsonConvert.DeserializeObject<PreguntasTablaPeriodicaRoot>(File.ReadAllText(jsonPath));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Extractor] Fallo al leer el JSON '{jsonPath}'. Error: {e.Message}");
            return; // Salir si el JSON no se puede leer
        }

        // VERIFICACIÓN #1: Asegurarse de que el objeto raíz y la lista no son nulos
        if (root?.gruposPreguntas == null)
        {
            Debug.LogWarning($"[Extractor] El archivo '{jsonPath}' no contiene 'gruposPreguntas' o está vacío.");
            return;
        }

        foreach (var grupo in root.gruposPreguntas)
        {
            // VERIFICACIÓN #2: Saltar cualquier entrada de grupo que sea nula
            if (grupo == null) continue;

            AddKey(ref keys, $"ptp_{SanitizeKey(grupo.grupo)}_title", grupo.grupo);

            // VERIFICACIÓN #3: Asegurarse de que la lista de elementos del grupo no es nula
            if (grupo.elementos == null) continue;

            foreach (var elemento in grupo.elementos)
            {
                // VERIFICACIÓN #4: Saltar cualquier entrada de elemento que sea nula
                if (elemento == null) continue;

                AddKey(ref keys, $"ptp_{SanitizeKey(elemento.elemento)}_name", elemento.elemento);

                // VERIFICACIÓN #5: Asegurarse de que la lista de preguntas no es nula
                if (elemento.preguntas == null) continue;

                for (int i = 0; i < elemento.preguntas.Count; i++)
                {
                    var pregunta = elemento.preguntas[i];
                    if (pregunta == null) continue; // Saltar preguntas nulas

                    AddKey(ref keys, $"ptp_{SanitizeKey(elemento.elemento)}_q{i}_text", pregunta.textoPregunta);

                    if (pregunta.opcionesRespuesta == null) continue; // Saltar si no hay opciones

                    for (int j = 0; j < pregunta.opcionesRespuesta.Count; j++)
                    {
                        AddKey(ref keys, $"ptp_{SanitizeKey(elemento.elemento)}_q{i}_opt{j}", pregunta.opcionesRespuesta[j]);
                    }
                }
            }
        }
    }

    private static void ProcessOraciones(string jsonPath, ref Dictionary<string, string> keys)
    {
        if (!File.Exists(jsonPath)) return;
        Debug.Log($"Procesando: {jsonPath}");

        var root = JObject.Parse(File.ReadAllText(jsonPath));
        foreach (var prop in root.Properties())
        {
            string elementoKey = SanitizeKey(prop.Name);
            var oraciones = prop.Value.ToObject<List<JObject>>();
            for (int i = 0; i < oraciones.Count; i++)
            {
                AddKey(ref keys, $"oracion_{elementoKey}_{i}_text", oraciones[i]["oracion"].ToString());
                var opciones = oraciones[i]["opciones"].ToObject<List<string>>();
                for (int j = 0; j < opciones.Count; j++)
                {
                    AddKey(ref keys, $"oracion_{elementoKey}_{i}_opt{j}", opciones[j]);
                }
            }
        }
    }

    private static void ProcessQuimicados(string jsonPath, ref Dictionary<string, string> keys)
    {
        if (!File.Exists(jsonPath)) return;
        Debug.Log($"Procesando: {jsonPath}");

        var root = JsonConvert.DeserializeObject<QuimicadosRoot>(File.ReadAllText(jsonPath));
        if (root?.categorias == null) return;

        AddKey(ref keys, $"quimicados_title", JObject.Parse(File.ReadAllText(jsonPath))["titulo"].ToString());
        AddKey(ref keys, $"quimicados_desc", JObject.Parse(File.ReadAllText(jsonPath))["descripcion"].ToString());

        foreach (var categoria in root.categorias)
        {
            AddKey(ref keys, $"quimicados_cat_{SanitizeKey(categoria.nombre)}", categoria.nombre);
            AddKey(ref keys, $"quimicados_elem_{SanitizeKey(categoria.elemento)}", categoria.elemento);
            for (int i = 0; i < categoria.preguntas.Count; i++)
            {
                var pregunta = categoria.preguntas[i];
                AddKey(ref keys, $"quimicados_{SanitizeKey(categoria.elemento)}_q{i}_text", pregunta.pregunta);
                AddKey(ref keys, $"quimicados_{SanitizeKey(categoria.elemento)}_q{i}_expl", pregunta.explicacion);
                for (int j = 0; j < pregunta.opciones.Count; j++)
                {
                    AddKey(ref keys, $"quimicados_{SanitizeKey(categoria.elemento)}_q{i}_opt{j}", pregunta.opciones[j]);
                }
            }
        }
    }

    private static void ProcessInformacion(string jsonPath, ref Dictionary<string, string> keys)
    {
        if (!File.Exists(jsonPath)) return;
        Debug.Log($"Procesando: {jsonPath}");

        JObject root = JObject.Parse(File.ReadAllText(jsonPath));
        var categorias = root["Informacion"]["Categorias"].Value<JObject>();

        foreach (var categoriaProp in categorias.Properties())
        {
            AddKey(ref keys, $"info_cat_{SanitizeKey(categoriaProp.Name)}", categoriaProp.Name);
            var elementos = categoriaProp.Value.Value<JObject>();
            foreach (var elementoProp in elementos.Properties())
            {
                var elemento = elementoProp.Value.ToObject<InfoElemento>();
                string elemKey = SanitizeKey(elemento.nombre);
                AddKey(ref keys, $"info_elem_{elemKey}_name", elemento.nombre);
                AddKey(ref keys, $"info_elem_{elemKey}_desc", elemento.descripcion);
                AddKey(ref keys, $"info_elem_{elemKey}_prop_mass", elemento.propiedades.masa_atomica.info);
                AddKey(ref keys, $"info_elem_{elemKey}_prop_fusion", elemento.propiedades.punto_fusion.info);
                AddKey(ref keys, $"info_elem_{elemKey}_prop_boil", elemento.propiedades.punto_ebullicion.info);
                AddKey(ref keys, $"info_elem_{elemKey}_prop_state", elemento.propiedades.estado.info);
                AddKey(ref keys, $"info_elem_{elemKey}_prop_elec", elemento.propiedades.electronegatividad.info);
            }
        }
    }

    private static void ProcessLogros(string jsonPath, ref Dictionary<string, string> keys)
    {
        if (!File.Exists(jsonPath)) return;
        Debug.Log($"Procesando: {jsonPath}");

        JObject root = JObject.Parse(File.ReadAllText(jsonPath));
        var categorias = root["Logros"]["Categorias"].Value<JObject>();

        foreach (var catProp in categorias.Properties())
        {
            AddKey(ref keys, $"logro_cat_{SanitizeKey(catProp.Name)}_title", catProp.Name);
            var categoria = catProp.Value.ToObject<LogrosCategoria>();
            AddKey(ref keys, $"logro_cat_{SanitizeKey(catProp.Name)}_main", categoria.logro_categoria.nombre);
            foreach (var logroElem in categoria.logros_elementos)
            {
                AddKey(ref keys, $"logro_elem_{SanitizeKey(logroElem.Key)}", logroElem.Value.nombre);
            }
        }
    }

    private static void ProcessMisiones(string jsonPath, ref Dictionary<string, string> keys)
    {
        if (!File.Exists(jsonPath)) return;
        Debug.Log($"Procesando: {jsonPath}");

        JObject root = JObject.Parse(File.ReadAllText(jsonPath));
        var categorias = root["Misiones"]["Categorias"].Value<JObject>();

        foreach (var catProp in categorias.Properties())
        {
            AddKey(ref keys, $"mision_cat_{SanitizeKey(catProp.Name)}_title", catProp.Name);
            var categoria = catProp.Value.ToObject<MisionesCategoria>();

            if (categoria.Elementos != null)
            {
                foreach (var elem in categoria.Elementos)
                {
                    for (int i = 0; i < elem.Value.misiones.Count; i++)
                    {
                        var mision = elem.Value.misiones[i];
                        string misionKey = $"mision_{SanitizeKey(catProp.Name)}_{SanitizeKey(elem.Key)}_{i}";
                        AddKey(ref keys, $"{misionKey}_title", mision.titulo);
                        AddKey(ref keys, $"{misionKey}_desc", mision.descripcion);
                    }
                }
            }

            if (categoria.MisionFinal != null)
            {
                foreach (var misionFinal in categoria.MisionFinal)
                {
                    AddKey(ref keys, $"mision_final_{SanitizeKey(catProp.Name)}_title", misionFinal.Value.titulo);
                    if (misionFinal.Value.descripcion != null) AddKey(ref keys, $"mision_final_{SanitizeKey(catProp.Name)}_desc", misionFinal.Value.descripcion);
                }
            }
        }
    }

    // --- Helpers for JSON processors ---
    private static void ProcessEstilo(List<PreguntaEstilo> lista, string tipo, ref Dictionary<string, string> keys)
    {
        if (lista == null) return;
        for (int i = 0; i < lista.Count; i++)
        {
            AddKey(ref keys, $"estilo_{tipo}_q{i}", lista[i].textoPregunta);
        }
    }

    private static void ProcessEstiloBinario(List<AfirmacionEstilo> lista, string tipo, ref Dictionary<string, string> keys)
    {
        if (lista == null) return;
        for (int i = 0; i < lista.Count; i++)
        {
            AddKey(ref keys, $"estilo_{tipo}_q{i}", lista[i].textoAfirmacion);
        }
    }
    #endregion


    // ==============================================================================
    // 3. MÉTODOS DE AYUDA GENERALES
    // ==============================================================================

    #region Helper Methods

    private static void AddKey(ref Dictionary<string, string> keys, string key, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (!keys.ContainsKey(key))
        {
            keys.Add(key, value);
        }
    }

    private static string SanitizeKey(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.ToLower().Replace(" ", "_").Replace("(", "").Replace(")", "").Replace("-", "_").Replace(".", "");
    }

    private static Dictionary<string, string> LoadExistingKeys()
    {
        var keys = new Dictionary<string, string>();
        if (File.Exists(CsvPath))
        {
            string[] lines = File.ReadAllLines(CsvPath, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] parts = lines[i].Split(new[] { ',' }, 2);
                if (parts.Length == 2 && !keys.ContainsKey(parts[0]))
                {
                    keys.Add(parts[0], UnescapeCsv(parts[1]));
                }
            }
        }
        return keys;
    }

    private static void SaveKeysToFile(Dictionary<string, string> keys)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CsvPath));
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Key,es,en"); // Cabecera para Unity Localization
        foreach (var pair in keys.OrderBy(p => p.Key)) // Ordenar alfabéticamente
        {
            sb.AppendLine($"{pair.Key},{EscapeCsv(pair.Value)},");
        }
        File.WriteAllText(CsvPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
    }

    private static string EscapeCsv(string text) => $"\"{text.Replace("\"", "\"\"")}\"";
    private static string UnescapeCsv(string text)
    {
        if (text.StartsWith("\"") && text.EndsWith("\""))
        {
            text = text.Substring(1, text.Length - 2);
            return text.Replace("\"\"", "\"");
        }
        return text;
    }

    #endregion
}
#endif