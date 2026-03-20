# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**PeriodicApp2** is a Unity 6000.0.38f1 educational mobile game for Android 13 that teaches chemistry through gamified experiences. Key features include an interactive periodic table quiz, Vuforia AR visualization of chemical elements, an AI tutor powered by Google Gemini, Firebase backend (Auth + Firestore), XP/rank progression, missions, and multi-language support.

## Build & Run

- Open the project in **Unity Editor 6000.0.38f1**
- Build target: Android (via File → Build Settings → Android)
- IDE: Visual Studio 2022 via `PeriodicApp2.sln`
- CLI build (requires Unity installed):
  ```
  "C:\Program Files\Unity\Hub\Editor\6000.0.38f1\Editor\Unity.exe" -projectPath "D:\PROYECTOS\PeriodicApp2" -buildTarget Android -quit -batchmode
  ```
- There is no automated test runner; testing is done by running the app in the Unity Editor Play mode or deploying to a device.

## Firebase Setup (Required for New Developers)

Firebase credentials are **not versioned**. Each developer must create `Assets/Resources/FirebaseConfig.asset` locally:

1. In Unity Editor: right-click `Assets/Resources` → Create → PeriodicApp → Firebase Configuration → name it `FirebaseConfig`
2. Fill in the Inspector fields with credentials from Firebase Console (project: `periodiccapp`):
   - API Key, App ID, Project ID, Storage Bucket, Database URL

See `FIREBASE_SETUP.md` for full details. If this file is missing, Firebase will throw a runtime error.

## Architecture

The project uses **Clean Architecture** with four layers enforced by Unity Assembly Definitions:

### Layer Dependency Flow
```
Presentation → Core.Application → Core.Domain ← Infrastructure
```

### Core.Domain (`Assets/Core/Domain/`)
- Entities: `Usuario`, `PreguntaEntity`, `PreguntaEstilo`, `DepartamentoCiudad`
- Repository/service interfaces: `IUsuarioRepositorio`, `IServicioFirestore`, `IAuthenticationService`, etc.
- Assembly: `PeriodicApp.Core.Domain.asmdef`

### Core.Application (`Assets/Core/Application/`)
- ~38 Use Case classes encapsulating business logic (login, registration, XP updates, survey calculation, etc.)
- Additional interfaces for cross-cutting concerns: `IPlayerPrefsService`, `INetworkService`, `IJsonService`, `ILoggingService`, `ISceneService`
- Assembly: `PeriodicApp.Core.Application.asmdef`

### Infrastructure (`Assets/Infrastructure/`)
- Implements domain interfaces: `FirebaseAuthService`, `FirebaseUsuarioRepositorio`, `FirestoreService`, `EncuestaConocimientoFirebase`
- Unity-specific service implementations: `UnityPersistenceService`, `UnityPlayerPrefsService`, `UnityNetworkService`, `UnitySceneService`, `UnityJsonService`
- Email via `EmailSenderBrevoService` (Brevo API)
- Network monitoring: `ApplicationReachabilityConnectionMonitor`
- Assembly: `PeriodicApp.Infrastructure.asmdef`

### Presentation (`Assets/Presentation/`)
- **ServiceLocator** (`Assets/Presentation/ServiceLocator.cs`): Singleton that initializes and wires all services — the entry point for DI. Always go here first to understand how services are composed.
- Controllers: `LoginController`, `RegistroEmailController`, `EncuestaConocimientoController`, `ModeloAI` (AI integration), `ControladorIdioma` (localization), etc.
- ViewModels: `MenuViewModel`, `PlayerMovementViewModel`
- Assembly: `PeriodicApp.Presentation.asmdef`

### Game Scripts (`Assets/SCRIPTS/`)
Legacy-style scripts outside the clean architecture layers. Contains game-specific logic:
- `Game/Game2.cs` — main periodic table quiz game loop with Firebase integration
- `AI/QuantumAIDiagnostico.cs`, `AI/Services/GeminiService.cs`, `AI/Services/GeminiPromptBuilder.cs` — AI tutor subsystem
- `Misiones/` — mission/quest system
- `Perfil/` — user profile management and XP/rank display
- `Vuforia/` — AR element visualization
- `Encuestas/` — learning style surveys
- `cambiarescena.cs` — scene transitions
- `NotificacionManager.cs` — in-app notifications

## Key Patterns

- **Service Locator**: `ServiceLocator.cs` is the composition root; avoid bypassing it when accessing services.
- **Repository Pattern**: Data access always goes through interfaces defined in `Core.Domain`, implemented in `Infrastructure`.
- **Use Cases**: All business logic lives in `Core.Application/UseCases/`. Add new features as new Use Case classes, not in controllers.
- **Debug Logging**: Wrapped with `#if UNITY_EDITOR || DEVELOPMENT_BUILD` — maintain this pattern to keep production builds clean.

## External Dependencies

- Firebase SDK (Auth, Firestore, Functions, Messaging)
- Google Gemini API (AI tutor via `GeminiService.cs`)
- Brevo Email API
- Vuforia AR Engine
- Unity Barracuda / ONNX Runtime (ML inference)
- DOTween (animations)
- Newtonsoft JSON
- TextMesh Pro
