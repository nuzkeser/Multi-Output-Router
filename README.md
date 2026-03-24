# Virtual Multi-Output Router 🎧

A sleek, native Windows WPF application that perfectly mirrors audio across multiple devices, replicating the macOS "Multi-Output Device" feature seamlessly on Windows.

## ✨ Features
- **Modern Dark UI**: Beautiful, fully responsive interface with custom layouts, drop shadows, and visual polish.
- **Ultra-Low Latency Routing**: Uses NAudio and Windows Audio Session API (WASAPI) with a hyper-aggressive EventSync engine (clamped natively to 15ms) to completely eliminate drift.
- **Per-Device Volume Controls**: Dynamically intercept raw audio bytes to modify volume individually per destination before playback, complete with smart editable percentage input boxes.
- **One-Click Driver Setup**: Automatically checks for the free VB-Audio Virtual Cable! If missing, the app offers a 1-click in-app installer that downloads the official zip, flawlessly executes the driver setup, and gets you ready for perfect zero-latency mirroring.
- **Smart Filtering**: Automatically hides your Source device from the destination panels to prevent accidental infinite audio loops.

## 🚀 How It Works
Rather than fighting Windows architecture by building a Kernel-mode driver from scratch (which requires the WDK and an EV Code Signing certificate), this User-mode application acts as an incredibly optimized WASAPI Router. 

By pairing this app automatically with the free VB-Cable driver, you can push audio into the "Virtual Cable", and this app will split that exact stream perfectly in-sync across your desktop speakers, your headset, and your Bluetooth devices simultaneously! You can also use it to just mirror whatever is playing on your existing active speakers.

## 🛠️ Requirements & Setup
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

To compile and run from source:
```bash
git clone https://github.com/nuzkeser/Multi-Output-Router
cd MultiOutputRouter
dotnet build
dotnet run
```

When you open the app, if you want authentic zero-latency mode, click the red **"Install Virtual Cable"** button to automatically run the driver installation. After restarting the app, select `CABLE Input` as your Source, tick your actual headphones/speakers as Destinations, and hit **Start Routing**!

## 📦 Dependencies
- [NAudio](https://github.com/naudio/NAudio) - Premier Audio API for .NET
