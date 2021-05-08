using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{

	// reference to this singleton
	public static Options me;
	
	/// <summary>
	/// filepath of config file to use (optional)
	/// </summary>
	public static string configFile = "";

	public static bool initialized { get; protected set; }

	public AudioSource music;
	public static float musicVol = 1f;
	public static float soundsVol = 1f;
	public Slider musicSlider;
	public Slider soundsSlider;

	[Tooltip("music fade in/out (0 = none, -1 = fade out, 1 = fade in")]
	public float musicFader;

	public static void Init()
	{

		if (initialized) return;

		// default config filepath
		if (configFile.Equals(string.Empty))
			configFile = Application.persistentDataPath + "/Options.xml";

		initialized = true;
	}

	void Awake()
	{

		// check for duplicate singleton
		if (me && me != this)
		{
			Debug.LogWarning("duplicate Options");
			enabled = false;
			return;
		}
		me = this;
	
		Init();
	}

	private void Start()
	{
		// initially hide this object (after Awake done)
		gameObject.SetActive(false);
	}
		
	void OnEnable()
	{
		// update music/sounds
		MusicVolChange(musicVol);
		SoundsVolChange(soundsVol);
		// read config
		ReadConfig();
	}
	void OnDisable()
	{
		// ensure config save on game/scene exit
		WriteConfig();
	}

	/// <summary>
	/// fade in/out music
	/// (called by Globals.Update())
	/// </summary>
	public void UpdateMusic()
	{
		// check if fade in/out requested
		if (musicFader != 0f)
		{
			if (musicVol > 0f)
			{
				// get new volume towards given direction
				// (divide by musicVol to ensure fade duration of 1 sec)
				float v = music.volume + musicFader * Time.unscaledDeltaTime / musicVol;
				
				// check if limit reached
				if (v < 0f || v > musicVol)
				{
					music.volume = Mathf.Clamp(v, 0, musicVol);
					if (v < 0) music.Stop();
					// stop fade
					musicFader = 0f;
				}
				else music.volume = v;
				// play music if not playing yet
				if (v > 0 && !music.isPlaying) music.Play();
			}
			else
			{
				// musicVol = 0 (mute)
				music.Stop();
				// stop fade
				musicFader = 0f;
			}
		}
	}

	/// <summary>
	/// update music volume
	/// </summary>
	/// <param name="value">new volume (0 to 1)</param>
	public void MusicVolChange(float value)
	{

		musicVol = Mathf.Clamp01(value);
		music.volume = Mathf.Clamp01(value);
		if (value > 0)
		{
			if (!music.isPlaying && Globals.playing) music.Play();
		}
		else
		{
			if (music.isPlaying) music.Pause();
		}
	}

	/// <summary>
	/// update sounds volume
	/// </summary>
	/// <param name="value">new volume (0 to 1)</param>
	public void SoundsVolChange(float value)
	{

		soundsVol = Mathf.Clamp01(value);
		Globals.UpdateIngameVolume();
	}

	/// <summary>
	/// read options from config file
	/// </summary>
	/// <returns>whether successful</returns>
	public static bool ReadConfig()
	{

		Init();
		if (!initialized || !File.Exists(configFile)) return false;
		
		bool complete = false;
		try
		{
			using (XmlReader reader = XmlReader.Create(configFile))
			{
				// loop through all option tags
				while (reader.ReadToFollowing("option"))
				{

					string optName = reader.GetAttribute("name");
					string optVal = reader.GetAttribute("value");
					switch (optName)
					{
						case "sounds":
							if (int.TryParse(optVal, out var sVal))
							{
								var v = Mathf.Clamp01(sVal / 100f);
								me.SoundsVolChange(v);
								if (me.soundsSlider) me.soundsSlider.value = v;
							}
							break;
						case "music":
							if (int.TryParse(optVal, out var mVal))
							{
								var v = Mathf.Clamp01(mVal / 100f);
								me.MusicVolChange(v);
								if (me.musicSlider) me.musicSlider.value = v;
							}
							break;
					}
				}
				complete = true;
			}
		}
		catch (System.Security.SecurityException e)
		{
			Debug.Log(e);
		}
		catch (XmlException e)
		{
			Debug.Log(e);
		}
		catch (System.InvalidOperationException e)
		{
			Debug.Log(e);
		}
		return complete;
	}

	/// <summary>
	/// save options to config file
	/// </summary>
	/// <returns>whether successful</returns>
	public static bool WriteConfig()
	{

		Init();
		if (!initialized) return false;
		
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
		{
			Indent = true,
			IndentChars = "\t",
			NewLineOnAttributes = true
		};

		bool complete = false;
		try
		{
			using (XmlWriter w = XmlWriter.Create(configFile, xmlWriterSettings))
			{
				// start
				w.WriteStartElement("optionsGroup");

				// music
				w.WriteStartElement("option");
				w.WriteAttributeString("name", "music");
				w.WriteAttributeString("value", Mathf.FloorToInt(musicVol * 100f).ToString());
				w.WriteEndElement();

				// sounds
				w.WriteStartElement("option");
				w.WriteAttributeString("name", "sounds");
				w.WriteAttributeString("value", Mathf.FloorToInt(soundsVol * 100f).ToString());
				w.WriteEndElement();

				// end
				w.WriteEndElement();

				w.Flush();
				complete = true;
			}
		}
		catch (System.InvalidOperationException e)
		{
			Debug.Log(e);
		}
		return complete;
	}
}
