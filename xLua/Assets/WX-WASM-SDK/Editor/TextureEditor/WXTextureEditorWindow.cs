using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using LitJson;
using WXTextureMin;
using System.Text;
using System.Linq;

namespace WeChatWASM
{


    public class JSTextureTaskConf
    {
        public string dst;
        public string dataPath;
        public bool useDXT5;
        public List<WXReplaceTextureData> textureList;
    }

    public class JSTextureData {
        public string p;
        public int w;
        public int h;
    }

    public class WXTextureEditorWindow : EditorWindow
    {
        public static WXEditorScriptObject miniGameConf;
        private static TextureManager manager;

        [MenuItem("微信小游戏 / 包体瘦身--压缩纹理")]
        public static void Open()
        {
			miniGameConf = UnityUtil.GetEditorConf();
			var win = GetWindow(typeof(WXTextureEditorWindow), false, "包体瘦身--压缩纹理", true);//创建窗口
			win.minSize = new Vector2(600, 250);
			win.maxSize = new Vector2(600, 250);
			win.Show();
        }

        public static void Log(string type,string msg) {

            if (type == "Error") {
                UnityEngine.Debug.LogError(msg);
            } else if (type == "Log") {
                UnityEngine.Debug.Log(msg);
            } else if (type == "Warn") {
                UnityEngine.Debug.LogWarning(msg);
            }

        }


        public static void CreateJSTask()
        {

            List<WXBundlePicDepsData>  bundlePicDeps = manager.GetBundlePicDeps();
            WXTextureReplacerScriptObject wXTextureReplacerScriptObject = UnityUtil.GetTextureEditorCacheConf();
            List<WXReplaceTextureData> list = new List<WXReplaceTextureData>();

            Dictionary<string, List<WXReplaceTextureData>> cacheMap = new Dictionary<string, List<WXReplaceTextureData>>();
            if (wXTextureReplacerScriptObject.bundlePicDeps == null) {
                wXTextureReplacerScriptObject.bundlePicDeps = new List<WXBundlePicDepsData>();
            }
            foreach (var item in wXTextureReplacerScriptObject.bundlePicDeps) {
                cacheMap.Add(item.bundlePath,item.pics);
            }
            foreach (var item in bundlePicDeps) {
                if (item == null) {
                    continue;
                }
                if (item.isCached) {
                    if (cacheMap.ContainsKey(item.bundlePath)) {
                        item.pics = cacheMap[item.bundlePath];
                    }
                    else
                    {
                        Log("Error","Cache有缺失，请删除webgl-min目录再重新执行处理资源！");
                        break;
                    }
                }
            }

            wXTextureReplacerScriptObject.bundlePicDeps = bundlePicDeps;
            

            foreach (var item in bundlePicDeps) {
                list = list.Union(item.pics).ToList();
            }


            cacheMap.Clear();
            foreach (var item in bundlePicDeps)
            {
                cacheMap.Add(item.bundlePath, item.pics);
            }

            var conf = new JSTextureTaskConf()
            {
                dst = miniGameConf.ProjectConf.DST,
                dataPath = Application.dataPath,
                useDXT5 = miniGameConf.CompressTexture.useDXT5,
                textureList = list,
            };

            EditorUtility.SetDirty(wXTextureReplacerScriptObject);

            File.WriteAllText(Application.dataPath + "/WX-WASM-SDK/Editor/Node/conf.js", "module.exports = " + JsonMapper.ToJson(conf));

            UnityEngine.Debug.LogError("最后一步请安装 Nodejs 然后进入WX-WASM-SDK/Editor/Node 目录用命令行，执行 ’node compress_astc_only.js‘ (开发阶段使用) 或 ’node compress_all.js‘（上线时候使用） 命令来生成纹理。");

            ModifiyJsFile(cacheMap);
        }

        public static void ModifiyJsFile(Dictionary<string, List<WXReplaceTextureData>> picDeps) {
            

            //修改使用纹理dxt
            string content = File.ReadAllText(Path.Combine(Application.dataPath, "WX-WASM-SDK", "wechat-default", "unity-sdk", "texture.js"), Encoding.UTF8);

            content = content.Replace("\"$UseDXT5$\"", miniGameConf.CompressTexture.useDXT5 ? "true" : "false");

            File.WriteAllText(Path.Combine(miniGameConf.ProjectConf.DST, "minigame", "unity-sdk", "texture.js"), content, Encoding.UTF8);

            Dictionary<string, List<JSTextureData>> picDepsShort = new Dictionary<string, List<JSTextureData>>();
            foreach(var item in picDeps)
            {
                if (item.Key != "unity_default_resources")
                {
                    var list = new List<JSTextureData>();
                    foreach (var data in item.Value) {
                        list.Add(new JSTextureData()
                        {
                            h = data.height,
                            w = data.width,
                            p = data.path
                        });
                    }
                    picDepsShort.Add(item.Key,list);
                }
            }

            var textureConfigPath = Path.Combine(miniGameConf.ProjectConf.DST, "minigame", "texture-config.js");

            if (miniGameConf.CompressTexture.parallelWithBundle)
            {

                File.WriteAllText(textureConfigPath, "GameGlobal.USED_TEXTURE_COMPRESSION=true;GameGlobal.TEXTURE_PARALLEL_BUNDLE=true;GameGlobal.TEXTURE_BUNDLES = " + JsonMapper.ToJson(picDepsShort) , Encoding.UTF8);
            }
            else
            {
                File.WriteAllText(textureConfigPath, "GameGlobal.USED_TEXTURE_COMPRESSION=true;GameGlobal.TEXTURE_PARALLEL_BUNDLE=false;GameGlobal.TEXTURE_BUNDLES = ''", Encoding.UTF8);
            }


        }

        

        public static void ReplaceBundle()
        {

            if (string.IsNullOrEmpty(miniGameConf.CompressTexture.bundleSuffix)) {
                UnityEngine.Debug.LogError("bundle后缀不能为空！");
                return;
            }
            if (string.IsNullOrEmpty(miniGameConf.ProjectConf.DST)) {
                UnityEngine.Debug.LogError("请先转换为小游戏！");
                return;
            }
            if (!File.Exists(miniGameConf.ProjectConf.DST+"/webgl/index.html")) {
                UnityEngine.Debug.LogError("请先转换为小游戏！并确保导出目录下存在webgl目录！");
                return;
            }
            UnityEngine.Debug.Log("Start! 【" + System.DateTime.Now.ToString("T") + "】");

            var dstDir = miniGameConf.ProjectConf.DST + "/webgl-min";
            var dstTexturePath = dstDir + "/Assets/Textures";
            var sourceDir = miniGameConf.ProjectConf.DST + "/webgl";

            manager = new TextureManager(new TextureManagerOption()
            {
                bundleSuffix = miniGameConf.CompressTexture.bundleSuffix.Split(';'),
                dstDir = dstDir,
                dstTexturePath = dstTexturePath,
                sourceDir = sourceDir,
                Logger = Log,
                classDataPath = "Assets/WX-WASM-SDK/Editor/TextureEditor/classdata.tpk"
            });

            manager.Init();

            OnReplaceEnd();


        }

        private static void OnReplaceEnd() {

            CreateJSTask();

            if (miniGameConf.ProjectConf.assetLoadType == 1)
            {
                var sourceDataFile = manager.GetUnityDataFile();
                var dstDataFile = miniGameConf.ProjectConf.DST + "/minigame/data-package" + sourceDataFile.Substring(sourceDataFile.LastIndexOf('/'));
                if (File.Exists(dstDataFile))
                {
                    File.Delete(dstDataFile);
                }
                File.Copy(sourceDataFile, dstDataFile);
            }

            UnityEngine.Debug.Log("Done! 【" + System.DateTime.Now.ToString("T") + "】");
        }

        private void OnDisable()
        {
            EditorUtility.SetDirty(miniGameConf);
        }

        private void OnEnable()
        {
            miniGameConf = UnityUtil.GetEditorConf();
        }

        private void OnGUI()
		{

            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.fontSize = 14;

            labelStyle.margin.left = 20;
            labelStyle.margin.top = 10;
            labelStyle.margin.bottom = 10;

            GUILayout.Label("基本设置", labelStyle);

            var inputStyle = new GUIStyle(EditorStyles.textField);
            inputStyle.fontSize = 14;
            inputStyle.margin.left = 20;
            inputStyle.margin.bottom = 10;
            inputStyle.margin.right = 20;

            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.margin.left = 20;
            toggleStyle.margin.right = 20;

            miniGameConf.CompressTexture.bundleSuffix = EditorGUILayout.TextField(new GUIContent("bunlde文件后缀(?)", "多个不同后缀可用;分割开来"),miniGameConf.CompressTexture.bundleSuffix, inputStyle);

            GUILayout.Label(new GUIContent("功能选项(?)", "每次变更了下列选项都需要重新发布小游戏包"), labelStyle);
            GUILayout.BeginHorizontal();

            var labelStyle2 = new GUIStyle(EditorStyles.label);

            labelStyle2.margin.left = 20;
            GUILayout.Label(new GUIContent("支持PC端压缩纹理(?)", "使PC微信也支持压缩纹理，不过会在开发阶段增加纹理生成耗时。"), labelStyle2, GUILayout.Height(22), GUILayout.Width(150));

            miniGameConf.CompressTexture.useDXT5 = GUILayout.Toggle(miniGameConf.CompressTexture.useDXT5, "", GUILayout.Height(22), GUILayout.Width(50));

            GUILayout.Label(new GUIContent("纹理与bundle并行加载(?)", "默认纹理是解析bundle后才加载，勾选后加载bundle时bundle对应纹理就会同时加载。"), labelStyle2, GUILayout.Height(22), GUILayout.Width(150));

            miniGameConf.CompressTexture.parallelWithBundle = GUILayout.Toggle(miniGameConf.CompressTexture.parallelWithBundle, "", GUILayout.Height(22), GUILayout.Width(50));

            GUILayout.EndHorizontal();

            GUILayout.Label("操作", labelStyle);

            GUIStyle pathButtonStyle = new GUIStyle(GUI.skin.button);
            pathButtonStyle.fontSize = 12;
            pathButtonStyle.margin.left = 20;
            pathButtonStyle.margin.right = 20;

            EditorGUILayout.BeginHorizontal();


            var replaceTexture = GUILayout.Button(new GUIContent("处理资源(?)", "处理完成后会在导出目录生成webgl-min目录，bundle文件要换成使用webgl-min目录下的bundle文件，xx.webgl.data.unityweb.bin.txt文件也要换成使用webgl-min目录下对应的文件，注意要将导出目录里面Assets目录下的都上传至CDN对应路径，小游戏里才会显示成正常的压缩纹理。注意bundle文件不能开启crc校验，否则会展示异常。"), pathButtonStyle, GUILayout.Height(40), GUILayout.Width(140));


            var goReadMe = GUILayout.Button(new GUIContent("README"), pathButtonStyle, GUILayout.Height(40), GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();

            if (replaceTexture)
            {
                ReplaceBundle();
            }

            if (goReadMe)
            {
                Application.OpenURL("https://github.com/wechat-miniprogram/minigame-unity-webgl-transform/blob/main/Design/CompressedTexture.md");
            }

        }

	}

}