TorchMonitor
===

A Torch plugin to collect and send game data to an InfluxDB database. 

Example dashboard (if still alive): http://play.se.hnz.asia:3000/d/9UUUl7pGk/hnz-gaalsien

Note that this plugin won't set up the database/dashboard. It just sends data to a specified endpoint URL of an InfluxDB instance.
The example above uses a self-hosted Grafana instance that fetches data from the InfluxDB instance.

Note that this plugin is in a preview state. It may take some effort to set it up.

Prerequisites
---

You need following things in order to run this plugin.

- Our fork of Torch (embeds InfluxDB). https://github.com/HnZGaming/Torch
- Our post-build packaging program (unless you have your own). https://github.com/HnZGaming/TorchPluginPackager

How to Use
---

1. Run Torch once and you'll see a config file generated in the root folder.
2. Set your InfluxDB instance's IP/port and whatever blank in the config.
3. Create tables in InfluxDB according to the plugin code.
3. Restart Torch.
4. See game data flowing into the database.

"Premade" Monitors
---

Following monitors come with the plugin.

- `AsteroidMonitor`. Counts the number of voxel bodies.
- `FactionConcealmentMonitor`. Counts the number of total/active blocks/grids per faction.
- `FloatingObjectsMonitor`. Counts the number of "floating objects" in the game's term.
- `GridMonitor`. Measures a lot of things around each grid and faction. Probably the most useful of all.
- `PlayerCountMonitor`. Counts the number of online players.
- `PlayersMonitor`. Measures the current online time of each player and faction.
- `RamUsageMonitor`. Measures the RAM usage of the program.
- `SyncMonitor`. Measures the simspeed.

Note that you need to create tables in your InfluxDB according to the code in these classes.

Extension
---

The plugin goes through a list of "listeners" every second:

    public interface IIntervalListener
    {
        void OnInterval(int intervalsSinceStart);
    }

Monitors should implement this interface and override the interval with their own frequency:

    public void OnInterval(int intervalsSinceStart)
    {
        if (intervalsSinceStart % 20 != 0) return;
        
        // work every 20 seconds
        ...
    }

The interval time is mostly reliable as each implementation is run in parallel. 
The plugin will print a warning if any implementation spends more than 1 second during an interval.

Frame of reference: iterating over every block in 1000 grids often takes 200 to 300 milliseconds.

Contribution
---

Please fork the repo and make your own monitors. There's a lot unexplored in the game. Please share your monitors with the community by PR.

License
---

MIT but please follow InfluxDB's license first.
