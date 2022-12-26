﻿using System;
using PLATEAU.Interop;
using PLATEAU.Network;

namespace PLATEAU.Dataset
{
    public class DatasetSource : PInvokeDisposable
    {
        public DatasetSource(IntPtr handle) : base(handle)
        {
        }
        
        /// <summary>
        /// <see cref="DatasetSource"/> を生成します。
        /// </summary>
        /// <param name="isServer">データの場所は true ならサーバー、falseならローカルです。</param>
        /// <param name="localSourcePath">ローカルモードでのみ利用します。インポート元のパスを渡します。</param>
        /// <param name="serverDatasetID">
        /// サーバーモードでのみ利用します。データセットのIDを渡します。
        /// そのIDとは、APIサーバーにデータセットの一覧を問い合わせたときに得られるID文字列です。例: 東京23区のデータセットのIDは "23ku"
        /// </param>
        public static DatasetSource Create(bool isServer, string localSourcePath, string serverDatasetID)
        {
            return Create(new DatasetSourceConfig(isServer, localSourcePath, serverDatasetID));
        }
        
        public static DatasetSource Create(DatasetSourceConfig config)
        {
            switch (config.IsServer)
            {
                case true:
                    return CreateServer(config.ServerDatasetID);
                case false:
                    return CreateLocal(config.LocalSourcePath);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
         /// ローカルPCのデータセットを指す <see cref="DatasetSource"/> を作ります。
         /// </summary>
        private static DatasetSource CreateLocal(string path)
         {
             var pathUtf8 = DLLUtil.StrToUtf8Bytes(path);
             var result = NativeMethods.plateau_create_dataset_source_local(out var datasetSourcePtr, pathUtf8);
             DLLUtil.CheckDllError(result);
             return new DatasetSource(datasetSourcePtr);
         }

         /// <summary>
         /// リモートPCのデータセットを指す <see cref="DatasetSource"/> を作ります。
         /// </summary>
         private static DatasetSource CreateServer(string datasetID)
         {
             Client client = Client.Create();
             var result = NativeMethods.plateau_create_dataset_source_server(
                 out var ptr, datasetID, client.Handle);
             DLLUtil.CheckDllError(result);
             return new DatasetSource(ptr);
         }

         public DatasetAccessor Accessor
         {
             get
             {
                 ThrowIfDisposed();
                 var result = NativeMethods.plateau_dataset_source_get_accessor(
                     Handle, out var accessorPtr);
                 DLLUtil.CheckDllError(result);
                 return new DatasetAccessor(accessorPtr);
             }
         }

        protected override void DisposeNative()
        {
            var result = NativeMethods.plateau_delete_dataset_source(Handle);
            DLLUtil.CheckDllError(result);
        }
    }
}