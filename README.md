## 本地化翻译工具 | Localization Translator

[中文](#chinese) | [English](#english)

<a name="chinese"></a>
# 本地化翻译工具

Unity本地化管理工具，支持飞书同步和AI翻译功能。

## 功能

- **飞书文档同步**：将Unity本地化文件与飞书文档同步
- **AI辅助翻译**：利用AI模型自动翻译缺失的本地化内容
- **多语言支持**：在一个界面中处理多种语言
- **高效工作流**：简化本地化过程
- **双语用户界面**：支持在英文和中文界面之间切换

## 安装方法

### 方法一：通过Git URL引用（推荐）

这种方法可以让您轻松地在多个项目间共享和更新包。

1. 打开你的Unity项目
2. 打开Package Manager (Window > Package Manager)
3. 点击"+"按钮，选择"Add package from git URL..."
4. 输入：`https://github.com/Youjixian/LocalizationTranslator.git`
5. 点击"Add"

### 方法二：本地包引用

如果您不想使用Git：

1. 复制整个`LocalizationTranslator`目录到您的计算机上的某个位置
2. 打开你的Unity项目
3. 打开Package Manager (Window > Package Manager)
4. 点击"+"按钮，选择"Add package from disk..."
5. 浏览并选择`LocalizationTranslator`目录中的`package.json`文件
6. 点击"Open"

### 方法三：直接复制到项目

如果您想要在项目中直接编辑和修改代码：

1. 复制整个`LocalizationTranslator`目录到你的项目的`Packages`目录下
2. Unity将自动识别并加载包

## 使用指南

### 语言切换

插件界面支持中英文切换：

1. 在任何插件窗口的右上角，你会找到一个语言下拉菜单
2. 选择你偏好的语言（英文或中文）
3. 界面将立即更新以反映你的语言选择
4. 你的偏好设置将在Unity会话之间保持

### 飞书同步模块

#### 配置设置

1. 打开Unity菜单 `Tools > Youjixian > Feishu Localization Sync Tool`
2. 在设置面板中填写以下信息：
   - 飞书API密钥：从飞书开放平台获取
   - 文档ID：要同步的飞书文档的ID
   - 工作表名称：指定要同步的工作表
   - 本地化文件路径：Unity项目中本地化文件的位置

#### 使用流程

1. 完成配置后，点击"验证连接"确认设置正确
2. 选择同步方向：
   - Unity → 飞书：将Unity中的本地化数据上传到飞书
   - 飞书 → Unity：从飞书下载最新翻译到Unity
   - 双向同步：自动合并两端的更改
3. 点击"开始同步"按钮执行同步操作
4. 同步完成后会显示更新统计信息

### AI翻译模块

#### 配置设置

1. 打开Unity菜单 `Tools > Youjixian > Localization Translator`
2. 在设置面板中配置AI服务：
   - 选择AI服务提供商
   - 输入API密钥
   - 设置模型参数（如需要）

#### 使用流程

1. 选择源语言和目标语言
2. 选择要处理的本地化文件或文件夹
3. 设置翻译选项：
   - 仅翻译空白条目
   - 覆盖现有翻译
   - 添加翻译注释
4. 点击"开始翻译"按钮
5. 查看翻译结果，并根据需要手动调整

## 更新包

### Git方式更新

如果您使用Git方式引用包：

1. 在原始包目录中进行修改
2. 提交并推送修改到Git仓库
3. 在使用此包的项目中，打开Package Manager
4. 找到Localization Translator包，点击"Update"按钮

### 本地包更新

如果使用本地包引用：

1. 在原始包目录中进行修改
2. 增加`package.json`中的版本号
3. 在使用此包的项目中，需要重新添加包或者手动复制更新的文件

## 常见问题

### 飞书同步常见问题

1. **问题：同步失败，提示API错误**
   解答：请检查您的API密钥是否有效，以及是否有正确的权限访问该文档。

2. **问题：某些字段未能正确同步**
   解答：检查飞书文档的列名是否与映射配置匹配。

### AI翻译常见问题

1. **问题：AI翻译质量不佳**
   解答：尝试调整AI提示词，或者使用更高级的模型。

2. **问题：翻译过程中断**
   解答：这可能是由于API速率限制或网络问题。请等待几分钟后重试。

## 项目结构

### 目录结构

```
LocalizationTranslator/
├── Documentation~/             // 文档目录
├── Editor/                     // 编辑器脚本目录
│   ├── FeishuSyncLocalization/ // 飞书同步模块
│   │   ├── FeishuConfig.cs     // 飞书配置类
│   │   ├── FeishuConfigWindow.cs // 飞书配置窗口
│   │   ├── FeishuLocalizationWindow.cs // 飞书同步主窗口
│   │   ├── FeishuService.cs    // 飞书API服务
│   │   └── LocalizationSyncManager.cs // 本地化同步管理器
│   ├── LLMAI/                  // AI翻译模块
│   │   ├── AIConfig.asset      // AI配置资源
│   │   ├── LLMAIConfig.cs      // AI配置类
│   │   ├── LLMAIConfigWindow.cs // AI配置窗口
│   │   ├── LLMAIService.cs     // AI服务接口
│   │   └── LocalizationTranslatorWindow.cs // AI翻译主窗口
│   ├── I18N.cs                 // 多语言支持
│   └── LocalizationTranslator.Editor.asmdef // 程序集定义文件
├── LICENSE                     // MIT许可证
├── README.md                   // 项目简介和快速入门
└── package.json                // 包配置文件
```

### 核心文件

- **package.json**: 定义包的元数据，包括名称、版本、依赖项等
- **LocalizationTranslator.Editor.asmdef**: 定义程序集，使包能正确编译和加载
- **I18N.cs**: 为插件UI提供国际化支持

### 飞书同步模块

- **FeishuConfig.cs**: 存储飞书API配置信息的类
- **FeishuConfigWindow.cs**: 飞书配置UI窗口
- **FeishuLocalizationWindow.cs**: 飞书同步主界面
- **FeishuService.cs**: 处理与飞书API的通信
- **LocalizationSyncManager.cs**: 管理Unity本地化文件与飞书文档的同步

### AI翻译模块

- **LLMAIConfig.cs**: 存储AI服务配置的类
- **LLMAIConfigWindow.cs**: AI配置UI窗口
- **LLMAIService.cs**: 处理与AI服务的通信
- **LocalizationTranslatorWindow.cs**: AI翻译主界面
- **AIConfig.asset**: 保存AI配置的资源文件

## 许可证

MIT许可证

---

<a name="english"></a>
# Localization Translator

A Unity localization management tool with Feishu (Lark) synchronization and AI translation capabilities.

## Features

- **Feishu Document Sync**: Synchronize Unity localization files with Feishu documents
- **AI-Assisted Translation**: Utilize AI models to automatically translate missing localization content
- **Multi-Language Support**: Handle multiple languages in a single interface
- **Efficient Workflow**: Streamline the localization process
- **Bilingual UI**: Switch between English and Chinese interfaces

## Installation

### Method 1: Via Git URL (Recommended)

This method allows you to easily share and update the package across multiple projects.

1. Open your Unity project
2. Open Package Manager (Window > Package Manager)
3. Click the "+" button, select "Add package from git URL..."
4. Enter: `https://github.com/Youjixian/LocalizationTranslator.git`
5. Click "Add"

### Method 2: Local Package Reference

If you prefer not to use Git:

1. Copy the entire `LocalizationTranslator` directory to a location on your computer
2. Open your Unity project
3. Open Package Manager (Window > Package Manager)
4. Click the "+" button, select "Add package from disk..."
5. Browse and select the `package.json` file in the `LocalizationTranslator` directory
6. Click "Open"

### Method 3: Direct Project Copy

If you want to directly edit and modify the code in your project:

1. Copy the entire `LocalizationTranslator` directory to your project's `Packages` directory
2. Unity will automatically recognize and load the package

## Usage Guide

### Language Switching

The plugin interface supports switching between Chinese and English:

1. In the upper right corner of any plugin window, you'll find a language dropdown menu
2. Select your preferred language (English or Chinese)
3. The interface will immediately update to reflect your language choice
4. Your preference will be maintained between Unity sessions

### Feishu Sync Module

#### Configuration Settings

1. Open the Unity menu `Tools > Youjixian > Feishu Localization Sync Tool`
2. Fill in the following information in the settings panel:
   - Feishu API Key: Obtained from the Feishu Open Platform
   - Document ID: The ID of the Feishu document to synchronize
   - Worksheet Name: Specify the worksheet to synchronize
   - Localization File Path: Location of localization files in the Unity project

#### Usage Flow

1. After completing the configuration, click "Verify Connection" to confirm the settings are correct
2. Choose synchronization direction:
   - Unity → Feishu: Upload localization data from Unity to Feishu
   - Feishu → Unity: Download the latest translations from Feishu to Unity
   - Bidirectional Sync: Automatically merge changes from both ends
3. Click the "Start Sync" button to execute the synchronization operation
4. After synchronization is complete, update statistics will be displayed

### AI Translation Module

#### Configuration Settings

1. Open the Unity menu `Tools > Youjixian > Localization Translator`
2. Configure the AI service in the settings panel:
   - Select AI service provider
   - Enter API key
   - Set model parameters (if needed)

#### Usage Flow

1. Select source and target languages
2. Choose localization files or folders to process
3. Set translation options:
   - Only translate empty entries
   - Override existing translations
   - Add translation comments
4. Click the "Start Translation" button
5. Review translation results and adjust manually as needed

## Updating the Package

### Git Method Update

If you reference the package via Git:

1. Make modifications in the original package directory
2. Commit and push changes to the Git repository
3. In projects using this package, open Package Manager
4. Find the Localization Translator package and click the "Update" button

### Local Package Update

If using local package reference:

1. Make modifications in the original package directory
2. Increase the version number in `package.json`
3. In projects using this package, you need to re-add the package or manually copy the updated files

## Common Issues

### Feishu Sync Common Issues

1. **Issue: Sync fails with API error**
   Solution: Check if your API key is valid and if you have the correct permissions to access the document.

2. **Issue: Some fields are not syncing correctly**
   Solution: Check if the column names in the Feishu document match the mapping configuration.

### AI Translation Common Issues

1. **Issue: Poor AI translation quality**
   Solution: Try adjusting the AI prompt words or use a more advanced model.

2. **Issue: Translation process interrupts**
   Solution: This may be due to API rate limits or network issues. Wait a few minutes and try again.

## Project Structure

### Directory Structure

```
LocalizationTranslator/
├── Documentation~/             // Documentation directory
├── Editor/                     // Editor scripts directory
│   ├── FeishuSyncLocalization/ // Feishu sync module
│   │   ├── FeishuConfig.cs     // Feishu configuration class
│   │   ├── FeishuConfigWindow.cs // Feishu configuration window
│   │   ├── FeishuLocalizationWindow.cs // Feishu sync main window
│   │   ├── FeishuService.cs    // Feishu API service
│   │   └── LocalizationSyncManager.cs // Localization sync manager
│   ├── LLMAI/                  // AI translation module
│   │   ├── AIConfig.asset      // AI configuration asset
│   │   ├── LLMAIConfig.cs      // AI configuration class
│   │   ├── LLMAIConfigWindow.cs // AI configuration window
│   │   ├── LLMAIService.cs     // AI service interface
│   │   └── LocalizationTranslatorWindow.cs // AI translation main window
│   ├── I18N.cs                 // Multilingual support
│   └── LocalizationTranslator.Editor.asmdef // Assembly definition file
├── LICENSE                     // MIT license
├── README.md                   // Project introduction and quick start
└── package.json                // Package configuration file
```

### Core Files

- **package.json**: Defines package metadata including name, version, dependencies, etc.
- **LocalizationTranslator.Editor.asmdef**: Defines the assembly for proper compilation and loading
- **I18N.cs**: Provides internationalization support for the plugin UI

### Feishu Sync Module

- **FeishuConfig.cs**: Class for storing Feishu API configuration information
- **FeishuConfigWindow.cs**: Feishu configuration UI window
- **FeishuLocalizationWindow.cs**: Feishu sync main interface
- **FeishuService.cs**: Handles communication with the Feishu API
- **LocalizationSyncManager.cs**: Manages synchronization between Unity localization files and Feishu documents

### AI Translation Module

- **LLMAIConfig.cs**: Class for storing AI service configuration
- **LLMAIConfigWindow.cs**: AI configuration UI window
- **LLMAIService.cs**: Handles communication with AI services
- **LocalizationTranslatorWindow.cs**: AI translation main interface
- **AIConfig.asset**: Resource file for saving AI configuration

## License

MIT License