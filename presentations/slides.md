---
marp: true
theme: default
paginate: true
size: 16:9
style: |
  section {
    font-size: 26px;
  }
  section h1 {
    font-size: 40px;
  }
  section h2 {
    font-size: 32px;
  }
---

# PLATEAUの3D都市データを
# Clusterワールドへ

- PLATEAU SDK for Unity 入門
- CityGML → Unity → Cluster のフローでアップロードできる

---

## PLATEAUとは

- 国土交通省が推進する3D都市モデル整備・活用プロジェクト
- 全国の都市の建物・地形・道路などをオープンデータとして公開
- 高さ・用途などの属性情報を含む3Dの都市データ

---

## データ形式: CityGML

- PLATEAUのデータは「CityGML」(GMLベースの都市モデル標準フォーマット)で提供
- そのままではUnity/Clusterで扱えず、変換が必要
- 変換手段の一つに「FME」(データ変換ツール)があるが、専用ツールの導入・学習コストが必要
- Pythonで変換ロジックを自作する方法もあるが、Cluster開発者(Unity前提)にとっては別途導入・学習コストが必要

---

## PLATEAUをClusterワールドの3Dモデルにする

- PLATEAU SDK for UnityでCityGMLをUnityのメッシュ/マテリアルに変換
- 変換した都市データをUnityシーン上に配置し、Cluster Creator Kit(CCK)と組み合わせ
- Unity上で完結するフローでClusterワールドとして公開できる

---

## このリポジトリでの結果

- PLATEAU SDK for UnityでCityGMLデータをUnity上に読み込めるように整備
- 読み込んだ都市データをCCKと組み合わせ、Clusterワールドの3Dモデルとして利用できることを確認

---

## 詳細な課題①: iOSビルドでのテクスチャ問題

- ClusterワールドはiOS向けにもビルドされる
- Clusterへワールドをアップロードする際、iOSのテクスチャ圧縮設定が漏れてビルドに失敗するケースがある
- この問題を回避するには、PLATEAU SDK for Unity側で一部編集が必要

---

## 詳細な課題②: データのライセンス

- PLATEAUの都市データにはライセンス規約が存在する
- 利用時は出典表示など規約の遵守が必須
- 再配布・商用利用の条件は事前に確認する

---

## 詳細な課題③: 都市データの整備状況への依存

- 実際に建物モデルが存在するか、テクスチャが用意されているかは都市データ次第
- 対象エリアの整備状況にばらつきがあり、インポートしてみるまで期待通りのデータかは分からない

---

## 今後の展望

- 実際の都市の建物や風景をClusterワールドに取り込める
- PLATEAUを活用したハッカソンやコンテストへの応募にトライできる
