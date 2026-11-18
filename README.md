<div align="center">

# Ubiquiti.AirOs

A .NET library for managing Ubiquiti devices running airOS firmware (versions 6 and 8) over SSH.

**[English](README.md)** · **[Русский](README.ru.md)**

[![NuGet](https://img.shields.io/nuget/v/Ubiquiti.AirOs?style=flat-square&logo=nuget&label=Ubiquiti.AirOs)](https://www.nuget.org/packages/Ubiquiti.AirOs)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Ubiquiti.AirOsy?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/Ubiquiti.AirOs)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

</div>

### Installation

Add a reference to the `Ubiquiti.AirOs` project (targets .NET 10).

### Creating a Manager

**Without dependency injection:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Ubiquiti.AirOs;
using Ubiquiti.AirOs.Abstractions;
using Ubiquiti.AirOs.Extensions;




var services = new ServiceCollection().AddAirOs();
await using var provider = services.BuildServiceProvider();

var factory = provider.GetRequiredService<IAirOsManagerFactory>();
IAirOsManager manager = factory.Create(AirOsVersion.AirOs8);
```

**With dependency injection:** call `AddAirOs()` on your `IServiceCollection` and inject `IAirOsManagerFactory`.

The factory creates the appropriate manager (`AirOs6Manager` or `AirOs8Manager`) for the requested version. You can also instantiate a manager directly: `new AirOs8Manager(ipValidator, passwordHasher)`.

### Connecting

```csharp
await manager.ConnectAsync("192.168.1.20", username: "ubnt", password: "ubnt", port: 22);
```

### Reading Configuration

```csharp
string? ssid = await manager.GetSsidAsync();
string? ip = await manager.GetStaticIpAsync();
WirelessMode? mode = await manager.GetWirelessModeAsync();
int? channelWidth = await manager.GetChannelWidthAsync();
bool? dhcpEnabled = await manager.GetDhcpAsync();
FirmwareVersionInfo? fw = await manager.GetFirmwareVersionAsync();
AirOsRadioStatus? status = await manager.GetRadioStatusAsync();
IReadOnlyList<AirOsStation> stations = await manager.GetStationsAsync();
string? log = await manager.GetSystemLogAsync(lines: 200);
```

### Changing Configuration (Transactional Model)

`Set*` methods only **enqueue** commands — nothing is written to the device until you apply them.

```csharp
manager
    .SetSsid("MyNetwork", WirelessMode.AccessPoint)
    .SetChannel(40)
    .SetTxPower(20);

bool prepared = await manager.ChangeAsync();       // stage changes in /tmp/system.cfg (with backup)
bool applied  = await manager.ApplyChangesAsync(); // save to flash and soft-restart
```

- If network parameters were changed, the SSH connection may drop during `ApplyChangesAsync`.
- To roll back staged (but not yet applied) changes, use `await manager.DiscardAsync()`.
- To cancel queued commands without touching the device, use `manager.ClearCommands()`.
- The number of unsaved commands is available via `manager.PendingCommandsCount`.

### Convenience Extensions

```csharp
using Ubiquiti.AirOs.Extensions;

manager.SetAccessPoint(ssid: "MyAP", password: "secret123", channelWidth: 40, txPower: 20);
manager.SetStation(ssid: "MyAP", password: "secret123", channelWidth: 40, txPower: 20);
manager.SetStaticNetwork("192.168.1.20", "255.255.255.0", "192.168.1.1");
manager.EnableDhcp("169.254.100.100");
manager.EnableWatchdog("8.8.8.8", delay: 300, period: 300, retry: 3);
manager.DisableWatchdog();
```

Remember to call `ChangeAsync()` and then `ApplyChangesAsync()` to make any of these changes persistent.

### Firmware Upgrade

```csharp
byte[] firmware = await File.ReadAllBytesAsync("firmware.bin");
bool ok = await manager.UpgradeFirmwareAsync(firmware);
```

### Device Maintenance

```csharp
await manager.RebootAsync();          // reboot without applying pending changes
await manager.ResetToFactoryAsync();  // factory reset (wipes all configuration)
```

### Raw Commands

```csharp
CommandResult result = await manager.ExecuteBatchAsync(["uptime", "cat /tmp/system.cfg"]);
Console.WriteLine(result.Output);
```

Dispose the manager (`await using` / `DisposeAsync()`) to close the SSH connection.
