# vpnc - VPN Control

Universal tool for automatic and remote control of VPN connection via cli, desktop application (system tray) and `API`.

## What for

I'm tired that the application I'm using may periodically disconnect my VPN connection, most often due to prolonged absence of Internet connection. In this case, even if the automatic reconnection setting is enabled, it may be considered by the application as a manual disconnection.

<!-- 
```shell
dotnet add package Newtonsoft.Json
dotnet add package Microsoft.AspNetCore.App
dotnet add package Swashbuckle.AspNetCore
dotnet build
dotnet run [start|stop|status|api]
```

Get metrics:

```shell
dotnet run status

{
  "processName": "hsscp",
  "processStatus": "Running",
  "processUptime": "0.0:48:40",
  "systemUptime": "0.1:28:10",
  "interfaceName": "HotspotShield Network Adapter",
  "interfaceStatus": "Up",
  "pingAddress": "8.8.8.8",
  "pingStatus": "Connected",
  "pingTimeout": "141 ms",
  "country": "US",
  "timeZone": "America/New_York",
  "region": "New York",
  "city": "New York City",
  "location": "40.7143,-74.0060",
  "externalIp": "45.56.198.228"
}
```
-->