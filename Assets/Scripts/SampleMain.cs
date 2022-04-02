using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SoundManager;

/// <summary>メイン (参考)</summary>
public class SampleMain : MonoBehaviour {

	// オブジェクト要素
	// 停止ボタン
	[SerializeField] private Button effectStopButton = default;
	[SerializeField] private Button musicStopButton = default;
	// 効果音パネル
	[SerializeField] private Image [] effectPanels = default;
	// 曲パネル
	[SerializeField] private Image [] musicPanels = default;
	// 音量スライダー
	[SerializeField] private Slider effectVolumeSlider = default;
	[SerializeField] private Slider musicVolumeSlider = default;
	[SerializeField] private Slider musicTempVolumeSlider = default;
	// 消音トグル
	[SerializeField] private Toggle soundMuteToggle = default;
	// 再生中パネル色
	[SerializeField] private Color activeEffectColor = Color.white;
	[SerializeField] private Color activeMusicColor = Color.white;

	/// <summary>初期化</summary>
	private void Start () {
		Debug.Log ("Start");
		effectVolumeSlider.normalizedValue = Sound.EffectVolume;
		musicVolumeSlider.normalizedValue = Sound.MusicVolume;
		musicTempVolumeSlider.normalizedValue = Sound.MusicTmpVolume;
		soundMuteToggle.isOn = Sound.Mute;
		OnPressSEButton (SE.Started); // 起動しました。
		OnPressSEButton (SE.Thunderstorm1); // 雷雨
		OnPressSMButton (-2);
	}

	/// <summary>駆動</summary>
    private void Update () {
		// 再生していないなら停止ボタンを操作不能に
		effectStopButton.interactable = Sound.IsPlayingEffect;
		musicStopButton.interactable = Sound.IsPlayingMusic;
		// 再生中のパネルに着色
		for (var i = 0; i < effectPanels.Length; i++) {
			effectPanels [i].color = Color.white;
		}
		foreach (var i in Sound.Effects) {
			if (i < effectPanels.Length) {
				effectPanels [i].color = activeEffectColor;
            }
        }
		for (var i = 0; i < musicPanels.Length - 1; i++) {
			musicPanels [i].color = Color.white;
        }
		foreach (var i in Sound.Musics) {
			if (i < musicPanels.Length) {
				musicPanels [i].color = activeMusicColor;
			}
		}
		// プレイリスト再生中ならパネルに着色
		musicPanels [musicPanels.Length - 1].color = (Sound.Playlist != null) ? activeMusicColor : Color.white;
	}

	/// <summary>効果音ボタン 重複再生</summary>
	public void OnPressSEButton (int number) {
		Debug.Log ($"Play Effect {number}");
		Sound.Effect = number; // -1なら全停止
	}

	/// <summary>効果音ボタン 同音が再生中なら止めてから再生</summary>
	public void OnPressStopPlusButton (int number) {
		Debug.Log ($"Stop & Play Effect {number}");
		Sound.StopAndEffect = number;
	}

	/// <summary>効果音ボタン 同音が再生中でなければ再生</summary>
	public void OnPressIfNotButton (int number) {
		Debug.Log ($"Play Effect {number} If not playing");
		Sound.EffectIfNot = number;
	}

	/// <summary>効果音ボタン 停止</summary>
	public void OnPressSEStopButton (int number) {
		Debug.Log ($"Stop Effect {number}");
		Sound.EffectStop = number;
	}

	/// <summary>効果音 音量設定</summary>
	public void OnChangeEffectVolumeSlider () {
		Debug.Log ($"Effect Volue {effectVolumeSlider.normalizedValue}");
		Sound.EffectVolume = effectVolumeSlider.normalizedValue;
	}

	/// <summary>楽曲音ボタン 切り替え</summary>
	public void OnPressSMButton (int number) {
		if (number == -2) {
			Debug.Log ($"Play Music List");
			Sound.Playlist = new [] { BGM.Neorock83, BGM.Fantasy01, BGM.Orchestra16, BGM.Fantasy15, };
		} else {
			Debug.Log ($"Play Music {number}");
			Sound.Music = number; // -1なら停止
		}
	}

	/// <summary>楽曲音 音量設定</summary>
	public void OnChangeMusicVolumeSlider () {
		Debug.Log ($"Music Volue {musicVolumeSlider.normalizedValue}");
		Sound.MusicVolume = musicVolumeSlider.normalizedValue;
	}

	/// <summary>楽曲音 一時音量設定</summary>
	public void OnChangeMusicTempVolumeSlider () {
		Debug.Log ($"Music Temp Volue {musicTempVolumeSlider.normalizedValue}");
		Sound.MusicTmpVolume = musicTempVolumeSlider.normalizedValue;
	}

	/// <summary>消音トグル</summary>
	public void OnChangeMuteToggle () {
		Debug.Log ($"Mute {soundMuteToggle.isOn}");
		Sound.Mute = soundMuteToggle.isOn;
	}

}