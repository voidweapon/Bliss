# C# 异常处理规则（Agent 编码约束）

> **适用**：本仓库所有 C# / .NET 代码的编写与修改。
> **性质**：MUST / SHOULD / NEVER 为硬性约束；违反 MUST 或 NEVER 视为交付不合格。
> **例外**：确需偏离时，在代码注释中说明原因，例如 `// exception-rule: R4 — 原因`，并由审查者确认。
> **依据**：[.NET 最佳异常实践](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)。

## 0. 总则

- **R0.1** 异常用于真正异常的情况，不用于正常控制流。
- **R0.2** 只在能恢复、或能补充有价值信息时捕获；否则向上传播。
- **R0.3** 抛出异常时必须留下能定位问题的证据：准确类型、有信息量的消息和完整栈。
- **R0.4** 修改既有代码时，不扩大异常处理范围；不做与需求无关的 `catch` 重构。
- **R0.5** 未处理异常的崩溃通常比继续运行在未定义状态更可靠、可诊断。

## 1. 捕获（Catch）

**R1 · NEVER** 空捕获或静默吞掉异常。

```csharp
// 禁止
catch { }
catch (Exception) { }
catch (Exception ex) { /* 什么也不做 */ }

// 至少执行明确降级、记录或重新抛出
catch (IOException ex)
{
    _logger.LogError(ex, "读取 {Path} 失败，返回空结果", path);
    return Array.Empty<byte>();
}
```

**R2 · MUST** 只捕获能够处理的具体异常。无法恢复时让异常传播。

**R3 · MUST** `catch` 子句按“最派生异常到最不派生异常”排列。基类异常放在派生异常之前会使派生分支无法执行。

**R4 · MUST** 捕获后重新抛出时保留原始栈。凡出现 `throw ex;` 一律视为缺陷。

**R5 · SHOULD** 资源释放优先使用 `using` / `await using`；不实现 `IDisposable` 的资源清理优先放入 `finally`。

**R5.1 · SHOULD** 可恢复时使用 `try`/`catch`；不可恢复时允许调用栈中合适的层级处理。

**R5.2 · SHOULD** 对经常发生且属于正常执行的条件进行预检查；真正罕见且表示错误的条件使用异常处理。

**R5.3 · MUST** 预检查存在竞态时仍可能抛出异常，不能把检查成功等同于不存在异常或无事务边界。

## 2. 优先避免异常

**R6 · SHOULD** 优先使用状态检查、`TryParse`、`TryGetValue` 等 API 或替代方法，避免可预见异常。

```csharp
// 高频、可预期的解析失败使用 Try* API
if (!int.TryParse(input, out int value))
{
    return false;
}

// 状态通常不允许关闭时，先检查状态
if (connection.State != ConnectionState.Closed)
{
    connection.Close();
}
```

**R6.1 · SHOULD** 常见错误场景可按 API 约定返回 `null`、`false`、`default` 或等价结果，但不得隐藏由调用方无法解释的状态。

**R6.2 · SHOULD** 对值类型先判断 `Nullable<T>` 或 `default` 是否能清晰表达“存在/不存在”，避免引入不必要的额外分支。

## 3. 重抛（Rethrow）

**R7 · MUST** 在捕获异常的同一个 `catch` 块内重抛时使用不带参数的 `throw;`。

```csharp
// 栈被重置到当前方法
catch (Exception ex)
{
    _logger.LogError(ex, "处理失败");
    throw ex;
}

// 保留原始栈
catch (Exception ex)
{
    _logger.LogError(ex, "处理失败");
    throw;
}
```

**R8 · MUST** 在原始 `catch` 块之外重抛时使用 `ExceptionDispatchInfo`。

```csharp
ExceptionDispatchInfo? captured = null;
try
{
    DoWork();
}
catch (Exception ex)
{
    captured = ExceptionDispatchInfo.Capture(ex);
}

captured?.Throw();
```

**R8.1 · NEVER** 用 `throw captured.SourceException;` 替代 `captured.Throw()`。

**R8.2 · MUST** 同一场景二选一：原样重抛或包装补充上下文，不叠加造成重复或语义歧义。

**R9 · MUST** 改变异常语义或补充业务信息时包装异常并保留原异常作为 `InnerException`。

```csharp
catch (Exception ex)
{
    throw new PaymentFailedException(
        $"订单 {orderId} 支付失败。",
        innerException: ex);
}
```

不改变调用方异常类型和语义时使用 R7 或 R8；需要领域异常或业务上下文时使用 R9。

## 4. 抛出（Throw）

**R10 · MUST** 优先使用预定义异常类型。

| 场景 | 类型 |
| --- | --- |
| 对象状态不允许该操作 | `InvalidOperationException` |
| 参数为 `null` | `ArgumentNullException` |
| 参数值越界 | `ArgumentOutOfRangeException` |
| 参数格式或内容非法 | `ArgumentException` |
| 对象已释放 | `ObjectDisposedException` |
| 操作不受支持 | `NotSupportedException` |
| 资源不存在 | `FileNotFoundException` / `KeyNotFoundException` |

**R11 · NEVER** 主动抛出运行时保留类型：`NullReferenceException`、`IndexOutOfRangeException`、`AccessViolationException`、`StackOverflowException`。

**R12 · NEVER** 使用 `throw new Exception(...)` 作为基类兜底。

**R13 · SHOULD** 优先使用框架提供的 Throw 辅助方法。

```csharp
// 手写可用但重复
if (path is null)
{
    throw new ArgumentNullException(nameof(path));
}

// 优先
ArgumentException.ThrowIfNullOrEmpty(path);
```

可用辅助方法包括 `ArgumentNullException.ThrowIfNull`、`ArgumentException.ThrowIfNullOrEmpty`、`ArgumentException.ThrowIfNullOrWhiteSpace`、`ArgumentOutOfRangeException` 系列的范围检查、`ObjectDisposedException.ThrowIf` 和 `CancellationToken.ThrowIfCancellationRequested`。

**R14 · MUST** 异常消息完整描述问题，以句号结尾，不包含密钥、令牌、个人数据或完整连接串。本地化应用从资源文件提供面向用户的文案。

**R15 · MUST** `throw` 语句位于能提供有意义栈帧的位置，不隐藏在仅做参数转发的私有辅助方法中。

**R16 · NEVER** 在 `finally` 块中抛出异常。

**R16.1 · NEVER** 在资源清理或回滚成功后覆盖仍应传播的原始异常。

**R17 · NEVER** 从 `Equals`、`GetHashCode`、`ToString`、静态构造函数或相等运算符中抛出异常。

**R18 · SHOULD** 同一异常在多处抛出时抽取异常工厂或领域异常创建方法，保持消息和上下文一致。

## 5. 异步与并发

**R19 · MUST** task-returning 方法在第一个 `await` 之前同步验证参数，并同步抛出参数异常。

```csharp
// 错误：调用方无法在同步调用阶段捕获参数异常
public async Task<string> ReadAsync(string path)
{
    var text = await File.ReadAllTextAsync(path);
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    return text;
}

// 正确
public async Task<string> ReadAsync(string path)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    return await File.ReadAllTextAsync(path);
}
```

**R20 · MUST** 捕获可取消异步操作的基类 `OperationCanceledException`，而非只捕获派生的 `TaskCanceledException`。

**R21 · MUST** 已确认的请求取消按取消语义处理，不记为 Error，也不转换成业务失败。需要继续传播时使用 `throw;`；明确负责停机的边界可以完成约定的停机流程。未确认原因的取消异常应继续传播。

**R22 · SHOULD** 异步任务或 `Task.WhenAll` 中保留每个子任务异常的诊断栈；聚合或转换异常时保留原始异常、内部异常或适用的 `AggregateException`。

### 协作式取消

取消是请求，不是强制终止：操作必须自行观察取消信号或将令牌传给支持取消的操作。

**R22.1 · MUST** 接收 `CancellationToken` 的方法必须实际使用它：在适当的安全点调用 `ThrowIfCancellationRequested()`，或传给下层可取消 API。长时间执行的循环应在适当间隔检查，而不是只在开始时检查。

**R22.2 · MUST** 在调用链中向支持取消的下层 API 传递适用令牌；需要组合停机和超时时使用链接令牌，不要无意丢弃调用方的取消请求。

```csharp
using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
timeout.CancelAfter(TimeSpan.FromSeconds(5));
await service.ProcessAsync(items, timeout.Token);
```

**R22.3 · NEVER** 捕获取消异常后静默返回正常成功结果。向调用方传播取消，或在明确负责停机的边界按约定退出。

**R22.4 · MUST** 当外部停机与单次超时需要不同行为时，使用 `when` 过滤器检查异常关联的令牌及相应取消源，区分取消原因；未能确认原因时传播异常。超时是否重试、记录或返回失败取决于调用方契约，不得一律当作停机或成功。

```csharp
catch (OperationCanceledException ex) when (ex.CancellationToken == timeout.Token && stoppingToken.IsCancellationRequested)
{
    break; // 当前循环负责处理停机
}
catch (OperationCanceledException ex) when (ex.CancellationToken == timeout.Token && timeout.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
{
    _logger.LogWarning("本轮超时，继续下一轮");
}
```

上述示例仅适用于被调用方以链接令牌报告取消、且契约允许超时后继续下一轮的循环。检查到多个令牌同时取消时优先处理停机；并发状态可能在过滤器检查后变化，循环仍须再次检查停机信号。若被调用方报告其他令牌或不提供令牌，按其契约识别原因，无法确认时传播异常。

**R22.5 · MUST** 由创建者负责释放 `CancellationTokenSource`；使用 `using` 或明确的生命周期管理，并确保消费者不再访问它、回调执行完毕后再释放。不要在相关操作仍在使用时重置或复用取消源；重试通常创建新的取消源。

**R22.6 · SHOULD** 使用 `CancelAfter` 或 `CreateLinkedTokenSource` 实现超时及组合取消源，避免手写计时器和布尔标志。

**R22.7 · NEVER** 使用 `Thread.Abort`、`Task.Dispose` 或仅依赖 `Task.Run(..., token)` 强制终止已运行任务。传给 `Task.Run` 的令牌本身不会中断已启动的委托；委托必须自行观察或向下传递令牌。

## 6. 状态与副作用

**R23 · MUST** 方法抛出异常时不得留下部分生效的副作用。使用事务原子化或显式补偿。

```csharp
string? withdrawalTransactionId = from.Withdrawal(amount);
try
{
    to.Deposit(amount);
}
catch
{
    from.RollbackTransaction(withdrawalTransactionId);
    throw;
}
```

**R24 · MUST** 回滚失败时保留原业务异常和回滚失败，不得静默丢失其中一项。优先包装或聚合，并保留原始异常与恢复证据。

**R24.1 · MUST** 不修改公开可观察状态后才发现参数无效；先完成同步参数和状态验证。

**R24.2 · SHOULD** 长事务或跨资源修改优先使用现有事务、幂等或补偿机制，不自行设计隐藏副作用协议。

## 7. 自定义异常类型

**R25 · MUST** 自定义异常的类名以 `Exception` 结尾，并直接或间接继承 `System.Exception`。

**R26 · MUST** 至少提供三个构造函数：无参、`(string message)` 和 `(string message, Exception innerException)`。

**R27 · SHOULD** 只在调用方确有编程用途时增加附加属性，例如调用方据此分支或补偿；纯日志美化用途不增加。

**R28 · SHOULD** 只在跨进程、持久化或框架要求序列化时实现异常序列化，并按目标框架和安全约束提供相应构造函数及序列化支持。

**R28.1 · MUST** 自定义异常在公开 API 文档中说明何时抛出、包含何种上下文以及调用方如何恢复。

## 8. 资源释放

**R29 · SHOULD** 优先使用 `using` 或 `await using` 释放实现了 `IDisposable` 或 `IAsyncDisposable` 的资源。

**R30 · SHOULD** 不实现释放接口的资源清理由 `finally` 完成。

**R31 · MUST** 清理失败不得静默覆盖主异常；记录、包装或聚合后保留两项诊断证据。

**R32 · MUST** 不在资源可能泄漏的路径中通过提前返回绕过清理。

## 9. 公共 API 和文档

**R33 · MUST** 新增或修改公共类、方法、属性和事件时，在 XML 文档中说明用途、参数约束、返回值以及调用方可预期处理的异常。

**R34 · SHOULD** 明确参数验证异常、对象状态异常和领域异常；不承诺捕获内部依赖抛出的所有异常。

**R35 · MUST** 回滚、重试、幂等和取消语义在注释、公开契约和回归测试中保持一致。

## 10. 异常日志和敏感信息

**R36 · MUST** 日志保留异常类型、技术上下文和必要定位证据，不记录密码、令牌、密钥、完整连接串或个人敏感数据。

**R37 · SHOULD** 日志和异常消息分别承担定位与用户提示职责；面向用户的文案只显示调用方可处理且安全的信息。

## 11. 反模式速查

| 反模式 | 违反 | 正确做法 |
| --- | --- | --- |
| `catch { }` | R1 | 记录并明确降级，或删除 `catch` |
| `catch (Exception ex) { throw ex; }` | R4/R7 | `throw;` |
| `catch (Exception) { throw; }` | R2 | 无信息增益时删除整个 `catch` |
| `throw new Exception("出错了")` | R12 | 使用具体类型或领域异常 |
| `catch (Exception) { return null; }` | R1 | 明确失败语义或传播 |
| 先 `await` 再校验参数 | R19 | 校验放到首个 `await` 之前 |
| 把 `OperationCanceledException` 记为 Error | R21 | 单独传播或完成取消语义 |
| `finally { throw ...; }` | R16 | 移除异常抛出或保留原异常 |
| 用异常控制 `int.Parse` 正常流程 | R10/R13 | 使用 `int.TryParse` |
| 自定义异常只提供消息构造函数 | R26 | 补全三个构造函数 |
| 接收令牌后不检查也不传递 | R22.1/R22.2 | 在安全点检查，或传给下层可取消操作 |
| 捕获取消后静默返回默认值 | R22.3 | 传播取消或按约定完成停机 |
| 混淆停机与单次超时 | R22.4 | 按令牌状态区分；原因不明时传播 |
| 用 `Thread.Abort`、`Task.Dispose` 强制取消任务 | R22.7 | 使用协作式取消 |

## 12. 提交前自检

Agent 在提交或报告完成异常相关改动前逐项确认：

1. 无空捕获或静默吞掉异常。
2. 无 `throw ex;` 或 `throw captured.SourceException;`。
3. 无 `throw new Exception(...)` 基类兜底。
4. 所有 task-returning 方法在首个 `await` 前完成参数校验。
5. 所有新增自定义异常命名以 `Exception` 结尾，至少三个构造函数齐全。
6. 所有 `finally` 块内无异常抛出且不会覆盖原异常。
7. 捕获后重抛保持原栈；改变语义时保留 inner exception。
8. 异步取消未记为业务失败或 Error。
9. 部分成功操作已回滚或具备事务边界。
10. 异常消息和日志未包含敏感信息。
11. 编译通过，相关回归测试和公开 API 注释已同步更新。
12. 本次新增或修改的令牌被实际使用，并传递给适用的下层可取消操作；取消源在消费者结束后释放。
13. 本次新增或修改的取消处理不把取消伪装成成功；需要区分停机与超时的路径已验证不同原因及竞态。

若无法确认某处是否该捕获，默认让异常传播，并说明判断依据。不要为了消除编译或诊断告警而扩大捕获范围或吞掉异常。

## 13. 静态检查配置

仓库尚未提供全局 `.editorconfig`。若项目决定启用以下分析器，将配置限定在 C# 工程并验证现有构建影响：

```ini
[*.cs]
dotnet_diagnostic.CA2200.severity = error
dotnet_diagnostic.CA2201.severity = error
dotnet_diagnostic.CA2219.severity = error
dotnet_diagnostic.CA1065.severity = warning
dotnet_diagnostic.CA1510.severity = suggestion
dotnet_diagnostic.CA1511.severity = suggestion
dotnet_diagnostic.CA1512.severity = suggestion
dotnet_diagnostic.CA1513.severity = suggestion
dotnet_diagnostic.CA2250.severity = warning
dotnet_diagnostic.CA1031.severity = warning
```
