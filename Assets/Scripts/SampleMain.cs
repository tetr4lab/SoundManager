using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SoundManager;

/// <summary>メイン (参考)</summary>
public class SampleMain : MonoBehaviour {

	// オブジェクト要素
	/// <summary>音量スライダー</summary>
	[SerializeField] private Slider effectVolumeSlider = default;
	[SerializeField] private Slider musicVolumeSlider = default;
	[SerializeField] private Toggle soundMuteToggle = default;

	// 効果音番号 ©効果音ラボ https://soundeffect-lab.info/
	public const int SE_Started = 0;
	public const int SE_SwordGesture1 = 1;
	public const int SE_SwordSlash1 = 2;
	public const int SE_MagicFlame2 = 3;
	public const int SE_Thunderstorm1 = 4;
	// 楽曲音番号 ©魔王魂 https://maoudamashii.jokersounds.com/
	public const int BGM_Fantasy01 = 0;
	public const int BGM_Fantasy15 = 1;
	public const int BGM_Neorock83 = 2;
	public const int BGM_Orchestra16 = 3;


	/// <summary>初期化</summary>
	private void Start () {
		musicVolumeSlider.normalizedValue = Sound.MusicVolume;
		effectVolumeSlider.normalizedValue = Sound.EffectVolume;
		soundMuteToggle.isOn = Sound.Mute;
		Debug.Log ("Started");
		Sound.Effect = SE_Started; // 起動しました。
		Sound.Effect = SE_Thunderstorm1; // 雷雨
		Sound.Music = BGM_Neorock83; // ロックなBGM
	}

	/// <summary>効果音ボタン 重複再生</summary>
	public void OnPressSEButton (int number) {
		Debug.Log ($"Play SE {number}");
		Sound.Effect = number; // -1なら全停止
	}

	/// <summary>効果音ボタン 同音が再生中なら止めてから再生</summary>
	public void OnPressStopPlusButton (int number) {
		Debug.Log ($"Stop & Play SE {number}");
		Sound.StopAndEffect = number;
	}

	/// <summary>効果音ボタン 同音が再生中でなければ再生</summary>
	public void OnPressIfNotButton (int number) {
		Debug.Log ($"Play SE {number} If not playing");
		Sound.EffectIfNot = number;
	}

	/// <summary>効果音ボタン 停止</summary>
	public void OnPressSEStopButton (int number) {
		Debug.Log ($"Stop SE {number}");
		Sound.EffectStop = number;
	}

	/// <summary>効果音 音量設定</summary>
	public void OnChangeEffectVolumeSlider () {
		Debug.Log ($"SE Volue {effectVolumeSlider.normalizedValue}");
		Sound.EffectVolume = effectVolumeSlider.normalizedValue;
	}

	/// <summary>楽曲音ボタン 切り替え</summary>
	public void OnPressSMButton (int number) {
		Debug.Log ($"Play BGM {number}");
		Sound.Music = number; // -1なら停止
	}

	/// <summary>楽曲音 音量設定</summary>
	public void OnChangeMusicVolumeSlider () {
		Debug.Log ($"BGM Volue {musicVolumeSlider.normalizedValue}");
		Sound.MusicVolume = musicVolumeSlider.normalizedValue;
	}

	/// <summary>消音トグル</summary>
	public void OnChangeMuteToggle () {
		Debug.Log ($"Mute {soundMuteToggle.isOn}");
		Sound.Mute = soundMuteToggle.isOn;
	}

}