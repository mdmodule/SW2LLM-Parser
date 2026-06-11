# 🏭 SW2LLM-Parser — SolidWorks → LLM 训练语料提取引擎 (v6.0 Super Pipeline)

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue?logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![SolidWorks](https://img.shields.io/badge/SolidWorks-2016%2B-red?logo=dassault-systemes)](https://www.solidworks.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](./LICENSE)

> 将 SolidWorks 零部件与装配体的几何特征树、高级草图算子、空间装配拓扑、以及自定义属性表，**全自动静默提取**并压缩为 LLM 可直接微调的 DSL 语料，输出标准工业级 JSON 数据集。

---

## 🎯 一句话解决什么问题

传统机械制造与设计企业积累了成千上万的 `.sldprt` 零件和 `.sldasm` 装配体，想要喂给 AI 做设计推理、结构审查、参数推荐或自动出图，但手搓标注根本是不可能的任务。

**SW2LLM-Parser v6.0 就是这条数据管道的自动化超级引擎**：选个文件夹 → 自动流式解构双模图纸 → 榨干特征树/草图/装配拓扑/属性表 → 直接下盘输出 AI 训练 JSON。整个过程全静默运行，且经过海量图纸压力测试，不留存内存，不干扰你手头的正常设计工作。

---

## ✨ 核心能力

| 能力标签 | 核心技术说明 | v6.0 升级特性 |
|----------|-------------|--------------|
| 🚀 **零内存流式直写** | 放弃内存拼接，改用全流式硬盘直写通道（Streaming JsonWriter）。 | **[全新]** 纵深遍历 10万+ 级巨型图纸库，进程内存始终稳如狗，彻底根除 OOM 爆栈。 |
| 🤖 **双模态协同处理器** | 深度支持 `.sldprt`（零件）与 `.sldasm`（装配体）双向检索。 | **[全新]** 不仅能抓单体特征，还能解构装配体的 BOM 层级、固定状态与空间齐次变换矩阵。 |
| 🧬 **全要素几何解构** | 智能拉伸、旋转、扫描、放样、钣金、曲面、圆角、倒角、阵列、镜像、孔特征提取。 | **[优化]** 草图引擎全面补齐样条曲线（Splines）、圆（Circles）、椭圆（Ellipses），完美还原复杂曲面骨架。 |
| ⚡ **分级惰性 GC** | 摒弃单件高频阻塞式垃圾回收，引入每 50 件分批惰性主动回收。 | **[优化]** 彻底释放 COM 绑定的同时，流水线吞吐速度飙升 3~5 倍。 |
| 📋 **双轨属性矩阵** | 遍历主属性与特定配置属性，保留 **表达式** 与 **评估值** 双轨数据。 | 方便大模型学习诸如 "Material → 304不锈钢" 或重量/图号的映射逻辑。 |
| 📁 **OS 原生路径安全** | 使用 .NET 现代路径算子计算 `relative_path`。 | 免疫盘符、斜杠及大小写污染，保障大模型资产分类标签的绝对纯净。 |
| 🤫 **真·静默沙盒模式** | 隐式挂钩已存在的 SW 实例或开启静默沙盒核心。 | 视窗不弹、不闪、不抢焦点，让数据清洗在后台无感完成。 |

---

## 📊 输出 Schema (v6.0 动态流式标准)

```json
{
  "root_folder": "E:\\HL_Office_Furniture_Lib\\\\",
  "execution_time": "2026-06-10 17:45:00",
  "master_dataset": [
    {
      "file_name": "人体工学背板.sldprt",
      "relative_path": "结构件\\坐靠组件\\人体工学背板.sldprt",
      "asset_type": "swDocPART",
      "custom_properties": {
        "Material": { "expression": "\"PA66+30GF\"", "value": "尼龙玻纤" },
        "Designer": { "expression": "\"Abel\"", "value": "Abel" }
      },
      "feature_tree": [
        {
          "feature": "草图1",
          "type": "ProfileFeature",
          "depends_on": [],
          "code": "Sketch([Line(S:[0,0];E:[120.5,0]) | Spline(PointsCount:12) | Arc(C:[50,60];R:25.5)])"
        },
        {
          "feature": "凸台-拉伸1",
          "type": "BaseExtrude",
          "depends_on": ["草图1"],
          "code": "Extrude(D:12.5, End:Blind)"
        }
      ]
    },
    {
      "file_name": "办公椅底盘总成.sldasm",
      "relative_path": "装配体\\总成\\办公椅底盘总成.sldasm",
      "asset_type": "swDocASSEMBLY",
      "custom_properties": {
        "BOM_Level": { "expression": "\"A\"", "value": "A" }
      },
      "assembly_topology": [
        {
          "component": "底座支架-1",
          "path": "底座支架.sldprt",
          "state": "Fixed",
          "transform": "Identity"
        },
        {
          "component": "气压棒-1",
          "path": "三级气压棒.sldprt",
          "state": "Float",
          "transform": "Pos:[0,0,150];Rot:[1,0,0|0,1,0]"
        }
      ]
    }
  ]
}
```

---

## 🛠️ 快速开始

### 1. 环境依赖

- Windows 10 / 11 x64
- .NET 10.0 SDK 或更高版本
- SolidWorks 2016 或更高版本（需正常激活 API 接口）
- Visual Studio 2022 或 `dotnet` CLI

### 2. 克隆与编译

```bash
git clone https://github.com/mdmodule/SW2LLM-Parser.git
cd SW2LLM-Parser
dotnet publish -c Release -r win-x64 -o publish
```

### 3. 运行清洗流水线

**方式 A（推荐）**：直接前往 [Releases](https://github.com/mdmodule/SW2LLM-Parser/releases) 下载最新编译好的 `SwFeatureExporter.exe`，双击运行。

**方式 B（源码运行）**：

```bash
.\publish\SwFeatureExporter.exe
```

运行后会弹出原生文件夹选择框，选择你的图纸根目录，引擎即刻开启双模流式管道。清洗完成后，会在该目录下生成一份带有时间戳的 `STREAM_DATASET_*.json` 超级数据集。

---

## 🧱 项目结构

```plaintext
SW2LLM-Parser/
├── Program.cs                  # 核心流式管道代码（单文件高内聚，便于维护）
├── SwFeatureExporter.csproj    # .NET 10.0 + SolidWorks COM 依赖配置
├── publish/                    # 编译分发产物目录（已忽略）
├── README.md                   # 项目说明文档
└── .gitignore                  # 数据安全防护网
```

---

## 🔒 商业安全与数据合规

本仓库严格遵循 **"核心控制链开源，敏感业务数据隔离"** 的工业安全原则：

- `bin/`、`obj/`、`.vs/` 等环境缓存已被 `.gitignore` 严密拦截。
- **安全锁底线**：所有以 `.json` 结尾的数据集文件（包括生成的 `STREAM_DATASET_*.json`）已在 `.gitignore` 中被强力封锁，绝不会随着代码提交不小心误推至公开仓库。
- ⚠️ 请各企业开发人员注意，切勿手动强行将包含商业机密零部件几何参数的数据集提交到 GitHub 公开网络。

---

## 📄 License

MIT License © 2026 mdmodule

---

*Applying first-principles thinking to bridge Mechanical Engineering & Generative AI.*
