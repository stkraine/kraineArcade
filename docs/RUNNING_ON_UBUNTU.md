# Running kraineArcade in a Desktop Simulation (Ubuntu 22.04)

This guide explains how to run and play-test kraineArcade on an Ubuntu 22.04 desktop so you can evaluate how the app looks and feels before releasing to the Android/iOS app stores.

kraineArcade is a Unity-based mobile app (Android/iOS). On Ubuntu, the most practical "simulation" workflow is to run the project in the **Unity Editor** (Play Mode) and use the **Device Simulator** to preview mobile screen sizes, resolutions, and orientations on your desktop.

## 1. Prerequisites

- **OS:** Ubuntu 22.04 LTS (64-bit)
- **Hardware:** A machine that meets Unity Editor requirements (GPU with up-to-date drivers recommended; 8 GB+ RAM recommended)
- **Accounts/tools:**
  - A Unity account (free Personal license is sufficient for eligible users)
  - `git` and access to this repository

Install common desktop dependencies (some may already be present):

```bash
sudo apt update
sudo apt install -y libgtk-3-0 libnss3 libasound2 libxcb-xinerama0 libxss1 libxtst6
```

> Note: Exact dependency names can vary by Ubuntu image and Unity version. If Unity Hub or the Editor reports missing libraries, install the packages named in the error message.

## 2. Install Unity Hub (Ubuntu 22.04)

1. Download **Unity Hub for Linux** from Unity's official download page: <https://unity.com/download>.
2. Install the downloaded package (for a `.deb` build):

   ```bash
   sudo apt install -y ./UnityHub*.deb
   ```

   If you downloaded an AppImage instead, mark it executable and run it per Unity's instructions.
3. Launch Unity Hub, sign in, and activate a license (Unity Personal is fine if you qualify).

> For the latest Linux installation details, refer to Unity's official documentation, as Hub packaging/steps can change.

## 3. Install a Unity Editor

1. In Unity Hub, go to **Installs** → **Install Editor**.
2. Install a **Unity LTS** version. Because this repository does not currently commit `ProjectVersion.txt`, there is no pinned Editor version in git.
   - Start with a recent **2022.3 LTS** or **2021.3 LTS** Editor unless the team specifies otherwise.
   - If a `ProjectSettings/ProjectVersion.txt` file is added later, use that exact version.
3. (Optional, for later device testing) Add modules such as **Android Build Support** (SDK/NDK/OpenJDK) and/or **iOS Build Support** if you plan to produce mobile builds from a supported environment. Android emulation/build setup on Ubuntu is outside the core "desktop simulation" flow but useful before store release.

## 4. Open the Project

This repository currently includes Unity content under `Assets/` and `Tests/` (for example, `Assets/Games/Tetris/`, `Assets/SharedLibrary/`, `Assets/UI/`), but it may **not** include full Unity project metadata such as `ProjectSettings/` and `Packages/manifest.json`.

### If the repo contains a complete Unity project
If you see `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json` in the checkout:

1. In Unity Hub, click **Open** → **Add project from disk**.
2. Select the repository root (`kraineArcade`).
3. Open it with the matching Editor version.

### If Unity project metadata is missing (bootstrap workflow)
If Hub reports that the folder is not a valid Unity project:

1. In Unity Hub, create a **new Unity project** (a 2D/mobile-oriented template is a reasonable starting point) using your installed LTS Editor. Choose a location *outside* the repo or a temporary location.
2. Close Unity.
3. Copy or merge this repository's `Assets/` and `Tests/` folders into the new project's root (preserving folder structure). If the new project already has an `Assets/` folder, merge carefully and avoid overwriting template files you still need.
4. Reopen the new project in Unity and let it import assets/compile scripts.
5. Create or open a scene that wires up the gameplay/UI you want to test (scene setup may be required depending on what is currently committed).

> **Tip:** Once the team agrees on an Editor version and project settings, commit `ProjectSettings/` and `Packages/manifest.json` (and the relevant `.unity` scenes) so future developers can open the repo directly.

## 5. Run the App in Desktop "Simulation"

### A. Play Mode (functional testing)
1. Open the relevant scene in the Project window.
2. Press **Play** in the Unity Editor toolbar.
3. Use the **Game** view to interact with the app. Check the **Console** window for errors/warnings.

Play Mode is the fastest way to validate gameplay logic on Ubuntu.

### B. Device Simulator (mobile look-and-feel preview)
To preview how the UI/layout behaves on phone-like screens:

1. In Unity, open **Window** → **Package Manager**.
2. Install the **Device Simulator** package (Unity Technologies). In newer Unity versions it may appear as **Device Simulator Devices** or be integrated into the Game view workflow; follow the option available in your Editor version.
3. Switch the **Game** view to **Simulator** (instead of "Game"/free aspect), then choose a device/profile (e.g., common Android/iPhone resolutions).
4. Test different orientations (portrait/landscape) and safe-area/layout behavior.
5. Enter **Play Mode** to interact while simulating device dimensions.

This is the recommended desktop simulation for catching UI scaling, aspect ratio, and layout issues before app-store submission.

### C. Linux Standalone build (optional desktop run)
For a quick desktop build (not a mobile device simulation, but useful for smoke testing):

1. Go to **File** → **Build Settings**.
2. Select **Linux** (or **PC, Mac & Linux Standalone** with Target Platform = Linux, depending on Editor UI).
3. Add the scenes you need to **Scenes In Build**.
4. Click **Build** and run the produced executable.

### D. Android Emulator (optional, closer to store target)
For behavior closer to an Android release, install Android Build Support in Unity, configure the Android SDK/NDK, and run on an Android Virtual Device (AVD) from Android Studio. This requires additional setup on Ubuntu 22.04 but complements desktop simulation.

## 6. What to Validate During Play-Testing

- Core gameplay works without Console errors.
- UI scales correctly across common mobile aspect ratios (Device Simulator).
- Portrait/landscape behavior matches expectations (if supported).
- Input/navigation flows are usable.
- Performance is acceptable in Play Mode; investigate spikes in the Profiler if needed (**Window** → **Analysis** → **Profiler**).

## 7. Troubleshooting

- **Unity Hub/Editor won't start or shows blank UI:** verify required system libraries, GPU drivers, and that you're on a supported Ubuntu 22.04 setup.
- **Project won't open:** confirm you selected the correct folder (one containing `Assets/` and, ideally, `ProjectSettings/`). If metadata is missing, use the bootstrap workflow above.
- **Scripts fail to compile:** ensure your Editor version supports the C# language/features used by the project; try the nearest LTS version or ask the team for the intended Unity version.
- **Device Simulator option missing:** confirm the package is installed and that your Game view is set to Simulator mode.

## 8. Reporting Findings

When play-testing reveals issues or UX suggestions, open/update the relevant GitHub issue with:
- Unity Editor version
- Ubuntu 22.04 details
- Steps to reproduce
- Screenshots/video (especially for UI/layout issues)
- Device Simulator profile/orientation used (if applicable)
