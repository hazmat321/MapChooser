<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" />
  <h2><strong>MapChooser</strong></h2>
  <h3>A powerful and customizable map voting system for SwiftlyS2.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build Status">
  <img src="https://img.shields.io/github/downloads/SwiftlyS2-Plugins/MapChooser/total" alt="Downloads">
  <img src="https://img.shields.io/github/stars/SwiftlyS2-Plugins/MapChooser?style=flat&logo=github" alt="Stars">
  <img src="https://img.shields.io/github/license/SwiftlyS2-Plugins/MapChooser" alt="License">
</p>

## Overview

MapChooser is a map voting plugin for SwiftlyS2. It handles Rock The Vote (RTV), map nominations, and manual voting. It also triggers an automated vote at the end of the map based on time or rounds played.

## Commands

| Command | Description |
| :--- | :--- |
| `!rtv` | Votes to trigger a map vote immediately (Rock The Vote). |
| `!nominate [map]` | Nominates a map to be included in the next map vote. |
| `!timeleft` | Shows the remaining time or rounds left on the current map. |
| `!nextmap` | Shows which map will be played next (if decided). |
| `!votemap [map]` | Directly votes for a specific map to change to. |
| `!revote` | Reopens the map vote menu if a vote is currently active. |
| `!extend` | Votes to extend the current map. |
| `!unrtv` | Removes your current RTV vote. |
| `!setnextmap [map]` | Sets the next map directly or opens a selection menu (Admin only). |
| `!mapsvote` | Opens a menu to select multiple maps and start a custom vote (Admin only). |
| `!map` | Loads requested map (Admin only). |

## Configuration (`config.jsonc`)

### RTV Settings (`Rtv`)
| Setting | Default | Description |
| :--- | :--- | :--- |
| `Enabled` | `true` | Enable or disable the Rock The Vote system. |
| `EnabledInWarmup` | `true` | Allow players to RTV during warmup. |
| `NominationEnabled` | `true` | Allow players to nominate maps for the vote. |
| `MinPlayers` | `0` | Minimum number of players required to enable RTV. |
| `MinRounds` | `0` | Minimum rounds that must be played before RTV is allowed. |
| `ChangeMapImmediately` | `true` | Change the map immediately after a successful RTV vote (3s delay). |
| `MapsToShow` | `6` | Number of maps to display in the RTV vote menu. |
| `VoteDuration` | `30` | How long the vote menu remains open (seconds). |
| `VotePercentage` | `60` | Percentage of players required to trigger the vote. |
| `VoteCooldownTime` | `300` | Cooldown time in seconds between failed RTV votes. |

### Votemap Settings (`Votemap`)
| Setting | Default | Description |
| :--- | :--- | :--- |
| `Enabled` | `true` | Enable or disable the manual `!votemap` command. |
| `VotePercentage` | `60` | Percentage of players required to reach the vote threshold. |
| `ChangeMapImmediately` | `true` | Change map immediately once the threshold is reached. |
| `MinPlayers` | `0` | Minimum players required to use `!votemap`. |

### End Of Map Settings (`EndOfMap`)
| Setting | Default | Description |
| :--- | :--- | :--- |
| `Enabled` | `true` | Enable or disable the automated vote at the end of the map. |
| `MapsToShow` | `6` | Number of maps to show in the automated vote. |
| `VoteDuration` | `30` | Duration of the automated vote menu. |
| `TriggerSecondsBeforeEnd` | `120` | Seconds before timelimit to trigger the vote. |
| `TriggerRoundsBeforeEnd` | `2` | Rounds (or score difference from winning) before map end to trigger the vote. |
| `AllowExtend` | `true` | Allow extending the map from the end-of-map vote menu. |
| `ExtendTimeStep` | `15` | Minutes to add when extending by time. |
| `ExtendRoundStep` | `5` | Rounds to add when extending by rounds. |
| `ExtendLimit` | `3` | Maximum number of times the map can be extended. |

### Extend Map Settings (`ExtendMap`)
| Setting | Default | Description |
| :--- | :--- | :--- |
| `Enabled` | `true` | Enable or disable the `!extend` command. |
| `EnabledInWarmup` | `false` | Allow players to vote to extend during warmup. |
| `MinPlayers` | `0` | Minimum players required to use `!extend`. |
| `MinRounds` | `0` | Minimum rounds required before `!extend` can be used. |
| `VotePercentage` | `60` | Percentage of players required to pass the extend vote. |

### Global Settings
| Setting | Default | Description |
| :--- | :--- | :--- |
| `MapsInCooldown` | `3` | Number of recently played maps to exclude from the next vote. |
| `AllowSpectatorsToVote` | `false` | Allow spectators to participate in votes. |
| `SetNextMapPermission` | `admin.changemap` | Permission flag required for the `!setnextmap` command. |
| `MapsVotePermission` | `admin.mapsvote` | Permission flag required for the `!mapsvote` command. |
| `Maps` | (List) | List of maps available. Use `ws:ID` for workshop maps. |

### Map Configuration

Each map entry supports the following properties:

| Property | Default | Description |
| :--- | :--- | :--- |
| `Name` | (required) | Display name shown in menus and chat. |
| `Id` | (required) | Map identifier. Use the map name (e.g. `de_dust2`) or a workshop ID (e.g. `3124567099`). |
| `MinPlayers` | `0` | Minimum number of real players required for this map to appear in votes and nominations. `0` means no minimum. |
| `MaxPlayers` | `0` | Maximum number of real players allowed for this map to appear in votes and nominations. `0` means no maximum. |

> **Note:** Maps without `MinPlayers`/`MaxPlayers` (or set to `0`) are considered valid for any number of players. If you want a map to **only** appear when there are few players, you must also set `MinPlayers` on your other maps to exclude them at low player counts. Bots are not counted.

#### Example
```jsonc
"Maps": [
    {
        "Name": "Dust II",
        "Id": "de_dust2",
        "MinPlayers": 4
    },
    {
        "Name": "Inferno Night",
        "Id": "3124567099",
        "MinPlayers": 4
    },
    {
        "Name": "Mirage",
        "Id": "de_mirage",
        "MinPlayers": 1,
        "MaxPlayers": 3
    },
    {
        "Name": "Overpass",
        "Id": "de_overpass",
        "MinPlayers": 1,
        "MaxPlayers": 3
    }
]
```

In this example, when there are 1–3 real players on the server, only `aim_redline_snow` and `AWP LEGO` will appear in votes and nominations. Once there are 4+ players, only `Dust II` and `Inferno Night` will be available.

## Features

- **Live Vote Updates**: Menus refresh in real-time as players cast their votes.
- **Smart Triggers**: Automated votes trigger based on time remaining, rounds remaining, or when a team is close to winning.
- **Map Cooldown**: Prevents recently played maps from appearing in the vote too soon.
- **Map Extension**: Players can vote to extend the current map time or round limit.
- **Spectator Control**: Configurable option to allow or disallow spectators from voting.
- **Vote Removal**: Players can change their mind and remove their RTV vote with `!unrtv`.
- **Workshop Support**: Seamlessly change to workshop maps using their IDs.
- **Localized**: Full translation support via JSONC files.

## Installation

1. Download the latest release or build the plugin yourself.
2. Copy the published plugin folder to your server:
   `.../game/csgo/addons/swiftlys2/plugins/MapChooser/`
3. Ensure the plugin has its `resources/` folder alongside the DLL.
4. Start/restart the server.

## Building

- Open the project in your preferred .NET IDE (e.g., VS Code, Visual Studio).
- Build the project. The output DLL and resources will be placed in the `build/` directory.
