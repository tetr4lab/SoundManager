# シンプルなサウンドマネージャー (unity)
tags: Unity C# uGUI

# 前提
- Unity 2019.4.5f1
- 使用に際して、C#でスクリプトを書く必要があります。
- UIの操作音など、音源の位置が画面に固定されている場合に適しています。

# できること
- BGMとSEの番号を指定して再生できます。
    - BGM
        - 最大同時再生数は2で固定です。(クロスフェード用)
        - あらかじめ、インスペクタで、フェードイン、フェードアウト、インターバルの時間を指定します。
        - 再生はループします。
    - SE
        - あらかじめ、インスペクタで、最大同時再生数を指定します。
        - 再生時に、「同じ音でも重ねて鳴らす」、「同じ音が鳴っていたら止めてから鳴らす」、「同じ音が鳴っていない場合だけ鳴らす」ことが選択可能です。
        - 再生はループしません。
- BGMとSEの音量を独立して設定できます。
- 全体の一時的なミュートが可能です。

# アセットの入手 (GitHub)
ダウンロード ⇒ [SoundManager.unitypackage](https://github.com/tetr4lab/SoundManager/raw/master/SoundManager.unitypackage)
[ソースはこちらです。](https://github.com/tetr4lab/SoundManager)

# 使い方
### 準備
- プロジェクトにアセットをインポートしてください。
- シーンの適当なオブジェクトに、スクリプト`Sound.cs`をアタッチしてください。
- インスペクタで、`Sound Effect Clip`と`Sound Music Clip`のSizeを必要なだけ増やし、オーディオクリップを設定してください。
- 必要に応じてパラメータを調整してください。

![コメント 2020-07-26 214625.png](https://qiita-image-store.s3.ap-northeast-1.amazonaws.com/0/365845/18d361cc-4516-56f0-2839-09e6a6e96ba2.png)

|項目|説明|初期値|
|:---|:---|---:|
|Sound Effect Max|SE同時再生数|5|
|Sound Effect Initial Volume|SE初期音量|0.5|
|Sound Music Initial Volume|BGM初期音量|0.5|
|Sound Music Fade In Time|BGMフェードイン時間|0|
|Sound Music Fade Out Time|BGMフェードアウト時間|3|
|Sound Music Interval Time|BGMインターバル時間 (フェードアウト時間が`0`でなく負数なら重なる)|0|
|Sound Effect Clip|SEオーディオクリップ|-|
|Sound Music Clip|BGMオーディオクリップ|-|

### SEを再生する
#### 同じSEでも重ねて鳴らす
```cs:
Sound.Effect = number;
```

#### 同じSEが鳴っていたら止めてから鳴らす
```cs:
Sound.StopAndEffect = number;
```

#### 同じSEが鳴っていない場合だけ鳴らす
```cs:
Sound.EffectIfNot = number;
```

#### 最後に再生中の音番号を得る
```cs:
var number = Sound.Effect;
```

#### 指定したSEを止める
```cs:
Sound.EffectStop = number;
```

#### 全てのSEを止める
```cs:
Sound.Effect = Sound.Silent;
```

#### SE音量を設定する
```cs:
Sound.EffectVolume = volume;
```

#### 登録されているSE数を得る
```cs:
var count = Sound.EffectCount;
```

### BGMを再生する
```cs:
Sound.Music = number;
```

#### 再生中の曲番号を得る
```cs:
var number = Sound.Music;
// (Sound.Music == Sound.Silent) であれば何も再生していない
```

#### BGM音量を設定する
```cs:
Sound.MusicVolume = 0.5f;
```

#### 登録されているBGM数を得る
```cs:
var count = Sound.MusicCount;
```

#### BGM再生を止める
```cs:
Sound.Music = Sound.Silent;
```

### 一時的に全ての音を消す、戻す
```cs:
Sound.Mute = true; // 一時的に音を消す
Sound.Mute = false; // 音を戻す
```

---

以下の素材を使わせていただきました。
どうもありがとうございました。

- SoundEffects: ©効果音ラボ https://soundeffect-lab.info/
- Music: ©魔王魂 https://maoudamashii.jokersounds.com/
