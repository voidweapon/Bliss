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

## 共享编码规范

Unity 与 Server 的新增、修改及审查代码统一遵循仓库根目录 `CODING_STANDARDS.md`，包括命名例外、注释、容器字段以及 Entity、System 与回调约定。该文档是两端共用规则的唯一来源；本文件只保留仓库导航和 Agent 工作流。

## 提交工作流

完成代码修改后，向用户展示变更及拟创建 commit 的 Summary 和 Description，并征求审查与提交批准。只有获得用户明确同意后才创建 commit，否则保持修改未提交。若用户指定了 issue，拟定提交信息和创建 commit 前都要检查是否正确引用该 issue。

# 每次做出决定时请务必检查代码是否符合上述规范，确保代码的可读性和维护性,执行完毕后再次检测查代码是否符合上述规范
# 做Plane时显示将要提交或修改的代码

## Agent skills

### 会话复盘与后续工作

继续本轮复盘提出的开发环境改进时，先阅读 `Doc/Bliss.AgentSessionRetrospective.2026-11-20.md`；其中区分已完成工作、候选改进和后续验收条件。

### 工作区代码审查

审查未提交的项目改动时，按 `Doc/Bliss.WorktreeCodeReviewWorkflow.md` 固定基点、选择补丁范围并验证审查前后的补丁一致性。该项目内流程补充外部审查技能，不要求修改用户目录或第三方技能。

### Skill 加载排错

读取或加载 skill 失败时，按 `Doc/Bliss.AgentSkillTroubleshooting.md` 排查。

### Issue tracker

Issues and specs are tracked in this repository's GitHub Issues. See `docs/agents/issue-tracker.md`.

### Triage labels

Use `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, and `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Use the single-context layout: root `GLOSSARY.md` and `docs/adr/`. See `docs/agents/domain.md`.