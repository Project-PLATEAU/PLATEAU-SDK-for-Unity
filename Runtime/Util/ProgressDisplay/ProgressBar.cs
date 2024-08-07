
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PLATEAU.Util
{
    /// <summary>
    /// Editorの場合、<see cref="Display"/>でプログレスバーを表示し、廃棄時にプログレスバーを消します。
    /// Editorでない場合、何もしません。
    /// <see cref="Display"/>にinfo文字列を与えない場合に表示されるデフォルト文字列をコンストラクタで指定できます。
    /// 指定しない場合は空文字列がデフォルトになります。
    /// </summary>
    public class ProgressBar : IProgressBar
    {
        private string defaultInfo;

        public ProgressBar(string defaultInfo)
        {
            this.defaultInfo = defaultInfo;
        }
        
        public ProgressBar() : this(""){}
        
        public void Display(string info, float progress)
        {
            #if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("PLATEAU", info, progress);
            #endif
        }

        public void Display(float progress)
        {
            Display(this.defaultInfo, progress);
        }

        public void Dispose()
        {
            #if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
            #endif
        }
    }

    /// <summary>
    /// 処理の進行状況を表示するためのインターフェイスです。
    /// </summary>
    public interface IProgressBar : IDisposable
    {
        public void Display(string info, float progress);
    }

    /// <summary>
    /// プログレスバーを表示したくない場合に使う、空のプログレスバー実装です。
    /// </summary>
    public class DummyProgressBar : IProgressBar
    {
        public void Display(string info, float progress){}
        public void Dispose(){}
    }
}