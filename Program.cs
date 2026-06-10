using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using Env = System.Environment;

namespace SwFeatureExporter
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("=======================================================");
            Console.WriteLine("=== Abel's SolidWorks AI 数据提取工具 v5.2 深度递归版 ===");
            Console.WriteLine("=======================================================");

            // ── 1. 弹窗选根文件夹 ──
            string folderPath;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择多级零部件资产的【根文件夹】(将深度遍历所有子文件夹)";
                dialog.ShowNewFolderButton = false;
                dialog.InitialDirectory = Env.GetFolderPath(Env.SpecialFolder.Desktop);

                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    Console.WriteLine("用户取消选择，程序退出。");
                    Console.ReadKey();
                    return;
                }
                folderPath = dialog.SelectedPath;
            }

            // ── 2. 深度递归扫描 ──
            string[] files = Directory.GetFiles(folderPath, "*.sldprt", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Console.WriteLine($"在 [{folderPath}] 及其所有子文件夹中均未找到 .sldprt 零件。");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\n根目录: {folderPath}");
            Console.WriteLine($"纵深检索: {files.Length} 个零件，启动跨目录流式流水线...\n");

            // ── 3. 智能连接 SW ──
            SldWorks swApp;
            bool swWasRunning;

            try
            {
                swApp = (SldWorks)GetActiveSldWorks();
                swWasRunning = true;
                Console.WriteLine("→ 检测到运行中的 SolidWorks，保持现有视窗状态，绝不弹窗。");
            }
            catch
            {
                Type? swType = Type.GetTypeFromProgID("SldWorks.Application");
                swApp = (SldWorks)Activator.CreateInstance(swType!)!;
                swWasRunning = false;
                Console.WriteLine("→ 未检测到运行中实例，创建全隐式后台核心引擎...");
            }

            // ── 4. Smart 静默：只在新建实例时隐藏，已有实例不乱动 Visible ──
            if (!swWasRunning)
            {
                swApp.Visible = false;
                swApp.UserControl = false;
            }
            swApp.DocumentVisible(false, (int)swDocumentTypes_e.swDocPART);

            // ── 5. 流式处理 + 聚合输出 ──
            string globalTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            int successCount = 0;
            var allPartsJson = new List<string>();

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string relativePath = file.Replace(folderPath, "").TrimStart(Path.DirectorySeparatorChar);
                Console.WriteLine($"[{i + 1}/{files.Length}] {relativePath}");

                string? partJson = ProcessSinglePart(swApp, file, relativePath);
                if (partJson != null)
                {
                    allPartsJson.Add(partJson);
                    successCount++;
                }
            }

            // ── 5.5 聚合输出单文件到根文件夹 ──
            if (successCount > 0)
            {
                string folderName = Path.GetFileName(folderPath);
                if (string.IsNullOrEmpty(folderName)) folderName = "MASTER_ROOT";
                string outputPath = Path.Combine(folderPath, $"MASTER_DATASET_{folderName}_{globalTimestamp}.json");

                var folderSb = new StringBuilder();
                folderSb.AppendLine("{");
                folderSb.AppendLine($"  \"root_folder\": \"{folderPath.Replace("\\", "\\\\")}\",");
                folderSb.AppendLine($"  \"execution_time\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
                folderSb.AppendLine($"  \"total_parts\": {successCount},");
                folderSb.AppendLine("  \"master_dataset\": [");
                folderSb.AppendLine(string.Join(",\n", allPartsJson));
                folderSb.AppendLine("  ]");
                folderSb.Append("}");
                File.WriteAllText(outputPath, folderSb.ToString(), Encoding.UTF8);

                Console.WriteLine("\n=======================================================");
                Console.WriteLine($"多级递归完成！成功: {successCount}/{files.Length}");
                Console.WriteLine($"  → {outputPath}");
                Console.WriteLine("=======================================================");
            }
            else
            {
                Console.WriteLine("\n未能成功解析任何零件。");
            }

            // ── 6. Smart 退出 ──
            if (swWasRunning)
            {
                // 已有实例：只还文档可见性，不动 UserControl 和 Visible
                swApp.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
                Console.WriteLine("→ 已安全解除挂钩，不干扰您的日常操作。");
            }
            else
            {
                swApp.ExitApp();
                Marshal.ReleaseComObject(swApp);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Console.WriteLine("→ 隐式核心引擎已销毁。");
            }

            Console.WriteLine("\n按回车键退出...");
            Console.ReadKey();
        }

        // ══════════════════════════════════════════════
        //  Win32 API 获取已运行 COM 对象（.NET 10 兼容）
        // ══════════════════════════════════════════════
        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern int CLSIDFromProgID(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszProgID,
            out Guid lpclsid);

        [DllImport("oleaut32.dll", ExactSpelling = true)]
        private static extern int GetActiveObject(
            ref Guid rclsid,
            IntPtr pvReserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        static object GetActiveSldWorks()
        {
            int hr = CLSIDFromProgID("SldWorks.Application", out Guid clsid);
            if (hr != 0) throw new Win32Exception(hr);
            hr = GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
            if (hr != 0) throw new Win32Exception(hr);
            return obj;
        }

        // ══════════════════════════════════════════════
        //  处理单个零件 → 返回 JSON 字符串（含属性表 + 相对路径）
        // ══════════════════════════════════════════════
        static string? ProcessSinglePart(SldWorks swApp, string filePath, string relativePath)
        {
            ModelDoc2? swModel = null;
            try
            {
                int errors = 0;
                int warnings = 0;
                int options = (int)swOpenDocOptions_e.swOpenDocOptions_Silent
                            | (int)swOpenDocOptions_e.swOpenDocOptions_ReadOnly;

                swModel = swApp.OpenDoc6(filePath, (int)swDocumentTypes_e.swDocPART,
                    options, "", ref errors, ref warnings);

                if (swModel == null)
                {
                    Console.WriteLine($"  ERR: 无法打开 (errors={errors})");
                    return null;
                }

                swModel.FeatureManager.EnableFeatureTree = false;

                // ── v5.0 新增：提取自定义属性和配置属性 ──
                var propsDict = new Dictionary<string, (string expression, string value)>();

                // 自定义(全局)属性
                CustomPropertyManager custPropMgr = swModel.Extension.get_CustomPropertyManager("");
                ExtractProps(custPropMgr, propsDict);

                // 配置属性
                Configuration? activeCfg = (Configuration?)swModel.GetActiveConfiguration();
                if (activeCfg != null)
                {
                    CustomPropertyManager cfgPropMgr = swModel.Extension.get_CustomPropertyManager(activeCfg.Name);
                    ExtractProps(cfgPropMgr, propsDict); // 配置属性优先覆盖
                }

                // 拼接属性 JSON
                var propRows = new List<string>();
                foreach (var kv in propsDict)
                {
                    string escapedExp = kv.Value.expression.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    string escapedVal = kv.Value.value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    propRows.Add($"    \"{kv.Key}\": {{ \"expression\": \"{escapedExp}\", \"value\": \"{escapedVal}\" }}");
                }
                string propsJson = string.Join(",\n", propRows);
                if (propsJson.Length == 0) propsJson = "    (none)";

                Console.WriteLine($"  提取了 {propsDict.Count} 个属性字段");

                // ── 特征树提取 ──
                var featureJsonList = new List<string>();
                Feature? swFeature = (Feature?)swModel.FirstFeature();

                while (swFeature != null)
                {
                    string featType = swFeature.GetTypeName2();

                    if (IsDesignFeature(featType))
                    {
                        var deps = ExtractTopology(swFeature);
                        string dsl;

                        if (featType == "ProfileFeature")
                            dsl = CompressSketch(swFeature);
                        else
                            dsl = CompressFeature(swFeature, swModel);

                        string depsJson = string.Join("\",\"", deps);
                        string featJson =
                            $"    {{\n" +
                            $"      \"feature\": \"{swFeature.Name}\",\n" +
                            $"      \"type\": \"{featType}\",\n" +
                            $"      \"depends_on\": [\"{depsJson}\"],\n" +
                            $"      \"code\": \"{dsl}\"\n" +
                            $"    }}";
                        featureJsonList.Add(featJson);
                    }
                    swFeature = (Feature?)swFeature.GetNextFeature();
                }

                Console.WriteLine($"  提取了 {featureJsonList.Count} 个设计特征");

                // ── 总装 JSON ──
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"file_name\": \"{Path.GetFileName(filePath)}\",");
                sb.AppendLine($"  \"relative_path\": \"{relativePath.Replace("\\", "\\\\")}\",");
                sb.AppendLine($"  \"part_title\": \"{Path.GetFileNameWithoutExtension(filePath)}\",");
                sb.AppendLine("  \"custom_properties\": {");
                sb.AppendLine(propsJson);
                sb.AppendLine("  },");
                sb.AppendLine("  \"feature_tree\": [");
                sb.AppendLine(string.Join(",\n", featureJsonList));
                sb.AppendLine("  ]");
                sb.Append("}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERR: {ex.Message}");
                return null;
            }
            finally
            {
                if (swModel != null)
                {
                    swApp.CloseDoc(swModel.GetTitle());
                    Marshal.ReleaseComObject(swModel);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        // ══════════════════════════════════════════════
        //  v5.0：提取 CustomProperty 表达式+评估值
        // ══════════════════════════════════════════════
        static void ExtractProps(CustomPropertyManager propMgr,
            Dictionary<string, (string expression, string value)> dict)
        {
            if (propMgr == null) return;

            object? namesObj = propMgr.GetNames();
            if (namesObj == null) return;

            string[] names = (string[])namesObj;
            foreach (string name in names)
            {
                string valOut = "";
                string resOut = "";
                bool wasResolved;
                propMgr.Get6(name, false, out valOut, out resOut, out wasResolved, out bool linkToProp);
                // 配置属性覆盖自定义属性
                dict[name] = (valOut, resOut);
            }
        }

        // ══════════════════════════════════════════════
        //  草图参数提取
        // ══════════════════════════════════════════════
        static string CompressSketch(Feature swFeature)
        {
            try
            {
                Sketch swSketch = (Sketch)swFeature.GetSpecificFeature2();
                if (swSketch == null) return "Sketch()";

                object[]? segments = (object[]?)swSketch.GetSketchSegments();
                if (segments == null || segments.Length == 0)
                    return "Sketch(Empty)";

                var tokens = new List<string>();
                foreach (object obj in segments)
                {
                    SketchSegment swSeg = (SketchSegment)obj;
                    int segType = swSeg.GetType();

                    if (segType == (int)swSketchSegments_e.swSketchLINE)
                    {
                        SketchLine swLine = (SketchLine)swSeg;
                        // IsConstruction 在某些 interop 版本不存在，用反射
                        bool isCtr = false;
                        try { isCtr = (bool)swLine.GetType().InvokeMember("IsConstruction", System.Reflection.BindingFlags.InvokeMethod, null, swLine, null)!; } catch { }
                        if (!isCtr)
                        {
                            SketchPoint? start = (SketchPoint?)swLine.GetStartPoint2();
                            SketchPoint? end = (SketchPoint?)swLine.GetEndPoint2();
                            tokens.Add($"Line(S:[{R(start?.X)},{R(start?.Y)}];E:[{R(end?.X)},{R(end?.Y)}])");
                        }
                    }
                    else if (segType == (int)swSketchSegments_e.swSketchARC)
                    {
                        SketchArc swArc = (SketchArc)swSeg;
                        SketchPoint? center = (SketchPoint?)swArc.GetCenterPoint2();
                        double radius = swArc.GetRadius() * 1000;
                        tokens.Add($"Arc(C:[{R(center?.X)},{R(center?.Y)}];R:{R(radius)})");
                    }
                }

                return tokens.Count > 0
                    ? $"Sketch([{string.Join(" | ", tokens)}])"
                    : "Sketch(Empty)";
            }
            catch (Exception ex)
            {
                return $"Sketch(error:{ex.Message})";
            }
        }

        static double R(double? val) => Math.Round((val ?? 0) * 1000, 1);

        // ══════════════════════════════════════════════
        //  拓扑依赖提取
        // ══════════════════════════════════════════════
        static List<string> ExtractTopology(Feature swFeature)
        {
            var parents = new List<string>();
            try
            {
                object[]? raw = (object[]?)swFeature.GetParents();
                if (raw != null)
                {
                    foreach (object obj in raw)
                    {
                        Feature pf = (Feature)obj;
                        string pt = pf.GetTypeName2();
                        if (pt != "RefPlane" && pt != "OriginProfileFeature"
                            && pt != "RefAxis" && pt != "RefPoint" && pt != "CoordSys")
                        {
                            parents.Add(pf.Name);
                        }
                    }
                }
            }
            catch { }
            return parents;
        }

        // ════════════════════════════════════════════════
        //  全命令矩阵：参数降维 → DSL
        // ════════════════════════════════════════════════
        static string CompressFeature(Feature swFeature, ModelDoc2 swModel)
        {
            string featType = swFeature.GetTypeName2();

            try
            {
                // ────────── 1. 实体拉伸 & 切除 ──────────
                if (featType == "BaseExtrude" || featType == "Extrude"
                    || featType == "BossExtrude" || featType == "CutExtrude")
                {
                    var extData = (IExtrudeFeatureData2)swFeature.GetDefinition();
                    if (extData != null)
                    {
                        extData.AccessSelections(swModel, null);
                        double depth = Math.Round(extData.GetDepth(true) * 1000, 1);
                        string op = (featType == "CutExtrude") ? "CutExtrude" : "Extrude";
                        string dsl = $"{op}(D:{depth}";

                        try
                        {
                            Type? et = extData.GetType();
                            var m = et?.GetMethod("GetEndCondition2");
                            if (m != null)
                            {
                                int etv = (int)m.Invoke(extData, new object[] { false })!;
                                dsl += $", End:{EndTypeLabel(etv)}";
                            }
                        }
                        catch { }

                        dsl += ")";
                        extData.ReleaseSelectionAccess();
                        return dsl;
                    }
                }

                // ────────── 2. 旋转 & 旋转切除 ──────────
                if (featType == "BaseRevolve" || featType == "Revolve"
                    || featType == "CutRevolve" || featType == "Revolved")
                {
                    var revData = (IRevolveFeatureData2)swFeature.GetDefinition();
                    if (revData != null)
                    {
                        revData.AccessSelections(swModel, null);
                        double angle = 360;
                        try
                        {
                            var m = revData.GetType().GetMethod("GetRotationAngle");
                            if (m != null) angle = Math.Round((double)m.Invoke(revData, new object[] { true })! * 180.0 / Math.PI, 1);
                        }
                        catch { }
                        string op = (featType == "CutRevolve") ? "CutRevolve" : "Revolve";
                        string dsl = $"{op}(Angle:{angle}°)";
                        revData.ReleaseSelectionAccess();
                        return dsl;
                    }
                }

                // ────────── 3. 扫描 ──────────
                if (featType == "BaseSweep" || featType == "Sweep")
                {
                    var swpData = (ISweepFeatureData)swFeature.GetDefinition();
                    if (swpData != null)
                    {
                        swpData.AccessSelections(swModel, null);
                        swpData.ReleaseSelectionAccess();
                        return "Sweep(Path+Contour)";
                    }
                }

                // ────────── 4. 放样 ──────────
                if (featType == "BaseLoft" || featType == "Loft")
                {
                    return "Loft(MultiProfile)";
                }

                // ────────── 5. 钣金 ──────────
                if (featType == "SMBaseFlange")
                {
                    var bfData = (IBaseFlangeFeatureData)swFeature.GetDefinition();
                    if (bfData != null)
                    {
                        bfData.AccessSelections(swModel, null);
                        double thick = Math.Round(bfData.Thickness * 1000, 2);
                        bfData.ReleaseSelectionAccess();
                        return $"SheetMetalBase(Thick:{thick})";
                    }
                }
                if (featType == "EdgeFlange")
                {
                    return "EdgeFlange()";
                }
                if (featType == "Hem")
                {
                    return "Hem()";
                }

                // ────────── 6. 曲面 ──────────
                if (featType == "SurfaceExtrude")
                {
                    var surfExt = (IExtrudeFeatureData2)swFeature.GetDefinition();
                    if (surfExt != null)
                    {
                        surfExt.AccessSelections(swModel, null);
                        double depth = Math.Round(surfExt.GetDepth(true) * 1000, 1);
                        surfExt.ReleaseSelectionAccess();
                        return $"SurfaceExtrude(D:{depth})";
                    }
                }
                if (featType == "SurfaceRevolve")
                {
                    var surfRev = (IRevolveFeatureData2)swFeature.GetDefinition();
                    if (surfRev != null)
                    {
                        surfRev.AccessSelections(swModel, null);
                        double angle = 360;
                        try
                        {
                            var m = surfRev.GetType().GetMethod("GetRotationAngle");
                            if (m != null) angle = Math.Round((double)m.Invoke(surfRev, new object[] { true })! * 180.0 / Math.PI, 1);
                        }
                        catch { }
                        surfRev.ReleaseSelectionAccess();
                        return $"SurfaceRevolve(Angle:{angle}°)";
                    }
                }
                if (featType == "SurfaceOffset")
                {
                    // IOffsetSurfaceFeatureData 在部分 interop 中不存在，用基类降级
                    return "SurfaceOffset()";
                }
                if (featType == "Thicken")
                {
                    var thickData = (IThickenFeatureData)swFeature.GetDefinition();
                    if (thickData != null)
                    {
                        thickData.AccessSelections(swModel, null);
                        double val = Math.Round(thickData.Thickness * 1000, 2);
                        thickData.ReleaseSelectionAccess();
                        return $"SurfaceThicken(Thick:{val})";
                    }
                }

                // ────────── 7. 圆角 / 倒角 ──────────
                if (featType == "Fillet")
                {
                    var filletData = (ISimpleFilletFeatureData2)swFeature.GetDefinition();
                    if (filletData != null)
                    {
                        filletData.AccessSelections(swModel, null);
                        double radius = Math.Round(filletData.DefaultRadius * 1000, 1);
                        filletData.ReleaseSelectionAccess();
                        return $"Fillet(R:{radius})";
                    }
                }
                if (featType == "Chamfer")
                {
                    var chamferData = (IChamferFeatureData2)swFeature.GetDefinition();
                    if (chamferData != null)
                    {
                        chamferData.AccessSelections(swModel, null);
                        chamferData.ReleaseSelectionAccess();
                        return "Chamfer()";
                    }
                }

                // ────────── 8. 抽壳 ──────────
                if (featType == "Shell")
                {
                    return "Shell()";
                }

                // ────────── 9. 阵列 / 镜像 ──────────
                if (featType == "LinearPattern") return "LinearPattern()";
                if (featType == "CircularPattern") return "CircularPattern()";
                if (featType == "MirrorFeature" || featType == "MirrorPattern"
                    || featType == "MirrorBody") return "Mirror()";

                // ────────── 10. 孔 ──────────
                if (featType == "HoleWzd") return "HoleWizard()";
                if (featType == "SimpleHole") return "SimpleHole()";

                // ────────── 11. 筋 ──────────
                if (featType == "Rib") return "Rib()";

                // ────────── 12. 圆顶 ──────────
                if (featType == "Dome") return "Dome()";

                // ────────── 13. 参考几何体 ──────────
                if (featType == "RefPlane") return "RefPlane()";
                if (featType == "RefAxis") return "RefAxis()";
                if (featType == "RefPoint") return "RefPoint()";
            }
            catch
            {
                return $"{featType}(QueryFallback)";
            }

            return $"{featType}()";
        }

        static bool IsDesignFeature(string typeName)
        {
            string[] excludes =
            {
                "MaterialFolder", "MeshFolder", "AmbientLight",
                "DirectionalLight", "FavoriteFolder", "HistoryFolder",
                "SelectionSetFolder", "SensorFolder", "DocsFolder",
                "DetailCabinet", "InkMarkupFolder", "EnvFolder",
                "SolidBodyFolder", "SurfaceBodyFolder", "CommentsFolder",
                "EqnFolder", "Attribute", "CoordSys",
                "SpotLight", "PointLight", "PlaneFolder",
                "AxisFolder", "CurvesFolder", "AnnotationsFolder",
                "CutListFolder", "WeldmentsFolder", "BodiesFolder"
            };
            return Array.IndexOf(excludes, typeName) == -1;
        }

        static string EndTypeLabel(int et)
        {
            return et switch
            {
                0 => "Blind", 1 => "ThroughAll", 2 => "ThroughNext",
                3 => "UpToVertex", 4 => "UpToSurface", 5 => "OffsetFromSurface",
                6 => "MidPlane", 7 => "UpToBody",
                _ => $"Unknown({et})"
            };
        }
    }
}
