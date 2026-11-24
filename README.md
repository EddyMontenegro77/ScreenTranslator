# Niholator (ScreenTranslator)
 
A Windows desktop app that reads Japanese text anywhere on your screen and translates it in place, using local OCR and locally-running language models.
 
![Niholator_screenshot](https://i.ibb.co/MDXmcqRV/niholator-capture.webp)
 
## Features
 
- **On-screen capture + OCR** using the built-in `Windows.Media.Ocr` engine, so recognition runs entirely on your machine.
- **Smart text block detection**: detected lines are grouped into coherent blocks with a union-find clustering algorithm and adaptive gap detection.
- **Local translation with Ollama**, using Qwen models (default: `qwen2.5:7b`) for strong Japanese/CJK quality.
- **Floating overlays**: translations appear in transparent overlay windows on top of the original text.
- **Multiple translation backends** through a common `ITranslationService` abstraction: Ollama (local), Gemini API, DeepSeek and Azure Translator.
- **Automatic model management**: model download with streaming progress and automatic Ollama process handling.
- **System tray integration**, log window, settings window and color picker.
- **Dark theme** and **English / Spanish** interface.
## How it works
 
1. Select a region of the screen.
2. The app runs OCR on the capture and detects text blocks.
3. Each block is sent to the configured translation backend.
4. The result is shown as an overlay over the original text.
## Requirements
 
- Windows 10 / 11
- Japanese OCR language pack installed in Windows
- [Ollama](https://ollama.com) (installed automatically by the installer) for local translation
- A GPU with enough VRAM for the chosen model (8 GB is enough for `qwen2.5:7b`)
## Installation
 
1. Download the latest installer from the [Releases](../../releases) page.
2. Run the installer. It installs Ollama silently if it isn't already present.
3. Launch the app. The translation model is downloaded on first run.
## Usage
 
1. Open the app; it stays in the system tray.
2. Trigger a capture and select the area containing Japanese text.
3. Read the translation in the overlay.
4. Use **Settings** to change the translation backend, model, language and appearance.
## Building from source
 
**Requirements:** Visual Studio with the **.NET desktop development** workload.
 
```bash
git clone https://github.com/EddyMontenegro77/ScreenTranslator.git
cd ScreenTranslator
```
 
Open the solution in Visual Studio, restore NuGet packages, and run the WPF project.
 
## Tech stack
 
- C# / .NET, WPF
- `Windows.Media.Ocr`
- Ollama (Qwen2.5)
- Inno Setup (installer)
