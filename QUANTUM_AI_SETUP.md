# 🚀 QUANTUM AI - GUÍA DE CONFIGURACIÓN

## ✅ FASE 1 COMPLETADA

Se han creado todos los archivos base del sistema de IA omnipresente Quantum:

### 📁 Archivos Creados

```
Assets/SCRIPTS/AI/
├── Core/
│   └── QuantumAICore.cs          ✅ Singleton central del sistema
├── Models/
│   └── StudentContext.cs         ✅ Modelo de datos del estudiante
├── Config/
│   └── AIConfig.cs               ✅ ScriptableObject de configuración
├── Services/
│   ├── GeminiService.cs          ✅ Cliente API de Gemini
│   ├── GeminiPromptBuilder.cs    ✅ Constructor de prompts contextuales
│   └── ResponseCache.cs          ✅ Sistema de caché
└── UI/
    ├── QuantumUIController.cs    ✅ Controlador de UI
    ├── FloatingAssistant.cs      ✅ Icono flotante
    └── ChatPanel.cs              ✅ Panel de chat
```

---

## 🔧 PASOS DE CONFIGURACIÓN

### **1. Instalar Dependencias**

#### A) DOTween (Animaciones)
```
1. Window > Package Manager
2. Buscar "DOTween" en Asset Store
3. Instalar DOTween (FREE) v1.2+
4. Tools > Demigiant > DOTween Utility Panel > Setup DOTween
```

**Alternativa sin DOTween:**
Si no quieres usar DOTween, reemplaza las animaciones con `LeanTween` o elimina las animaciones (comentar líneas con `DOTween`/`DOVirtual`/`DOFade`).

#### B) SimpleJSON (Ya lo tienes)
✅ Ya está incluido en tu proyecto

---

### **2. Crear la Configuración de IA**

```
1. Click derecho en Project > Create > Quantum AI > AI Config
2. Nombrar: "QuantumAIConfig"
3. Mover a: Assets/Resources/QuantumAIConfig.asset
```

#### **Configurar AIConfig:**

**API de Gemini:**
1. Ir a: https://aistudio.google.com/app/apikey
2. Crear API Key (gratis)
3. Copiar la key
4. En Unity, seleccionar QuantumAIConfig
5. Pegar en campo "Gemini Api Key"

**Configuración recomendada inicial:**
```
Gemini API Configuration:
  Gemini Api Key: [TU_API_KEY_AQUÍ]
  Gemini Model: gemini-1.5-flash
  Api Timeout: 30
  Max Tokens: 500
  Temperature: 0.7

Estrategia Híbrida:
  Use Hybrid Approach: ✓
  Prioritize Local Responses: ✓
  Enable Response Cache: ✓
  Cache Duration Minutes: 60
  Max Cache Entries: 100

Configuración de Intervenciones:
  Default Interaction Preference: Equilibrado
  Inactivity Threshold: 10
  Stuck Threshold: 120
  Failure Threshold: 3
  Enable Celebrations: ✓
  Enable Proactive Recommendations: ✓

Configuración de UI:
  Icon Position: BottomRight
  Icon Size: 60
  Enable Icon Animations: ✓
  Show Notification Badge: ✓
  Typing Speed: 50

Configuración de Debug:
  Enable Debug Logs: ✓
  Show Gemini Prompts: ✗ (activar solo para debug)
  Simulate Gemini Responses: ✓ (para testing sin gastar API)
```

---

### **3. Crear el GameObject de Quantum**

#### A) Crear el Core

```
1. Hierarchy > Click derecho > Create Empty
2. Nombrar: "QuantumAI"
3. Add Component > QuantumAICore
4. En Inspector, asignar el AIConfig que creaste
```

#### B) Crear la UI

```
1. Hierarchy > Click derecho en QuantumAI > UI > Canvas
2. Nombrar: "QuantumUI"
3. Canvas settings:
   - Render Mode: Screen Space - Overlay
   - Canvas Scaler:
     - UI Scale Mode: Scale With Screen Size
     - Reference Resolution: 1080 x 1920
     - Match: 0.5

4. Add Component > QuantumUIController
```

#### C) Crear el Icono Flotante

```
1. Hierarchy > Click derecho en QuantumUI > UI > Image
2. Nombrar: "FloatingIcon"
3. Configurar RectTransform:
   - Width: 60
   - Height: 60
4. Add Component > FloatingAssistant
5. Add Component > Button
6. En FloatingAssistant Inspector:
   - Icon Image: [Arrastrar el componente Image]
   - Crear hijo: GameObject "NotificationBadge" (Circle Image rojo pequeño)
```

#### D) Crear el Chat Panel

```
1. Hierarchy > Click derecho en QuantumUI > UI > Panel
2. Nombrar: "ChatPanel"
3. Configurar:
   - Anchors: Stretch bottom
   - Height: 600
4. Add Component > ChatPanel
5. Dentro del panel crear:

   a) Scroll View (para mensajes):
      - Hierarchy > Click derecho en ChatPanel > UI > Scroll View
      - Nombrar: "MessagesScrollView"
      - Configurar Vertical Layout Group en Content

   b) Input Area (abajo):
      - Panel "InputArea" (Height: 80)
      - TMP Input Field "MessageInput"
      - Button "SendButton" (texto: "Enviar")
      - Button "VoiceButton" (icono: micrófono)

   c) Top Bar:
      - Text "Quantum AI"
      - Button "CloseButton" (X)

6. En ChatPanel Inspector, asignar todas las referencias
```

---

### **4. Crear Prefabs de Mensajes**

#### A) Burbuja de Usuario

```
1. Create > UI > Panel (nombrar: UserMessageBubble)
2. Configurar:
   - Background color: Azul claro
   - Bordes redondeados (usar Slice en sprite)
   - Content Size Fitter: Preferred Size vertical
3. Agregar TextMeshPro Text hijo
4. Guardar como prefab: Assets/SCRIPTS/AI/UI/Prefabs/UserMessageBubble.prefab
5. Asignar en ChatPanel > User Message Prefab
```

#### B) Burbuja de IA

```
1. Create > UI > Panel (nombrar: AIMessageBubble)
2. Configurar:
   - Background color: Gris claro
   - Bordes redondeados
   - Content Size Fitter: Preferred Size vertical
3. Agregar TextMeshPro Text hijo
4. Guardar como prefab: Assets/SCRIPTS/AI/UI/Prefabs/AIMessageBubble.prefab
5. Asignar en ChatPanel > AI Message Prefab
```

#### C) Mensaje de Sistema

```
1. Create > UI > Panel (nombrar: SystemMessageBubble)
2. Configurar:
   - Background color: Amarillo pálido
   - Italic text style
3. Guardar como prefab: Assets/SCRIPTS/AI/UI/Prefabs/SystemMessageBubble.prefab
4. Asignar en ChatPanel > System Message Prefab
```

---

### **5. Conectar con MiniLM Existente**

Tu `MiniLMEmbedder` ya existe. Solo necesitas asegurar que esté en la escena:

```
1. Buscar el GameObject que tiene MiniLMEmbedder
2. Asegurar que esté activo al inicio de la app
3. QuantumAICore lo encontrará automáticamente con FindObjectOfType
```

---

### **6. Testing Básico**

#### Modo de Simulación (sin gastar API):

```
1. En QuantumAIConfig, activar:
   - Simulate Gemini Responses: ✓

2. Play mode en Unity

3. Click en el icono flotante de Quantum

4. Escribir: "Hola"

5. Deberías ver:
   - Burbuja del usuario con tu mensaje
   - Animación del icono (thinking)
   - Respuesta simulada de Quantum
```

#### Testing con Gemini Real:

```
1. En QuantumAIConfig:
   - Simulate Gemini Responses: ✗
   - Pegar tu API Key real

2. Play mode

3. Escribir: "Explícame qué es el oxígeno"

4. Deberías ver:
   - Respuesta real de Gemini (tarda 1-2 segundos)
   - Efecto de escritura gradual
```

---

## 🔗 INTEGRACIÓN CON SISTEMAS EXISTENTES (Próximos Pasos)

### **FASE 2: Integrar con Managers**

Ahora que tienes el core funcionando, el siguiente paso es integrar con tus managers existentes.

#### Ejemplo: Integrar con GestorMisiones

**Archivo: `Assets/SCRIPTS/Misiones/GestorMisiones.cs`**

Agregar al final del método que inicia una misión:

```csharp
using QuantumAI.Core;

public void IniciarMision(int idMision, string nombreMision, string tipoMision)
{
    // ... tu código existente ...

    // NUEVO: Notificar a Quantum
    QuantumAICore.Instance?.NotifyMissionStarted(
        elementoActual,  // Variable con el elemento actual
        tipoMision,      // "AR", "Juego", "Quiz", etc.
        idMision
    );
}
```

Al completar misión:

```csharp
public void CompletarMision(bool exitoso)
{
    // ... tu código existente ...

    // NUEVO: Notificar a Quantum
    QuantumAICore.Instance?.NotifyMissionCompleted(
        elementoActual,
        idMisionActual,
        exitoso
    );
}
```

#### Ejemplo: Integrar con SistemaXP

**Archivo: `Assets/SCRIPTS/Perfil/InicioPerfil/SistemaXP.cs`**

```csharp
using QuantumAI.Core;

public void AgregarXP(int cantidad, string razon)
{
    // ... tu código existente ...

    // NUEVO: Notificar a Quantum
    QuantumAICore.Instance?.NotifyXPGained(cantidad, razon, xpTotal);
}
```

---

## 📊 VERIFICACIÓN DE INSTALACIÓN

### Checklist:

- [ ] DOTween instalado y configurado
- [ ] QuantumAIConfig creado en Resources/
- [ ] API Key de Gemini configurada
- [ ] GameObject QuantumAI en escena inicial
- [ ] QuantumUI con Canvas configurado
- [ ] FloatingIcon visible en esquina
- [ ] ChatPanel se abre al click en icono
- [ ] Prefabs de burbujas de mensaje creados
- [ ] Test básico funciona (modo simulación)

---

## 🎨 PERSONALIZACIÓN

### Cambiar Posición del Icono:

```
AIConfig > Icon Position:
  - TopLeft
  - TopRight
  - BottomLeft
  - BottomRight (default)
```

### Cambiar Estilo de Intervención:

```
AIConfig > Default Interaction Preference:
  - Proactivo: IA sugiere constantemente
  - Equilibrado: IA interviene en momentos clave (recomendado)
  - Reactivo: IA solo responde cuando se le pregunta
```

### Ajustar Velocidad de Escritura:

```
AIConfig > Typing Speed: 10-100 caracteres/segundo
  - Más bajo = más lento (más dramático)
  - Más alto = más rápido (más eficiente)
```

---

## 🐛 SOLUCIÓN DE PROBLEMAS

### Error: "AIConfig no encontrado"
**Solución:** Asegurar que el archivo esté en `Assets/Resources/QuantumAIConfig.asset`

### Error: "DOTween namespace not found"
**Solución:**
1. Instalar DOTween desde Asset Store
2. Tools > Demigiant > DOTween Utility Panel > Setup DOTween
3. O comentar todas las líneas con DOTween

### Icono no aparece
**Solución:**
1. Verificar que QuantumUI esté en la escena
2. Verificar que Canvas esté en modo Screen Space Overlay
3. Verificar que FloatingIcon tenga componente Image asignado

### Chat no responde
**Solución:**
1. Verificar que API Key esté configurada
2. Activar "Simulate Gemini Responses" para testing
3. Revisar Console para errores
4. Activar "Show Gemini Prompts" para debug

### Respuestas de Gemini muy lentas
**Solución:**
1. Reducir Max Tokens en config (probar con 200-300)
2. Activar caché más agresivo
3. Usar "Prioritize Local Responses" para queries simples

---

## 📈 PRÓXIMOS PASOS

### Inmediatos:
1. ✅ Testing del sistema base
2. ✅ Ajustar configuración según preferencias
3. ✅ Crear sprites personalizados para el icono de Quantum
4. ✅ Diseñar burbujas de chat con tu estilo visual

### Fase 2 (siguiente semana):
1. Integrar con GestorMisiones
2. Integrar con juegos (ElementMatchingGame, etc.)
3. Integrar con SistemaXP
4. Integrar con LogrosCat
5. Integrar con RachaManager

### Fase 3 (en 2 semanas):
1. Narración en AR (Vuforia)
2. Coaching en juegos
3. Sistema de recomendaciones personalizadas

---

## 💡 TIPS

1. **Empieza en modo simulación** para no gastar API mientras configuras
2. **Activa debug logs** para entender el flujo
3. **Prueba primero el chat** antes de integrar con otros sistemas
4. **Gemini gratuito tiene límites** (60 requests/minuto), pero es generoso para desarrollo
5. **El caché ahorra mucho** - preguntas repetidas no gastan API

---

## 📞 SOPORTE

Si encuentras errores o tienes dudas:
1. Revisar Console de Unity para mensajes [Quantum AI]
2. Activar todos los debug logs en AIConfig
3. Verificar que todos los namespaces estén correctos
4. Asegurar que las referencias en Inspector estén asignadas

---

**¡El sistema está listo para usar! 🎉**

Comienza con el testing básico y luego avanza a las integraciones.
