# QuakeCityText
震度速報：最大震度を観測した予報区名
 
震源・震度に関する情報：最大震度を観測した市区町村

## 使用方法
起動するだけです。サーバーとの接続が確立されている限り、WebSocket通信により震度情報を受信できます。

> [!WARNING]
> WebSocket接続、起動時のHTTPが成功しなかった・中断された場合、メッセージウィンドウが表示されます。
> 
> その際、表示数に制限がないため、無制限に表示されることがあります。させたくない場合、専用のメッセージウィンドウを自作するか、ウィンドウを表示しないようにしてください。
 
プロジェクトでは、震源に関する情報はスルーしないため、各々で自由にコードを弄ってください。
### 震度配色を変える
shindo_colors.jsonを作り、その中に次のサンプルを貼り付けてください。

```
{
  "-1": "#00000000",
  "10": "#686870",
  "20": "#0084FF",
  "30": "#32B364",
  "40": "#FFE05D",
  "46": "#FEB416",
  "45": "#FEB416",
  "50": "#FF6600",
  "55": "#FF0000",
  "60": "#A00000",
  "70": "#640064"
}
```
色コードは各位自由に変更なさってください。

### 表示時間を変更する
変数は用意しておりません。
 
```Form1.cs```
 
の
  
```pictureBox1.Image = QuakeRenderer.Render(pictureBox1.Size, _cachedProcessed, pfc, DateTime.Now, 7.5);```
 
の4番目の引数の値を変更してください(秒)。

## ビルド環境
* .NET Framework v4.7.2以上
* x64
* IDE: Visual Studio 2022
> pdbは無効です。
 
## 謝辞
ソース：P2PQuake API v2
 
震度配色：JQuake v1.8~

# Lisence
This project is licensed under the MIT License, see the LICENSE.txt file for details

- このアプリを用いたいかなる損害について、製作者は一切の責任を負いません。
