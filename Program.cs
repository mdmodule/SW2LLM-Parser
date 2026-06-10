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
            Console.WriteLine("=================================================================");
            Console.WriteLine("=== Abel's SolidWorks AI 数据提取引擎 v6.0 工业级流式管道版 ===");
            Console.WriteLine("=================================================================");

            // ── 1. 弹窗选根文件夹 ──
            string folderPath;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择工业资产【根文件夹】(将流式遍历所有零件与装配体)";
                dialog.ShowNewFolderButton = false;
                dialog.InitialDirectory = Env.GetFolderPath(Env.SpecialFolder.Desktop);

                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    Console.WriteLine("用户取消操作，程序安全退出。");
                    Console.ReadKey();
                    return;
                }
                folderPath = dialog.SelectedPath;
            }

            // ── 2. 双模全量检索 ──
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(folderPath, "*.sldprt", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(folderPath, "*.sldasm", SearchOption.AllDirectories));

            if (files.Count == 0)
            {
                Console.WriteLine($"在 [{folderPath}] 中未检测到任何 .sldprt 或 .sldasm 工业资产。");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\n[初始化] 发现目标资产: {files.Count} 个对象，正在激活双模挂钩流水线...\n");

            // ── 3. 智能 SW 生命周期接管 ──
            SldWorks swApp;
            bool swWasRunning;
            try
            {
                swApp = (SldWorks)GetActiveSldWorks();
                swWasRunning = true;
                Console.WriteLine("→ 成功接管当前运行中的 SolidWorks，开启后台静默互操作。");
            }
            catch
            {
                Type? swType = Type.GetTypeFromProgID("SldWorks.Application");
                swApp = (SldWorks)Activator.CreateInstance(swType!)!;
                swWasRunning = false;
                Console.WriteLine("→ 未检测到活跃实例，已创建隐式沙盒核心引擎。");
            }

            // ── 4. 极致静默 ──
            if (!swWasRunning)
            {
                swApp.Visible = false;
                swApp.UserControl = false;
            }
            swApp.DocumentVisible(false, (int)swDocumentTypes_e.swDocPART);
            swApp.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);

            // ── 5. 流式输出：StraemWriter 直接落盘，不滞留内存 ──
            string globalTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(folderName)) folderName = "MASTER_ROOT";
            string outputPath = Path.Combine(folderPath, $"STREAM_DATASET_{folderName}_{globalTimestamp}.json");

            int successCount = 0;

            using (var writer = new StreamWriter(outputPath, false, Encoding.UTF8, 65536))
            {
                writer.WriteLine("{");
                writer.WriteLine($"  \"root_folder\": \"{folderPath.Replace("\\", "\\\\")}\",");
                writer.WriteLine($"  \"execution_time\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
                writer.WriteLine("  \"master_dataset\": [");

                for (int i = 0; i < files.Count; i++)
                {
                    string file = files[i];
                    string relativePath = Path.GetRelativePath(folderPath, file);
                    string ext = Path.GetExtension(file).ToLower();
                    swDocumentTypes_e docType = ext == ".sldasm" ? swDocumentTypes_e.swDocASSEMBLY : swDocumentTypes_e.swDocPART;

                    Console.Write($"[{i + 1}/{files.Count}] 正在解构 → {relativePath} ");

                    string? docJson = ProcessSingleDocument(swApp, file, relativePath, docType);

                    if (docJson != null)
                    {
                        if (successCount > 0) writer.WriteLine(",");
                        writer.Write(docJson);
                        successCount++;
                        Console.WriteLine("[成功]");
                    }
                    else
                    {
                        Console.WriteLine("[跳过/失败]");
                    }

                    // 每 50 件触发惰性 GC，兼顾内存释放与吞吐速度
                    if (i > 0 && i % 50 == 0)
                    {
                        GC.Collect(2, GCCollectionMode.Forced, true);
                        GC.WaitForPendingFinalizers();
                    }
                }

                writer.WriteLine();
                writer.WriteLine("  ]");
                writer.Write("}");
            }

            // ── 6. 优雅脱钩 ──
            if (swWasRunning)
            {
                swApp.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
                swApp.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
                Console.WriteLine("→ 资产清洗流结束，已安全解除挂钩，保留您的主视窗。");
            }
            else
            {
                swApp.ExitApp();
                Marshal.ReleaseComObject(swApp);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Console.WriteLine("→ 资产清洗流结束，隐式核心引擎已安全销毁。");
            }

            Console.WriteLine($"\n清洗完成！共计成功导出: {successCount}/{files.Count} 项数据集。");
            Console.WriteLine($"→ 目标管道文件: {outputPath}");
            Console.WriteLine("按任意键退出控制台...");
            Console.ReadKey();
        }

        // ══════════════════════════════════════════════
        //  Win32 API 获取已运行 COM 对象（.NET 10 兼容）
        // ══════════════════════════════════════════════
        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid lpclsid);

        [DllImport("oleaut32.dll", ExactSpelling = true)]
        private static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        static object GetActiveSldWorks()
        {
            int hr = CLSIDFromProgID("SldWorks.Application", out Guid clsid);
            if (hr != 0) throw new Win32Exception(hr);
            hr = GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
            if (hr != 0) throw new Win32Exception(hr);
            return obj;
        }

        // ══════════════════════════════════════════════
        //  双模统一文档处理器 (Part + Assembly)
        // ══════════════════════════════════════════════
        static string? ProcessSingleDocument(SldWorks swApp, string filePath, string relativePath, swDocumentTypes_e docType)
        {
            ModelDoc2? swModel = null;
            try
            {
                int errors = 0;
                int warnings = 0;
                int options = (int)swOpenDocOptions_e.swOpenDocOptions_Silent |
                              (int)swOpenDocOptions_e.swOpenDocOptions_ReadOnly;

                swModel = swApp.OpenDoc6(filePath, (int)docType, options, "", ref errors, ref warnings);
                if (swModel == null) return null;

                swModel.FeatureManager.EnableFeatureTree = false;

                // ── 提取属性矩阵 ──
                var propsDict = new Dictionary<string, (string expression, string value)>();
                CustomPropertyManager custPropMgr = swModel.Extension.get_CustomPropertyManager("");
                ExtractProps(custPropMgr, propsDict);

                Configuration? activeCfg = (Configuration?)swModel.GetActiveConfiguration();
                if (activeCfg != null)
                {
                    CustomPropertyManager cfgPropMgr = swModel.Extension.get_CustomPropertyManager(activeCfg.Name);
                    ExtractProps(cfgPropMgr, propsDict);
                }

                var propRows = new List<string>();
                foreach (var kv in propsDict)
                {
                    string escapedExp = kv.Value.expression.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    string escapedVal = kv.Value.value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    propRows.Add($"    \"{kv.Key}\": {{ \"expression\": \"{escapedExp}\", \"value\": \"{escapedVal}\" }}");
                }
                string propsJson = string.Join(",\n", propRows);

                // ── 根据类型提取结构 ──
                string innerBodyJson;
                if (docType == swDocumentTypes_e.swDocPART)
                {
                    innerBodyJson = $"  \"feature_tree\": [\n{ExtractFeatureTree(swModel)}\n  ]";
                }
                else
                {
                    innerBodyJson = $"  \"assembly_topology\": [\n{ExtractAssemblyTopology((AssemblyDoc)swModel)}\n  ]";
                }

                var sb = new StringBuilder();
                sb.AppendLine("  {");
                sb.AppendLine($"    \"file_name\": \"{Path.GetFileName(filePath)}\",");
                sb.AppendLine($"    \"relative_path\": \"{relativePath.Replace("\\", "\\\\")}\",");
                sb.AppendLine($"    \"asset_type\": \"{docType}\",");
                sb.AppendLine("    \"custom_properties\": {");
                sb.AppendLine(propsJson.Length > 0 ? propsJson : "    (none)");
                sb.AppendLine("    },");
                sb.Append(innerBodyJson);
                sb.Append("\n  }");

                return sb.ToString();
            }
            catch
            {
                return null;
            }
            finally
            {
                if (swModel != null)
                {
                    swApp.CloseDoc(swModel.GetTitle());
                    Marshal.ReleaseComObject(swModel);
                }
            }
        }

        static void ExtractProps(CustomPropertyManager propMgr, Dictionary<string, (string expression, string value)> dict)
        {
            if (propMgr == null) return;
            object? namesObj = propMgr.GetNames();
            if (namesObj == null) return;

            foreach (string name in (string[])namesObj)
            {
                string valOut = "", resOut = "";
                try
                {
                    // 反射调用 Get6 兼容 5 参数 / 6 参数
                    var get6Methods = propMgr.GetType().GetMethods()
                        .Where(m => m.Name == "Get6");
                    var get6 = get6Methods.FirstOrDefault(m => m.GetParameters().Length >= 5);
                    if (get6 != null)
                    {
                        var prms = get6.GetParameters();
                        object[] args = prms.Length >= 6
                            ? new object[] { name, false, valOut, resOut, false, false }
                            : new object[] { name, false, valOut, resOut, false };
                        get6.Invoke(propMgr, args);
                        valOut = (string)args[2];
                        resOut = (string)args[3];
                    }
                }
                catch { continue; }
                
                if (!string.IsNullOrEmpty(name))
                    dict[name] = (valOut, resOut);
            }
        }

        // ══════════════════════════════════════════════
        //  特征树提取
        // ══════════════════════════════════════════════
        static string ExtractFeatureTree(ModelDoc2 swModel)
        {
            var featureJsonList = new List<string>();
            Feature? swFeature = (Feature?)swModel.FirstFeature();

            while (swFeature != null)
            {
                string featType = swFeature.GetTypeName2();
                if (IsDesignFeature(featType))
                {
                    var deps = ExtractTopology(swFeature);
                    string dsl = featType == "ProfileFeature" ? CompressSketch(swFeature) : CompressFeature(swFeature, swModel);
                    string depsJson = string.Join("\",\"", deps);

                    featureJsonList.Add(
                        $"    {{\n" +
                        $"      \"feature\": \"{swFeature.Name}\",\n" +
                        $"      \"type\": \"{featType}\",\n" +
                        $"      \"depends_on\": [\"{depsJson}\"],\n" +
                        $"      \"code\": \"{dsl}\"\n" +
                        $"    }}");
                }
                swFeature = (Feature?)swFeature.GetNextFeature();
            }
            return string.Join(",\n", featureJsonList);
        }

        // ══════════════════════════════════════════════
        //  装配体拓扑降维
        // ══════════════════════════════════════════════
        static string ExtractAssemblyTopology(AssemblyDoc assembly)
        {
            var compJsonList = new List<string>();
            object[]? components = (object[]?)assembly.GetComponents(false);
            if (components == null) return "";

            foreach (object obj in components)
            {
                Component2 comp = (Component2)obj;
                if (comp.IsSuppressed()) continue;

                string compName = comp.Name2;
                string compPath = comp.GetPathName();

                // 变换矩阵提取（AI 理解装配空间位置的关键维度）
                string matrixDsl = "Identity";
                try
                {
                    dynamic swXform = comp.Transform2;
                    if (swXform != null)
                    {
                        double[] m = (double[])swXform.ArrayData;
                        matrixDsl = $"Pos:[{R(m[9])},{R(m[10])},{R(m[11])}];Rot:[{R(m[0])},{R(m[1])},{R(m[2])}|{R(m[3])},{R(m[4])},{R(m[5])}]";
                    }
                }
                catch { }

                compJsonList.Add(
                    $"    {{\n" +
                    $"      \"component\": \"{compName}\",\n" +
                    $"      \"path\": \"{Path.GetFileName(compPath)}\",\n" +
                    $"      \"state\": \"{(comp.IsFixed() ? "Fixed" : "Float")}\",\n" +
                    $"      \"transform\": \"{matrixDsl}\"\n" +
                    $"    }}");
            }
            return string.Join(",\n", compJsonList);
        }

        // ══════════════════════════════════════════════
        //  高阶全要素草图解析
        // ══════════════════════════════════════════════
        static string CompressSketch(Feature swFeature)
        {
            try
            {
                Sketch? swSketch = (Sketch?)swFeature.GetSpecificFeature2();
                if (swSketch == null) return "Sketch()";

                object[]? segments = (object[]?)swSketch.GetSketchSegments();
                if (segments == null || segments.Length == 0) return "Sketch(Empty)";

                var tokens = new List<string>();
                foreach (object obj in segments)
                {
                    SketchSegment swSeg = (SketchSegment)obj;
                    int segType = swSeg.GetType();

                    // A. 直线
                    if (segType == (int)swSketchSegments_e.swSketchLINE)
                    {
                        SketchLine swLine = (SketchLine)swSeg;
                        if (!IsConstructionEntity(swLine))
                        {
                            SketchPoint? s = (SketchPoint?)swLine.GetStartPoint2();
                            SketchPoint? e = (SketchPoint?)swLine.GetEndPoint2();
                            tokens.Add($"Line(S:[{R(s?.X)},{R(s?.Y)}];E:[{R(e?.X)},{R(e?.Y)}])");
                        }
                    }
                    // B. 圆弧/圆
                    else if (segType == (int)swSketchSegments_e.swSketchARC)
                    {
                        SketchArc swArc = (SketchArc)swSeg;
                        SketchPoint? c = (SketchPoint?)swArc.GetCenterPoint2();
                        double radius = swArc.GetRadius() * 1000;
                        string shapeLabel = swArc.IsCircle() == 1 ? "Circle" : "Arc";
                        tokens.Add($"{shapeLabel}(C:[{R(c?.X)},{R(c?.Y)}];R:{R(radius)})");
                    }
                    // C. 样条曲线 (Spline)
                    else if (segType == (int)swSketchSegments_e.swSketchSPLINE)
                    {
                        SketchSpline swSpline = (SketchSpline)swSeg;
                        int pointCount = swSpline.GetPointCount();
                        tokens.Add($"Spline(PointsCount:{pointCount})");
                    }
                    // D. 椭圆
                    else if (segType == (int)swSketchSegments_e.swSketchELLIPSE)
                    {
                        SketchEllipse swEllipse = (SketchEllipse)swSeg;
                        SketchPoint? c = (SketchPoint?)swEllipse.GetCenterPoint2();
                        tokens.Add($"Ellipse(C:[{R(c?.X)},{R(c?.Y)}])");
                    }
                    // E. 抛物线
                    else if (segType == (int)swSketchSegments_e.swSketchPARABOLA)
                    {
                        tokens.Add("Parabola()");
                    }
                }

                return tokens.Count > 0 ? $"Sketch([{string.Join(" | ", tokens)}])" : "Sketch(FilterOut)";
            }
            catch (Exception ex)
            {
                return $"Sketch(Error:{ex.Message})";
            }
        }

        static bool IsConstructionEntity(SketchLine line)
        {
            try
            {
                return (bool)line.GetType().InvokeMember("IsConstruction",
                    System.Reflection.BindingFlags.InvokeMethod, null, line, null)!;
            }
            catch { return false; }
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
                        if (pt != "RefPlane" && pt != "OriginProfileFeature" && pt != "RefAxis" && pt != "RefPoint" && pt != "CoordSys")
                        {
                            parents.Add(pf.Name);
                        }
                    }
                }
            }
            catch { }
            return parents;
        }

        // ══════════════════════════════════════════════
        //  全命令矩阵参数降维
        // ══════════════════════════════════════════════
        static string CompressFeature(Feature swFeature, ModelDoc2 swModel)
        {
            string featType = swFeature.GetTypeName2();
            try
            {
                // ── 拉伸族 ──
                if (featType == "BaseExtrude" || featType == "Extrude" || featType == "BossExtrude" || featType == "CutExtrude")
                {
                    IExtrudeFeatureData2? extData = (IExtrudeFeatureData2?)swFeature.GetDefinition();
                    if (extData != null)
                    {
                        extData.AccessSelections(swModel, null);
                        double depth = Math.Round(extData.GetDepth(true) * 1000, 1);
                        string op = featType == "CutExtrude" ? "CutExtrude" : "Extrude";
                        string dsl = $"{op}(D:{depth}";
                        try
                        {
                            var m = extData.GetType().GetMethod("GetEndCondition2");
                            if (m != null)
                            {
                                int etv = (int)m.Invoke(extData, new object[] { false })!;
                                dsl += $", End:{EndTypeLabel(etv)}";
                            }
                        }
                        catch { }
                        // Draft angle (reflection fallback)
                        try
                        {
                            var draftProp = extData.GetType().GetProperty("DraftWhileExtruding");
                            if (draftProp != null && (bool)draftProp.GetValue(extData)!)
                            {
                                var draftAng = extData.GetType().GetProperty("DraftAngle");
                                if (draftAng != null)
                                {
                                    double dA = (double)draftAng.GetValue(extData)!;
                                    dsl += $", Draft:{Math.Round(dA * (180.0 / Math.PI), 1)}°";
                                }
                            }
                        }
                        catch { }
                        dsl += ")";
                        extData.ReleaseSelectionAccess();
                        return dsl;
                    }
                }

                // ── 旋转族 ──
                if (featType == "BaseRevolve" || featType == "Revolve" || featType == "CutRevolve" || featType == "Revolved")
                {
                    IRevolveFeatureData2? revData = (IRevolveFeatureData2?)swFeature.GetDefinition();
                    if (revData != null)
                    {
                        revData.AccessSelections(swModel, null);
                        double angle = 360;
                        try
                        {
                            var m = revData.GetType().GetMethod("GetRotationAngle");
                            if (m != null)
                                angle = Math.Round((double)m.Invoke(revData, new object[] { true })! * 180.0 / Math.PI, 1);
                        }
                        catch { }
                        string op = featType == "CutRevolve" ? "CutRevolve" : "Revolve";
                        string dsl = $"{op}(Angle:{angle}°)";
                        revData.ReleaseSelectionAccess();
                        return dsl;
                    }
                }

                // ── 扫描 ──
                if (featType == "BaseSweep" || featType == "Sweep")
                    return "Sweep(Path+Contour)";

                // ── 放样 ──
                if (featType == "BaseLoft" || featType == "Loft")
                    return "Loft(MultiProfile)";

                // ── 钣金 ──
                if (featType == "SMBaseFlange")
                {
                    IBaseFlangeFeatureData? bfData = (IBaseFlangeFeatureData?)swFeature.GetDefinition();
                    if (bfData != null)
                    {
                        bfData.AccessSelections(swModel, null);
                        double thick = Math.Round(bfData.Thickness * 1000, 2);
                        try
                        {
                            var d1Prop = bfData.GetType().GetProperty("D1Thickness");
                            if (d1Prop != null)
                            {
                                double depth = Math.Round((double)d1Prop.GetValue(bfData)! * 1000, 1);
                                bfData.ReleaseSelectionAccess();
                                return $"SheetMetalBase(Thick:{thick}, D:{depth})";
                            }
                        }
                        catch { }
                        bfData.ReleaseSelectionAccess();
                        return $"SheetMetalBase(Thick:{thick})";
                    }
                }
                if (featType == "EdgeFlange")
                {
                    IEdgeFlangeFeatureData? efData = (IEdgeFlangeFeatureData?)swFeature.GetDefinition();
                    if (efData != null)
                    {
                        efData.AccessSelections(swModel, null);
                        try
                        {
                            var lenProp = efData.GetType().GetProperty("Length");
                            var angProp = efData.GetType().GetProperty("Angle");
                            double length = lenProp != null ? Math.Round((double)lenProp.GetValue(efData)! * 1000, 1) : 0;
                            double angle = angProp != null ? Math.Round((double)angProp.GetValue(efData)! * (180.0 / Math.PI), 1) : 90;
                            efData.ReleaseSelectionAccess();
                            return $"EdgeFlange(L:{length}, A:{angle}°)";
                        }
                        catch
                        {
                            efData.ReleaseSelectionAccess();
                            return "EdgeFlange()";
                        }
                    }
                }

                // ── 圆角 ──
                if (featType == "Fillet")
                {
                    ISimpleFilletFeatureData2? filletData = (ISimpleFilletFeatureData2?)swFeature.GetDefinition();
                    if (filletData != null)
                    {
                        filletData.AccessSelections(swModel, null);
                        double radius = Math.Round(filletData.DefaultRadius * 1000, 1);
                        filletData.ReleaseSelectionAccess();
                        return $"Fillet(R:{radius})";
                    }
                }

                // ── 倒角 ──
                if (featType == "Chamfer")
                    return "Chamfer()";

                // ── 抽壳 ──
                if (featType == "Shell")
                    return "Shell()";

                // ── 异形孔向导 ──
                if (featType == "HoleWzd")
                    return "HoleWizard()";

                // ── 阵列/镜像 ──
                if (featType == "LinearPattern")
                    return "LinearPattern()";
                if (featType == "CircularPattern")
                    return "CircularPattern()";
                if (featType == "MirrorFeature" || featType == "MirrorPattern" || featType == "MirrorBody")
                    return "Mirror()";
                if (featType == "SketchPattern" || featType == "SketchDrivenPattern")
                    return "SketchPattern()";

                // ── 筋/圆顶 ──
                if (featType == "Rib")
                    return "Rib()";
                if (featType == "Dome")
                    return "Dome()";

                // ── 简单孔 ──
                if (featType == "Hole" || featType == "SimpleHole")
                    return "SimpleHole()";

                // ── 参考几何 ──
                if (featType == "RefPlane")
                    return "RefPlane()";
            }
            catch
            {
                return $"{featType}(QueryFallback)";
            }
            return $"{featType}()";
        }

        // ══════════════════════════════════════════════
        //  设计特征过滤
        // ══════════════════════════════════════════════
        static bool IsDesignFeature(string typeName)
        {
            string[] excludes =
            {
                "MaterialFolder", "MeshFolder", "AmbientLight", "DirectionalLight", "FavoriteFolder", "HistoryFolder",
                "SelectionSetFolder", "SensorFolder", "DocsFolder", "DetailCabinet", "InkMarkupFolder", "EnvFolder",
                "SolidBodyFolder", "SurfaceBodyFolder", "CommentsFolder", "EqnFolder", "Attribute", "CoordSys",
                "SpotLight", "PointLight", "PlaneFolder", "AxisFolder", "CurvesFolder", "AnnotationsFolder",
                "CutListFolder", "WeldmentsFolder", "BodiesFolder"
            };
            return Array.IndexOf(excludes, typeName) == -1;
        }

        static string EndTypeLabel(int et)
        {
            return et switch
            {
                0 => "Blind",
                1 => "ThroughAll",
                2 => "ThroughNext",
                3 => "UpToVertex",
                4 => "UpToSurface",
                5 => "OffsetFromSurface",
                6 => "MidPlane",
                7 => "UpToBody",
                _ => $"Unknown({et})"
            };
        }
    }
}
