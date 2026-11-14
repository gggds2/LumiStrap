<div align="center">
  <img src="Images/LumiStrap-brand.jpg" alt="LumiStrap" width="760">
  <p><strong>A customizable Roblox bootstrapper for Windows.</strong></p>
  <p>
    <a href="https://github.com/gggds2/LumiStrap/releases/latest"><img src="https://img.shields.io/github/v/release/gggds2/LumiStrap?style=for-the-badge&color=4F7CF7" alt="Latest release"></a>
    <a href="https://github.com/gggds2/LumiStrap/releases"><img src="https://img.shields.io/github/downloads/gggds2/LumiStrap/total?style=for-the-badge&color=8B5CF6" alt="Total downloads"></a>
    <a href="LICENSE"><img src="https://img.shields.io/github/license/gggds2/LumiStrap?style=for-the-badge&color=7C3AED" alt="License"></a>
  </p>
  <p>
    <a href="https://github.com/gggds2/LumiStrap/releases/latest"><strong>Download LumiStrap</strong></a>
    · <a href="https://github.com/gggds2/LumiStrap/releases">Release notes</a>
    · <a href="https://github.com/gggds2/LumiStrap/issues">Report an issue</a>
  </p>
</div>

LumiStrap is an independent community fork of [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap), built to make Roblox setup and configuration easier to manage. It has its own LumiStrap branding, installer, and configuration profiles.

> LumiStrap is not affiliated with or endorsed by Roblox Corporation.

## What you can do

| Feature | Description |
| --- | --- |
| **LumiFix** | Review the Roblox installation, repair client files, or clear the download cache. LumiStrap explains the action and asks before making changes. |
| **Profiles** | Save and switch between LumiStrap settings and custom FastFlags. Profiles store configuration; they do not copy Roblox files or mods. |
| **Session panel** | Open quick links while playing and view the available experience and server details. |
| **Clearer downloads and errors** | Track download progress and speed, cancel or retry, and get plain-language explanations with an option to copy logs. |
| **Graphics and FastFlags** | Choose Automatic, Direct3D 11, or Vulkan rendering; adjust mesh detail and FRM quality; manage FastFlags. Roblox may limit or ignore some flags. |
| **Safer updates** | Review version notes before updating, postpone an update, and keep up to five recent configuration backups. |

## Install

1. Open the [latest release](https://github.com/gggds2/LumiStrap/releases/latest).
2. Download `LumiStrap.exe`.
3. Run it and follow the LumiStrap setup wizard.

## Privacy and compatibility

- LumiStrap is for Windows.
- Analytics are disabled by default.
- The installer uses a separate LumiStrap installation folder from Bloxstrap.
- FastFlags are subject to Roblox's own limits and behavior.

## Build from source

Use Windows and the .NET SDK version listed in `global.json`. Initialize the `wpfui` submodule, then restore and build the solution:

```powershell
git submodule update --init --recursive
dotnet restore
dotnet build -c Release
```

## Credits and license

LumiStrap is derived from the open-source [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap) project and retains its MIT license and upstream copyright notice in [`LICENSE`](LICENSE). Keep that file with any redistribution.

