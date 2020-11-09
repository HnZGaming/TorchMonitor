TorchMonitor
===

Example use of [TorchInfluxDb plugin](https://github.com/HnZGaming/TorchInfluxDb).

Following monitors come with this plugin.

- `AsteroidMonitor`. Counts the number of voxel bodies.
- `FactionGridMonitor`. Counts the number of total/active blocks/grids per faction.
- `FloatingObjectsMonitor`. Counts the number of "floating objects" in the game's term.
- `GridMonitor`. Measures a lot of things around each grid and faction. Probably the most useful of all.
- `PlayerCountMonitor`. Counts the number of online players.
- `PlayersMonitor`. Measures the current online time of each player and faction.
- `RamUsageMonitor`. Measures the RAM usage of the program.
- `SyncMonitor`. Measures the simspeed.

[Example dashboard](http://play.se.hnz.asia:3000/d/9UUUl7pGk/hnz-gaalsien?orgId=1&refresh=30s) (if still alive).

Follow [the official instruction](https://grafana.com/docs/grafana/latest/datasources/influxdb) to map these monitors to a [Grafana dashboard](https://grafana.com).

Making New Monitors
---

Feel free to fork this repo and make your own monitors in the existing architecture.

Implement `Intervals.IIntervalListener`:

```C#
public interface IIntervalListener
{
    void OnInterval(int intervalsSinceStart);
}
```

Implement the monitor's logic in `OnInterval()`:

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

Register to `TorchMonitorPlugin`:

```C#
void OnGameLoaded()
{
    _intervalRunner.AddListeners(new IIntervalListener[]
    {
        ...
        new YourMonitor(),
        ...
    });
    
    ...
```

See bundled monitors for a coding example.

Contribution
---

PR with your monitors is always welcome.
