<div align="center">

# Ubiquiti.AirOs

.NET-библиотека для управления устройствами Ubiquiti с прошивкой airOS (версии 6 и 8) по SSH.

**[English](README.md)** · **[Русский](README.ru.md)**

[![NuGet](https://img.shields.io/nuget/v/Ubiquiti.AirOs?style=flat-square&logo=nuget&label=Ubiquiti.AirOs)](https://www.nuget.org/packages/Ubiquiti.AirOs)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Ubiquiti.AirOsy?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/Ubiquiti.AirOs)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

</div>

### Установка

Добавьте ссылку на проект `Ubiquiti.AirOs` (целевая платформа — .NET 10).

### Создание менеджера

**Без внедрения зависимостей:**

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

**С внедрением зависимостей:** вызовите `AddAirOs()` для вашего `IServiceCollection` и внедрите `IAirOsManagerFactory`.

Фабрика создаёт подходящий менеджер (`AirOs6Manager` или `AirOs8Manager`) для запрошенной версии. Менеджер можно создать и напрямую: `new AirOs8Manager(ipValidator, passwordHasher)`.

### Подключение

```csharp
await manager.ConnectAsync("192.168.1.20", username: "ubnt", password: "ubnt", port: 22);
```

### Чтение конфигурации

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

### Изменение конфигурации (транзакционная модель)

Методы `Set*` только **добавляют команды в очередь** — ничего не записывается на устройство, пока изменения не применены.

```csharp
manager
    .SetSsid("MyNetwork", WirelessMode.AccessPoint)
    .SetChannel(40)
    .SetTxPower(20);

bool prepared = await manager.ChangeAsync();       // подготовить изменения в /tmp/system.cfg (с резервной копией)
bool applied  = await manager.ApplyChangesAsync(); // сохранить во flash и выполнить мягкую перезагрузку
```

- Если были изменены сетевые параметры, при `ApplyChangesAsync` SSH-соединение может разорваться.
- Для отката подготовленных (но ещё не применённых) изменений используйте `await manager.DiscardAsync()`.
- Для очистки очереди команд без воздействия на устройство используйте `manager.ClearCommands()`.
- Количество неприменённых команд доступно через `manager.PendingCommandsCount`.

### Удобные методы расширения

```csharp
using Ubiquiti.AirOs.Extensions;

manager.SetAccessPoint(ssid: "MyAP", password: "secret123", channelWidth: 40, txPower: 20);
manager.SetStation(ssid: "MyAP", password: "secret123", channelWidth: 40, txPower: 20);
manager.SetStaticNetwork("192.168.1.20", "255.255.255.0", "192.168.1.1");
manager.EnableDhcp("169.254.100.100");
manager.EnableWatchdog("8.8.8.8", delay: 300, period: 300, retry: 3);
manager.DisableWatchdog();
```

Не забудьте вызвать `ChangeAsync()`, а затем `ApplyChangesAsync()`, чтобы любые эти изменения сохранились.

### Обновление прошивки

```csharp
byte[] firmware = await File.ReadAllBytesAsync("firmware.bin");
bool ok = await manager.UpgradeFirmwareAsync(firmware);
```

### Обслуживание устройства

```csharp
await manager.RebootAsync();          // перезагрузка без применения ожидающих изменений
await manager.ResetToFactoryAsync();  // сброс к заводским настройкам (стирает всю конфигурацию)
```

### Выполнение произвольных команд

```csharp
CommandResult result = await manager.ExecuteBatchAsync(["uptime", "cat /tmp/system.cfg"]);
Console.WriteLine(result.Output);
```

Удаляйте менеджер (`await using` / `DisposeAsync()`), чтобы закрыть SSH-соединение.