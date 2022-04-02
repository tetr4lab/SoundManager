//	Copyright© tetr4lab.

using System.Collections;
using System.Collections.Generic;
using System;
using DateTime = System.DateTime;
using UnityEngine;

namespace SoundManager {

	/// <summary>サウンドマネージャー</summary>
	/// 概念
	///   効果音
	///     繰り返さない、制限まで複数同時に再生する
	///   楽曲
	///     繰り返す、同時再生はクロスフェードのみ
	/// 使い方
	///   GameObjectにアタッチしておく
	///     インスペクタで設定を行う
	///     効果音と楽曲のAudioClipを必要なだけ設定する
	///     実行時に初期化される
	///   シーンに複数のインスタンスが存在する場合
	///     最初に初期化されたひとつだけがアクティブになる
	///     アクティブなインスタンスが消滅すると、次のインスタンスがアクティベートされる
	/// 設定
	///   SerializeFieldを参照
	/// 機能
	///   共通
	///     消音
	///   効果音
	///     重ねて再生、止めてから再生、再生していなければ再生、停止、音量設定
	///   楽曲
	///     再生、停止、音量設定 (0にすると再生を停止)、一時的な音量設定
	public class Sound : MonoBehaviour {

		// オブジェクト要素
		/// <summary>効果音同時再生数</summary>
		[SerializeField, Tooltip ("効果音同時再生数")] protected int soundEffectMax = SoundEffectMax;
		/// <summary>効果音初期音量</summary>
		[SerializeField, Tooltip ("効果音初期音量"), Range (MinimumVolume, MaximumVolume)] protected float soundEffectInitialVolume = SoundEffectInitialVolume;
		/// <summary>楽曲音初期音量</summary>
		[SerializeField, Tooltip ("楽曲音初期音量"), Range (MinimumVolume, MaximumVolume)] protected float soundMusicInitialVolume = SoundMusicInitialVolume;
		/// <summary>楽曲音フェードイン時間</summary>
		[SerializeField, Tooltip ("楽曲音フェードイン時間")] protected float soundMusicFadeInTime = SoundMusicFadeInTime;
		/// <summary>楽曲音フェードアウト時間</summary>
		[SerializeField, Tooltip ("楽曲音フェードアウト時間")] protected float soundMusicFadeOutTime = SoundMusicFadeOutTime;
		/// <summary>楽曲音インターバル時間 (フェードアウトありで負数ならクロスフェード)</summary>
		[SerializeField, Tooltip ("楽曲音インターバル時間 (フェードアウトありで負数ならクロスフェード)")] protected float soundMusicIntervalTime = SoundMusicIntervalTime;
		/// <summary>効果音クリップ</summary>
		[SerializeField, Tooltip ("効果音")] protected AudioClip [] soundEffectClip = null;
		/// <summary>楽曲音クリップ</summary>
		[SerializeField, Tooltip ("楽曲音")] protected AudioClip [] soundMusicClip = null;

		// 定数
		/// <summary>効果音同時再生数</summary>
		protected const int SoundEffectMax = 5;
		/// <summary>効果音初期音量</summary>
		protected const float SoundEffectInitialVolume = 0.5f;
		/// <summary>楽曲音初期音量</summary>
		protected const float SoundMusicInitialVolume = 0.5f;
		/// <summary>楽曲音フェードイン時間</summary>
		protected const float SoundMusicFadeInTime = 0f;
		/// <summary>楽曲音フェードアウト時間</summary>
		protected const float SoundMusicFadeOutTime = 3f;
		/// <summary>楽曲音インターバル時間</summary>
		protected const float SoundMusicIntervalTime = 0f;
		/// <summary>楽曲音同時再生数</summary>
		protected const int soundMusicMax = 2;
		/// <summary>無音</summary>
		public const int Silent = -1;
		/// <summary>最小音量</summary>
		protected const float MinimumVolume = 0f;
		/// <summary>最大音量</summary>
		protected const float MaximumVolume = 1f;

		// メンバー要素
		/// <summary>ミュート</summary>
		protected bool soundMute = false;
		/// <summary>効果音発声体</summary>
		protected AudioSource [] seSource;
		/// <summary>効果音再生開始時</summary>
		protected DateTime [] sePlayTime;
		/// <summary>効果音基準音量</summary>
		protected float seVolume;
		/// <summary>楽曲音発声体</summary>
		protected AudioSource [] smSource;
		/// <summary>楽曲音再生状態</summary>
		protected musicStatus [] smState;
		/// <summary>直前の楽曲音再生状態</summary>
		protected musicStatus [] smLastState;
		/// <summary>楽曲音状態残存時間</summary>
		protected float [] smRemainTime;
		/// <summary>楽曲音発声チャネル</summary>
		protected int smPlayChannel;
		/// <summary>楽曲音量係数</summary>
		protected float smCoefficient = MaximumVolume;
		/// <summary>楽曲音基準音量</summary>
		protected float smVolume;
		/// <summary>楽曲音再生リスト</summary>
		protected int [] playlist;
		/// <summary>楽曲音再生インデックス</summary>
		protected int playindex;
		/// <summary>楽曲発声サブチャネル</summary>
		protected virtual int smSubChannel => musicSubChannel (smPlayChannel);
		/// <summary>楽曲サブチャネル</summary>
		protected virtual int musicSubChannel (int mainChannel) => (mainChannel == 0) ? 1 : 0;

		/// <summary>起動</summary>
		protected virtual void Awake () => Add (this);

		/// <summary>抹消</summary>
		protected virtual void OnDestroy () => Remove (this);

		/// <summary>初期化済み</summary>
		protected bool inited = false;

		/// <summary>初期化</summary>
		protected virtual void Init () {
			if (inited || sound != this) { return; }
			inited = true;
			if (soundEffectMax <= 0) { soundEffectMax = 1; }
			if (soundMusicFadeInTime <= 0) { soundMusicFadeInTime = 0; }
			if (soundMusicFadeOutTime <= 0) { soundMusicFadeOutTime = 0; }
			seVolume = soundEffectInitialVolume;
			smVolume = soundMusicInitialVolume;
			seSource = new AudioSource [soundEffectMax];
			sePlayTime = new DateTime [soundEffectMax];
			for (var i = 0; i < seSource.Length; i++) {
				seSource [i] = gameObject.AddComponent<AudioSource> ();
				seSource [i].playOnAwake = false;
				seSource [i].loop = false;
			}
			smSource = new AudioSource [soundMusicMax];
			for (var i = 0; i < smSource.Length; i++) {
				smSource [i] = gameObject.AddComponent<AudioSource> ();
				smSource [i].playOnAwake = false;
				smSource [i].loop = true;
			}
			smState = new musicStatus [soundMusicMax];
			smLastState = new musicStatus [soundMusicMax];
			smRemainTime = new float [soundMusicMax];
			smPlayChannel = 1;
		}

		/// <summary>楽曲音再生状態</summary>
		protected enum musicStatus {
			/// <summary>停止</summary>
			STOP = 0,
			/// <summary>再生中</summary>
			PLAYING,
			/// <summary>インターバル待ち</summary>
			WAIT_INTERVAL,
			/// <summary>フェードイン中</summary>
			FADEIN,
			/// <summary>フェードアウト中</summary>
			FADEOUT,
		}

		/// <summary>駆動</summary>
		protected virtual void Update () {
			if (!inited || sound != this) { return; }
			for (var i = 0; i < smSource.Length; i++) {
				if (smLastState [i] != smState [i]) {
					// 状態の切り替え
					switch (smState [i]) {
						case musicStatus.STOP:
							smRemainTime [i] = 0f;
							smSource [i].volume = MinimumVolume;
							smSource [i].Stop ();
							break;
						case musicStatus.PLAYING:
							smRemainTime [i] = 0f;
							smSource [i].volume = smCoefficient * smVolume;
							if (smLastState [i] != musicStatus.FADEIN) {
								smSource [i].Play ();
							}
							break;
						case musicStatus.WAIT_INTERVAL:
							smRemainTime [i] = ((smState [musicSubChannel (i)] == musicStatus.FADEOUT) ? soundMusicFadeOutTime : 0) + soundMusicIntervalTime;
							smSource [i].volume = MinimumVolume;
							break;
						case musicStatus.FADEIN:
							smRemainTime [i] = soundMusicFadeInTime * (MusicVolume - smSource [i].volume) / MusicVolume;
							if (!smSource [i].isPlaying) {
								smSource [i].Play ();
							}
							break;
						case musicStatus.FADEOUT:
							if (smLastState [i] == musicStatus.FADEIN) {
								smRemainTime [i] = soundMusicFadeOutTime * smSource [i].volume / MusicVolume;
							} else {
								smRemainTime [i] = soundMusicFadeOutTime;
							}
							break;
					}
					smLastState [i] = smState [i];
				} else {
					// 状態の継続時間を記録
					smRemainTime [i] -= Time.deltaTime;
					switch (smState [i]) {
						case musicStatus.WAIT_INTERVAL:
							if (smRemainTime [i] <= 0f) {
								smState [i] = musicStatus.FADEIN;
							}
							break;
						case musicStatus.FADEIN:
							if (smRemainTime [i] >= 0f) {
								smSource [i].volume = smCoefficient * smVolume * (1f - smRemainTime [i] / soundMusicFadeInTime);
							} else {
								smState [i] = musicStatus.PLAYING;
							}
							break;
						case musicStatus.FADEOUT:
							if (smRemainTime [i] >= 0f) {
								smSource [i].volume = smCoefficient * smVolume * smRemainTime [i] / soundMusicFadeOutTime;
							} else {
								smState [i] = musicStatus.STOP;
							}
							break;
					}
				}
			}
			// プレイリスト
			if (playlist != null && !smSource [smPlayChannel].loop && smState [smPlayChannel] == musicStatus.PLAYING && !smSource [smPlayChannel].isPlaying) {
				playindex = (playindex + 1) % playlist.Length;
				music = playlist [playindex];
			}
		}

		/// <summary>効果音が再生中</summary>
		protected virtual bool isPlayingEffect {
			get {
				for (var i = 0; i < seSource.Length; i++) {
					if (seSource [i].isPlaying) {
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>再生中の全効果音</summary>
		protected virtual int [] effects => Array.ConvertAll (Array.FindAll (seSource, s => s.isPlaying), e => Array.IndexOf (soundEffectClip, e.clip));

		/// <summary>効果音の設定</summary>
		protected virtual int effect {
			get {
				var index = Silent;
				var time = DateTime.MinValue;
				for (var i = 0; i < seSource.Length; i++) {
					if (seSource [i].isPlaying && sePlayTime [i] > time) {
						// 新しい方
						time = sePlayTime [i];
						index = Array.IndexOf (soundEffectClip, seSource [i].clip);
					}
				}
				return index;
			}
			set {
				if (value < 0 || value >= soundEffectClip.Length) {
					for (var i = 0; i < seSource.Length; i++) {
						seSource [i].Stop ();
					}
				} else {
					var index = 0;
					DateTime time = DateTime.MaxValue;
					for (var i = 0; i < seSource.Length; i++) {
						if (!seSource [i].isPlaying) {
							// 再生していない
							index = i;
							break;
						} else if (sePlayTime [i] < time) {
							// 古い方
							time = sePlayTime [i];
							index = i;
						}
					}
					seSource [index].clip = soundEffectClip [value];
					seSource [index].Play ();
					sePlayTime [index] = DateTime.Now;
				}
			}
		}

		/// <summary>最も早くに再生を始めた同音のAudioSourceインデックス</summary>
		/// <param name="number">音番号</param>
		/// <returns>見つかったインデックス、見つからなければ-1</returns>
		protected virtual int elderPlayingSource (int number) {
			var index = -1;
			if (number >= 0 && number < soundEffectClip.Length) {
				var time = DateTime.MaxValue;
				for (var i = 0; i < seSource.Length; i++) {
					if (seSource [i].isPlaying && seSource [i].clip == soundEffectClip [number]) {
						if (sePlayTime [i] < time) {
							// 古い方
							time = sePlayTime [i];
							index = i;
						}
					}
				}
			}
			return index;
		}

		/// <summary>効果音の停止</summary>
		protected virtual int effectStop {
			set {
				if (value < 0 || value >= soundEffectClip.Length) {
					effect = Silent;
				} else {
					int index = elderPlayingSource (value);
					if (index >= 0) {
						seSource [index].Stop ();
					}
				}
			}
		}

		/// <summary>効果音の音量</summary>
		protected virtual float effectVolume {
			get => seVolume;
			set {
				if (value >= MinimumVolume && value <= MaximumVolume) {
					seVolume = value;
				}
				for (var i = 0; i < seSource.Length; i++) {
					seSource [i].volume = seVolume;
				}
			}
		}

		/// <summary>楽曲音が再生中</summary>
		protected virtual bool isPlayingMusic {
			get {
				for (var i = 0; i < smSource.Length; i++) {
					if ((smSource [i].isPlaying && (smState [i] == musicStatus.FADEIN || smState [i] == musicStatus.PLAYING)) || smState [i] == musicStatus.WAIT_INTERVAL) {
						return true;
                    }
				}
				return false;
			}
        }

		/// <summary>再生中の全楽曲音</summary>
		protected virtual int [] musics => Array.ConvertAll (Array.FindAll (smSource, s => s.isPlaying), e => Array.IndexOf (soundMusicClip, e.clip));

		/// <summary>楽曲音の設定</summary>
		protected virtual int music {
			get => Array.IndexOf (soundMusicClip, smSource [smPlayChannel].clip);
			set {
				var smsc = smSubChannel;
				if (value < 0 || value >= soundMusicClip.Length) {
					// 表をフェードアウトして、裏を停止
					smState [smPlayChannel] = smSource [smPlayChannel].isPlaying ? musicStatus.FADEOUT : musicStatus.STOP;
					smState [smsc] = musicStatus.STOP;
				} else {
					if (smSource [smsc].isPlaying && smSource [smsc].clip == soundMusicClip [value]) {
						// 再生中の裏と一致したら、表を停止して、裏を開始、表裏入れ替え
						smState [smPlayChannel] = smSource [smPlayChannel].isPlaying ? musicStatus.FADEOUT : musicStatus.STOP;
						smState [smsc] = musicStatus.FADEIN;
						smPlayChannel = smsc;
					} else if (!smSource [smPlayChannel].isPlaying || smSource [smPlayChannel].clip != soundMusicClip [value]) {
						// どちらとも一致しない
						if (smSource [smsc].isPlaying && smSource [smPlayChannel].isPlaying && smSource [smPlayChannel].volume < smSource [smsc].volume) {
							// 裏の方がやかましいなら、表裏入れ替え
							smPlayChannel = smsc;
							smsc = smSubChannel;
						}
						// 表をフェードアウトして、裏を即時停止、裏に曲セットして開始、表裏入れ替え
						smState [smPlayChannel] = smSource [smPlayChannel].isPlaying ? musicStatus.FADEOUT : musicStatus.STOP;
						smLastState [smsc] = musicStatus.STOP;
						smSource [smsc].Stop ();
						smSource [smsc].volume = MinimumVolume;
						smSource [smsc].clip = soundMusicClip [value];
						smState [smsc] = musicStatus.WAIT_INTERVAL;
						smPlayChannel = smsc;
					}
				}
			}
		}

		/// <summary>楽曲再生のループ</summary>
		protected bool musicLoop {
			get => smSource [smPlayChannel].loop;
			set {
				for (var i = 0; i < smSource.Length; i++) {
					smSource [i].loop = value;
				}
			}
		}

		// プレイリストの設定
		protected virtual int [] musicPlaylist {
			get => playlist;
			set {
				if (value == null || value.Length == 0) {
					// リストが空なら停止
					if (playlist != null) {
						music = Silent;
					}
					playlist = null;
					musicLoop = true;
					return;
				} else if (isPlayingMusic && playingSamelist (playlist, value)) {
					// 同じプレイリストが再生中なら何もしない
					return;
                }
				// リストを設定して最初の曲を再生
				music = (playlist = value) [playindex = 0];
				musicLoop = false;

				// プレイリストの比較
				bool playingSamelist (int [] a, int [] b) {
					if (a != null && b != null) {
						var len = a.Length;
						if (len == b.Length) {
							for (var i = 0; i < len; i++) {
								if (a [i] != b [i]) {
									return false;
								}
							}
							return true;
						}
					}
					return false;
				}
			}
		}

		/// <summary>楽曲音の音量</summary>
		protected virtual float musicVolume {
			get => smVolume;
			set {
				if (value >= MinimumVolume && value <= MaximumVolume) {
					smVolume = value;
				}
				for (var i = 0; i < smSource.Length; i++) {
					if (smState [i] != musicStatus.STOP) {
						smSource [i].volume = smCoefficient * smVolume;
						if (smVolume <= MinimumVolume) {
							// 音量設定ゼロなら停止
							smState [i] = musicStatus.STOP;
						}
					}
				}
			}
		}

		/// <summary>楽曲音の一時的音量</summary>
		protected virtual float musicTmpVolume {
			get => smCoefficient;
			set {
				if (value >= MinimumVolume && value <= MaximumVolume) {
					smCoefficient = value;
				}
				// 音量を再設定
				musicVolume = musicVolume;
			}
		}

		/// <summary>消音</summary>
		protected virtual bool mute {
			get => soundMute;
			set {
				soundMute = value;
				for (var i = 0; i < seSource.Length; i++) {
					seSource [i].mute = soundMute;
				}
				for (var i = 0; i < smSource.Length; i++) {
					smSource [i].mute = soundMute;
				}
			}
		}

		// クラス要素
		#region static

		/// <summary>アクティブインスタンス</summary>
		protected static Sound sound => sounds.Count > 0 ? sounds [0] : null;

		/// <summary>インスタンスリスト</summary>
		protected static List<Sound> sounds;

		/// <summary>
		/// クラス初期化
		/// </summary>
		static Sound () {
			sounds = new List<Sound> { };
        }

		/// <summary>追加</summary>
		protected static void Add (Sound sound) {
			if (!sounds.Contains (sound)) {
                sounds.Add (sound);
			}
			Sound.sound?.Init ();
		}

		/// <summary>破棄</summary>
		protected static void Remove (Sound sound) {
			if (sounds.Contains (sound)) {
				sounds.Remove (sound);
			}
			Sound.sound?.Init ();
		}

		/// <summary>登録されている効果音数</summary>
		public static int EffectCount => sound?.soundEffectClip?.Length ?? 0;

		/// <summary>効果音が再生中</summary>
		public static bool IsPlayingEffect => sound?.isPlayingEffect ?? false;

		/// <summary>再生中の全効果音</summary>
		public static int [] Effects => sound?.effects;

		/// <summary>効果音の設定</summary>
		public static int Effect {
			get => sound?.effect ?? Silent;
			set {
				if (sound) {
					sound.effect = value;
				}
			}
		}

		/// <summary>止めてから鳴らす</summary>
		public static int StopAndEffect {
			set {
				if (sound) {
					sound.effectStop = value;
					sound.effect = value;
				}
			}
		}

		/// <summary>鳴っていなければ鳴らす</summary>
		public static int EffectIfNot {
			set {
				if (sound && sound.elderPlayingSource (value) < 0) {
					sound.effect = value;
				}
			}
		}

		/// <summary>効果音の停止</summary>
		public static int EffectStop {
			set {
				if (sound) {
					sound.effectStop = value;
				}
			}
		}

		/// <summary>効果音の音量</summary>
		public static float EffectVolume {
			get => sound?.effectVolume ?? SoundEffectInitialVolume;
			set {
				if (sound) {
					sound.effectVolume = value;
				}
			}
		}

		/// <summary>登録されている楽曲音数</summary>
		public static int MusicCount => sound?.soundMusicClip?.Length ?? 0;

		/// <summary>楽曲音が再生中</summary>
		public static bool IsPlayingMusic => sound?.isPlayingMusic ?? false;

		/// <summary>再生中の全楽曲音</summary>
		public static int [] Musics => sound?.musics;

		/// <summary>楽曲音の設定</summary>
		public static int Music {
			get => sound?.music ?? Silent;
			set {
				if (sound) {
					sound.musicPlaylist = null;
					sound.music = value;
				}
			}
		}

		/// <summary>プレイリストの設定</summary>
		public static int [] Playlist {
			get => sound?.musicPlaylist;
			set {
				if (sound) {
					sound.musicPlaylist = value;
				}
			}
		}

		/// <summary>楽曲音の音量</summary>
		public static float MusicVolume {
			get => sound?.musicVolume ?? SoundMusicInitialVolume;
			set {
				if (sound) {
					sound.musicVolume = value;
				}
			}
		}

		/// <summary>楽曲音の一時的音量</summary>
		public static float MusicTmpVolume {
			get => sound?.musicTmpVolume ?? MaximumVolume;
			set {
				if (sound) {
					sound.musicTmpVolume = value;
				}
			}
		}

		/// <summary>消音</summary>
		public static bool Mute {
			get => sound?.mute ?? false;
			set {
				if (sound) {
					sound.mute = value;
				}
			}
		}

		#endregion

		/// <summary>
		/// コンポーネントの動的生成
		/// </summary>
		/// <param name="gameObject">アタッチの対象</param>
		/// <param name="effectMax">効果音同時再生数</param>
		/// <param name="effectInitialVolume">効果音初期音量</param>
		/// <param name="musicInitialVolume">楽曲音初期音量</param>
		/// <param name="musicFadeInTime">楽曲音フェードイン時間</param>
		/// <param name="musicFadeOutTime">楽曲音フェードアウト時間</param>
		/// <param name="musicIntervalTime">楽曲音インターバル時間 (フェードアウトありで負数ならクロスフェード)</param>
		/// <param name="effectClip">効果音クリップ</param>
		/// <param name="musicClip">楽曲音クリップ</param>
		/// <returns>生成されたコンポーネント</returns>
		public static Sound Attach (
			GameObject gameObject, 
			int effectMax = SoundEffectMax, 
			float effectInitialVolume = SoundEffectInitialVolume, 
			float musicInitialVolume = SoundMusicInitialVolume, 
			float musicFadeInTime = SoundMusicFadeInTime, 
			float musicFadeOutTime = SoundMusicFadeOutTime, 
			float musicIntervalTime = SoundMusicIntervalTime, 
			ICollection<AudioClip> effectClip = null, 
			ICollection<AudioClip> musicClip = null
		) {
			if (!gameObject) { return null; }
			var sound = gameObject.GetComponent<Sound> ();
			if (sound) { Destroy (sound); }
            sound = (Sound) gameObject.AddComponent (typeof (Sound));
			sound.soundEffectMax = effectMax;
			sound.soundEffectInitialVolume = effectInitialVolume;
			sound.soundMusicInitialVolume = musicInitialVolume;
			sound.soundMusicFadeInTime = musicFadeInTime;
			sound.soundMusicFadeOutTime = musicFadeOutTime;
			sound.soundMusicIntervalTime = musicIntervalTime;
			sound.soundEffectClip = new AudioClip [(effectClip != null) ? effectClip.Count : 0];
			effectClip?.CopyTo (sound.soundEffectClip, 0);
			sound.soundMusicClip = new AudioClip [(musicClip != null) ? musicClip.Count : 0];
			musicClip?.CopyTo (sound.soundMusicClip, 0);
			return sound;
		}

		/// <summary>
		/// コンポーネントの動的生成
		/// </summary>
		/// <param name="gameObject">アタッチの対象</param>
		/// <param name="origin">複製元</param>
		/// <returns>生成されたコンポーネント</returns>
		public static Sound Attach (GameObject gameObject, Sound origin) {
			return (gameObject && origin) ? Attach (gameObject, origin.soundEffectMax, origin.soundEffectInitialVolume, origin.soundMusicInitialVolume, origin.soundMusicFadeInTime, origin.soundMusicFadeOutTime, origin.soundMusicIntervalTime, origin.soundEffectClip, origin.soundMusicClip) : null;
		}

	}

}