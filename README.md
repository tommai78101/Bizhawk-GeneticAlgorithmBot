# Bizhawk-GeneticAlgorithmBot
Genetic algorithm control input bot for Bizhawk for the creation of tools-assisted speedruns.

# Source Code Installation

1. Make sure you unzip a working copy of BizHawk (the full folder) and copy the entire folder to this project's root directory. File structure should look something like this:

```
GeneticAlgorithmBot
 ├ BizHawk/
 ├ src/
 ├ run_build.cmd
 ├ LICENSE
 ├ README.md
 └ .gitignore
```

2. Run the run_build.cmd batch file.

# Release Installation

1. Make sure you back up your BizHawk's `config.ini` file before you attempt to load the bot (See https://github.com/TASEmulators/BizHawk/issues/3337 for more info).
2. Download and unzip the contents to `BizHawk/ExternalTools`. If this folder directory doesn't exist, you will need to make a new folder.
3. Run EmuHawk.
4. Make sure you have U+D/L+R controller buttons set to **Allow**.
5. Open TAStudio. (Required)
6. In the toolbar menu: Tools -> External Tools -> Genetic Algorithm Bot.
