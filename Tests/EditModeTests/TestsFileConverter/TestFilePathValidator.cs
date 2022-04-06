using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;
using PlateauUnitySDK.Editor.FileConverter;
using UnityEngine;

namespace PlateauUnitySDK.Tests.EditModeTests.TestsFileConverter {
    
    public class TestFilePathValidator {

        /// <summary>
        /// 入力ファイル用のパスとして正しいかどうかを判定できるか確認します。
        /// </summary>
        
        // 実在するファイルが与えられたときのテストケース
        #if UNITY_STANDALONE_WIN
        // このコードを動かしているWindows PCであればおそらく存在するであろうファイルのパスを例にとり、存在するファイルが与えられたときに有効判定が出ることをチェックします。
        [TestCase("C:\\Windows\\System32\\input.dll", "dll", true)]
        // 拡張子が合わないときに false になることもチェックします。
        [TestCase("C:\\Windows\\System32\\input.dll", "wrongExtension", false)]
        #else
        [Ignore("Windows以外での実行には対応していないテストです")]
        #endif
        
        // 実在しないファイルが与えられたときのテストケース
        [TestCase("/NotFound/Dummy/Missing.fbx", "fbx", false)]
        public void Test_IsValidInputFilePath(string filePath, string extension, bool expected) {
            LogAssert.ignoreFailingMessages = true;
            Assert.AreEqual(FilePathValidator.IsValidInputFilePath(filePath, extension, false), expected);
        }
        
        
        
        /// <summary>
        /// 出力ファイル用のパスとして正しいかどうかを判定できるか確認します。
        /// </summary>
        
        // 実在するファイルが与えられたときのテストケース
        #if UNITY_STANDALONE_WIN
        // このコードを動かしているWindows PCであればおそらく存在するであろうフォルダ(Program Files)に例にとり、
        // 存在するフォルダでの新規ファイル作成(fbx)を想定したときに有効判定が出ることをチェックします。
        [TestCase("C:\\Program Files\\User_wants_to_save_here.fbx", "fbx", true)]
        // 拡張子が合わないときに false になることもチェックします。
        [TestCase("C:\\Program Files\\User_wants_to_save_here.fbx", "wrongExtension", false)]
        #else
        [Ignore("Windows以外での実行には対応していないテストです")]
        #endif
        
        // 実在しないファイルが与えられたときのテストケース
        [TestCase("/NotFound/Dummy/Missing.fbx", "fbx", false)]
        public void Test_IsValidOutputFilePath(string filePath, string extension, bool expected) {
            LogAssert.ignoreFailingMessages = true;
            Assert.AreEqual(FilePathValidator.IsValidOutputFilePath(filePath, extension), expected);
        }
        
        
        
        /// <summary>
        /// フルパスから Assets で始まるパスへの変換ができることを確認します。
        /// プロジェクトのAssetsフォルダが assetsDir にあると仮定して、 フルパス を変換したら expected になることを確認します。
        /// </summary>
        
        // Windowsのパス表記への対応をチェックします。
        [TestCase("C:/DummyUnityProjects/Assets", "C:\\DummyUnityProjects\\Assets\\FooBar\\FooBarModelFile.fbx", "Assets/FooBar/FooBarModelFile.fbx")]
        // Linuxのパス表記への対応をチェックします。
        [TestCase("/home/linuxUser/DummyUnityProjects/Assets", "/home/linuxUser/DummyUnityProjects/Assets/foobar.obj", "Assets/foobar.obj")]
        // 紛らわしい名前への対応をチェックします。
        [TestCase("Assets/Assets", "Assets/Assets/Assets/Assets", "Assets/Assets/Assets")]
        // 日本語名、絵文字、スペースへの対応をチェックします。
        [TestCase("C:/日本語話者の プロジェクト♪🎶/Assets", "C:/日本語話者の プロジェクト♪🎶/Assets/♪ 🎶.wav", "Assets/♪ 🎶.wav" )]
        
        public void Test_FullPathToAssetsPath_Normal(string assetsDir, string fullPath, string expectedAssetsPath) {
            // 後でAssetsフォルダのパス設定を戻すために覚えておきます。
            string prevDataPath = GetPrivateStaticFieldVal<string>(typeof(FilePathValidator), "unityProjectDataPath");
            
            // Assetsフォルダがこのような場所にあると仮定します。
            SetPrivateStaticFieldVal(assetsDir, typeof(FilePathValidator), "unityProjectDataPath");
            
            // テストケースをチェックします。 
            Assert.AreEqual(expectedAssetsPath, FilePathValidator.FullPathToAssetsPath(fullPath));
            
            // Assetsフォルダの設定を戻します。
            SetPrivateStaticFieldVal(prevDataPath, typeof(FilePathValidator), "unityProjectDataPath");
        }

        // フルパスからアセットパス変換で、Assetsフォルダの外が指定されたときに例外を出すことを確認します。
        [Test] 
        public void Test_FullPathToAssetsPath_Error() {
            Assert.That(()=> {
                    FilePathValidator.FullPathToAssetsPath("C:\\dummy\\OutsideAssets\\a.fbx");
                },
                Throws.TypeOf<IOException>());
        }


        private static void SetPrivateStaticFieldVal<TField>(TField newFieldValue, Type targetType, string fieldName) {
            var fieldInfo = GetPrivateStaticFieldInfo(targetType, fieldName); 
            if (fieldInfo == null) {
                Debug.LogError($"Reflection failed. Field '{fieldName}' is not found.");
                return;
            }
            fieldInfo.SetValue(null, newFieldValue);
        }

        private static TField GetPrivateStaticFieldVal<TField>(Type targetType, string fieldName) {
            var fieldInfo = GetPrivateStaticFieldInfo(targetType, fieldName);
            if (fieldInfo == null) {
                Debug.LogError($"Reflection failed. Field '{fieldName}' is not found.");
                return default;
            }
            return (TField)fieldInfo.GetValue(null);
        }

        private static FieldInfo GetPrivateStaticFieldInfo(Type targetType, string fieldName) {
            return targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        }
    }
}