# WordCollector（Word_Translator&Collector）

一个面向非母语者的 Windows 桌面英语查词沉淀工具：无论你在做什么，随手划词/复制，一键查询并自动积累到当日词库，晚上打开复盘窗口温习。

Designed for non-native English speakers to seamlessly translate and save on-screen words and sentences, no matter what you're doing.

## 功能

- **快速查询**：紧凑置顶小窗，输入单词、词组或句子，回车查询
- **三级查询管线**：本地词库缓存 → 可插拔词典源（默认 dictionaryapi.dev + MyMemory 翻译）→ AI 兜底（OpenAI / DeepSeek / 自定义 OpenAI 兼容接口）。词典源实现 `IDictionarySource` 接口，按注册顺序尝试，新增来源只需加入组合列表
- **自动沉淀**：查询结果自动保存到当日词库（SQLite），重复查询累计次数
- **每日复盘**：按日期回看、搜索、按熟悉度筛选、跨全部日期搜索、标记熟悉度、编辑词条、导出 Markdown / CSV
- **间隔重复复习（SRS）**：基于 SM-2 算法安排复习计划，"开始复习"进入复习模式，按「忘记 / 模糊 / 记得」评分自动排定下次复习时间
- **统计面板**：复盘窗口顶部展示总词条、今日待复习、连续打卡天数、已掌握数量、学习天数
- **内置专业词库导入**：设置中一键导入内置的土木工程基础学术词汇（约 140 个核心词，覆盖结构力学、构件、材料、岩土、水利、道路测量与施工管理，含音标、释义与例句），按原文去重，导入后即进入复习队列
- **朗读**：Windows 语音合成朗读英文，支持语速和语音选择，可查询后自动朗读
- **全局热键**：`Ctrl+Shift+Space` 唤起主窗口；`Ctrl+Shift+Q` 直接查询剪贴板内容（两者均可在设置中自定义，保存后即时生效）
- **系统托盘**：关闭窗口最小化到托盘，双击图标唤回
- **外观**：明/暗主题 × 5 种主题色

## 快捷键

| 快捷键 | 作用 |
| --- | --- |
| `Ctrl+Shift+Space`（全局，可自定义） | 显示主窗口 |
| `Ctrl+Shift+Q`（全局，可自定义） | 查询剪贴板内容 |
| `Enter` | 查询 |
| `Ctrl+Enter` | 查询后执行设置的动作（隐藏/清空/保留） |
| `Ctrl+L` | 清空输入 |
| `Esc` | 隐藏窗口 |

## 构建与运行

需要 .NET 8 SDK（Windows）：

```powershell
dotnet build WordCollector\WordCollector.csproj        # 构建
dotnet run --project WordCollector\WordCollector.csproj # 运行
dotnet test WordCollector.Tests\WordCollector.Tests.csproj # 单元测试
dotnet publish WordCollector\WordCollector.csproj -c Release # 发布单文件 exe
```

## 配置与数据

- 设置文件：`%APPDATA%\WordCollector\settings.json`（API Key 使用 Windows DPAPI 按当前用户加密存储）
- 词库数据库：`%APPDATA%\WordCollector\words.db`
- AI 接口需在「设置」中配置 Provider、API Key，可选自定义 Base URL 和模型名

## 项目结构

```
WordCollector/
  Models/        数据模型（词条、设置、AI 结果）
  Services/      查询管线、数据库、AI、词典、TTS、托盘、热键、主题
  ViewModels/    MVVM 视图模型
  Views/         主窗口、每日复盘、设置
  Helpers/       文本归一化、窗口尺寸策略、行为选项等纯逻辑
WordCollector.Tests/  xUnit 单元测试
```
