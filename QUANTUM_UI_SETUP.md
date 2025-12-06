# 🎨 Guía de Configuración Visual de Quantum AI

Esta guía te muestra cómo hacer visible la presencia de Quantum AI en tu aplicación.

---

## 📍 **Opción 1: Badge de Notificación en btnChatBot** (⏱️ 5 minutos)

### **Resultado:**
Un círculo rojo con número aparece sobre el botón de chat cuando Quantum envía un mensaje.

### **Pasos en Unity:**

#### 1. **Agregar el Badge Visual al btnChatBot**

En la escena `Inicio.unity`:

1. Selecciona el GameObject `btnChatBot` en la Hierarchy
2. **Click derecho** > Create Empty Child
3. Renombra a: `NotificationBadge`

#### 2. **Configurar el Badge**

Con `NotificationBadge` seleccionado:

**Add Component > Image:**
- Source Image: Circle (o un sprite circular)
- Color: `#FF3333` (rojo brillante)
- Width: `30`
- Height: `30`

**Posición:**
- Anchor: Top-Right del botón padre
- Pos X: `20`
- Pos Y: `-20`
- Esto lo coloca en la esquina superior derecha del botón

#### 3. **Agregar el Texto del Contador**

Con `NotificationBadge` seleccionado:
- **Click derecho** > UI > Text - TextMeshPro
- Renombra a: `BadgeText`

Configurar:
- Font Size: `16`
- Alignment: Center (horizontal y vertical)
- Color: White
- Text: `1` (placeholder)
- Best Fit: Activado (opcional)

**RectTransform:**
- Stretch (full)
- Left: `0`, Right: `0`, Top: `0`, Bottom: `0`

#### 4. **Agregar el Script**

Con `btnChatBot` seleccionado:
- **Add Component** > `Chat Notification Badge`

En el Inspector:
- **Badge Object**: Arrastra `NotificationBadge`
- **Badge Text**: Arrastra `BadgeText`
- **Notification Color**: `#FF3333`
- **Pulse Scale**: `1.2`
- **Pulse Duration**: `0.5`

#### 5. **Probar**

1. Ejecuta el juego
2. Selecciona un elemento e inicia una misión
3. El badge debería aparecer y pulsar
4. Al abrir el chat, el badge desaparece

---

## 🔔 **Opción 2: Sistema de Notificaciones Toast** (⏱️ 15 minutos)

### **Resultado:**
Mensajes flotantes aparecen en la parte superior derecha cuando ocurren eventos importantes.

### **Pasos en Unity:**

#### 1. **Crear el Canvas de Notificaciones**

En TODAS las escenas donde quieras notificaciones (Inicio, Categorías, Vuforia, etc.):

1. **Click derecho en Hierarchy** > UI > Canvas
2. Renombra a: `QuantumNotificationsCanvas`

Configurar el Canvas:
- **Render Mode**: Screen Space - Overlay
- **Canvas Scaler** > UI Scale Mode: Scale With Screen Size
- **Reference Resolution**: `1080 x 1920` (ajusta a tu resolución)
- **Sort Order**: `999` (para que esté siempre encima)

#### 2. **Agregar el Script de Notificaciones**

Con `QuantumNotificationsCanvas` seleccionado:
- **Add Component** > `Quantum Toast Notification`

En el Inspector:
- **Display Duration**: `3`
- **Animation Duration**: `0.3`
- **Max Simultaneous Toasts**: `3`
- **Toast Spacing**: `120`

Checkboxes (según prefieras):
- ✅ Show Mission Notifications
- ✅ Show XP Notifications
- ✅ Show Achievement Notifications
- ✅ Show Game Hints

#### 3. **Crear el Prefab de Toast**

**A. Crear el GameObject del Toast:**

1. **Click derecho en Hierarchy** > UI > Image
2. Renombra a: `ToastPrefab`

Configurar el Image:
- Width: `400`
- Height: `80`
- Color: `#444444DD` (gris oscuro semi-transparente)

**Add Component > Shadow:**
- Color: Black
- Distance: `(3, -3)`

**B. Agregar el Texto:**

Con `ToastPrefab` seleccionado:
- **Click derecho** > UI > Text - TextMeshPro
- Renombra a: `ToastText`

Configurar:
- Font Size: `18`
- Alignment: Left, Center (vertical)
- Color: White
- Wrapping: Enabled
- Overflow: Truncate

**RectTransform:**
- Left: `20`, Right: `20`, Top: `10`, Bottom: `10`

**C. Agregar Icono (opcional):**

Con `ToastPrefab` seleccionado:
- **Click derecho** > UI > Image
- Renombra a: `Icon`

Configurar:
- Width: `40`
- Height: `40`
- Anchor: Left-Center
- Pos X: `25`

**D. Redondear bordes (opcional):**

Puedes importar un sprite con bordes redondeados y usarlo como `Source Image` del Toast.

#### 4. **Convertir a Prefab**

1. Arrastra `ToastPrefab` desde la Hierarchy a la carpeta `Assets/Prefabs/`
2. **Delete** el ToastPrefab de la Hierarchy

#### 5. **Asignar el Prefab al Sistema**

Vuelve a `QuantumNotificationsCanvas`:
- En el componente `Quantum Toast Notification`
- **Toast Prefab**: Arrastra el prefab que acabas de crear

#### 6. **Posicionar el Canvas**

Agrega un Empty GameObject hijo a `QuantumNotificationsCanvas`:
- Renombra a: `ToastContainer`
- Anchor: Top-Right
- Pos X: `-50`
- Pos Y: `-50`

El script instanciará los toasts aquí automáticamente.

---

## 🎯 **Opción 3: Indicador "Quantum Activo"** (⏱️ 10 minutos)

### **Resultado:**
Un pequeño icono/logo de Quantum AI siempre visible que pulsa cuando la IA está procesando.

### **Pasos:**

#### 1. **Crear el Indicador Visual**

En la escena `Inicio.unity`:

1. **Click derecho en Canvas** > UI > Image
2. Renombra a: `QuantumIndicator`

Configurar:
- Source Image: Un icono de cerebro/robot/IA (crea uno o usa un sprite)
- Width: `50`
- Height: `50`
- Anchor: Bottom-Left
- Pos X: `40`
- Pos Y: `40`

#### 2. **Agregar Animación Idle**

Con `QuantumIndicator` seleccionado:
- **Add Component** > `QuantumAI.UI.QuantumIndicator` (crear este script)

**Script simple:**

```csharp
using UnityEngine;
using DG.Tweening;

public class QuantumIndicator : MonoBehaviour
{
    void Start()
    {
        // Animación idle de respiración
        transform.DOScale(1.1f, 1.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}
```

---

## 📋 **Resumen de Opciones**

| Opción | Tiempo | Visibilidad | Intrusión | Recomendado para |
|--------|--------|-------------|-----------|------------------|
| **Badge en Chat** | 5 min | Media | Baja | Mostrar mensajes pendientes |
| **Toast Notifications** | 15 min | Alta | Media | Feedback inmediato de eventos |
| **Indicador Permanente** | 10 min | Baja | Muy baja | Presencia constante de IA |

### **Mi Recomendación:**

✅ **Combina las 3 opciones:**

1. **Badge en btnChatBot** - Para mensajes de chat pendientes
2. **Toast Notifications** - Para eventos importantes (logros, XP, misiones)
3. **Indicador permanente** - Para recordar que Quantum está siempre presente

---

## 🧪 **Probar el Sistema**

### **Test 1: Badge de Chat**
1. Ejecuta el juego
2. Inicia una misión
3. Verás el badge aparecer en el botón de chat
4. Abre el chat → badge desaparece

### **Test 2: Notificaciones Toast**
1. Ejecuta el juego
2. Completa un juego
3. Verás toast: "⭐ +12 XP - Completar misión"
4. Desbloquea un logro
5. Verás toast: "🏆 Elemento X dominado"

### **Test 3: Verificar en Consola**
Deberías ver logs como:
```
💬 [ChatNotificationBadge] Conectado con Quantum AI
🔔 [QuantumToastNotification] Sistema de notificaciones inicializado
[Quantum AI] +10 XP (Completar misión). Total: 150
```

---

## 🎨 **Personalización**

### **Colores del Badge:**
En `ChatNotificationBadge.cs`:
```csharp
notificationColor = new Color(1f, 0.2f, 0.2f); // Rojo
// Cambia a:
notificationColor = new Color(0.2f, 0.6f, 1f); // Azul Quantum
```

### **Duración de Toasts:**
En `QuantumToastNotification`:
```csharp
displayDuration = 3f; // Cambia a 5f para más tiempo
```

### **Desactivar Notificaciones Específicas:**
En el Inspector de `QuantumToastNotification`:
- ❌ Desmarca "Show Mission Notifications" si no quieres ver notificaciones de misiones

---

## ⚠️ **Problemas Comunes**

### **"El badge no aparece"**
- ✅ Verifica que `QuantumAICore` esté en la escena Start.unity
- ✅ Revisa que el badge esté **activo** al inicio (debe estar desactivado)
- ✅ Comprueba que las referencias estén asignadas en el Inspector

### **"Los toasts no se ven"**
- ✅ Verifica que el Canvas tenga `Sort Order: 999`
- ✅ Asegúrate de que el prefab esté asignado
- ✅ Revisa la posición del `ToastContainer`

### **"DOTween error"**
- ✅ Asegúrate de que DOTween esté importado (Asset Store)
- ✅ Si no tienes DOTween, comenta las líneas con `.DO...`

---

## 🚀 **Siguiente Nivel**

¿Quieres más? Puedes agregar:

1. **Sonidos** - Reproducir un "ping" cuando llega un mensaje
2. **Vibración** - Vibrar el dispositivo al recibir notificaciones importantes
3. **Animación de avatar** - Un personaje animado que representa a Quantum
4. **Chat emergente** - Mini-chat que aparece sin cambiar de escena

¿Te ayudo a implementar alguna de estas? 😊
