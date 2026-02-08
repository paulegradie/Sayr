# Sayr

Local Windows dictation using a hotkey and a local speech-to-text backend.

## What This Repo Gives You
1. A minimal Windows tray app that:
   - Registers `Alt+Y` as a global hotkey.
   - Records audio while toggled on.
   - Sends audio to a local transcription backend.
   - Pastes the transcription into the focused window.
2. A containerized LocalAI backend with a place to store models **outside of Git**.

## Quick Start (Windows 10)
1. Start the LocalAI backend:
   ```powershell
   cd infra/localai
   docker compose up -d
   ```
2. Build and run the tray app:
   ```powershell
   dotnet build .\Sayr.sln
   .\apps\Sayr.Tray\bin\Debug\net8.0-windows\Sayr.Tray.exe
   ```
3. Focus any text field, hit `Alt+Y`, speak, then hit `Alt+Y` again.

## Configuration
The tray app reads settings from `apps/Sayr.Tray/appsettings.json`:
```json
{
  "BackendUrl": "http://localhost:8080",
  "Model": "whisper-1",
  "Hotkey": "Alt+Y"
}
```

## Models Are Local Only
Models live under `infra/localai/models/` and are ignored by `.gitignore`.
Do **not** commit models to GitHub.

## Multi-GPU Notes
Most Whisper backends will use a single GPU per process. If you want to spread load
across your TITAN + GTX, the simplest approach is to run two backend containers and
route requests round-robin. You can later add GPU pinning (e.g., `CUDA_VISIBLE_DEVICES`)
once you confirm the LocalAI container honors that environment variable.

## Roadmap (Optional)
- Add a small UI to show status and audio level.
- Add a “push-to-talk” mode (hold hotkey to record).
- Add a second backend container for GPU load balancing.
