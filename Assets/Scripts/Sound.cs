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
	/// 主要機能
	///   共通
	///     消音、動的生成、再設定
	///   効果音
	///     重ねて再生、止めてから再生、再生していなければ再生、停止、音量設定
	///   楽曲
	///     再生、停止、音量設定、一時的な音量設定
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
		protected const float SoundMusicFadeOutTime = 0f;
		/// <summary>楽曲音インターバル時間</summary>
		protected const float SoundMusicIntervalTime = 0f;
		/// <summary>楽曲音同時再生数</summary>
		protected const int soundMusicMax = 2;
		/// <summary>無音</summary>
		public const int Silent = -1;
		/// <summary>即時停止</summary>
		public const int ShutUp = -2;
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
		protected MusicStatus [] smState;
		/// <summary>直前の楽曲音再生状態</summary>
		protected MusicStatus [] smLastState;
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
		/// <summary>楽曲発声体の曲番号</summary>
		protected virtual int numberOfMusic (int channel) => Array.IndexOf (soundMusicClip, smSource [channel].clip);

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
			smState = new MusicStatus [soundMusicMax];
			smLastState = new MusicStatus [soundMusicMax];
			smRemainTime = new float [soundMusicMax];
			smPlayChannel = 1;
		}

		/// <summary>破棄</summary>
		protected virtual void Remove () {
			if (!inited || sound != this) { return; }
			inited = false;
			sound.seVolume = sound.smVolume = MinimumVolume;
			sound.effect = sound.music = ShutUp;
			foreach (var se in seSource) {
				Destroy (se);
			}
			foreach (var sm in smSource) {
				Destroy (sm);
			}
		}

		/// <summary>楽曲音再生状態</summary>
		protected enum MusicStatus {
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
						case MusicStatus.STOP:
							smRemainTime [i] = 0f;
							smSource [i].volume = MinimumVolume;
							smSource [i].Stop ();
							break;
						case MusicStatus.PLAYING:
							smRemainTime [i] = 0f;
							smSource [i].volume = smCoefficient * smVolume;
							if (smLastState [i] != MusicStatus.FADEIN) {
								smSource [i].Play ();
							}
							break;
						case MusicStatus.WAIT_INTERVAL:
							smRemainTime [i] = ((smState [musicSubChannel (i)] == MusicStatus.FADEOUT) ? soundMusicFadeOutTime : 0) + soundMusicIntervalTime;
							smSource [i].volume = MinimumVolume;
							break;
						case MusicStatus.FADEIN:
							smRemainTime [i] = soundMusicFadeInTime * (MusicVolume - smSource [i].volume) / MusicVolume;
							if (!smSource [i].isPlaying) {
								smSource [i].Play ();
							}
							break;
						case MusicStatus.FADEOUT:
							if (smLastState [i] == MusicStatus.FADEIN) {
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
						case MusicStatus.WAIT_INTERVAL:
							if (smRemainTime [i] <= 0f) {
								smState [i] = MusicStatus.FADEIN;
							}
							break;
						case MusicStatus.FADEIN:
							if (smRemainTime [i] >= 0f) {
								smSource [i].volume = smCoefficient * smVolume * (1f - smRemainTime [i] / soundMusicFadeInTime);
							} else {
								smState [i] = MusicStatus.PLAYING;
							}
							break;
						case MusicStatus.PLAYING:
							// プレイリスト
							if (playlist != null && smSource [i].time >= smSource [i].clip.length - soundMusicFadeOutTime) {
								playindex = (playindex + 1) % playlist.Length;
								music = playlist [playindex];
							}
							break;
						case MusicStatus.FADEOUT:
							if (smRemainTime [i] >= 0f) {
								smSource [i].volume = smCoefficient * smVolume * smRemainTime [i] / soundMusicFadeOutTime;
							} else {
								smState [i] = MusicStatus.STOP;
							}
							break;
					}
				}
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
					if (smState [i] != MusicStatus.STOP) {
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>再生中の全楽曲音</summary>
		protected virtual int [] musics {
			get {
				var found = new List<int> { };
				for (var i = 0; i < smSource.Length; i++) {
					if (smState [i] != MusicStatus.STOP) {
						found.Add (numberOfMusic (i));
                    }
                }
				return found.ToArray ();
			}
		}

		/// <summary>楽曲音の設定</summary>
		protected virtual int music {
			get => (smState [smPlayChannel] == MusicStatus.STOP) ? Silent : numberOfMusic (smPlayChannel);
			set {
				var smsc = smSubChannel;
				if (value == ShutUp) {
					// 即時停止
					smStop (smPlayChannel);
					smStop (smsc);
				} else if (value < 0 || value >= soundMusicClip.Length) {
					// 範囲外なら、表裏とも停止
					smFadeOut (smPlayChannel);
					smFadeOut (smsc);
				} else if (smSource [smPlayChannel].isPlaying && smSource [smPlayChannel].clip == soundMusicClip [value]) {
					// 再生中の表と一致したら、表を再生して、裏を停止
					smState [smPlayChannel] = MusicStatus.FADEIN;
					smFadeOut (smsc);
				} else if (smSource [smsc].isPlaying && smSource [smsc].clip == soundMusicClip [value]) {
					// 再生中の裏と一致したら、表を停止して、裏を開始、表裏入れ替え
					smFadeOut (smPlayChannel);
					smState [smsc] = MusicStatus.FADEIN;
					smPlayChannel = smsc;
				} else {
					// どちらとも一致しないなら、表をフェードアウトして、裏を即時停止、裏に曲をセットして開始、表裏入れ替え
					if (smSource [smsc].isPlaying && smSource [smPlayChannel].isPlaying && smSource [smPlayChannel].volume < smSource [smsc].volume) {
						// 裏の方がやかましいなら、先に表裏入れ替え
						smPlayChannel = smsc;
						smsc = smSubChannel;
					}
					smFadeOut (smPlayChannel);
					smLastState [smsc] = MusicStatus.STOP;
					smSource [smsc].Stop ();
					smSource [smsc].volume = MinimumVolume;
					smSource [smsc].clip = soundMusicClip [value];
					smState [smsc] = MusicStatus.WAIT_INTERVAL;
					smPlayChannel = smsc;
				}
			
				// 再生中ならフェードアウト
				void smFadeOut (int channel) => smState [channel] = smSource [channel].isPlaying ? MusicStatus.FADEOUT : MusicStatus.STOP;
				// 即時停止
				void smStop (int channel) {
					smLastState [channel] = smState [channel] = MusicStatus.STOP;
					smSource [channel].Stop ();
					smSource [channel].volume = MinimumVolume;
					smSource [smsc].clip = null;
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
					// 同じプレイリストが再生中なら、現在の曲から開始 (停止処理中の再開に配慮)
				} else {
					playlist = value;
					playindex = int.MaxValue;
					// 再生中の曲がリストにあればそこから開始
					for (var i = 0; i < smSource.Length; i++) {
						var index = Array.IndexOf (playlist, numberOfMusic (i));
						if (index >= 0 && smSource [i].isPlaying && playindex > index) {
							playindex = index;
						}
					}
					if (playindex == int.MaxValue) {
						playindex = 0;
					}
				}
				// リストを再生
				music = playlist [playindex];
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

		/// <summary>プレイリストの曲送り</summary>
		/// <param name="step">変位量</param>
		/// <returns>再生中のインデックス</returns>
		protected virtual int musicPlayNext (int step = 1) {
			if (playlist != null) {
				if (step < 0) {
					step += (-step / playlist.Length + 1) * playlist.Length;
				}
				playindex = (playindex + step) % playlist.Length;
				music = playlist [playindex];
				return playindex;
			}
			return Silent;
		}

		/// <summary>楽曲音の音量</summary>
		protected virtual float musicVolume {
			get => smVolume;
			set {
				if (value >= MinimumVolume && value <= MaximumVolume) {
					smVolume = value;
				}
				for (var i = 0; i < smSource.Length; i++) {
					if (smState [i] != MusicStatus.STOP) {
						smSource [i].volume = smCoefficient * smVolume;
						if (smVolume <= MinimumVolume) {
							// 音量設定ゼロなら停止
							smState [i] = MusicStatus.STOP;
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

		#region sounds (この外では使わない)

		/// <summary>アクティブインスタンス</summary>
		protected static Sound sound => sounds.Count > 0 ? sounds [0] : null;

		/// <summary>インスタンスリスト (変化の際は即座に初期化される)</summary>
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
			Sound.sound?.Remove ();
			if (sounds.Contains (sound)) {
				sounds.Remove (sound);
			}
			Sound.sound?.Init ();
		}

        #endregion sounds

        /// <summary>有効</summary>
        public static bool IsValid => sound;

        #region Effect

        /// <summary>効果音クリップ</summary>
        public static AudioClip [] EffectClip {
			get => sound?.soundEffectClip;
			set {
				if (sound) {
					sound.effect = ShutUp;
					sound.soundEffectClip = value;
                }
            }
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

        #endregion Effect

        #region Music

        /// <summary>楽曲音フェードイン時間</summary>
        public static float MusicFadeInTime {
			get => sound?.soundMusicFadeInTime ?? SoundMusicFadeInTime;
			set {
				if (sound) {
					sound.soundMusicFadeInTime = value;
				}
			}
		}

		/// <summary>楽曲音フェードアウト時間</summary>
		public static float MusicFadeOutTime {
			get => sound?.soundMusicFadeOutTime ?? SoundMusicFadeOutTime;
			set {
				if (sound) {
					sound.soundMusicFadeOutTime = value;
				}
			}
		}

		/// <summary>楽曲音インターバル時間</summary>
		public static float MusicIntervalTime {
			get => sound?.soundMusicIntervalTime ?? SoundMusicIntervalTime;
			set {
				if (sound) {
					sound.soundMusicIntervalTime = value;
				}
			}
		}

		/// <summary>楽曲音クリップ</summary>
		public static AudioClip [] MusicClip {
			get => sound?.soundMusicClip;
			set {
				if (sound) {
					sound.music = ShutUp;
					sound.playlist = null;
					sound.soundMusicClip = value;
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

		/// <summary>全曲プレイリスト</summary>
		public static int [] AllMusicPlaylist {
			get {
				if (_allMusicPlaylist == null || _allMusicPlaylist.Length != MusicCount) {
					_allMusicPlaylist = new int [MusicCount];
					for (var i = 0; i < _allMusicPlaylist.Length; i++) {
						_allMusicPlaylist [i] = i;
					}
				}
				return _allMusicPlaylist;
			}
		}
		private static int [] _allMusicPlaylist = null; // キャッシュ

		/// <summary>プレイリストの設定</summary>
		public static int [] Playlist {
			get => sound?.musicPlaylist;
			set {
				if (sound) {
					sound.musicPlaylist = value;
				}
			}
		}

		/// <summary>プレイリストの曲送り</summary>
		/// <param name="step">変位量</param>
		/// <returns>再生中のインデックス</returns>
		public static int MusicPlayNext (int step = 1) => sound?.musicPlayNext (step) ?? Silent;

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

        #endregion Music

        /// <summary>全体消音</summary>
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
			foreach (var snd in gameObject.GetComponents<Sound> ()) {
				Destroy (snd);
			}
            var sound = (Sound) gameObject.AddComponent (typeof (Sound));
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