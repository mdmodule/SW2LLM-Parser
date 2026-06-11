# 馃彮 SW2LLM-Parser 鈥?SolidWorks 鈫?LLM 璁粌璇枡鎻愬彇寮曟搸 (v6.0 Super Pipeline)

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue?logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![SolidWorks](https://img.shields.io/badge/SolidWorks-2016%2B-red?logo=dassault-systemes)](https://www.solidworks.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](./LICENSE)

> 灏?SolidWorks 闆堕儴浠朵笌瑁呴厤浣撶殑鍑犱綍鐗瑰緛鏍戙€侀珮绾ц崏鍥剧畻瀛愩€佺┖闂磋閰嶆嫇鎵戙€佷互鍙婅嚜瀹氫箟灞炴€ц〃锛?*鍏ㄨ嚜鍔ㄩ潤榛樻彁鍙?*骞跺帇缂╀负 LLM 鍙洿鎺ュ井璋冪殑 DSL 璇枡锛岃緭鍑烘爣鍑嗗伐涓氱骇 JSON 鏁版嵁闆嗐€?
---

## 馃幆 涓€鍙ヨ瘽瑙ｅ喅浠€涔堥棶棰?
浼犵粺鏈烘鍒堕€犱笌璁捐浼佷笟绉疮浜嗘垚鍗冧笂涓囩殑 `.sldprt` 闆朵欢鍜?`.sldasm` 瑁呴厤浣擄紝鎯宠鍠傜粰 AI 鍋氳璁℃帹鐞嗐€佺粨鏋勫鏌ャ€佸弬鏁版帹鑽愭垨鑷姩鍑哄浘锛屼絾鎵嬫悡鏍囨敞鏍规湰鏄笉鍙兘鐨勪换鍔°€?
**SW2LLM-Parser v6.0 灏辨槸杩欐潯鏁版嵁绠￠亾鐨勮嚜鍔ㄥ寲瓒呯骇寮曟搸**锛氶€変釜鏂囦欢澶?鈫?鑷姩娴佸紡瑙ｆ瀯鍙屾ā鍥剧焊 鈫?姒ㄥ共鐗瑰緛鏍?鑽夊浘/瑁呴厤鎷撴墤/灞炴€ц〃 鈫?鐩存帴涓嬬洏杈撳嚭 AI 璁粌 JSON銆傛暣涓繃绋嬪叏闈欓粯杩愯锛屼笖缁忚繃娴烽噺鍥剧焊鍘嬪姏娴嬭瘯锛屼笉鐣欏瓨鍐呭瓨锛屼笉骞叉壈浣犳墜澶寸殑姝ｅ父璁捐宸ヤ綔銆?
---

## 鉁?鏍稿績鑳藉姏

| 鑳藉姏鏍囩 | 鏍稿績鎶€鏈鏄?| v6.0 鍗囩骇鐗规€?|
|----------|-------------|--------------|
| 馃殌 **闆跺唴瀛樻祦寮忕洿鍐?* | 鏀惧純鍐呭瓨鎷兼帴锛屾敼鐢ㄥ叏娴佸紡纭洏鐩村啓閫氶亾锛圫treaming JsonWriter锛夈€?| **[鍏ㄦ柊]** 绾垫繁閬嶅巻 10涓? 绾у法鍨嬪浘绾稿簱锛岃繘绋嬪唴瀛樺缁堢ǔ濡傜嫍锛屽交搴曟牴闄?OOM 鐖嗘爤銆?|
| 馃 **鍙屾ā鎬佸崗鍚屽鐞嗗櫒** | 娣卞害鏀寔 `.sldprt`锛堥浂浠讹級涓?`.sldasm`锛堣閰嶄綋锛夊弻鍚戞绱€?| **[鍏ㄦ柊]** 涓嶄粎鑳芥姄鍗曚綋鐗瑰緛锛岃繕鑳借В鏋勮閰嶄綋鐨?BOM 灞傜骇銆佸浐瀹氱姸鎬佷笌绌洪棿榻愭鍙樻崲鐭╅樀銆?|
| 馃К **鍏ㄨ绱犲嚑浣曡В鏋?* | 鏅鸿兘鎷変几銆佹棆杞€佹壂鎻忋€佹斁鏍枫€侀挘閲戙€佹洸闈€佸渾瑙掋€佸€掕銆侀樀鍒椼€侀暅鍍忋€佸瓟鐗瑰緛鎻愬彇銆?| **[浼樺寲]** 鑽夊浘寮曟搸鍏ㄩ潰琛ラ綈鏍锋潯鏇茬嚎锛圫plines锛夈€佸渾锛圕ircles锛夈€佹き鍦嗭紙Ellipses锛夛紝瀹岀編杩樺師澶嶆潅鏇查潰楠ㄦ灦銆?|
| 鈿?**鍒嗙骇鎯版€?GC** | 鎽掑純鍗曚欢楂橀闃诲寮忓瀮鍦惧洖鏀讹紝寮曞叆姣?50 浠跺垎鎵规儼鎬т富鍔ㄥ洖鏀躲€?| **[浼樺寲]** 褰诲簳閲婃斁 COM 缁戝畾鐨勫悓鏃讹紝娴佹按绾垮悶鍚愰€熷害椋欏崌 3~5 鍊嶃€?|
| 馃搵 **鍙岃建灞炴€х煩闃?* | 閬嶅巻涓诲睘鎬т笌鐗瑰畾閰嶇疆灞炴€э紝淇濈暀 **琛ㄨ揪寮?* 涓?**璇勪及鍊?* 鍙岃建鏁版嵁銆?| 鏂逛究澶фā鍨嬪涔犺濡?"Material 鈫?304涓嶉攬閽? 鎴栭噸閲?鍥惧彿鐨勬槧灏勯€昏緫銆?|
| 馃搧 **OS 鍘熺敓璺緞瀹夊叏** | 浣跨敤 .NET 鐜颁唬璺緞绠楀瓙璁＄畻 `relative_path`銆?| 鍏嶇柅鐩樼銆佹枩鏉犲強澶у皬鍐欐薄鏌擄紝淇濋殰澶фā鍨嬭祫浜у垎绫绘爣绛剧殑缁濆绾噣銆?|
| 馃か **鐪熉烽潤榛樻矙鐩掓ā寮?* | 闅愬紡鎸傞挬宸插瓨鍦ㄧ殑 SW 瀹炰緥鎴栧紑鍚潤榛樻矙鐩掓牳蹇冦€?| 瑙嗙獥涓嶅脊銆佷笉闂€佷笉鎶㈢劍鐐癸紝璁╂暟鎹竻娲楀湪鍚庡彴鏃犳劅瀹屾垚銆?|

---

## 馃搳 杈撳嚭 Schema (v6.0 鍔ㄦ€佹祦寮忔爣鍑?

```json
{
  "root_folder": "E:\\HL_Office_Furniture_Lib\\\\",
  "execution_time": "2026-06-10 17:45:00",
  "master_dataset": [
    {
      "file_name": "浜轰綋宸ュ鑳屾澘.sldprt",
      "relative_path": "缁撴瀯浠禱\鍧愰潬缁勪欢\\浜轰綋宸ュ鑳屾澘.sldprt",
      "asset_type": "swDocPART",
      "custom_properties": {
        "Material": { "expression": "\"PA66+30GF\"", "value": "灏奸緳鐜荤氦" },
        "Designer": { "expression": "\"Abel\"", "value": "Abel" }
      },
      "feature_tree": [
        {
          "feature": "鑽夊浘1",
          "type": "ProfileFeature",
          "depends_on": [],
          "code": "Sketch([Line(S:[0,0];E:[120.5,0]) | Spline(PointsCount:12) | Arc(C:[50,60];R:25.5)])"
        },
        {
          "feature": "鍑稿彴-鎷変几1",
          "type": "BaseExtrude",
          "depends_on": ["鑽夊浘1"],
          "code": "Extrude(D:12.5, End:Blind)"
        }
      ]
    },
    {
      "file_name": "鍔炲叕妞呭簳鐩樻€绘垚.sldasm",
      "relative_path": "瑁呴厤浣揬\鎬绘垚\\鍔炲叕妞呭簳鐩樻€绘垚.sldasm",
      "asset_type": "swDocASSEMBLY",
      "custom_properties": {
        "BOM_Level": { "expression": "\"A\"", "value": "A" }
      },
      "assembly_topology": [
        {
          "component": "搴曞骇鏀灦-1",
          "path": "搴曞骇鏀灦.sldprt",
          "state": "Fixed",
          "transform": "Identity"
        },
        {
          "component": "姘斿帇妫?1",
          "path": "涓夌骇姘斿帇妫?sldprt",
          "state": "Float",
          "transform": "Pos:[0,0,150];Rot:[1,0,0|0,1,0]"
        }
      ]
    }
  ]
}
```

---

## 馃洜锔?蹇€熷紑濮?
### 1. 鐜渚濊禆

- Windows 10 / 11 x64
- .NET 10.0 SDK 鎴栨洿楂樼増鏈?- SolidWorks 2016 鎴栨洿楂樼増鏈紙闇€姝ｅ父婵€娲?API 鎺ュ彛锛?- Visual Studio 2022 鎴?`dotnet` CLI

### 2. 鍏嬮殕涓庣紪璇?
```bash
git clone https://github.com/mdmodule/SW2LLM-Parser.git
cd SW2LLM-Parser
dotnet publish -c Release -r win-x64 -o publish
```

### 3. 杩愯娓呮礂娴佹按绾?
**鏂瑰紡 A锛堟帹鑽愶級**锛氱洿鎺ュ墠寰€ [Releases](https://github.com/mdmodule/SW2LLM-Parser/releases) 涓嬭浇鏈€鏂扮紪璇戝ソ鐨?`SwFeatureExporter.exe`锛屽弻鍑昏繍琛屻€?
**鏂瑰紡 B锛堟簮鐮佽繍琛岋級**锛?
```bash
.\publish\SwFeatureExporter.exe
```

杩愯鍚庝細寮瑰嚭鍘熺敓鏂囦欢澶归€夋嫨妗嗭紝閫夋嫨浣犵殑鍥剧焊鏍圭洰褰曪紝寮曟搸鍗冲埢寮€鍚弻妯℃祦寮忕閬撱€傛竻娲楀畬鎴愬悗锛屼細鍦ㄨ鐩綍涓嬬敓鎴愪竴浠藉甫鏈夋椂闂存埑鐨?`STREAM_DATASET_*.json` 瓒呯骇鏁版嵁闆嗐€?
---

## 馃П 椤圭洰缁撴瀯

```plaintext
SW2LLM-Parser/
鈹溾攢鈹€ Program.cs                  # 鏍稿績娴佸紡绠￠亾浠ｇ爜锛堝崟鏂囦欢楂樺唴鑱氾紝渚夸簬缁存姢锛?鈹溾攢鈹€ SwFeatureExporter.csproj    # .NET 10.0 + SolidWorks COM 渚濊禆閰嶇疆
鈹溾攢鈹€ publish/                    # 缂栬瘧鍒嗗彂浜х墿鐩綍锛堝凡蹇界暐锛?鈹溾攢鈹€ README.md                   # 椤圭洰璇存槑鏂囨。
鈹斺攢鈹€ .gitignore                  # 鏁版嵁瀹夊叏闃叉姢缃?```

---

## 馃敀 鍟嗕笟瀹夊叏涓庢暟鎹悎瑙?
鏈粨搴撲弗鏍奸伒寰?**"鏍稿績鎺у埗閾惧紑婧愶紝鏁忔劅涓氬姟鏁版嵁闅旂"** 鐨勫伐涓氬畨鍏ㄥ師鍒欙細

- `bin/`銆乣obj/`銆乣.vs/` 绛夌幆澧冪紦瀛樺凡琚?`.gitignore` 涓ュ瘑鎷︽埅銆?- **瀹夊叏閿佸簳绾?*锛氭墍鏈変互 `.json` 缁撳熬鐨勬暟鎹泦鏂囦欢锛堝寘鎷敓鎴愮殑 `STREAM_DATASET_*.json`锛夊凡鍦?`.gitignore` 涓寮哄姏灏侀攣锛岀粷涓嶄細闅忕潃浠ｇ爜鎻愪氦涓嶅皬蹇冭鎺ㄨ嚦鍏紑浠撳簱銆?- 鈿狅笍 璇峰悇浼佷笟寮€鍙戜汉鍛樻敞鎰忥紝鍒囧嬁鎵嬪姩寮鸿灏嗗寘鍚晢涓氭満瀵嗛浂閮ㄤ欢鍑犱綍鍙傛暟鐨勬暟鎹泦鎻愪氦鍒?GitHub 鍏紑缃戠粶銆?
---

## 馃搫 License

MIT License 漏 2026 mdmodule

---

*Applying first-principles thinking to bridge Mechanical Engineering & Generative AI.*
