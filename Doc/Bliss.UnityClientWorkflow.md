# Unity 客户端工程处理与编译排错

## 工程与源码位置

- 客户端 Unity 工程位于仓库根目录的 `Unity/`；使用 `Unity/ProjectSettings/ProjectVersion.txt` 中指定的 Editor 版本。
- 客户端与服务端共用的 Model、Logic 源码分别放在 `Unity/Assets/Scripts/Model/Shard/` 和 `Unity/Assets/Scripts/Logic/Shard/`。服务端对应的 `.csproj` 直接链接这些文件；修改共用逻辑应修改这里的实际源码，并验证两端编译，不要只修改某一端生成的工程文件。
- 客户端专用视图逻辑位于 `Unity/Assets/Scripts/ViewLogic/`，服务端专用逻辑位于 `Server/` 对应程序集目录。处理源码时遵循程序集边界。
- 客户端自动化测试环境的已确认建设范围与验收标准见 `Doc/Bliss.UnityTestEnvironmentPlan.md`；该计划标明实施状态，不代表测试环境已建成。

## 用 Unity CLI 获取编译错误

Unity 生成的 `Unity/*.csproj` 和 `Unity/*.sln` 被仓库忽略，源文件清单可能过期；单独运行 `dotnet build` 的缺失类型错误不一定代表 Unity Editor 的实际编译结果。优先使用项目指定版本的 Unity Editor 编译脚本，并根据实际错误修复源码，而不是提交或长期手改生成的项目文件。

从仓库根目录执行以下命令，先确认项目版本、已安装 Editor 和是否已有 Editor 打开该工程：

```bash
cat Unity/ProjectSettings/ProjectVersion.txt
unity editors --installed --format json
unity editors running --format json
unity status --project-path "$PWD/Unity" --format json
```

若工程已有可连接的 Editor 且安装了 Pipeline 包，可使用 `unity recompile --project-path "$PWD/Unity" --format json` 获取脚本编译诊断。该命令依赖运行中的 Editor；Editor 在 Safe Mode、Pipeline 不可用或无法连接时，不能把连接失败视为编译成功。不要为了运行批处理而关闭其他人的 Editor。

若工程未被其他 Editor 打开，可在仓库根目录用批处理启动项目指定版本的 Editor，日志写到仓库外：

```bash
unity run "$PWD/Unity" --timeout 600 \
  --log-file /tmp/bliss-unity-client-compile.log --no-tail --format json -- -nographics
```

该命令会启动 Editor 并导入项目、编译脚本；检查命令结果及 Editor 日志中的编译错误，逐批修复后重新运行。`unity run` 自行管理 `-batchmode`、`-quit` 和 `-projectPath`，不要通过 `--` 重复传入。若 Editor 未安装、许可不可用或命令超时，应报告实际阻碍；不要自动安装 Editor、修改许可或提交日志。批处理编译通过不等于已验证 Player 构建、场景切换或运行时行为。

## 本次问题的处理

Issue #8 的客户端编译先后暴露了以下问题，已在 `60d94ea` 中修复：

- `ResourcesLoaderComponentSystems.cs` 中 Unity 和 YooAsset 都定义了 `SceneHandle`，使用类型别名明确指定 `YooAsset.SceneHandle`。
- LevelScene 光照快照使用的 `SphericalHarmonicsL2` 位于 `UnityEngine.Rendering`，需要导入该命名空间。
- LevelScene 客户端事件处理器调用资源加载扩展方法，需要导入其所在的 `Bliss.ViewLogic` 命名空间。

两处初始错误修复后重新编译，才暴露扩展方法的后续错误，因此应持续编译直至没有脚本错误。此次 Unity CLI 批处理编译成功；日志仍提示许可访问令牌更新失败，未验证客户端运行时。

## 生成文件与提交

- 当前仓库的 `.gitignore` 忽略 `*.meta`、Unity 生成的项目文件、UnitTest `TestResults` 和 BenchmarkDotNet 结果目录；不要把这些生成结果加入本次提交。已被 Git 跟踪的文件不会仅因为添加忽略规则而自动取消跟踪，应检查 `git status` 和暂存内容。
- Unity 编译、导入可能生成或修改本地文件；运行前后检查工作区，保留他人已有变更，不要随意删除或覆盖。
- `Unity/Assets/CodeAnalyzer/Bliss.ProjectRule.dll` 是仓库中的分析器 DLL，不应与测试或基准结果混淆；如需随源码提交其更新，先检查变更并在提交范围中明确列出。
- 修改完成后展示变更和拟提交的 Summary、Description，取得用户明确批准后才创建 commit；若任务关联 issue，提交信息应引用对应编号。
