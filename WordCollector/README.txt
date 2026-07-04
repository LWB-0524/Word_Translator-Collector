WordCollector 1.1.0
===================

一款轻量的 Windows 英语表达收集工具。复制或输入单词、短语、句子后，
WordCollector 会调用已配置的 AI 服务生成中文释义，将结果保存在本机，
并可使用 Windows 系统语音朗读英文内容。

快速开始
--------
1. 双击 WordCollector.exe。
2. 点击标题栏“设置”，选择 API 服务商并填写 API Key、Base URL 和模型。
3. 点击“测试连接”，成功后保存设置。
4. 在主窗口输入英文表达，按 Enter 或点击“查询”。

主题与外观
----------
- 点击主窗口标题栏“主题”，可即时切换日间 / 夜间色调。
- 内置海蓝、靛紫、青绿、琥珀、玫红五种主题色。
- 主题会自动保存，下次启动继续使用。
- 设置页也可选择色调和主题色。

快捷键
------
- Ctrl+Shift+Space：在任意位置显示快速捕获窗口。
- Ctrl+Shift+Q：读取剪贴板并立即查询。
- Enter：查询释义并自动保存。
- Ctrl+Enter：查询后执行设置中的后续行为。
- Ctrl+L：清空当前内容并停止朗读。
- Esc：隐藏到系统托盘并停止朗读。

每日复盘
--------
点击标题栏“复盘”可按日期查看、搜索和筛选已保存的词条，并支持：
- 编辑释义与例句；
- 标记“陌生 / 学习中 / 已掌握”；
- 朗读、删除词条；
- 导出 Markdown 或 CSV。

本地数据与安全
--------------
数据库：%AppData%\WordCollector\words.db
设置文件：%AppData%\WordCollector\settings.json

API Key 使用 Windows DPAPI 加密，只能由当前 Windows 用户解密。
远程 API 地址必须使用 HTTPS；仅 localhost 允许 HTTP。

系统要求
--------
- Windows 10/11 x64
- 网络连接（AI 查询）
- 英文语音包（朗读；可在 Windows 设置中安装）
- 发布版已包含 .NET 8 运行时

开发测试
--------
在项目根目录运行：
dotnet test .\WordCollector.Tests\WordCollector.Tests.csproj
