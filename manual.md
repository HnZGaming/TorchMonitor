# Setting Up Torch Monitor Cloud

## Talk to Cloud Admin

Hit up @ryo at #general in Torch Discord to sign up.

You will shortly receive following things:

- Credentials to write to your dedicated database.
- A URL to your dedicated dashboard page.

## Download Torch Plugins

Save these plugin zips in your Plugins folder:

Profiler v3.1.0.17-6-g47b0d09<br/>
https://torchapi.net/plugins/item/da82de0f-9d2f-4571-af1c-88c7921bc063<br/>
`<guid>da82de0f-9d2f-4571-af1c-88c7921bc063</guid> <!--Profiler-->`

TorchInfluxDB v2.1.0<br/>
https://github.com/HnZGaming/TorchInfluxDb/releases<br/>
`<guid>5af3a335-0e25-4ddd-9fc7-6084d7e42e79</guid> <!--TorchInfluxDB-->`

TorchMonitor v2.1.1<br/>
https://github.com/HnZGaming/TorchMonitor/releases <br/>
`<guid>5ffdf796-4fca-446b-bc2e-2dee3d971532</guid> <!--TorchMonitor-->`

Make sure GUIDs are registered to your Torch.cfg as well.

## Configure TorchInfluxDB Plugin

Navigate to `InfluxDB Integration` in the plugin list on Torch UI and 
enter following credentials that you've received from the cloud admin:

- `Host URL`
- `Organization`
- `Bucket`
- `Authentication Token`

You do NOT need to restart Torch to change these configs.

Or, fill the following XML & save as `TorchInfluxDbPlugin.cfg` in your Instance folder:

```
<?xml version="1.0" encoding="utf-8"?>
<TorchInfluxDbConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Enable>true</Enable>
  <HostUrl>____</HostUrl>
  <Organization>____</Organization>
  <Bucket>____</Bucket>
  <AuthenticationToken>____</AuthenticationToken>
  <SuppressResponseError>false</SuppressResponseError>
  <WriteIntervalSecs>10</WriteIntervalSecs>
</TorchInfluxDbConfig>
```

Other plugins don't need initial configuration.

Make sure both TorchInfluxDB and TorchMonitor plugins are enabled by their plugin configs.

## Common Issues & Diagnosis

### Dashboard has red markers with a descriptive error message

Talk to the cloud admin.

### Can't see data in the dashboard

Dashboard will not immediately fill in because your data needs to accumulate first.

1. From the top right corner of the page, change "Last 24 hours" to "Last 5 minutes". Wait for data to flow in.
1. Make sure your server's OS time zone is in sync with the machine's clock.
1. Wait for 5 minutes and see if the dashboard starts filling.

### Still can't see it

Look into the plugin's logs.

1. Find the log file (`Torch.log`) or the console, and see if any errors are yielded by `InfluxDb.*` or `TorchMonitor.*`.
1. Set new NLog rule for `InfluxDb.*` and `TorchMonitor.*` with `minlevel` set to `Trace`. See `NLog.config` in Torch directory. Note a restart is needed for this to take effect.

### `System.IO.FileNotFoundException` in Torch log

You have install wrong version of plugins.

### `Failed to write to database (NotFound)`

You have input wrong Host URL or Organization ID.

### `Failed to write to database (AccessDenied)`

You have input wrong auth credentials.

## Questions & Feedbacks

Ping @ryo at #general in Torch Discord.
