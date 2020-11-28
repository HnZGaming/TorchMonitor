#TorchMonitor

Sends game data over to an InfluxDB instance for server health monitoring.

## Example Usage

Combine with a graphical dashboard (such as [Grafana](https://grafana.com/)) to monitor your game.

[See an example dashboard](http://play.se.hnz.asia:3000/d/9UUUl7pGk/hnz-gaalsien?orgId=1&refresh=30s) (if still alive).

## Dependencies

* [TorchInfluxDb plugin](https://github.com/HnZGaming/TorchInfluxDb) to write to the database.
* [Profiler plugin](https://github.com/TorchAPI/Profiler) to collect some game data.

## Installation

Grab the latest release binary (and dependency above) and put it in your Torch.

## Default Monitors

Following monitors come with this plugin.

- `FloatingObjectsMonitor` counts the number of "floating objects".
- `GeoLocationMonitor` monitors which countries your players are from.
- `GridMonitor` monitors a lot of things about grids and factions.
- `OnlinePlayersMonitor` counts the number of online players and their factions.
- `RamUsageMonitor` monitors the total RAM usage of Torch and SE program.
- `SyncMonitor` monitors the simulation speed.
- `VoxelMonitor` counts the number of voxel bodies (planets and asteroids).

## Custom Monitors

Fork this repo and make your own monitors.

### Architecture

This plugin circulates every registered monitor every second in a loop.

`Intervals.IIntervalListener` is the interface to work with the plugin's loop:

```C#
public interface IIntervalListener
{
    void OnInterval(int intervalsSinceStart);
}
```

`intervalsSinceStart` increments every loop.

Once you've implemented the interface, register it to the plugin's loop by editing `TorchMonitorPlugin.cs`:

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

### Implementation

Implement your main logic in `IIntervalListener.OnInterval()`:

```C#
public void OnInterval(int intervalsSinceStart)
{
    // Do work every 20 seconds
    if (intervalsSinceStart % 20 != 0) return;

    ...
    
    // Write to the database
    InfluxDbPointFactory
        .Measurement("my_measurement")
        .Tag("my_tag", myTagValue)
        .Field("my_field", myFieldValue)
        .Write();
}
```

See default monitors for code examples.

## Contribution

Feel free to send us your PR or participate in our discussion in Torch Discord.