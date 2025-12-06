using UnityEngine;
using QuantumAI.Core;
using QuantumAI.UI;
using QuantumAI.Config;

/// <summary>
/// Script de diagnóstico para verificar la configuración de Quantum AI
/// Adjunta este script a cualquier GameObject y ejecútalo para ver qué falta
/// </summary>
public class QuantumAIDiagnostico : MonoBehaviour
{
    [Header("Ejecutar Diagnóstico")]
    [Tooltip("Haz click en este botón en el Inspector")]
    public bool ejecutarDiagnostico = false;

    private void Update()
    {
        if (ejecutarDiagnostico)
        {
            ejecutarDiagnostico = false;
            EjecutarDiagnostico();
        }
    }

    [ContextMenu("Ejecutar Diagnóstico Quantum AI")]
    public void EjecutarDiagnostico()
    {
        Debug.Log("=== 🔍 DIAGNÓSTICO QUANTUM AI ===\n");

        int problemas = 0;
        int advertencias = 0;

        // 1. Verificar QuantumAICore
        Debug.Log("1️⃣ Verificando QuantumAICore...");
        QuantumAICore core = FindObjectOfType<QuantumAICore>();
        if (core == null)
        {
            Debug.LogError("❌ PROBLEMA: No se encontró QuantumAICore en la escena");
            Debug.LogError("   SOLUCIÓN: Crea un GameObject vacío llamado 'QuantumAI' y agrégale el componente 'QuantumAICore'");
            problemas++;
        }
        else
        {
            Debug.Log("✅ QuantumAICore encontrado: " + core.gameObject.name);

            // Verificar configuración
            if (core.Config == null)
            {
                Debug.LogError("❌ PROBLEMA: QuantumAICore no tiene AIConfig asignado");
                Debug.LogError("   SOLUCIÓN: Asigna el QuantumAIConfig desde Resources en el Inspector");
                problemas++;
            }
            else
            {
                Debug.Log("✅ AIConfig cargado correctamente");
                Debug.Log($"   - API Key configurada: {!string.IsNullOrEmpty(core.Config.geminiApiKey)}");
                Debug.Log($"   - Modo debug: {core.Config.enableDebugLogs}");
                Debug.Log($"   - Simulación: {core.Config.simulateGeminiResponses}");
            }
        }

        // 2. Verificar QuantumUIController
        Debug.Log("\n2️⃣ Verificando QuantumUIController...");
        QuantumUIController uiController = FindObjectOfType<QuantumUIController>();
        if (uiController == null)
        {
            Debug.LogError("❌ PROBLEMA: No se encontró QuantumUIController en la escena");
            Debug.LogError("   SOLUCIÓN: Encuentra el Canvas 'QuantumUI' y agrégale el componente 'QuantumUIController'");
            problemas++;
        }
        else
        {
            Debug.Log("✅ QuantumUIController encontrado: " + uiController.gameObject.name);
        }

        // 3. Verificar FloatingAssistant
        Debug.Log("\n3️⃣ Verificando FloatingAssistant (Icono Flotante)...");
        FloatingAssistant floatingIcon = FindObjectOfType<FloatingAssistant>();
        if (floatingIcon == null)
        {
            Debug.LogWarning("⚠️ ADVERTENCIA: No se encontró FloatingAssistant");
            Debug.LogWarning("   RECOMENDACIÓN: Crea un GameObject hijo de QuantumUI llamado 'FloatingIcon' con los componentes:");
            Debug.LogWarning("   - Image (para el icono visual)");
            Debug.LogWarning("   - Button (para hacerlo clickeable)");
            Debug.LogWarning("   - FloatingAssistant (script)");
            advertencias++;
        }
        else
        {
            Debug.Log("✅ FloatingAssistant encontrado: " + floatingIcon.gameObject.name);
            if (!floatingIcon.gameObject.activeSelf)
            {
                Debug.LogWarning("⚠️ El FloatingAssistant está desactivado");
                advertencias++;
            }
        }

        // 4. Verificar ChatPanel
        Debug.Log("\n4️⃣ Verificando ChatPanel...");
        ChatPanel chatPanel = FindObjectOfType<ChatPanel>(true); // true = incluir inactivos
        if (chatPanel == null)
        {
            Debug.LogWarning("⚠️ ADVERTENCIA: No se encontró ChatPanel");
            Debug.LogWarning("   RECOMENDACIÓN: Crea un panel de chat como hijo de QuantumUI");
            advertencias++;
        }
        else
        {
            Debug.Log("✅ ChatPanel encontrado: " + chatPanel.gameObject.name);
        }

        // 5. Verificar Canvas de QuantumUI
        Debug.Log("\n5️⃣ Verificando Canvas de QuantumUI...");
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas quantumCanvas = null;
        foreach (var canvas in canvases)
        {
            if (canvas.gameObject.name.Contains("QuantumUI") || canvas.gameObject.name.Contains("Quantum"))
            {
                quantumCanvas = canvas;
                break;
            }
        }

        if (quantumCanvas == null)
        {
            Debug.LogError("❌ PROBLEMA: No se encontró un Canvas para Quantum UI");
            Debug.LogError("   SOLUCIÓN: Crea un Canvas llamado 'QuantumUI' con:");
            Debug.LogError("   - Render Mode: Screen Space - Overlay");
            Debug.LogError("   - Sort Order: 999");
            problemas++;
        }
        else
        {
            Debug.Log("✅ Canvas encontrado: " + quantumCanvas.gameObject.name);
            Debug.Log($"   - Render Mode: {quantumCanvas.renderMode}");
            Debug.Log($"   - Sort Order: {quantumCanvas.sortingOrder}");

            if (quantumCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning("⚠️ El Canvas no está en modo Screen Space - Overlay");
                Debug.LogWarning("   RECOMENDACIÓN: Cambia el Render Mode a 'Screen Space - Overlay'");
                advertencias++;
            }

            if (quantumCanvas.sortingOrder < 100)
            {
                Debug.LogWarning("⚠️ El Sort Order es bajo, la UI podría quedar detrás de otros elementos");
                Debug.LogWarning($"   RECOMENDACIÓN: Aumenta Sort Order a 999 (actual: {quantumCanvas.sortingOrder})");
                advertencias++;
            }
        }

        // 6. Verificar DOTween
        Debug.Log("\n6️⃣ Verificando DOTween...");
        #if DOTWEEN
        Debug.Log("✅ DOTween está instalado");
        #else
        Debug.LogWarning("⚠️ ADVERTENCIA: DOTween no está instalado");
        Debug.LogWarning("   Las animaciones no funcionarán correctamente");
        Debug.LogWarning("   SOLUCIÓN: Instala DOTween desde el Asset Store o Package Manager");
        advertencias++;
        #endif

        // 7. Verificar MiniLM
        Debug.Log("\n7️⃣ Verificando MiniLMEmbedder...");
        MiniLMEmbedder miniLM = FindObjectOfType<MiniLMEmbedder>();
        if (miniLM == null)
        {
            Debug.LogWarning("⚠️ ADVERTENCIA: No se encontró MiniLMEmbedder");
            Debug.LogWarning("   El sistema funcionará solo con Gemini API");
            advertencias++;
        }
        else
        {
            Debug.Log("✅ MiniLMEmbedder encontrado");
        }

        // 8. Verificar persistencia entre escenas
        Debug.Log("\n8️⃣ Verificando DontDestroyOnLoad...");
        if (core != null)
        {
            if (core.transform.parent != null)
            {
                Debug.LogWarning("⚠️ ADVERTENCIA: QuantumAICore tiene un padre");
                Debug.LogWarning("   Esto puede causar problemas con DontDestroyOnLoad");
                Debug.LogWarning("   RECOMENDACIÓN: QuantumAI debe ser un GameObject raíz (sin padre)");
                advertencias++;
            }
            else
            {
                Debug.Log("✅ QuantumAI es un GameObject raíz");
            }
        }

        // Resumen
        Debug.Log("\n" + new string('=', 50));
        Debug.Log("📊 RESUMEN DEL DIAGNÓSTICO");
        Debug.Log(new string('=', 50));

        if (problemas == 0 && advertencias == 0)
        {
            Debug.Log("✅ ¡Todo está perfecto! Quantum AI está correctamente configurado");
            Debug.Log("   Puedes ejecutar el juego y el icono flotante debería aparecer");
        }
        else
        {
            Debug.Log($"❌ Problemas críticos encontrados: {problemas}");
            Debug.Log($"⚠️ Advertencias encontradas: {advertencias}");
            Debug.Log("\nRevisa los mensajes anteriores para ver las soluciones sugeridas");
        }

        Debug.Log(new string('=', 50) + "\n");
    }

    // Método para crear la estructura básica automáticamente
    [ContextMenu("🔧 Crear Estructura Básica de Quantum AI")]
    public void CrearEstructuraBasica()
    {
        Debug.Log("🔧 Creando estructura básica de Quantum AI...\n");

        // 1. Crear o encontrar QuantumAI
        QuantumAICore core = FindObjectOfType<QuantumAICore>();
        GameObject quantumAI;

        if (core == null)
        {
            quantumAI = new GameObject("QuantumAI");
            core = quantumAI.AddComponent<QuantumAICore>();
            Debug.Log("✅ QuantumAI GameObject creado con QuantumAICore");
        }
        else
        {
            quantumAI = core.gameObject;
            Debug.Log("✅ QuantumAI ya existe");
        }

        // 2. Crear o encontrar Canvas QuantumUI
        Canvas quantumCanvas = null;
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            if (canvas.gameObject.name.Contains("QuantumUI"))
            {
                quantumCanvas = canvas;
                break;
            }
        }

        if (quantumCanvas == null)
        {
            GameObject canvasObj = new GameObject("QuantumUI");
            quantumCanvas = canvasObj.AddComponent<Canvas>();
            quantumCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            quantumCanvas.sortingOrder = 999;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            QuantumUIController uiController = canvasObj.AddComponent<QuantumUIController>();

            Debug.Log("✅ Canvas QuantumUI creado");
        }
        else
        {
            Debug.Log("✅ Canvas QuantumUI ya existe");

            // Agregar QuantumUIController si no existe
            if (quantumCanvas.GetComponent<QuantumUIController>() == null)
            {
                quantumCanvas.gameObject.AddComponent<QuantumUIController>();
                Debug.Log("✅ QuantumUIController agregado al Canvas");
            }
        }

        Debug.Log("\n✅ Estructura básica creada");
        Debug.Log("⚠️ NOTA: Aún necesitas crear manualmente:");
        Debug.Log("   - FloatingIcon (Image + Button + FloatingAssistant)");
        Debug.Log("   - ChatPanel con sus elementos UI");
        Debug.Log("   - Asignar referencias en el Inspector");
        Debug.Log("\nConsulta el archivo QUANTUM_AI_SETUP.md para más detalles\n");
    }
}
