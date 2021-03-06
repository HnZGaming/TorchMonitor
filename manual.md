# Abstract

This manual will cover cloud-hosting and self-hosting workflows.

1. Cloud setup -- 5 minutes.
2. Self-hosting setup -- 1-2 days depending on your configuration.

Cloud setup is recommended for first timers or light users 
because the cloud admin will manage the infrastructure on behalf of you.
As you get the grip of it you should consider a self-hosted setup.
The cloud admin will let you export your data in case of conversion.

In any event feel free to reach out to the community on Discord: <br/>
https://discord.gg/AaqdbWa3AP

# Cloud Setup

## Talk to the cloud admin

Ping someone at [#torch-monitor](https://discord.gg/TE26pukevH) on Discord.

## Install Torch plugins

See: [How to install plugins to your Torch server](https://wiki.torchapi.net/index.php/Plugins)

TorchMonitor<br/>
https://github.com/HnZGaming/TorchMonitor/releases <br/>
`<guid>5ffdf796-4fca-446b-bc2e-2dee3d971532</guid> <!--TorchMonitor-->`

TorchInfluxDB<br/>
https://github.com/HnZGaming/TorchInfluxDb/releases <br/>
`<guid>5af3a335-0e25-4ddd-9fc7-6084d7e42e79</guid> <!--TorchInfluxDB-->`

Profiler v3.1.*<br/>
https://torchapi.net/plugins/item/da82de0f-9d2f-4571-af1c-88c7921bc063 <br/>
`<guid>da82de0f-9d2f-4571-af1c-88c7921bc063</guid> <!--Profiler-->`

## Configure Torch plugins

Navigate to `InfluxDB Integration` plugin on Torch UI and 
enter following credentials that you've received from the cloud admin:

- `Host URL`
- `Organization`
- `Bucket`
- `Authentication Token`

You do NOT need to restart Torch to apply these changes.

If your server is not running, you can save following config as `Instance/TorchInfluxDbPlugin.cfg`:

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

Configure other plugins per your need or leave them as-is.

## Watch your dashboard

Sign in & watch the dashboard via links that you've received from the cloud admin.

## Configure guest access

Guests can watch your dashboards via `guest-grafana.torchmonitor.net` to the same path.
To prevent a guest access to your dashboard, remove the view permission in the dashboard settings.
For more information see:

https://grafana.com/docs/grafana/latest/permissions/dashboard_folder_permissions/

## Common Issues

### Dashboard has a red marker with an error message

Talk to the cloud admin.

### Dashboard has empty panels

1. Wait for your Torch monitor to push some data into the database.
1. Set the window time to "Last 5 minutes" on the dashboard.
1. Make sure your server's OS time zone is in sync with the hardware clock.

### `System.IO.FileNotFoundException`

You might have install wrong version of plugins.

### `Failed to write to database (NotFound)`

You might have wrong Host URL or Organization ID in the plugin config.

### `Failed to write to database (AccessDenied)`

You might have wrong auth credentials in the plugin config.

## Questions & Feedbacks

Ping someone at the #help channel on Discord.

# Self-hosting Setup

Setting up Torch Monitor on your own server can be tricky & involved especially if you haven't worked on DB etc.
The community has cooked up an in-depth document to guide you through every detail on the procedure:
https://docs.google.com/document/d/1U4H3BWdPg9kjEfuI8yzML4YEMYNbEAPv1QopeI8Ljsw/edit?usp=sharing

## Architecture

### Platforms

Either Windows or Linux works.

### Data Flow

1. Torch Monitor plugin (Torch) will collect game data using Profiler plugin.
1. Torch InfluxDB plugin (Torch) will write the game data to your InfluxDB instance.
1. Your InfluxDB instance will store the game data.
1. Your Grafana instance will read and display the game data from your InfluxDB instance.

### Ports

1. Torch doesn't need one open (for this feature).
1. InfluxDB doesn't need one open either, unless written/read via network.
1. Grafana needs one open (default TCP 3000) so that you can watch the dashboard via network.

## Download & install InfluxDB OSS v1.8 or v2.0

### Windows

InfluxDB v1.8 is recommended for Windows because there's an official build available.
Note that some terminology had changed from v1.8 to v2.0: "database" -> "bucket" etc.

### Linux

InfluxDB v2.0 is the most reliable option for Linux.

## Download & install Grafana OSS or Enterprise

Enterprise version is recommended. It's called "enterprise" but free to download & use.
If you plan to edit the program you might choose the OSS version. See the official manual for details.

Port (default TCP 3000) needs to open so that you can access the Web interface from the Internet.

## Import the sample Grafana dashboard

Get the JSON model of the sample Grafana dashboard linked in the #external-link channel on Discord.
You will see all kinds of errors in the imported dashboard because datasources are not set up yet.

## Set up InfluxDB datasources in Grafana

For the sample dashboard to work, you need to configure both InfluxQL and Flux datasources in Grafana.
InfluxQL and Flux are query interfaces of InfluxDB and must point to the same database.
Note that those datasource names must confirm to the regex pattern in the datasource variables defined in the sample dashboard.

### Create DBRP mapping for InfluxQL in InfluxDB v2.0

InfluxQL interface's HTTP API is not accessible by default in InfluxDB v2.0 and you'll need to wire it up manually 
(so that Grafana can query your database via InfluxQL interface).
For more information and instruction on how to "map" v1.8 databases to v2.0 HTTP API, see:

https://docs.influxdata.com/influxdb/v2.0/reference/api/influxdb-1x/dbrp

## Metrics

For existing metrics and making your own, see here:

https://github.com/HnZGaming/TorchMonitor/blob/master/readme.md

Note that the plugin ships with some "commented out" metrics that you may be interested to try out.
Those metrics consume some more database space but give you some more detail into the server health.

## Configure guest access in Grafana

Grafana allows unauthenticated users to sign in anonymously in a guest organization
that you define in the app config.
You can set up public dashboards in that organization.
Guest users cannot view any dashboards outside the organization.

## Common Issues

### Can't find "bucket" in InfluxDB v1.8

It's termed as "database" in InfluxDB v1.8, updated to "bucket" in v2.0.
Means the same thing. For more information see:

https://docs.influxdata.com/influxdb/v1.8/concepts/glossary/

## Questions & Feedback

In any event feel free to reach out to the community on Discord.
