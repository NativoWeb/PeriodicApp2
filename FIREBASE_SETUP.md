# Configuración de Firebase para PeriodicApp2

## 🔐 Configuración de Credenciales

Este proyecto ahora usa un **ScriptableObject** para almacenar las credenciales de Firebase de forma segura.

### Pasos para Configurar:

1. **Crear el archivo de configuración en Unity:**
   - Abre Unity Editor
   - Ve a la carpeta `Assets/Resources` (o créala si no existe)
   - Click derecho → `Create` → `PeriodicApp` → `Firebase Configuration`
   - Nombra el archivo como `FirebaseConfig`

2. **Configurar las credenciales:**
   - Selecciona el archivo `FirebaseConfig.asset`
   - En el Inspector, completa los siguientes campos:
     ```
     API Key: [Tu API Key de Firebase Console]
     App ID: [Tu App ID de Firebase Console]
     Project ID: [Tu Project ID]
     Storage Bucket: [Tu Storage Bucket URL]
     Database URL: [Tu Realtime Database URL]
     ```

3. **Obtener las credenciales de Firebase:**
   - Ve a [Firebase Console](https://console.firebase.google.com/)
   - Selecciona tu proyecto `periodiccapp`
   - Ve a `Project Settings` → `General`
   - Scroll hasta `Your apps` y selecciona tu app Android/iOS
   - Copia las credenciales necesarias

### ⚠️ Credenciales Actuales (TEMPORAL)

Para configuración inicial, usa estas credenciales (DEBEN ser reemplazadas):

```
API Key: AIzaSyDga959UgRVlfvY3zgKyirXYlSvVScdYRU
App ID: 1:22318390969:android:5eb17f4f1901e17037c568
Project ID: periodiccapp
Storage Bucket: periodiccapp.firebasestorage.app
Database URL: https://periodiccapp-default-rtdb.firebaseio.com
```

**IMPORTANTE:**
- ✅ El archivo `FirebaseConfig.asset` está en `.gitignore` y NO se subirá al repositorio
- ❌ NUNCA commitees credenciales reales al código fuente
- 🔄 Cada desarrollador debe crear su propio `FirebaseConfig.asset` localmente

### 🛡️ Seguridad

- El archivo `FirebaseConfig.asset` en `Assets/Resources/` está ignorado por Git
- Las credenciales solo se cargan en tiempo de ejecución
- Los Debug.Log están ahora condicionados con `#if UNITY_EDITOR || DEVELOPMENT_BUILD`

### 📝 Notas

- Si el archivo `FirebaseConfig` no existe, Firebase mostrará un error en el log
- Puedes habilitar/deshabilitar logs de Firebase desde el Inspector del ScriptableObject
- Para builds de producción, considera usar Firebase Remote Config o servicios de secretos

### 🔧 Solución de Problemas

**Error: "FirebaseConfig no encontrado en Resources"**
- Verifica que el archivo esté en `Assets/Resources/FirebaseConfig.asset`
- Asegúrate de que el nombre sea exactamente `FirebaseConfig`

**Error: "FirebaseConfig inválido"**
- Verifica que API Key, App ID y Project ID no estén vacíos
- Revisa que las credenciales sean correctas desde Firebase Console

---

## 🎯 Beneficios de este Cambio

1. ✅ **Seguridad mejorada**: Credenciales fuera del código fuente
2. ✅ **Fácil configuración**: Cada desarrollador configura sus propias credenciales
3. ✅ **Sin riesgos**: El archivo de configuración no se sube al repositorio
4. ✅ **Flexible**: Puedes tener diferentes configuraciones para dev/staging/prod
5. ✅ **Mejor performance**: Debug logs solo en desarrollo
