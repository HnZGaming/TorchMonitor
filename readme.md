# TorchMonitor

Collects and sends various game data to a database. 

## Usage

Intended for server health monitoring and analysis. 

To view the data, hook up a graphical dashboard like [Grafana](https://grafana.com/).

[See an example dashboard](https://guest-grafana.torchmonitor.net/d/9UUUl7pGk/short-term-monitor?orgId=6&refresh=1m) (if still alive).

## Dependencies

* [TorchInfluxDb plugin](https://github.com/HnZGaming/TorchInfluxDb) to write to the database.
* [Profiler plugin](https://github.com/TorchAPI/Profiler) to collect some game data.

## Default Monitors

Following monitors come with this plugin.

- `FloatingObjectsMonitor` counts the number of "floating objects".
- `GeoLocationMonitor` monitors which countries your players are from.
- `GridMonitor` monitors a lot of things about grids and factions.
- `OnlinePlayersMonitor` counts the number of online players and more.
- `RamUsageMonitor` monitors the total RAM usage of Torch and SE program.
- `SyncMonitor` monitors the simulation speed.
- `VoxelMonitor` counts the number of voxel bodies (planets and asteroids).

## Custom Monitors

Fork this repo and make your own monitors.

Implement `Intervals.IIntervalListener` and register into the plugin.

```C#
public class YourMonitor : IIntervalListener
{
    public void OnInterval(int intervalsSinceStart)
    {
        // your monitor's implementation
    }
}
```

```C#
void OnGameLoaded()
{
    _intervalRunner.AddListeners(new IIntervalListener[]
    {
        ...
        new YourMonitor(), // Add this line
        ...
    });
    
    ...
```

This will make the plugin invoke your monitor during the game.

### Example Implementation

```C#
public void OnInterval(int intervalsSinceStart)
{
    // Do work every 20 seconds
    if (intervalsSinceStart % 20 != 0) return;

    ...
    
    // Write to an InfluxDB database
    InfluxDbPointFactory
        .Measurement("my_measurement")
        .Tag("my_tag", myTagValue)
        .Field("my_field", myFieldValue)
        .Write();
}
```

See default monitors in the plugin for code examples.

## Contribution

Feel free to send us your PR or participate in our discussion in Torch Discord.
