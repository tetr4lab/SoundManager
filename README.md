---
title: シンプルなサウンドマネージャー (Unity)
tags: Unity C# uGUI
---

# シンプルなサウンドマネージャー (Unity)

## 前提
- Unity 2020.3.31f1
- 使用に際して、C#でスクリプトを書く必要があります。
- UIの操作音など、音源の位置が画面に固定されている場合に適しています。

## できること
- 音源の内容にかかわらず、ループ再生するものをBGM、しないものをSEとして扱います。
  - 例えば、ファンファーレのような一度だけ再生する楽曲はSEとして扱います。
- SEとBGMの番号を指定して再生できます。
    - SE
        - あらかじめ、インスペクタで、最大同時再生数を指定します。
        - 再生時に、「同じ音でも重ねて鳴らす」、「同じ音が鳴っていたら止めてから鳴らす」、「同じ音が鳴っていない場合だけ鳴らす」ことが選択可能です。
        - 再生はループしません。
    - BGM
        - あらかじめ、インスペクタで、フェードイン、フェードアウト、インターバルの時間を指定します。
          - 最大同時再生数は2で固定です。(クロスフェード用)
        - プレイリストに対応しています。
        - 再生はループします。
- SEとBGMの音量を独立して設定できます。
- 全体の一時的なミュートが可能です。

## 導入と設定
- プロジェクトにアセットをインポートしてください。
- シーンの適当なオブジェクトに、スクリプト`Sound.cs`をアタッチしてください。
- インスペクタで、`Sound Effect Clip`と`Sound Music Clip`のSizeを必要なだけ増やし、オーディオクリップを設定してください。
- 必要に応じてパラメータを調整してください。

![18d361cc-4516-56f0-2839-09e6a6e96ba2](https://user-images.githubusercontent.com/48040768/158546598-c2e6527a-000c-48b4-b05b-f1a4980fc912.png)

|項目|説明|初期値|範囲
|:---|:---|---:|:--:|
|Sound Effect Max|SE同時再生数|5|1\~
|Sound Effect Initial Volume|SE初期音量|0.5|0\~1.0
|Sound Music Initial Volume|BGM初期音量|0.5|0\~1.0
|Sound Music Fade In Time|BGMフェードイン時間|0.0|0\~
|Sound Music Fade Out Time|BGMフェードアウト時間|3.0|0\~
|Sound Music Interval Time|BGMインターバル時間|0.0|\~
|Sound Effect Clip|SEオーディオクリップ|-|-
|Sound Music Clip|BGMオーディオクリップ|-|-

## 使い方
### SEを再生する
#### 同じSEでも重ねて鳴らす
```cs:
Sound.Effect = number;
```

- 空きのチャネル(`AudioSource`)で再生します。
- 空きがない場合は、最も古くに再生開始されたチャネルの再生を止めて使います。

#### 同じSEが鳴っていたら止めてから鳴らす
```cs:
Sound.StopAndEffect = number;
```

- 以下と同じ処理です。

```cs:
Sound.EffectStop = number;
Sound.Effect = number;
```

#### 同じSEが鳴っていない場合だけ鳴らす
```cs:
Sound.EffectIfNot = number;
```

- 既に同じ音を再生中であれば、新たに再生しません。

#### 最後に再生中の効果音番号を得る
```cs:
int number = Sound.Effect;
```

- 何も再生されていない場合は、`Sound.Silent`が得られます。

#### 再生中の効果音番号一覧を得る
```cs:
int [] numbers = Sound.Effects;
```

- 何も再生されていない場合は、空の配列が得られます。

#### 効果音が再生中か検査する
```cs:
if (Sound.IsPlayingEffect) {
```

#### 指定したSEを止める
```cs:
Sound.EffectStop = number;
```

- 複数チャネルで再生している場合は、最も古い再生チャネルだけが止まります。

#### 全てのSEを止める
```cs:
Sound.Effect = Sound.Silent;
// or
Sound.EffectStop = Sound.Silent;
```

#### SE音量を設定する
```cs:
Sound.EffectVolume = volume;
```

- 正規化された値(`0`~`1f`)を設定します。取得もできます。
- 音量を`0`にしても再生は停止しません。

#### 登録されているSE数を得る
```cs:
int count = Sound.EffectCount;
```

### BGMを再生する
```cs:
Sound.Music = number;
```

- インスペクタで設定されたフェードイン、フェードアウト、インターバルの時間(秒)を勘案して曲を再生します。
    - 既に再生中の曲の場合は、単に再生を継続します。
    - 別の曲を再生中の場合は、まず、再生中の曲がフェードアウトします。
        - フェードアウト時間が`0`なら即座に止まります。
        - 負値は指定できません。
    - 次に、インターバル時間だけ、次の再生開始を待機します。
        - 負値の場合は、フェードアウト中に遡って待機を終えます。
        - フェードアウトに先だって待機を終える(フェードアウトの開始を越える負値を指定する)ことはできません。
    - 待機を終えると、指定された曲のフェードインを開始します。
        - フェードイン時間が`0`なら即座に既定音量で再生されます。
        - 負値は指定できません。
    - 再生中でない場合は、即座にフェードインが開始されます。
    - クロスフェード中に新たな再生指示があった場合は、以下の特例処理を行います。
        - 前の(フェードアウト中の)曲が再生指示された場合は、2曲のフェード方向が切り替わります。
        - フェードアウト・イン中のどちらとも異なる第3の曲が再生指示された場合は、再生中だった2曲の内で音量の大きい方をフェードアウトさせ、音量の小さい方は即座に停止して次の曲の開始シーケンスに移行します。
    - 曲はループ再生されます。ループの際に
- 例
    - 全ての時間が`0`なら、即座に切り替わります。
    - 全ての時間が`1`なら、前の曲が1秒でフェードアウト、1秒無音で、次の曲が開始され1秒でフェードインします。
    - フェードインとフェードアウトが`3`でインターバルが`-1`だと、前の曲が3秒でフェードアウトし、その終了1秒前に次の曲が再生を開始して3秒でフェードインします。フェードアウト開始からフェードイン終了までは5秒になります。

#### 再生中の曲番号を得る
```cs:
int number = Sound.Music;
```

- 再生されていない場合は、`Sound.Silent`が得られます。
- プレイリストの再生中であっても、単に曲番号が得られます。

#### 再生中の曲番号一覧を得る
```cs:
int [] numbers = Sound.Musics;
```

- 再生待機中も再生中と見なされます。
- クロスフェード中であれば要素2個、通常の再生中であれば要素1個、何も再生されていない場合は空の配列が得られます。

#### BGMが再生中か検査する
```cs:
if (Sound.IsPlayingMusic) {
```

- 再生待機中も再生中と見なされます。

#### BGMプレイリストを再生する
```cs:
Sound.Playlist = new int [] { number, number1, number2, number3, };
```

- 順に再生し全体を繰り返します。
- フェードアウトは行われません。
- 単一曲の再生中に同一の曲を含むプレイリストを再生した場合は、リストの途中からの開始として曲の再生が継続されます。

#### 再生中のBGMプレイリストを得る
```cs:
int [] playlist = Sound.Playlist;
```

- プレイリストが再生されていない場合は、`null`が得られます。
- 再生中の曲番号を得る場合は、`Sound.Music`を参照します。
  - 再生中のインデックスを得ることはできません。

#### BGM音量を設定する
```cs:
Sound.MusicVolume = 0.5f;
```

- 正規化された値(`0`~`1f`)を設定します。取得もできます。
- 音量を`0`にすると再生が停止します。

#### BGM音量を一時的に変更する
```cs:
MusicTmpVolume = 0.5f;
```

- 無音を最小(`0`)、`MusicVolume`を最大(`1f`)として、正規化された値を設定します。取得もできます。
  - 初期値は`1f`です。
  - `MusicVolume`とは独立した設定で、`MusicVolume`の設定値は影響を受けません。
- 音量を`0`にしても再生は停止しません。
- 一時的にBGMの音量を下げてSEを聴かせ、その後、元の音量に戻すような場合に使用します。

#### 登録されているBGM数を得る
```cs:
int count = Sound.MusicCount;
```

#### BGM再生を止める
```cs:
Sound.Music = Sound.Silent;
```

- フェードアウト時間の設定が`0`でない場合は、フェードアウトします。
- フェードアウト時間が`0`の場合は即座に止まります。

### 一時的に全ての音を消す、戻す
```cs:
Sound.Mute = true; // 一時的に音を消す
Sound.Mute = false; // 音を戻す
```

- 音が出ないだけで、既存の再生は継続しますし、新たな再生も有効です。

### コンポーネントの動的な生成
```cs:
Sound.Attach (GameObject gameObject, int effectMax, float effectInitialVolume, float musicInitialVolume, float musicFadeInTime, float musicFadeOutTime, float musicIntervalTime, ICollection<AudioClip> effectClip, ICollection<AudioClip> musicClip); // 引数に応じて生成
Sound.Attach (GameObject gameObject, Sound origin); // 複製して生成
```

## 謝辞

以下の素材を使わせていただきました。
どうもありがとうございました。

- SoundEffects: ©効果音ラボ https://soundeffect-lab.info/
- Music: ©魔王魂 https://maoudamashii.jokersounds.com/
