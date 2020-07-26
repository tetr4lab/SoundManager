//	Copyright© tetr4lab.

using System.Collections;
using System.Collections.Generic;
using System;
using DateTime = System.DateTime;
using UnityEngine;

namespace SoundManager {
		
	/// <summary>サウンドマネージャー</summary>
	public class Sound : MonoBehaviour {
		
		// オブジェクト要素
		/// <summary>効果音同時再生数</summary>
		[SerializeField, Tooltip("効果音同時再生数")] protected int soundEffectMax = 5;
		/// <summary>効果音初期音量</summary>
		[SerializeField, Tooltip ("効果音初期音量"), Range(0f, 1f)] protected float soundEffectInitialVolume = 0.5f;
		/// <summary>楽曲音初期音量</summary>
		[SerializeField, Tooltip ("楽曲音初期音量"), Range (0f, 1f)] protected float soundMusicInitialVolume = 0.5f;
		/// <summary>楽曲音フェードイン時間</summary>
		[SerializeField, Tooltip ("楽曲音フェードイン時間")] protected float soundMusicFadeInTime = 0f;
		/// <summary>楽曲音フェードアウト時間</summary>
		[SerializeField, Tooltip ("楽曲音フェードアウト時間")] protected float soundMusicFadeOutTime = 3f;
		/// <summary>楽曲音インターバル時間 (フェードアウトありで負数ならクロスフェード)</summary>
		[SerializeField, Tooltip ("楽曲音インターバル時間 (フェードアウトありで負数ならクロスフェード)")] protected float soundMusicIntervalTime = 0f;
		/// <summary>効果音クリップ</summary>
		[SerializeField, Tooltip ("効果音")] protected AudioClip [] soundEffectClip = null;
		/// <summary>楽曲音クリップ</summary>
		[SerializeField, Tooltip ("楽曲音")] protected AudioClip [] soundMusicClip = null;

		// 定数
		/// <summary>楽曲音同時再生数</summary>
		protected const int soundMusicMax = 2;
		/// <summary>無音</summary>
		public const int Silent = -1;

		// メンバー要素
		/// <summary>ミュート</summary>
		protected bool soundMute = false;
		/// <summary>効果音発生体</summary>
		protected AudioSource [] seSource;
		/// <summary>効果音再生開始時</summary>
		protected DateTime [] sePlayTime;
		/// <summary>効果音基準音量</summary>
		protected float seVolume;
		/// <summary>楽曲音発生体</summary>
		protected AudioSource [] smSource;
		/// <summary>楽曲音再生状態</summary>
		protected musicStatus [] smState;
		/// <summary>直前の楽曲音再生状態</summary>
		protected musicStatus [] smLastState;
		/// <summary>楽曲音状態残存時間</summary>
		protected float [] smRemainTime;
		/// <summary>楽曲音発生チャネル</summary>
		protected int smPlayChannel;
		/// <summary>楽曲音量係数</summary>
		protected float smCoefficient = 1f;
		/// <summary>楽曲音基準音量</summary>
		protected float smVolume;

		/// <summary>初期化</summary>
		protected virtual void Awake () {
			if (!init (this)) { return; }
			if (soundMusicFadeInTime <= 0) { soundMusicFadeInTime = 0; }
			if (soundMusicFadeOutTime <= 0) { soundMusicFadeOutTime = 0; }
			seVolume = soundEffectInitialVolume;
			smVolume = soundMusicInitialVolume;
			seSource = new AudioSource [soundEffectMax];
			sePlayTime = new DateTime [soundEffectMax];
			for (var i = 0; i < seSource.Length; i++) {
				seSource [i] = gameObject.AddComponent<AudioSource> ();
				seSource [i].loop = false;
			}
			smSource = new AudioSource [soundMusicMax];
			for (var i = 0; i < smSource.Length; i++) {
				smSource [i] = gameObject.AddComponent<AudioSource> ();
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
			if (sound != this) { return; }
			for (var i = 0; i < smSource.Length; i++) {
				var smsc = subChannel (i);
				if (smLastState [i] != smState [i]) {
					switch (smState [i]) {
						case musicStatus.STOP:
							smRemainTime [i] = 0f;
							smSource [i].volume = 0f;
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
							smRemainTime [i] = ((smState [smsc] == musicStatus.FADEOUT) ? soundMusicFadeOutTime : 0) + soundMusicIntervalTime;
							smSource [i].volume = 0f;
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
					smRemainTime [i] -= Time.deltaTime; // 経過時間セット
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
		}


		/// <summary>効果音の設定</summary>
		protected int effect {
			get {
				var index = Silent;
				var time = DateTime.MinValue;
				for (var i = 0; i < seSource.Length; i++) {
					if (seSource [i].isPlaying && sePlayTime [i] > time) { // 新しい方
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
						if (!seSource [i].isPlaying) { // 再生していない
							index = i;
							break;
						} else if (sePlayTime [i] < time) { // 古い方
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
		protected int elderPlayingSource (int number) {
			var index = -1;
			if (number >= 0 && number < soundEffectClip.Length) {
				var time = DateTime.MaxValue;
				for (var i = 0; i < seSource.Length; i++) {
					if (seSource [i].isPlaying && seSource [i].clip == soundEffectClip [number]) {
						if (sePlayTime [i] < time) { // 古い方
							time = sePlayTime [i];
							index = i;
						}
					}
				}
			}
			return index;
		}

		/// <summary>効果音の停止</summary>
		protected int effectStop {
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
		protected float effectVolume {
			get { return seVolume; }
			set {
				if (value >= 0f && value <= 1f) {
					seVolume = value;
				}
				for (var i = 0; i < seSource.Length; i++) {
					seSource [i].volume = seVolume;
				}
			}
		}

		/// <summary>サブチャネル</summary>
		protected int subChannel (int main) => (main == 0) ? 1 : 0;

		/// <summary>アクティブなチャネル</summary>
		protected int musicActiveChannel {
			get {
				var smsc = subChannel (smPlayChannel);
				if (smSource [smsc].isPlaying) {
					if (smSource [smPlayChannel].isPlaying && smSource [smPlayChannel].volume >= smSource [smsc].volume) {
						return smPlayChannel;
					}
					return smsc;
				}
				return smPlayChannel;
			}
		}

		/// <summary>楽曲音の設定</summary>
		protected int music {
			get {
				return Array.IndexOf (soundMusicClip, smSource [smPlayChannel].clip);
			}
			set {
				var smsc = subChannel (smPlayChannel); // 裏チャネル
				if (value < 0 || value >= soundEffectClip.Length) {
					smState [smPlayChannel] = smSource [smPlayChannel].isPlaying ? musicStatus.FADEOUT : musicStatus.STOP; // 表をフェードアウト
					smState [smsc] = musicStatus.STOP; // 裏を停止
				} else {
					if (smSource [smsc].isPlaying && smSource [smsc].clip == soundMusicClip [value]) { // 再生中の裏と一致
						smState [smPlayChannel] = smSource [smPlayChannel].isPlaying ? musicStatus.FADEOUT : musicStatus.STOP; // 表を停止
						smState [smsc] = musicStatus.FADEIN; // 裏を開始
						smPlayChannel = smsc; // 表裏入れ替え
					} else if (!smSource [smPlayChannel].isPlaying || smSource [smPlayChannel].clip != soundMusicClip [value]) { // どちらとも一致しない
						smPlayChannel = musicActiveChannel; // アクティブな方を表に
						smsc = subChannel (smPlayChannel);
						smState [smPlayChannel] = smSource [smPlayChannel].isPlaying ? musicStatus.FADEOUT : musicStatus.STOP; // 表をフェードアウト
						smLastState [smsc] = musicStatus.STOP; // 裏を即時停止
						smSource [smsc].Stop ();
						smSource [smsc].volume = 0f;
						smSource [smsc].clip = soundMusicClip [value]; // 裏に曲セット
						smState [smsc] = musicStatus.WAIT_INTERVAL; // 裏を開始
						smPlayChannel = smsc; // 表裏入れ替え
					}
				}
			}
		}

		/// <summary>楽曲音の音量</summary>
		protected float musicVolume {
			get { return smVolume; }
			set {
				if (value >= 0f && value <= 1f) {
					smVolume = value;
				}
				if (smState [0] == musicStatus.PLAYING) {
					smSource [0].volume = smCoefficient * smVolume;
				}
				if (smState [1] == musicStatus.PLAYING) {
					smSource [1].volume = smCoefficient * smVolume;
				}
			}
		}

		/// <summary>楽曲音の一時的音量</summary>
		protected float musicTmpVolume {
			get { return smCoefficient; }
			set {
				if (value >= 0f && value <= 1f) {
					smCoefficient = value;
				}
				musicVolume = -1f;
			}
		}

		/// <summary>消音</summary>
		protected bool mute {
			get { return soundMute; }
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

		/// <summary>シングルトン</summary>
		protected static Sound sound = null;

		/// <summary>初期化</summary>
		protected static bool init (Sound _sound) {
			if (sound == null && _sound != null) {
				sound = _sound;
				return true;
			}
			return false;
		}

		/// <summary>登録されている効果音数</summary>
		public static int EffectCount => sound.soundEffectClip.Length;

		/// <summary>登録されている楽曲音数</summary>
		public static int MusicCount => sound.soundMusicClip.Length;

		/// <summary>効果音の設定</summary>
		public static int Effect {
			get { return sound.effect; }
			set { sound.effect = value; }
		}

		/// <summary>止めてから鳴らす</summary>
		public static int StopAndEffect { set { sound.effectStop = value; sound.effect = value; } }

		/// <summary>鳴っていなければ鳴らす</summary>
		public static int EffectIfNot { set { if (sound.elderPlayingSource (value) < 0) { sound.effect = value; } } }

		/// <summary>効果音の停止</summary>
		public static int EffectStop { set { sound.effectStop = value; } }

		/// <summary>効果音の音量</summary>
		public static float EffectVolume {
			get { return sound.effectVolume; }
			set { sound.effectVolume = value; }
		}

		/// <summary>楽曲音の設定</summary>
		public static int Music {
			get { return sound.music; }
			set { sound.music = value; }
		}

		/// <summary>楽曲音の音量</summary>
		public static float MusicVolume {
			get { return sound.musicVolume; }
			set { sound.musicVolume = value; }
		}

		/// <summary>楽曲音の一時的音量</summary>
		public static float MusicTmpVolume {
			get { return sound.musicTmpVolume; }
			set { sound.musicTmpVolume = value; }
		}

		/// <summary>消音</summary>
		public static bool Mute {
			get { return sound.mute; }
			set { sound.mute = value; }
		}

		#endregion

	}

}