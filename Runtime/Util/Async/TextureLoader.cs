using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PLATEAU.Util.Async
{
    /// <summary>
    /// URLから画像をロードしてテクスチャとして返します。
    /// </summary>
    public static class TextureLoader
    {
        /// <summary>
        /// 非同期で url から画像をダウンロードしてテクスチャとして返します。
        /// コルーチンを使うので、メインスレッドで行われる必要があります。
        /// 失敗した場合は null を返します。
        /// </summary>
        public static async Task<Texture2D> LoadAsync(string url, int timeOutSec)
        {
            Uri uri = new Uri(url);
            var request = UnityWebRequestTexture.GetTexture(uri);

            request.timeout = timeOutSec;
            // 注意 :
            // 下の SendWebRequest は、見た目に反してメインスレッドで行われ、Unityのコルーチンによって await します。
            // UnityWebRequestExtension クラスの拡張メソッドにより、コルーチンを await できるようにする機能を使っており、
            // コルーチンはメインスレッド限定です。
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"failed to load texture : {url} result = {(int)request.result}");
                return null;
            }

            var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            return texture;
        }


        /// <summary>
        /// テクスチャのURL（パス） から、PLATEAUのテクスチャを非同期でロードします。
        /// 内容は<see cref="LoadAsync"/>に、PLATEAUテクスチャ用の処理を加えたものになります。
        /// メインスレッドで実行する必要があります。
        /// </summary>
        public static async Task<Texture2D> LoadPlateauTextureAsync(string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath))
                return null;


            Debug.Log($"Loading Texture : {texturePath}");

            // 非同期でテクスチャをロードします。
            var texture = await LoadAsync(texturePath, 3);

            if (texture == null)
                return null;

            // この Compress によってテクスチャ容量が 6分の1 になります。
            // 松山市のLOD2の建物モデルで計測したところ、 テクスチャのメモリ使用量が 2.6GB から 421.3MB になりました。
            // 画質は下がりますが、メモリ使用量を適正にするために必須と思われます。
            var compressedTex = Compress(texture);

            compressedTex.name = Path.GetFileName(texturePath);
            return compressedTex;
        }

        // テクスチャを圧縮します。
        private static Texture2D Compress(Texture2D src)
        {
            // Compressメソッドで圧縮する準備として、幅・高さを4の倍数にする必要があります。
            // 最も近い4の倍数を求めます。
            var widthX4 = (src.width + 2) / 4 * 4;
            var heightX4 = (src.height + 2) / 4 * 4;
            widthX4 = Math.Max(4, widthX4); // 幅・高さが 1ピクセルのケースで 0 に丸められてしまうのを防止します。
            heightX4 = Math.Max(4, heightX4);

            // テクスチャをリサイズします。
            // 参考: https://light11.hatenadiary.com/entry/2018/04/19/194015
            var rt = RenderTexture.GetTemporary(widthX4, heightX4);
            Graphics.Blit(src, rt);
            var prevRt = RenderTexture.active;
            RenderTexture.active = rt;
            var dst = new Texture2D(widthX4, heightX4);
            dst.ReadPixels(new Rect(0, 0, widthX4, heightX4), 0, 0);
            dst.Apply();
            RenderTexture.active = prevRt;
            RenderTexture.ReleaseTemporary(rt);

            // 圧縮のキモです。
            // dst.Compress(true);
            return dst;
        }
    }
}
