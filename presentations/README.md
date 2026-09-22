# presentations

Marpで作成するスライド用フォルダです。

## セットアップ

- `npm install` を実行(node_modules はgit管理外)

## ビルド

- `npm run build` : `slides.md` から `dist/slides.pdf` を生成
- `npm run build:html` : HTML版を生成
- `npm run watch` : 編集しながらプレビュー

## 図の作成

- 図はdrawioではなくMermaidで作成する
- `diagrams/*.mmd` に記述し、`npm run diagrams` でPNGに変換してから `slides.md` に埋め込む
- 生成物(`dist/`)や `node_modules/` はgit管理しない
