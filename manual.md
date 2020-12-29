# Abstract

This manual will cover cloud-hosting and self-hosting workflows.

1. Cloud setup -- 5 minutes.
2. Self-hosting setup -- 1-2 days depending on your configuration.

Cloud setup is recommended for first timers or light users 
because the cloud admin will manage the infrastructure on behalf of you.
As you get the grip of it you should consider a self-hosted setup.
The cloud admin will let you export your data in case of conversion.

In any event feel free to reach out to the community on Discord: <br/>
https://discord.gg/H5ncHjZyVD

# Cloud Setup

## Talk to the cloud admin

Ping someone at the #general channel on Discord.

## Install Torch plugins

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

You can 

## Configure guest access

Guests can watch your dashboards via `guest_grafana.torchmonitor.net` to the same path.
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

Note that the connector plugin doesn't support authentication for v1.8 (unless requested).
You'll need to disable the database's auth so that the connector can write.
Make sure the database's port is not open to the Internet.

Also note that "database" means "bucket" in InfluxDB v1.8.
Should you see "bucket" in the Torch InfluxDB plugin please translate it to "database" in v1.8.
Both words point to an equivalent concept (in our context) and you can treat them equally.

You can probably build v2.0 yourself starting from the source code.
Alternatively you can utilize WSL or InfluxDB Cloud v2.0.

### Linux

InfluxDB v2.0 is the most reliable option for Linux.

## Download & install Grafana OSS or Enterprise

Grafana provides OSS and Enterprise versions which differ slightly to one another.
Unless you plan to edit the source code the Enterprise version is recommended (it's free to host despite the name).

Port (default TCP 3000) needs to open so that you can access the Web interface from the Internet.

## Import the sample Grafana dashboard

Get the JSON model of the sample Grafana dashboard linked in the #external-link channel on Discord.
You will see all kinds of errors in the imported dashboard because datasources are not set up yet.

## Set up InfluxDB datasources in Grafana

For the sample dashboard to work, you need to configure both InfluxQL and Flux datasources in Grafana.
InfluxQL and Flux are query interfaces of InfluxDB and must point to the same database.
Note that those datasource names must confirm to the regex pattern in the datasource variables defined in the sample dashboard.

### Create DBRP mapping for InfluxQL in InfluxDB v2.0

InfluxQL interface is not accessible by default in InfluxDB v2.0 and you'll need to wire it up manually 
(so that Grafana can query your database via InfluxQL interface). 
For more information see:

https://docs.influxdata.com/influxdb/v2.0/reference/api/influxdb-1x/dbrp

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
