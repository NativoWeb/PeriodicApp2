# 🔗 EJEMPLOS PRÁCTICOS DE INTEGRACIÓN

Este documento muestra ejemplos reales de cómo integrar Quantum con tus sistemas existentes.

---

## 📋 EJEMPLO 1: Integrar con GestorMisiones

### **Archivo:** `Assets/SCRIPTS/Misiones/GestorMisiones.cs`

#### **Paso 1: Agregar el using**

```csharp
using QuantumAI.Core; // ← AGREGAR ESTA LÍNEA
```

#### **Paso 2: Notificar al iniciar misión**

Busca el método donde inicias una misión (probablemente algo como `MostrarMisiones` o similar) y agrega:

```csharp
public void MostrarMisiones()
{
    // ... tu código existente para cargar misiones ...

    // NUEVO: Notificar a Quantum cuando se muestra una misión
    if (QuantumAICore.Instance != null && misionActual != null)
    {
        QuantumAICore.Instance.NotifyMissionStarted(
            elementoActual,           // Variable con el elemento (ej: "Litio")
            misionActual.tipo,        // Tipo: "AR", "Juego", "Quiz"
            misionActual.id          // ID de la misión
        );
    }
}
```

**Resultado:** Quantum dirá algo como:
> "¡Genial! Vas a visualizar el Sodio en AR. Busca el marcador con cuidado. ¿Listo para ver su estructura atómica?"

#### **Paso 3: Notificar al completar misión**

En tu método `GuardarMisionCompletada` o similar:

```csharp
public void CompletarMision(bool exitosa)
{
    // ... tu código existente ...

    // NUEVO: Notificar a Quantum
    if (QuantumAICore.Instance != null)
    {
        QuantumAICore.Instance.NotifyMissionCompleted(
            elementoActual,
            misionActual.id,
            exitosa
        );
    }
}
```

**Resultado si exitosa:** Quantum celebra:
> "¡Excelente trabajo! Completaste la misión del Sodio. ¿Sabías que el sodio reacciona violentamente con el agua? Estás a 150 XP del siguiente rango. ¿Seguimos?"

**Resultado si falló:** Quantum anima:
> "No te preocupes, todos fallamos a veces. ¿Quieres que repasemos las propiedades del Sodio antes de intentar de nuevo?"

---

## 📋 EJEMPLO 2: Integrar con SistemaXP

### **Archivo:** `Assets/SCRIPTS/Perfil/InicioPerfil/SistemaXP.cs`

#### **Paso 1: Agregar el using**

```csharp
using QuantumAI.Core;
```

#### **Paso 2: Notificar al ganar XP**

En tu método `AgregarXP`:

```csharp
public void AgregarXP(int cantidad, string razon)
{
    // Tu código existente
    int xpAnterior = xpActual;
    xpActual += cantidad;

    // Actualizar PlayerPrefs o Firebase...
    // ...

    // NUEVO: Notificar a Quantum
    if (QuantumAICore.Instance != null)
    {
        QuantumAICore.Instance.NotifyXPGained(cantidad, razon, xpActual);
    }
}
```

**Resultado:** Quantum monitorea el progreso y motiva:

Si estás cerca de rankear:
> "¡+12 XP! Solo te faltan 88 XP para ser Científico en Formación. Si completas 2 misiones AR más, ¡lo logras hoy!"

Si acabas de rankear:
> "🎉 ¡FELICIDADES! Ascendiste a Experto Molecular. Este rango está en el top 15% de estudiantes. ¡Sigue así!"

---

## 📋 EJEMPLO 3: Integrar con Juegos

### **Archivo:** `Assets/SCRIPTS/Categorías/ElementMatchingGame.cs`

#### **Paso 1: Agregar el using**

```csharp
using QuantumAI.Core;
```

#### **Paso 2: Notificar fallos**

En el método donde detectas un match incorrecto:

```csharp
private int fallos = 0;

public void OnMatchAttempt(string simbolo, string nombre, bool correcto)
{
    if (!correcto)
    {
        fallos++;

        // NUEVO: Notificar a Quantum
        if (QuantumAICore.Instance != null)
        {
            string detalles = $"Intentó emparejar {simbolo} con {nombre}";
            QuantumAICore.Instance.NotifyGameFailure(
                "ElementMatching",
                detalles,
                fallos
            );
        }
    }
    else
    {
        // Match correcto
        // ... tu código ...
    }
}
```

**Resultado después de 3 fallos:**
> "Veo que tienes dudas con los símbolos. Recuerda: Li = Litio (viene del latín Lithium), no confundir con I (Yodo). ¿Quieres una tabla de referencia?"

#### **Paso 3: Notificar éxito**

Al completar el juego:

```csharp
public void FinalizarJuego()
{
    // ... tu código existente ...

    // NUEVO: Notificar a Quantum
    if (QuantumAICore.Instance != null)
    {
        QuantumAICore.Instance.NotifyGameSuccess(
            "ElementMatching",
            tiempoTotal,
            puntajeFinal
        );
    }
}
```

**Resultado:**
> "¡Impresionante! Completaste el juego en 45 segundos. Eso es 20% más rápido que tu promedio anterior. Estás mejorando notablemente. 🎯"

---

## 📋 EJEMPLO 4: Integrar con Logros

### **Archivo:** `Assets/SCRIPTS/Perfil/Dashboard/LogrosCat.cs`

#### **Paso 1: Agregar el using**

```csharp
using QuantumAI.Core;
```

#### **Paso 2: Notificar al desbloquear logro**

En tu método que desbloquea logros:

```csharp
public void DesbloquearLogro(string nombreLogro, int xpBonificacion)
{
    // ... tu código existente ...

    // NUEVO: Notificar a Quantum
    if (QuantumAICore.Instance != null)
    {
        QuantumAICore.Instance.NotifyAchievementUnlocked(
            nombreLogro,
            xpBonificacion
        );
    }
}
```

**Resultado:** Quantum celebra con overlay fullscreen:
> "✨🏆 ¡LOGRO DESBLOQUEADO! 🏆✨
>
> MAESTRO DE LOS GASES NOBLES
> +20 XP
>
> ¡Increíble! Dominaste todos los Gases Nobles. Estos elementos son únicos porque casi no reaccionan con nada, por eso se usan en letreros de neón y bombillas.
>
> Ahora eres experto en 7/10 categorías. ¿Quieres que te recomiende cuál seguir?"

---

## 📋 EJEMPLO 5: Integrar con Vuforia (AR)

### **Archivo:** `Assets/SCRIPTS/Vuforia/ScanearElemento.cs`

#### **Paso 1: Agregar el using**

```csharp
using QuantumAI.Core;
```

#### **Paso 2: Narración al escanear**

Cuando se detecta el marcador:

```csharp
public void OnTrackingFound()
{
    // ... tu código existente para mostrar modelo 3D ...

    // NUEVO: Quantum narra la experiencia
    if (QuantumAICore.Instance != null)
    {
        // El Core detectará que estamos en escena AR y narrará
        QuantumAICore.Instance.NotifyMissionStarted(
            elementoActual,
            "AR",
            idMisionAR
        );
    }
}
```

**Resultado:** Quantum narra (con voz si está activada):
> "Has desbloqueado el Carbono en realidad aumentada. Observa su estructura: 6 protones en el núcleo y 6 electrones distribuidos en dos capas. Rota el modelo para ver mejor. ¿Ves algo interesante?"

---

## 📋 EJEMPLO 6: Dashboard de Inicio

### **Archivo:** `Assets/SCRIPTS/Perfil/Dashboard/GenerarMisionesUI.cs`

#### **Paso 1: Saludo al entrar**

```csharp
using QuantumAI.Core;

void Start()
{
    // ... tu código existente ...

    // NUEVO: Quantum saluda proactivamente
    if (QuantumAICore.Instance != null)
    {
        // El Core detecta que entramos al dashboard y saluda automáticamente
        // No necesitas hacer nada más, pero puedes forzarlo:

        // Opcional: mostrar el panel de chat automáticamente
        // FindObjectOfType<QuantumAI.UI.QuantumUIController>()?.ShowChatPanel();
    }
}
```

**Resultado:** Quantum saluda al entrar al dashboard:
> "¡Hola Milo! 👋
>
> Llevas 6 días de racha, ¡solo uno más para +2 XP diario!
>
> Ayer completaste 3 misiones de Metales Alcalinos. ¿Seguimos con esa categoría o prefieres explorar algo nuevo?
>
> [Continuar Alcalinos] [Sorpréndeme] [Ver Progreso]"

---

## 📋 EJEMPLO 7: Multiplayer (Quimicados)

### **Archivo:** `Assets/SCRIPTS/Juegos/QUIMICADOS/JuegoQuimicadosManager.cs`

#### **Paso 1: Coaching durante partida**

```csharp
using QuantumAI.Core;

public void OnRondaCompletada(bool ganada)
{
    // ... tu código existente ...

    // NUEVO: Quantum analiza la partida
    if (QuantumAICore.Instance != null && ganada)
    {
        QuantumAICore.Instance.NotifyGameSuccess(
            "Quimicados",
            tiempoRonda,
            coronasJugadorA
        );
    }
}

public void OnPreguntaFallada(string categoria)
{
    fallosConsecutivos++;

    // NUEVO: Quantum da coaching táctico
    if (QuantumAICore.Instance != null && fallosConsecutivos >= 2)
    {
        QuantumAICore.Instance.NotifyGameFailure(
            "Quimicados",
            $"Falló pregunta de {categoria}",
            fallosConsecutivos
        );
    }
}
```

**Resultado durante partida:**
> "Tu oponente tiene 5 coronas, tú 3. Necesitas ganar esta ronda. Ha fallado varias preguntas de Metales de Transición. ¡Elige esa categoría si sale en la ruleta!"

**Resultado al ganar:**
> "¡VICTORIA! 🎉 Ganaste 30 XP y subiste 5 posiciones en el ranking. Tu estrategia de enfocarte en Gases Nobles fue brillante. ¿Otra partida?"

---

## 📋 EJEMPLO 8: Gestión de Racha

### **Archivo:** `Assets/SCRIPTS/Perfil/Dashboard/RachaManager.cs`

#### **Paso 1: Notificar milestones**

```csharp
using QuantumAI.Core;

public void ActualizarRacha()
{
    // ... tu código existente ...

    // NUEVO: Notificar a Quantum en milestones importantes
    if (QuantumAICore.Instance != null)
    {
        // Al alcanzar milestone (7, 30, 100 días)
        if (rachaActual == 7 || rachaActual == 30 || rachaActual == 100)
        {
            QuantumAICore.Instance.NotifyAchievementUnlocked(
                $"Racha de {rachaActual} días",
                xpBonificacion
            );
        }
    }
}
```

**Resultado al llegar a 7 días:**
> "🔥 ¡7 DÍAS DE RACHA! 🔥
>
> Has venido 7 días seguidos. Eso demuestra compromiso real. Ahora ganarás +2 XP por cada login diario.
>
> ¿Puedes llegar a 30 días? Solo el 15% de estudiantes lo logra. 💪"

---

## 🎯 PATRÓN GENERAL DE INTEGRACIÓN

### **Template Universal**

```csharp
// 1. Agregar using al inicio del archivo
using QuantumAI.Core;

// 2. En cualquier método donde ocurra un evento importante
public void AlgoImportanteSucede()
{
    // Tu código existente
    // ...

    // 3. Verificar que Quantum esté disponible y notificar
    if (QuantumAICore.Instance != null)
    {
        // Elegir el método apropiado según el evento:

        // Para misiones:
        // QuantumAICore.Instance.NotifyMissionStarted(elemento, tipo, id);
        // QuantumAICore.Instance.NotifyMissionCompleted(elemento, id, exito);

        // Para juegos:
        // QuantumAICore.Instance.NotifyGameFailure(tipo, detalles, fallos);
        // QuantumAICore.Instance.NotifyGameSuccess(tipo, tiempo, score);

        // Para progreso:
        // QuantumAICore.Instance.NotifyXPGained(cantidad, razon, total);
        // QuantumAICore.Instance.NotifyAchievementUnlocked(nombre, xp);
    }
}
```

---

## 📊 COMPARACIÓN: ANTES vs DESPUÉS

### **ANTES: Sin Quantum**

```csharp
public void CompletarMision()
{
    misionCompletada = true;
    xp += 12;
    ActualizarUI();
    // Fin - silencio
}
```

### **DESPUÉS: Con Quantum**

```csharp
public void CompletarMision()
{
    misionCompletada = true;
    xp += 12;
    ActualizarUI();

    // NUEVO: Quantum reacciona
    QuantumAICore.Instance?.NotifyMissionCompleted(
        elementoActual,
        misionId,
        true
    );

    // Quantum automáticamente:
    // ✅ Celebra el logro
    // ✅ Explica algo interesante del elemento
    // ✅ Sugiere siguiente paso
    // ✅ Motiva si está cerca de rankear
    // ✅ Actualiza perfil de aprendizaje
}
```

---

## 💡 TIPS DE INTEGRACIÓN

### ✅ **HACER:**
- Notificar en eventos importantes
- Pasar información contextual rica
- Dejar que Quantum decida si intervenir
- Usar el null-conditional operator `?.`

### ❌ **NO HACER:**
- No notificar en cada frame
- No notificar eventos triviales (clicks sin contexto)
- No asumir que Instance siempre existe
- No bloquear el flujo esperando respuesta

---

## 🚀 ORDEN RECOMENDADO DE INTEGRACIÓN

1. ✅ **Primero:** Sistema XP (más fácil, 3 líneas)
2. ✅ **Segundo:** Logros (visible, motivador)
3. ✅ **Tercero:** Misiones (core de la app)
4. ✅ **Cuarto:** Juegos principales
5. ✅ **Quinto:** AR (más complejo)
6. ✅ **Sexto:** Multiplayer

---

## 🎉 RESULTADO FINAL

Con estas integraciones simples, Quantum pasará de ser un chat estático a un **asistente omnipresente** que:

✨ Guía proactivamente
✨ Celebra logros
✨ Motiva en momentos clave
✨ Da feedback contextual
✨ Adapta al nivel del estudiante
✨ Recomienda siguiente paso
✨ Analiza progreso
✨ Crea sentido de acompañamiento

**Todo con cambios mínimos en tu código existente** 🚀

---

## ❓ ¿Necesitas Ayuda?

Si tienes dudas sobre cómo integrar con un sistema específico que no está en estos ejemplos, busca el patrón más similar y adáptalo.

**Estructura básica siempre es:**
```csharp
if (QuantumAICore.Instance != null)
{
    QuantumAICore.Instance.Notify[TipoEvento](...);
}
```

¡Es así de simple! 😊
