# 🏭 SW2LLM-Parser — SolidWorks → LLM 训练语料提取引擎

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue?logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![SolidWorks](https://img.shields.io/badge/SolidWorks-2016%2B-red?logo=dassault-systemes)](https://www.solidworks.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](./LICENSE)

> 将 SolidWorks 零部件的几何特征树、拓扑依赖、自定义属性表，**全自动静默提取**并压缩为 LLM 可直接微调的 DSL 语料，输出标准 JSON 数据集。

---

## 🎯 一句话解决什么问题

传统机械企业积累了几千个 `.sldprt` 零件，想要喂给 AI 做设计推理/参数推荐/自动出图，但手搓标注成千上万的零件根本不可能。

**SW2LLM-Parser 就是这条管道的自动化引擎** —— 选个文件夹 → 自动批量打开零件 → 扒特征树 + 属性表 → 输出 AI 训练 JSON。整个过程完全静默，不干扰你手头的工作。

---

## ✨ 核心能力

| 能力 | 说明 |
|------|------|
| 🕵️ **深度递归扫描** | 选中根目录，自动潜入所有子文件夹挖掘全部 `.sldprt` |
| 🤫 **真·静默模式** | 后台打开零件，SW 窗口不弹、不闪、不抢焦点。已有 SW 实例不被打扰 |
| 🧬 **全命令矩阵参数降维** | 拉伸(Extrude)、旋转(Revolve)、扫描(Sweep)、放样(Loft)、钣金、曲面、圆角(Fillet)、倒角(Chamfer)、阵列/镜像、孔/筋/圆顶 → DSL 压缩 |
| 🔗 **拓扑依赖提取** | 每个特征的 `GetParents()` 父子关系链 |
| 📋 **属性表提取** | SW 自定义属性 + 配置属性 → 表达式 + 评估值双双保留 |
| ✏️ **草图参数提取** | 线段起终点坐标、圆弧中心+半径 |
| 📦 **聚合单文件输出** | 万个零件汇入 1 个 `MASTER_DATASET_*.json` |
| 📁 **relative_path 注入** | 保留子文件夹分类逻辑，AI 能学到"标准件/螺栓/"vs"结构件/底盘/"的归纳规律 |
| 🧹 **智能善后** | 自己启的进程自己销毁；接管已有进程用完即还 |

---

## 📊 输出 Schema

```json
{
  "root_folder": "E:\\HL SW标准库\\",
  "execution_time": "2026-06-10 14:30:00",
  "total_parts": 1287,
  "master_dataset": [
    {
      "file_name": "底座支架.sldprt",
      "relative_path": "结构件\\底盘\\底座支架.sldprt",
      "part_title": "底座支架",
      "custom_properties": {
        "Material": { "expression": "\"304不锈钢\"", "value": "304不锈钢" },
        "Weight": { "expression": "\"SW-Mass\"", "value": "2.35" },
        "PartNo": { "expression": "\"BR-001\"", "value": "BR-001" }
      },
      "feature_tree": [
        {
          "feature": "拉伸1",
          "type": "BaseExtrude",
          "depends_on": ["草图1"],
          "code": "Extrude(D:50.0, End:Blind)"
        }
      ]
    }
  ]
}
```

---

## 🛠️ 快速开始

### 1. 依赖

- Windows 10/11 x64
- .NET 10.0 SDK
- SolidWorks 2016 或更高版本（已激活）
- Visual Studio 2022 或 `dotnet` CLI

### 2. 克隆 & 编译

```powershell
git clone https://github.com/mdmodule/SW2LLM-Parser.git
cd SW2LLM-Parser
dotnet publish -c Release -r win-x64 -o publish
```

### 3. 运行

```powershell
.\publish\SwFeatureExporter.exe
```

弹窗选择**根文件夹** → 全自动递归提取 → 在根文件夹生成 `MASTER_DATASET_*.json`。

---

## 🧱 项目结构

```
SW2LLM-Parser/
├── Program.cs                  # 主代码（全部逻辑在此单文件）
├── SwFeatureExporter.csproj    # .NET 10.0 项目文件
├── publish/                    # dotnet publish 产物（不提交）
├── README.md
└── .gitignore
```

---

## 🔒 商业安全 & 数据合规

本仓库遵循"**核心控制链开源，敏感业务数据隔离**"原则。

- 编译缓存 (`bin/`, `obj/`, `.vs/`) 已被 `.gitignore` 拦截
- **所有 `.json` 文件（包括 `MASTER_DATASET_*.json`）已被 `.gitignore` 强力封锁**，不会随代码推送到 GitHub
- ⚠️ 请勿手动将包含企业零部件参数的数据集文件提交到公开仓库

---

## 📄 License

MIT License © 2025 mdmodule

---

*Applying first-principles thinking to bridge Mechanical Engineering & Generative AI.*
