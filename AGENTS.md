# Instructions for AI Agents

## 仓库结构
### 1. Unity 目录
 - 用途: Unity客户端工程

### 2. Server 目录
 - 用途: 服务器工程

### 3. Tools 目录 
 - 用途: 日常开发所用的工具

### 4. Bliss.ProjectRule 目录 
 - 用途: Unity/Server 工程的代码规范分析器

### 5. Bliss.UnitTest 目录
 - 用途: Unity/Server 工程的单元测试工程

### 6. BlissBenchMark 目录
 - 用途: Unity/Server 工程的性能测试工程

### 7. Bin 目录
- 用途: Server工程的编译输出目录

### 8. ServerRes 目录
- 用途: Server工程的资源目录

### 9. Logs 目录
- 内容: 服务器日志

### 10. FileServer 目录
- 内容: 客户端资源的文件服务器

### 11. Doc 目录
- 内容: 项目文档

## 代码规范
1. 所有编程相关的命名均不能以下划线或美元符号开始，也不能以下划线或美元符号结束（例外：私有字段可使用 `_lowerCamelCase`，与现有代码保持一致）
2. 类名、公有的方法名、公有的成员变量使用 UpperCamelCase 风格（例外：框架事件/调用处理器类可沿用 `Xxx_YYY_ZZZ` 命名）
3. 非公有的方法名、非公有的成员变量、参数名、局部变量都统一使用 lowerCamelCase 风格
4. 常量命名应该全部大写，单词间用下划线隔开，力求语义表达完整清楚，不要嫌名字长
5. 枚举命名以大写E开头
6. IDE 的 text file encoding 设置为 UTF-8；IDE 中文件的换行符使用 Unix 格式，不要使用 Windows 格式
7. 注释的双斜线与注释内容之间有且仅有一个空格
   历史代码若不符合该规则，新增/修改代码需按本规则执行
8. 新增或修改的公共类、公共函数/方法、公共属性、公共字段和公共事件必须添加注释，说明用途及必要的使用约束；C# 使用 XML 文档注释，并按需说明参数、返回值和异常。非直观或特殊的实现方式也必须就近注释，说明其原因和作用
9. 新增、修改或优化性能敏感的组件、算法及影响复杂度的数据结构时，必须执行仓库根目录 `Doc/Bliss.PerformanceSensitiveWorkflow.md` 中的流程
10. 不允许任何魔法值（即未经预先定义的常量）直接出现在代码中
11. 字段类型不直接嵌套容器类型；需要多层容器时，用名称清晰的类或结构体封装每一层。字典等键值容器的字段名或注释必须说明键和值分别代表什么
12. 完成代码修改后，向用户展示变更及拟创建 commit 的 Summary 和 Description，一并征求审查与提交批准；只有获得用户明确同意后才创建 commit，否则保持修改未提交。若用户指定了 issue，在拟定 Summary 和 Description 时及创建 commit 前均检查提交信息是否正确引用指定 issue

# 每次做出决定时请务必检查代码是否符合上述规范，确保代码的可读性和维护性,执行完毕后再次检测查代码是否符合上述规范
# 做Plane时显示将要提交或修改的代码

## Agent skills

### Skill 加载排错

读取或加载 skill 失败时，按 `Doc/Bliss.AgentSkillTroubleshooting.md` 排查。

### Issue tracker

Issues and specs are tracked in this repository's GitHub Issues. See `docs/agents/issue-tracker.md`.

### Triage labels

Use `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, and `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Use the single-context layout: root `GLOSSARY.md` and `docs/adr/`. See `docs/agents/domain.md`.