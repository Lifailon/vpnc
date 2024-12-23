<p align="center">
  <img title="Ping Not Available"src="img/ping-not-available.png">
  <img title="next"src="img/next.png">
  <img title="Ping Available"src="img/ping-available.png">
  <img title="next"src="img/next.png">
  <img title="Ping Not Available"src="img/vpn.png">
</a>

<h1 align="center">
vpnc - VPN Control
</h1>

<h4 align="center">
    <strong>English</strong> | <a href="README_ru.md">Русский</a>
</h4>

A universal tool for automatic (local) and remote VPN connection management via a desktop application (system tray) and `API`.

In fact, this tool can act as an `API` to control the start and stop of any processes on a remote Windows system.

- [For what](#for-what)
- [Functionality](#functionality)
- [Installation and build](#installation-and-build)
- [Configuration](#configuration)
- [Usage](#usage)
- [API](#api)
  - [Get status](#get-status)
  - [Process start](#process-start)
  - [Process stop](#process-stop)
- [Backlog](#backlog)
- [Alternatives](#alternatives)

## For what

I am tired of the fact that the [Hotspot Shield](https://hotspotshield.com/vpn/vpn-for-windows) or [ProtonVPN](https://github.com/ProtonVPN/win-app) application can periodically disconnect the VPN connection, most often this happens due to a long connection to the VPN network (several dozen hours) or a long absence of an Internet connection. In this case, even if the automatic reconnection setting is enabled, the connection may not be re-established, or this may be regarded by the application as a manual disconnection.

This application performs exactly three functions - it terminates the process by name and starts the process along the path specified in the configuration file or passed in parameters via `API`, and also collects statistics for a reliable check of the VPN connection and Internet availability.

In addition, this approach can be useful if you need to remotely disable the VPN connection while a large volume of traffic is loading on the target machine, and also allows you to control this connection. This functionality is integrated into the [Kinozal-Bot](https://github.com/Lifailon/Kinozal-Bot) project in version `0.4.7` for remote control of processes via the **Telegram bot**.

## Functionality

![interface](/img/interface.jpg)

- The `API` interface, which allows you to configure remote management of the VPN connection on the target host. For example, in my network this is a dedicated machine, where access to specific content from other machines is carried out by means of Proxy (for example, [froxy](https://github.com/Lifailon/froxy)), this is convenient so as not to limit all traffic to the Internet to a VPN connection and at the same time not to be limited to separate tunneling to the VPN network.

- Monitoring the availability of the Internet connection. The check is performed every 5 seconds (by default, can be changed in the configuration file), if the connection is unavailable, a system notification will be sent (and the status of the application icon will also change) and the check will be performed every two seconds until the connection is stable (3 successful `icmp` requests in a row with an interval of 2 seconds), after which a second notification will be sent.

- Monitoring the availability and automatic restoration of the VPN connection. If the Internet is available and the VPN process is running (excluding manual termination), but the VPN network interface is in the offline state, the application will be automatically restarted to reconnect to the VPN network until the connection is restored (by default, every 120 seconds, can be changed in the configuration file).

- Record the history of pings and requests to `API` in a log file.

This approach is universal, so it can and will work with any VPN application. The only condition for this method to work is that your VPN client can automatically connect to the VPN network when you launch the application (most clients support this).

## Installation and build

A portable version of the application is available on the [releases](https://github.com/Lifailon/vpnc/releases/latest).

It is necessary that the platform [.NET Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) be installed on the system to run .NET applications.

To build the application, clone the repository and install dependencies:

```shell
git clone https://github.com/Lifailon/vpnc
cd vpnc
dotnet build
dotnet publish
```

To exclude installations of **.NET Runtime** on the target system, include it in the build:

```shell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

Packages used:

```shell
dotnet add package Newtonsoft.Json
dotnet add package Microsoft.AspNetCore.App
dotnet add package Swashbuckle.AspNetCore
```

## Configuration

To quickly access the configuration, use the **Open Configuration** button in the tray context menu. The configuration is located in the `vpnc.json` file, next to the executable file.

Configuration example:

```json
{
    "ProcessName": "ProtonVPN",
    "ProcessPath": "C:\\Program Files\\Proton\\VPN\\v3.4.3\\ProtonVPN.exe",
    "InterfaceName": "ProtonVPN TUN",
    "ApiPort": 1780,
    "ApiKey": "b1f8e72d-9c34-4a2e-a5f1-3d57b2a1c7f8",
    "PingHost": "8.8.8.8",
    "PingLog": true,
    "PingStartup": true,
    "VpnStartup": false,
    "ApiStartup": true,
    "PingTimeout": 5,
    "VpnTimeout": 5,
    "VpnRestartTimeout": 120
}
```

Parameter description:

- `ProcessName` - The name of the process to be terminated. Most often, this name corresponds to the executable file, it can also be determined in the Windows Task Manager.
- `ProcessPath` - The full path to the executable file that is responsible for starting the VPN client.
- `InterfaceName` - The name of the virtual VPN interface (created automatically when installing the VPN client). To determine the interface name, you can use the classic `ipconfig` command.
- `ApiPort` - The port on which the `Web/REST API` interface will be launched.
- `ApiKey` - Key for accessing `API` endpoints (access to Swagger is allowed without a key, and authorization via the interface has been added).
- `PingHost` - The address that will act as a node for checking the availability of the Internet connection.
- `PingLog` - Enable recording of pings to the log file.
- `PingStartup/VpnStartup/ApiStartup` - Defines the initial state when starting the interface. Takes a Boolean value of `true` or `false`.
- `PingTimeout/VpnTimeout` - The frequency of automatic checks in seconds.
- `VpnRestartTimeout` - Delay before restarting the process again (works only when `"VpnStartup": false`) in seconds.

To quickly get all the parameters, use PowerShell:

```PowerShell
# Process name for partial match in name
Get-Process *protonvpn*
# Path to executable file by process name
Get-Process *protonvpn* | Select-Object *path*
# Network adapter name
Get-NetIPConfiguration | Where-Object InterfaceAlias -match "proton"
```

The remaining parameters can be left as default. To apply the settings, you must restart the application.

## Usage

Run the application in command line mode:

```shell
dotnet run [start|stop|status|api|tray|process]
```

The `process` parameter is used to launch the application in the background process mode for control via the system tray (similar to `vpnc.exe tray`).

To launch the program at system boot, go to the startup directory (`Win+R` - `shell:startup`):

```
%USERNAME%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
```

Create a shortcut in the root of the `Startup` directory with the following content:

```
"C:\Users\<UserName>\Documents\vpnc\vpnc.exe" process
```

Replace the path to the executable file with your own. If elevated rights are required to end processes, in the shortcut properties, select `Compatibility` and enable `Run this program as an administrator`.

## API

The `API` interface can be launched in command line mode (cli), or from the context menu of the application in the tray. The automatic activation of the `API` when the application is started is defined by the parameter `ApiStartup` in the configuration file.

**Swagger** documentation is available via the `Swashbuckle.AspNetCore` library at: http://localhost:1780/swagger

![swagger](/img/swagger.jpg)

### Get status

`curl -s http://192.168.3.100:1780/api/status -H 'X-API-KEY: b1f8e72d-9c34-4a2e-a5f1-3d57b2a1c7f8' | jq`

```json
{
  "processName": "ProtonVPN",
  "processStatus": "Running",
  "processUptime": "0.0:8:2",
  "systemUptime": "3.0:33:21",
  "interfaceName": "ProtonVPN TUN",
  "interfaceStatus": "Up",
  "pingAddress": "8.8.8.8",
  "pingStatus": "Connected",
  "pingTimeout": "94 ms",
  "country": "RO",
  "timeZone": "Europe/Bucharest",
  "region": "București",
  "city": "Bucharest",
  "externalIp": "185.XXX.XXX.XXX"
}
```

To get data about the external connection (which is responsible for Internet access), the [ipinfo](https://ipinfo.io) service is used.

### Process start

```shell
curl -X 'POST' 'http://192.168.3.100:1780/api/start' \
  -H 'X-API-KEY: b1f8e72d-9c34-4a2e-a5f1-3d57b2a1c7f8' \
  -H 'path: C:\Program Files\Proton\VPN\v3.4.3\ProtonVPN.exe' \
  -d ''
```

At startup, a check is made of the passed parameter, the availability of the path, and that the process has been started.

### Process stop

```shell
curl -X 'POST' 'http://192.168.3.100:1780/api/stop' \
  -H 'X-API-KEY: b1f8e72d-9c34-4a2e-a5f1-3d57b2a1c7f8' \
  -H 'name: ProtonVPN' \
  -H 'wildcard: true' \
  -d ''
```

The `wildcard` parameter is used to stop all processes based on a partial match in the passed name.

## Backlog

Currently, this functionality covers my needs, but there are several things that may be implemented in the future.

- [ ] Universal search for a process by its name in the system
- [ ] Ping status in the system tray
- [ ] Get a list of processes and services with running status information
- [ ] Service management
- [ ] New endpoints for obtaining system information (system metrics)

## Alternatives

[WinAPI](https://github.com/Lifailon/WinAPI) - `REST API` and Web server (frontend) based on `.NET HttpListener` and backend `PowerShell Core` for Windows remote managment via Web browser or `curl` from Linux.
