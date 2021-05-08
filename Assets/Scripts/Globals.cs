using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using DentedPixel;

public class Globals : MonoBehaviour
{

	// reference to this singleton
	public static Globals me;

	/// <summary>
	/// whether game paused or ended
	/// </summary>
	public static bool paused;

	/// <summary>
	/// whether in game (and not mainmenu)
	/// </summary>
	public static bool playing;

	/// <summary>
	/// whether gameover triggered
	/// </summary>
	public static bool gameover;

	[Tooltip("gameObjects to enable only in gameplay")]
	public GameObject[] ingameObjects;
	public GameObject playerObject;
	public bool hidePlayerOnStart = true;

	[Tooltip("gameObjects to enable only in mainmenu")]
	public GameObject[] menuObjects;

	[Tooltip("gameObjects to hide on gameover")]
	public GameObject[] hideOnGameover;
	public GameObject gameoverObject;

	[Tooltip("object to enable for whichever team wins on gameover")]
	public GameObject[] gameoverWinnerObject;

	/// <summary>
	/// which gameObject requested the pause
	/// </summary>
	private GameObject requestedPause;

	/// <summary>
	/// max allowed fraction of fixedDeltaTime to execute (used by certain scripts)
	/// </summary>
	public const float maxSlowMo = .2f;

	public GameObject pauseMenu;
	private SmoothMenu smoothPause;
	public GameObject[] pauseSubMenus;

	/// <summary>
	/// timeScale we had before pausing
	/// </summary>
	private float lastTimeScale = 1f;

	public AudioMixer audioMixer;
	public IOMgr ioMgr;
	public Options options;

	// audio
	public const float audioFadeInDur = .5f;
	public const float audioMinVol = .001f;
	private float audioCurVol = audioMinVol;
	public static string volParam = "IngameVolume";
	private AudioSource[] au;

	// gameover slowdown/freeze
	public float slowDownDur = 1.5f;
	public float minSlowDown = .01f;
	public float freezeHoldDur = 1f;
	public float freezeHoldTimer;

	void Awake()
	{
		// check for duplicate singleton
		if (me && me != this)
		{
			Debug.LogWarning("duplicate Globals");
			enabled = false;
			return;
		}
		me = this;

		// enable IOMgr and Options if needed
		if (!ioMgr.gameObject.activeSelf && !IOMgr.me) ioMgr.gameObject.SetActive(true);
		if (!options.gameObject.activeSelf && !Options.me) options.gameObject.SetActive(true);
		
		au = GetComponents<AudioSource>();
		smoothPause = pauseMenu.GetComponent<SmoothMenu>();
	}

	private void Start()
	{
		UpdateIngameVolume();
		// show main menu with delay
		Invoke(nameof(ShowMainMenu), 1f);
	}

	private void Update()
	{
		// hide Player on Start (done here to ensure it executes after other Starts)
		if (hidePlayerOnStart)
		{
			playerObject.SetActive(false);
			var pdeath = playerObject.GetComponent<SSDeathAndSpawn>();
			pdeath.CancelInvoke();
			hidePlayerOnStart = false;
		}

		// match end showdown sequence
		if (!playing && gameover && !paused)
		{
			// decrease timeScale
			var t = Time.timeScale;
			t -= Time.fixedDeltaTime / slowDownDur;
			if (t <= minSlowDown)
			{
				// reached min timeScale, stop here and wait for a bit
				Time.timeScale = minSlowDown;
				freezeHoldTimer += Time.unscaledDeltaTime;
				if (freezeHoldTimer >= freezeHoldDur)
				{
					// show winner message
					freezeHoldTimer = 0f;
					paused = true;
					Time.timeScale = 0f;
					MenuSetActive(gameoverObject, true);
					int winner = Domination.me.points[0] > Domination.me.points[1] ? 0 : 1;
					MenuSetActive(gameoverWinnerObject[winner], true);
				}
			}
			else Time.timeScale = t;
		}

		// esc handler
		if (Input.GetKeyDown(KeyCode.Escape) && !IOMgr.me.HandleEscape())
		{
			bool closed = false;
			// check submenus
			for (int i = 0; i < me.pauseSubMenus.Length; i++)
			{
				var sm = me.pauseSubMenus[i].GetComponent<SmoothMenu>();
				if (sm && sm.shown)
				{
					// hide submenu and break (smooth)
					sm.Hide();
					closed = true;
					break;
				}
				else if (me.pauseSubMenus[i].activeSelf)
				{
					// hide submenu and break (hard)
					me.pauseSubMenus[i].SetActive(false);
					closed = true;
					break;
				}
			}
			if (closed) me.MenuSound(1);
			else
			{
				// unpause
				if (!gameover) TogglePause(null);
			}
		}

		// check if music volume changed
		Options.me.UpdateMusic();
		if (paused)
		{
			// if pausemenu was hidden, unpause
			if (!gameover && !pauseMenu.activeSelf) TogglePause(null);
			// exit here due to being paused
			return;
		}

		// audio target volume
		float targetVol = playing ? 1f : audioMinVol;
		if (audioCurVol != targetVol)
		{
			audioCurVol = Mathf.MoveTowards(audioCurVol, targetVol, Time.fixedDeltaTime / audioFadeInDur);
			UpdateIngameVolume();
		}
	}

	/// <summary>
	/// trigger match begin (used by button)
	/// </summary>
	public void MatchBegin()
	{

		playing = true;

		// enable/disable objects
		foreach (var go in ingameObjects) MenuSetActive(go, true);
		foreach (var go in menuObjects) MenuSetActive(go, false);
		playerObject.SetActive(true);

		// start music
		Options.me.music.volume = Options.musicVol;
		if (Options.me.music.volume > 0 && !Options.me.music.isPlaying)
		{
			Options.me.music.Play();
		}
		// reset point-related stuff
		Domination.ResetMe();
		Multipliers.ResetMe();

		// trigger respawn of all existing bots
		foreach (var ss in SSTeam.list)
		{
			var death = ss.GetComponent<SSDeathAndSpawn>();
			float v = Random.value;
			if (ss.GetComponent<SSControlPlayer>()) v = .75f + .25f * v;
			death.Die(true, v);
		}
	}

	/// <summary>
	/// trigger match end
	/// </summary>
	public void MatchEnd()
	{

		gameover = true;
		playing = false;
		CloseAllSubmenus();
		foreach (var go in hideOnGameover) MenuSetActive(go, false);
		if (paused) TogglePause(null);
	}

	/// <summary>
	/// exit to main menu
	/// </summary>
	public void ToMainMenu()
	{
		// ensure game unpauses
		paused = true;
		TogglePause(null);
		CloseAllSubmenus();
		Time.timeScale = 1f;

		gameover = false;
		playing = false;

		// enable/disable objects
		foreach (var go in ingameObjects) MenuSetActive(go, false);
		foreach (var go in menuObjects) MenuSetActive(go, true);
		playerObject.SetActive(false);
		MenuSetActive(gameoverObject, false);
		foreach (var go in gameoverWinnerObject) MenuSetActive(go, false);

		// music fadeout
		Options.me.musicFader = -1f;
		UpdateIngameVolume();

		// trigger respawn of all existing bots
		foreach (var ss in SSTeam.list)
		{
			var death = ss.GetComponent<SSDeathAndSpawn>();
			death.Die(true, Random.value);

			// abort player respawn if pending
			if (ss.GetComponent<SSControlPlayer>()) death.CancelInvoke();
		}
	}

	public void ShowMainMenu()
	{
		foreach (var go in menuObjects) MenuSetActive(go, true);
		me.MenuSound(1);
	}

	/// <summary>
	/// set game volume
	/// </summary>
	/// <param name="value">value between 0 and 1</param>
	public static void SetIngameVolume(float value)
	{
		if (!me) return;
		me.audioMixer.SetFloat(volParam, Mathf.Log10(
			Mathf.Clamp(value * Options.soundsVol, audioMinVol, 1f)) * 20);
	}

	public static void UpdateIngameVolume()
	{
		if (me) SetIngameVolume(me.audioCurVol);
	}

	public void RequestTogglePause()
	{
		TogglePause(null);
	}

	/// <summary>
	/// toggle pause state. If unpausing and requestedBy is non-null and different from the
	/// gameObject that requested the pause, it will be denied.
	/// </summary>
	/// <param name="requestedBy">gameObject that requests it (can be null)</param>
	public static void TogglePause(GameObject requestedBy)
	{

		me.MenuSound(1);
		if (!paused)
		{
			// remeber the gameObject that requested the pause
			me.requestedPause = requestedBy;
		}
		else
		{
			// if requestedBy and requestedPause are different and not null, don't unpause
			if (me.requestedPause && me.requestedPause != requestedBy
				&& me.requestedPause != me.gameObject && requestedBy != null) return;
			me.requestedPause = null;
		}
		// toggle pause
		paused = !paused;
		if (paused)
		{
			// store last timeScale and set current to 0
			me.lastTimeScale = Time.timeScale;
			Time.timeScale = 0f;
			// lower volume
			me.audioCurVol = audioMinVol;
			UpdateIngameVolume();
		}
		else
		{
			// restore last timeScale (now done by FinishUnpause)
			//Time.timeScale = me.lastTimeScale;
		}
		if (me.pauseMenu)
		{
			if (!paused)
			{
				// defer unpause (to allow pausemenu to fade out)
				me.smoothPause.Hide();
				me.smoothPause.hider.setOnComplete(() =>
				{
					me.FinishUnpause();
				});
				// ensure all submenus are closed
				me.CloseAllSubmenus();
			}
			else me.smoothPause.Show();
		}
	}

	public void CloseAllSubmenus()
	{

		foreach (var go in me.pauseSubMenus) MenuSetActive(go, false);
	}

	/// <summary>
	/// set active state of given menu, using smooth animation if available
	/// </summary>
	/// <param name="go">menu gameObject</param>
	/// <param name="active">new active state</param>
	public void MenuSetActive(GameObject go, bool active)
	{

		if (go.activeSelf == active) return;

		var sm = go.GetComponent<SmoothMenu>();
		if (sm != null)
		{
			// toggle with animation
			if (active && !sm.shown) sm.Show();
			else if (!active && sm.shown) sm.Hide();
		}
		else
		{
			var cg = go.GetComponent<CanvasGroup>();
			if (cg != null)
			{
				// toggle with CanvasGroup alpha
				LeanTween.alphaCanvas(cg, active ? 1f : 0f, SmoothMenu.fadeDur).setOnComplete(() =>
				{
					cg.alpha = 1f;
					go.SetActive(active);
				});
			}
			// hard toggle
			else go.SetActive(active);
		}
	}

	/// <summary>
	/// actually unpause (used to defer until pausemenu hides)
	/// </summary>
	private void FinishUnpause()
	{
		Time.timeScale = lastTimeScale;
		// ensure pausemenu is hidden
		pauseMenu.SetActive(paused);
	}

	/// <summary>
	/// play given menu sound.
	/// </summary>
	/// <param name="index">0 = confirm, 1 = cancel, 2 = shift, 3 = toggle</param>
	public void MenuSound(int index)
	{
		au[index].Play();
	}

	/// <summary>
	/// exits to mainmenu if playing, otherwise quits
	/// </summary>
	public void QuitGame()
	{

		if (playing) ToMainMenu();
		else if (Application.isEditor) Debug.Log("Quit!");
		else Application.Quit();
	}

	private void OnApplicationPause(bool pause)
	{
		// pause if focus lost
		if (pause && !paused) TogglePause(null);
	}
}
