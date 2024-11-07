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
    <strong>English</strong> | <a href="README_RU.md">Русский</a>
</h4>

A universal tool for automatic (local) and remote VPN connection management via a desktop application (system tray) and `API`.

## For what

I'm tired of the [Hotspot Shield](https://www.hotspotshield.com/vpn/vpn-for-windows) application periodically disconnecting the VPN connection, most often this happens due to a prolonged absence of an Internet connection. In this case, even if the automatic reconnection setting is enabled, the connection may simply not be established again, or this may be regarded by the application as a manual disconnection of the connection.

This application performs exactly three functions - it terminates the process by name and starts the process along the path specified in the configuration file, and also collects statistics for a reliable check of the VPN connection and the availability of the Internet.

This approach is universal, so it can and will work with any VPN application, the only condition for the operability of this method is the ability to automatically connect to the VPN network when the application is launched (supported by most clients).

## Functionality

- The `API` interface, which allows you to configure remote management of the VPN connection on the target host. For example, in my network this is a dedicated machine, where access to specific content from other machines is carried out by means of Proxy, this is convenient so as not to limit all traffic to the Internet to a VPN connection and at the same time not to be limited to separate tunneling to the VPN network. The ability to remotely disable VPN can be useful when loading a large amount of traffic on the target machine, and also allows you to remotely control the connection, for example, via a Telegram bot.

> 📢 This functionality will be implemented in the [Kinozal-Bot](https://github.com/Lifailon/Kinozal-Bot) project in version `0.4.7`.

- Monitoring the availability of the Internet connection. The check is performed every 5 seconds (by default, can be changed in the configuration file), if the connection is unavailable, a system notification will be sent (and the status of the application icon will also change) and the check will be performed every two seconds until the connection is stable (3 successful `icmp` requests in a row with an interval of 2 seconds), after which a second notification will be sent.

- Monitoring the availability and automatic restoration of the VPN connection. If the Internet is available and the VPN process is running (excluding manual termination), but the VPN network interface is in the offline state, the application will be automatically restarted to reconnect to the VPN network until the connection is restored.

## Installation and build

You can download a portable version of the application from the [GitHub repository](https://github.com/Lifailon/vpnc/releases/latest).

To build the application, clone the repository and install dependencies:

```shell
git clone https://github.com/Lifailon/vpnc
cd vpnc
dotnet restore
dotnet build
dotnet publish
```

To exclude installations of `.NET Runtime` on the target system, include it in the build:

```shell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

Packages used:

```shell
dotnet add package Newtonsoft.Json
dotnet add package Microsoft.AspNetCore.App
dotnet add package Swashbuckle.AspNetCore
```

### Usage

Run the application in command line mode:

```shell
dotnet run [start|stop|restart|status|api|tray|process]
```

The `process` parameter is used to launch the application in the background process mode for control via the system tray (similar to `vpnc.exe tray`).

To launch the program at system boot, go to the startup directory (`Win+R` - `shell:startup`):

```
C:\Users\<UserName>\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
```

Create a shortcut in the root of the `Startup` directory with the following content:

```
"C:\Users\<UserName>\Documents\vpnc\vpnc.exe" process
```

Replace the path to the executable file with your own.

## Configuration

For quick access to the configuration, use the **Open Configuration** button in the context menu.

![interface](/img/interface.jpg)

The configuration is located in the `vpnc.json` file, next to the executable file.

```json
{
    "ProcessName": "hsscp",
    "ProcessPath": "C:\\Program Files (x86)\\Hotspot Shield\\12.9.3\\bin\\hsscp.exe",
    "InterfaceName": "HotspotShield Network Adapter",
    "PingHost": "8.8.8.8",
    "ApiPort": 1780,
    "PingStartup": true,
    "VpnStartup": true,
    "ApiStartup": false,
    "PingTimeout": 5,
    "VpnTimeout": 5
}
```

- `ProcessName` - The name of the process to be terminated. Most often, this name corresponds to the executable file, it can also be determined in the Windows Task Manager.
- `ProcessPath` - The full path to the executable file that is responsible for starting the VPN client.
- `InterfaceName` - The name of the virtual VPN interface (created automatically when installing the VPN client). To determine the interface name, you can use the classic `ipconfig` command.
- `PingHost` - The address that will act as a node for checking the availability of the Internet connection.
- `ApiPort` - The port on which the `Web/REST API` interface will be launched.
- `PingStartup/VpnStartup/ApiStartup` - Defines the initial state when starting the interface. Takes a Boolean value of `true` or `false`.
- `PingTimeout/VpnTimeout` - The frequency of automatic checks in seconds.

## API

The `API` interface can be launched in command line mode (cli), or from the context menu of the application in the tray. The parameter for automatically enabling `API` when starting the application is determined by the `ApiStartup` parameter in the configuration file.

**Swagger** documentation is available via the `Swashbuckle.AspNetCore` library at: http://localhost:1780/swagger

An example request to get internet status: `curl -s http://192.168.3.100:1780/api/status | jq`

```json
{
  "processName": "hsscp",
  "processStatus": "Running",
  "processUptime": "0.2:14:45",
  "systemUptime": "4.5:46:14",
  "interfaceName": "HotspotShield Network Adapter",
  "interfaceStatus": "Up",
  "pingAddress": "yandex.ru",
  "pingStatus": "Connected",
  "pingTimeout": "239 ms",
  "country": "US",
  "timeZone": "America/New_York",
  "region": "New York",
  "city": "New York City",
  "externalIp": "45.56.XXX.XXX"
}
```

Endpoints to stop (`/api/stop`) and start (`/api/start`) the VPN application are also available.
