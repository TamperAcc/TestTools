# 串口工具程序架构文档

## ?? 当前功能清单

### 1. 核心功能
- **串口通信管理**
  - 串口连接/断开（异步非阻塞）
  - 串口配置（波特率115200, 8N1）
  - 数据接收事件处理
  - UTF-8编码支持

- **设备控制**
  - ON/OFF 命令发送
  - 电源状态跟踪和可视化
  - 按钮状态联动更新

### 2. 配置管理
- **持久化配置**
  - 串口选择记忆（`serialport.config`）
  - 串口锁定状态（`portlock.config`）
  - 设备名称配置（`devicename.config`）

### 3. 用户界面
- **状态显示**
  - 颜色编码状态标签
    - ?? 绿色：已连接
    - ? 深灰：未连接
  - ?? 橙色：警告/连接中
    - ?? 红色：错误

- **控制按钮**
  - 连接/断开按钮
  - ON/OFF 电源控制按钮
  - 设置菜单

- **UI 增强**
  - 自定义窗口边框拖动（10px感应区域）
  - 字体缓存（粗体/常规）

---

## ??? 当前架构问题

### ? 问题列表

1. **单一职责违反**
   - Form1 承担了太多职责：UI、串口管理、配置管理、状态管理

2. **代码重复**
   - 状态设置代码仍有重复
   - 配置文件读写逻辑重复

3. **硬编码问题**
- 波特率、编码、命令等硬编码在代码中
   - 颜色值分散在各处

4. **可测试性差**
   - 业务逻辑与UI紧耦合
   - 无法进行单元测试

5. **扩展性差**
   - 添加新设备类型困难
- 添加新命令需要修改多处代码

---

## ?? 重构目标

### 设计原则
- **SOLID 原则**
  - 单一职责原则（SRP）
  - 开闭原则（OCP）
  - 依赖倒置原则（DIP）

- **分层架构**
  - 表示层（UI）
  - 业务逻辑层
  - 数据访问层
  - 基础设施层

---

## ?? 新架构设计

### 层次结构

```
WinFormsApp3/
├── Presentation/      # 表示层
│   ├── Forms/
│   │   ├── MainForm.cs    # 主窗体（简化）
│   │   └── SettingsForm.cs          # 设置窗体
│   └── ViewModels/    # 视图模型（可选MVVM）
│
├── Business/         # 业务逻辑层
│   ├── Services/
│   │   ├── ISerialPortService.cs    # 串口服务接口
│   │   ├── SerialPortService.cs     # 串口服务实现
│   │   ├── IDeviceController.cs     # 设备控制接口
│   │   └── PowerDeviceController.cs # 电源设备控制器
│   ├── Models/
│   │   ├── DeviceStatus.cs          # 设备状态模型
│   │   ├── ConnectionConfig.cs      # 连接配置模型
│   │   └── PowerCommand.cs          # 命令模型
│   └── Enums/
│       ├── ConnectionState.cs    # 连接状态枚举
│       └── PowerState.cs            # 电源状态枚举
│
├── Data/       # 数据访问层
│ ├── IConfigRepository.cs         # 配置仓储接口
│   ├── FileConfigRepository.cs      # 文件配置仓储
│   └── Models/
│       └── AppConfig.cs  # 应用配置模型
│
├── Infrastructure/  # 基础设施层
│   ├── Constants/
│   │   ├── AppConstants.cs          # 应用常量
│   │   └── UIConstants.cs           # UI常量
│   ├── Helpers/
│   │   ├── UIHelper.cs    # UI辅助类
│   │   └── SerialPortHelper.cs      # 串口辅助类
│   └── Extensions/
│    └── ControlExtensions.cs     # 控件扩展方法
│
└── Core/          # 核心层
    ├── Interfaces/
    │   └── IDisposableService.cs    # 可释放服务接口
    └── Events/
        └── DeviceEventArgs.cs       # 设备事件参数
```

---

## ?? 核心组件设计

### 1. 串口服务（SerialPortService）
**职责**：管理串口连接和通信

```csharp
public interface ISerialPortService : IDisposable
{
    event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
event EventHandler<DataReceivedEventArgs> DataReceived;
    
    Task<bool> ConnectAsync(ConnectionConfig config);
  Task DisconnectAsync();
    Task<bool> SendCommandAsync(string command);
    
    bool IsConnected { get; }
    ConnectionConfig CurrentConfig { get; }
}
```

### 2. 设备控制器（PowerDeviceController）
**职责**：管理设备状态和命令

```csharp
public interface IDeviceController
{
    event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;
    
    Task<bool> TurnOnAsync();
    Task<bool> TurnOffAsync();
    
 DeviceStatus CurrentStatus { get; }
    string DeviceName { get; set; }
}
```

### 3. 配置仓储（IConfigRepository）
**职责**：配置的持久化

```csharp
public interface IConfigRepository
{
    Task<AppConfig> LoadAsync();
    Task SaveAsync(AppConfig config);
    
    Task<T> GetValue<T>(string key, T defaultValue);
    Task SetValue<T>(string key, T value);
}
```

---

## ?? 数据流

```
用户操作
    ↓
MainForm (UI层)
    ↓
DeviceController (业务层)
    ↓
SerialPortService (业务层)
    ↓
SerialPort (基础设施)
 ↓
硬件设备
```

---

## ?? UI状态管理

### 状态机设计

```
States:
- Disconnected (断开)
- Connecting (连接中)
- Connected (已连接)
- Error (错误)

Transitions:
Disconnected --[Connect]--> Connecting
Connecting --[Success]--> Connected
Connecting --[Failure]--> Error
Connected --[Disconnect]--> Disconnected
Error --[Retry]--> Connecting
```

---

## ?? 配置模型

```csharp
public class AppConfig
{
    public string SelectedPort { get; set; }
public bool IsPortLocked { get; set; }
    public string DeviceName { get; set; }
    public ConnectionSettings Connection { get; set; }
}

public class ConnectionSettings
{
    public int BaudRate { get; set; } = 115200;
    public Parity Parity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
    public Encoding Encoding { get; set; } = Encoding.UTF8;
}
```

---

## ?? 扩展点

### 1. 新设备类型
- 实现 `IDeviceController` 接口
- 添加新的命令集

### 2. 新通信协议
- 实现 `ISerialPortService` 接口
- 支持TCP/UDP/蓝牙等

### 3. 新配置存储
- 实现 `IConfigRepository` 接口
- 支持数据库/云存储等

### 4. 插件系统
- 定义插件接口
- 动态加载设备控制器

---

## ?? 性能优化

### 已实现
- ? 异步串口连接（非阻塞UI）
- ? Font对象缓存
- ? 资源正确释放

### 计划中
- ? 连接超时控制
- ? 命令队列管理
- ? 数据缓冲优化
- ? 日志系统集成

---

## ?? 测试策略

### 单元测试
- SerialPortService
- DeviceController
- ConfigRepository

### 集成测试
- UI与业务层交互
- 配置持久化

### E2E测试
- 完整的用户操作流程

---

## ?? 重构步骤

### Phase 1: 提取业务逻辑
1. ? 创建接口定义
2. ? 提取SerialPortService
3. ? 提取DeviceController
4. ? 提取ConfigRepository

### Phase 2: 重构UI层
1. ? 简化MainForm
2. ? 实现依赖注入
3. ? 事件驱动更新

### Phase 3: 添加高级功能
1. ? 日志系统
2. ? 错误处理中间件
3. ? 命令历史记录
4. ? 自动重连机制

---

## ?? 最佳实践

### 代码规范
- 使用异步/等待模式
- 实现IDisposable接口
- 遵循命名约定
- 添加XML文档注释

### 设计模式
- **Repository Pattern**: 配置管理
- **Service Pattern**: 业务逻辑
- **Observer Pattern**: 事件通知
- **Strategy Pattern**: 不同设备类型
- **Factory Pattern**: 对象创建

---

## ?? 技术栈

- **.NET 8.0**
- **C# 12.0**
- **Windows Forms**
- **System.IO.Ports**
- **Async/Await**

---

## ?? 依赖关系

```
Presentation → Business → Data
     ↓   ↓          ↓
     └─────> Infrastructure
```

**原则**: 高层不依赖低层的具体实现，都依赖于抽象

---

## ?? 总结

当前程序是一个功能完善的串口通信工具，主要用于控制电源设备。

通过重构，我们将：
- ? 提高代码可维护性
- ? 增强可测试性
- ? 提升扩展性
- ? 降低耦合度
- ? 改善代码质量

重构后的架构将支持：
- 多设备类型
- 多通信协议
- 插件扩展
- 单元测试
- 更好的错误处理
