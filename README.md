# Steam Presence Companion (UI Edition)

A modern, high-performance WPF interface for your Steam Discord Rich Presence. This tool allows you to show your current Steam activity on Discord with a sleek, user-friendly dashboard.

---

### Credits & Acknowledgments
This project is a UI-enhanced fork of the excellent [steam-presence](https://github.com/JustTemmie/steam-presence) by **JustTemmie** and its contributors. Made with ❤ by the original team and polished for Windows with a native UI.

---

## Why use this?
Discord already detects games, but it has severe limitations:
- **Enhanced Rich Presence**: Standard Discord only shows the game name. This tool can show "Battling out of Elysium" in Hades or "Browsing Menus" in BTD6.
- **Linux/Steam Deck Support**: While the UI is Windows-native, the underlying engine is designed to bridge the gap for Steam Deck users by running on a separate machine or the Deck itself.
- **Privacy & Control**: Blacklist games you don't want to show or customize the art/text for any game.

## Key UI Features
- **Real-time Dashboard**: Monitor your presence engine status directly.
- **Auto-Start**: Optionally launch with Windows and start minimized to tray.
- **Secure Configuration**: Manage your Steam ID and preferences through the UI.
- **Exclude List**: Ignore specific games or apps with a few clicks.
- **Mica Backdrop**: Beautiful modern Windows 11 design language.

## Installation
1. Download the latest `Setup_SteamPresence.exe` from the [Releases](https://github.com/ShaggyLorean/steam-presence-ui/releases) page.
2. Run the installer and follow the instructions.
3. Configure your Steam ID in the initial prompt.

## How to get cookies.txt (MANDATORY)
*Required for fetching non-steam games and specific rich presence details.*

1. **Login**: Go to [steamcommunity.com](https://steamcommunity.com) and login to your account.
2. **Install Extension**: Install the **[Get-cookies.txt LOCALLY](https://github.com/kairi003/Get-cookies.txt-LOCALLY)** extension (available for both Chrome and Firefox).
3. **Export**: Go back to steamcommunity.com, click the extension icon, and click **Export**.
4. **Placement**: Save this file as `cookies.txt` and place it directly into the application installation folder. The app will automatically detect it.

## Advanced Features (inherited from original)
- **Steam Grid DB**: Enhanced cover art (enable in `config.json` with an API key).
- **Local Game Detection**: Scan for local processes (like Minecraft) and show them on Discord.
- **Game Overwrite**: Manually set what you are "playing", even if you're just browsing.
- **Blacklist/Whitelist**: Fine-grained control over what gets shared to Discord.

## Requirements
- Windows 10/11
- .NET 9.0 (included in the standalone version)
- A verified Steam account

## License
MIT License - feel free to fork and enhance!
