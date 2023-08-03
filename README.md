# Bizhawk-GeneticAlgorithmBot
Genetic algorithm control inputs generation bot for Bizhawk for the creation of tools-assisted speedruns.

Also includes neuro-evolution augmented topology (NEAT) for control inputs generation.

# Requirements

* Runs in BizHawk Emulator v2.9 and above.

# Development Requirements

* [Visual Studio 2022 for C#](https://learn.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=vs-2022)
* [BizHawk Pre-requisite Installer](https://github.com/TASEmulators/BizHawk-Prereqs)
* [Git](https://git-scm.com/downloads)

# Source Code Installation

1. Make sure you unzip a working copy of BizHawk (the full folder) and copy the entire folder to this project's root directory. File structure should look something like this:

```
GeneticAlgorithmBot
 ├ BizHawk/         <-----    This is where you put the full Bizhawk folder at.
 ├ src/
 ├ run_build.cmd
 ├ LICENSE
 ├ README.md
 └ .gitignore
```

2. Run the run_build.cmd batch file.

Click the video below to see the installation process using Windows Sandbox:

[![Bizhawk Genetic Algorithm Bot Source Installation](https://img.youtube.com/vi/YSm8GEpnsLk/hqdefault.jpg)](https://youtu.be/YSm8GEpnsLk)

# Release Installation

1. Make sure you back up your BizHawk's `config.ini` file before you attempt to load the bot (See https://github.com/TASEmulators/BizHawk/issues/3337 for more info).
2. Download and unzip the contents to `BizHawk/ExternalTools`. If this folder directory doesn't exist, you will need to make a new folder.
3. Run EmuHawk.
4. Make sure you have U+D/L+R controller buttons set to **Allow**.
5. Open TAStudio. (Required)
6. In the toolbar menu: Tools -> External Tools -> Genetic Algorithm Bot.

# Credits

* Genetic Algorithm Bot - tom_mai78101
* [NEAT C# Implementation](https://github.com/dnazirso/NeatSharp) - [dnazirso](https://github.com/dnazirso)
