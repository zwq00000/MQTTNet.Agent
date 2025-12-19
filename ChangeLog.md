# 变更日志

## [1.0.7] - 2025-12-19

### 🔄 兼容性更新
- **MQTTnet 5.0 兼容性**: 升级以支持 MQTTnet 5.0.* 版本的 API 变更
- **CleanSession 设置**: 修复 `MqttConnectionOptions` 中 `CleanSession` 参数的设置方式，使用 `WithCleanSession(bool)` 方法而不是仅在有值时调用
- **RemoteEndpoint 访问**: 更新 TCP 连接选项中的端点访问方式，处理 MQTTnet 5.0 中的 "Unspecified/localhost" 格式

### 🔧 核心功能改进
- **AutoReConnectedClient 增强**:
  - 添加内部构造函数 `AutoReConnectedClient(IMqttClient, ILogger)` 用于测试支持
  - 改进事件处理器设置，确保连接和断开事件的正确订阅
  - 优化订阅恢复机制，提高重连后的订阅恢复可靠性

- **MqttConnectionOptions 优化**:
  - 修复 URI 查询参数解析（如 keepalive、clean 参数）
  - 改进凭据处理，支持用户名和密码的字节数组格式
  - 增强对不同 MQTT 协议方案（mqtt、mqtts、ws、wss）的支持

### 🏗️ 架构改进
- **依赖注入增强**:
  - 修复 `ServiceExtensions.AddMessageAgent()` 的服务依赖问题
  - 确保在注册消息代理服务前正确注册 MQTT 客户端服务
  - 改进服务生命周期管理

- **内部访问控制**:
  - 添加 `InternalsVisibleTo` 属性支持 `DynamicProxyGenAssembly2`
  - 改进测试框架对内部类型的访问能力

### 🛠️ 构建和配置
- **多目标框架**: 支持 .NET 8.0 和 .NET 9.0
- **编译优化**: 消除所有编译警告，提高代码质量
- **依赖更新**: 更新到最新稳定版本的依赖包

### 🔒 安全性
- **凭据处理**: 改进密码和敏感信息的处理方式
- **连接验证**: 增强连接参数的验证机制

---

## [1.0.6] - 2024-12-19

### 🚀 初始发布
- **核心功能**: MQTT 消息代理实现
- **自动重连**: 支持网络中断后的自动重连和订阅恢复
- **多协议支持**: 支持 MQTT、MQTTS、WebSocket、Secure WebSocket
- **依赖注入**: 完整的 .NET 依赖注入支持
- **序列化**: 内置 JSON 和二进制序列化支持