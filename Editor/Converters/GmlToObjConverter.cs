using System;
using System.IO;
using System.Threading;
using PLATEAU.CityGML;
using PLATEAU.Interop;
using PLATEAU.Util;
using PLATEAU.IO;
using UnityEditor;
using UnityEngine;

namespace PLATEAU.Editor.Converters
{
    /// <summary>
    /// gmlファイルをobjファイルに変換します。
    /// 使い方は <see cref="Config"/> をセットしてから <see cref="Convert"/> を呼んで <see cref="Dispose"/> します。
    /// <see cref="Dispose"/> を忘れると、変換後もファイルが使用中となり外部から変更できなくなります。
    /// usingステートメントを使うことで暗黙的に <see cref="Dispose"/> を呼ぶことができます。
    /// </summary>
    internal class GmlToObjConverter : IDisposable
    {
        /// <summary> meshConverter は変換処理をC++のDLLに委譲します。 </summary>
        private readonly MeshConverter meshConverter;

        private GmlToObjConverterConfig config;
        private CitygmlParserParams gmlParserParams;
        private DllLogger dllLogger;

        private int disposed;

        public GmlToObjConverter()
        {
            this.meshConverter = new MeshConverter();
            this.config = new GmlToObjConverterConfig();
            this.gmlParserParams = new CitygmlParserParams();
            ApplyConfig(this.config, ref this.gmlParserParams);
        }

        private void ApplyConfig(GmlToObjConverterConfig conf, ref CitygmlParserParams parserParams)
        {
            var logger = new DllLogger();
            logger.SetLogCallbacks(DllLogCallback.UnityLogCallbacks);
            logger.SetLogLevel(conf.LogLevel);
            this.dllLogger = logger;
            // TODO parserParams の2つの値が強制的に true になるのは分かりにくい。もっといいインターフェイスがあるはず。
            parserParams.Optimize = true; // Unityでは基本的に Optimize は全部 true で良いです。
            parserParams.Tessellate = true; // true でないと、頂点の代わりに LinearRing が生成されてしまい 3Dモデルには不適になります。
            this.meshConverter.Options = conf.DllConvertOption;
        }
        
        /// <summary> コンバートの設定です。setで設定を変えます。 </summary>
        public GmlToObjConverterConfig Config
        {
            set
            {
                this.config = value;
                ApplyConfig(value, ref this.gmlParserParams);
            }
            get => this.config;
        }

        /// <summary>
        /// gmlファイルをロードし、変換してobjファイルを出力します。
        /// 成功時はtrue,失敗時はfalseを返します。
        /// </summary>
        //TODO exportObjFilePath は本来は ファイルパスではなくディレクトリのパスであるべき（いちおうファイルパスでも動くけど、名前は変えられない仕様）
        public bool Convert(string gmlFilePath, string exportObjFilePath)
        {
            return ConvertInner(gmlFilePath, exportObjFilePath, null);
        }

        /// <summary>
        /// gmlファイルのロードを省略し、代わりにロード済みの cityModel を使って変換します。
        /// 成否を bool で返します。
        /// 設定で <see cref="gmlParserParams"/> はロード後は変更できませんが、
        /// meshGranularity, axesConversion の設定は反映されます。
        /// </summary>
        public bool ConvertWithoutLoad(CityModel cityModel, string gmlFilePath, string objDestDirFullPath)
        {
            if (cityModel == null)
            {
                Debug.LogError("cityModel is null.");
                return false;
            }

            return ConvertInner(gmlFilePath, objDestDirFullPath, cityModel);
        }

        /// <summary>
        /// 変換のインナーメソッドです。
        /// 引数の <paramref name="cityModel"/> が null なら gmlファイルをロードして変換します。
        /// null でなければ、ファイルロードを省略して代わりに渡された <see cref="CityModel"/> を変換します。
        /// 成否をboolで返します。
        /// </summary>
        private bool ConvertInner(string gmlFilePath, string exportDirFullPath, CityModel cityModel) 
        {
            if (!IsPathValid(gmlFilePath, exportDirFullPath)) return false;
            try
            {
                SlashPath(ref gmlFilePath);
                SlashPath(ref exportDirFullPath);
                if (!exportDirFullPath.EndsWith("/")) exportDirFullPath += "/";

                cityModel ??= CityGml.Load(gmlFilePath, this.gmlParserParams, DllLogCallback.UnityLogCallbacks, this.config.LogLevel);
                
                // 生成する obj のパスのリスト
                int minLod = this.config.MinLod;
                int maxLod = this.config.MaxLod;
                if (maxLod < minLod) (minLod, maxLod) = (maxLod, minLod);
                string[] objFullPaths = new string[maxLod - minLod + 1];
                for (int l = minLod; l <= maxLod; l++)
                {
                    objFullPaths[l-minLod] = Path.Combine(exportDirFullPath,
                        $"LOD{l}_{Path.GetFileNameWithoutExtension(gmlFilePath)}.obj");
                }

                // 出力先が Assets フォルダ内 かつ すでに同名ファイルが存在する場合、古いファイルを消します。
                // そうしないと上書きによって obj のメッシュ名が変わっても Unity に反映されないことがあるためです。
                if (PathUtil.IsSubDirectoryOfAssets(exportDirFullPath))
                {
                    foreach (var objFullPath in objFullPaths)
                    {
                        string assetPath = PathUtil.FullPathToAssetsPath(objFullPath);
                        AssetDatabase.DeleteAsset(assetPath);
                        AssetDatabase.Refresh();
                    }
                }
                
                // ReferencePointを設定します。
                if (this.config.DoAutoSetReferencePoint)
                {
                    this.meshConverter.Options.SetValidReferencePoint(cityModel);
                    this.meshConverter.Options.ReferencePoint = this.meshConverter.Options.ReferencePoint;
                }
                else
                {
                    var referencePoint = this.config.ManualReferencePoint;
                    if (referencePoint == null)
                        throw new Exception($"{nameof(this.config.ManualReferencePoint)} is null.");
                    this.meshConverter.Options.ReferencePoint = VectorConverter.ToPlateauVector(referencePoint.Value);
                }
                
                // 変換してファイルに書き込みます。
                this.meshConverter.Convert(exportDirFullPath, gmlFilePath, cityModel, this.dllLogger);
                
                // 出力先が Assets フォルダ内なら、それをUnityに反映させます。
                if (PathUtil.IsSubDirectoryOfAssets(exportDirFullPath))
                {
                    foreach (string objFullPath in objFullPaths)
                    {
                        string assetPath = PathUtil.FullPathToAssetsPath(objFullPath);
                        AssetDatabase.ImportAsset(assetPath);
                        AssetDatabase.Refresh();
                    }
                    
                }
            }
            catch (FileLoadException e)
            {
                Debug.LogError($"Failed to load gml file.\n gml path = {gmlFilePath}\n{e}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Gml to obj convert is Failed.\ngml path = {gmlFilePath}\n{e}");
                return false;
            }

            return true;
        }

        private static bool IsPathValid(string gmlFilePath, string exportDirectoryPath)
        {
            if (!PathUtil.IsValidInputFilePath(gmlFilePath, "gml", false)) return false;
            if (!Directory.Exists(exportDirectoryPath)) return false;
            return true;
        }


        /// <summary> objWriterに渡すパスの区切り文字は '/' である必要があるので '/' にします。 </summary>
        private static void SlashPath(ref string path)
        {
            path = path.Replace('\\', '/');
        }

        /// <summary>
        /// <paramref name="cityModel"/> に適する ReferencePointを計算し、
        /// その座標を変換の基準点として設定し、その座標を返します。
        /// </summary>
        public Vector3 SetValidReferencePoint(CityModel cityModel)
        {
            this.meshConverter.Options.SetValidReferencePoint(cityModel);
            var plateauCoord = this.meshConverter.Options.ReferencePoint;
            var coord = VectorConverter.ToUnityVector(plateauCoord);
            this.config.ManualReferencePoint = coord;
            return coord;
        }
        
        /// <summary>
        /// 変換後の obj ファイルを開放したい時に呼びます。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 0)
            {
                this.meshConverter.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        ~GmlToObjConverter()
        {
            Dispose();
        }
    }
}