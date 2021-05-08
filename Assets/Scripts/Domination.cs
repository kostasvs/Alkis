using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Domination : MonoBehaviour
{

	// reference to this singleton
	public static Domination me;

	[Tooltip("points per team")]
	public readonly int[] points = new int[2];

	[Tooltip("max summed points for both teams (when maxed, teams steal points from each other)")]
	public int maxPoints = 100;

	[Tooltip("points texts for the 2 teams")]
	public Text[] textPoints;

	[Tooltip("points fillbars for the 2 teams")]
	public Image[] barIm;
	private Color[] barCol;
	public RectTransform[] barTr;
	private Vector2[] barSize;

	/// <summary>
	/// current bar percentage per team
	/// </summary>
	private readonly float[] curBar = new float[2];

	[Tooltip("speed, in percentage/sec, at which a bar moves towards new value")]
	public float barSpeed = .25f;
	[Tooltip("amount of lerp-to-white for bar's white flash when points are added")]
	public float barWhiten = .5f;
	[Tooltip("duration in secs of bar's white flash when points are added")]
	public float barWhitenDur = .2f;

	/// <summary>
	/// current bar whiteout per team
	/// </summary>
	private readonly float[] curWhiten = new float[2];

	[Tooltip("end-game sound")]
	public AudioSource auEnd;

	void Awake()
	{

		// check for duplicate singleton
		if (me && me != this)
		{
			Debug.LogWarning("duplicate Domination");
			enabled = false;
			return;
		}
		me = this;

		// get bar normal sizes/colors
		barSize = new Vector2[barIm.Length];
		barCol = new Color[barIm.Length];
		for (int i = 0; i < barTr.Length; i++)
		{
			barSize[i] = barTr[i].sizeDelta;
			barTr[i].sizeDelta = new Vector2(0, barSize[i].y);
			barCol[i] = barIm[i].color;
		}
	}

	void Update()
	{

		if (Globals.paused) return;

		// get deltaTime, capped at fixedDeltaTime * maxSlowMo
		float dt = Mathf.Max(Time.deltaTime, Time.fixedDeltaTime * Globals.maxSlowMo);
		for (int i = 0; i < 2; i++)
		{
			// bar length
			var newBar = points[i] / (float)maxPoints;
			if (newBar != curBar[i])
			{
				curBar[i] = Mathf.MoveTowards(curBar[i], newBar, barSpeed * dt);
				var sd = barSize[i];
				sd.x *= curBar[i];
				barTr[i].sizeDelta = sd;
			}
			// bar whiten
			if (curWhiten[i] > 0)
			{
				curWhiten[i] = Mathf.Max(0, curWhiten[i] - dt / barWhitenDur);
				barIm[i].color = Color.Lerp(barCol[i], Color.white, curWhiten[i] * barWhiten);
			}
		}
	}

	/// <summary>
	/// Reset points and bars
	/// </summary>
	public static void ResetMe()
	{

		if (!me) return;
		for (int i = 0; i < 2; i++)
		{
			me.points[i] = 0;
			me.curWhiten[i] = 0f;
			me.textPoints[i].text = me.points[i].ToString();
			me.barTr[i].sizeDelta = new Vector2(0, me.barSize[i].y);
			me.barIm[i].color = me.barCol[i];
			me.curBar[i] = 0;
		}
	}

	/// <summary>
	/// Add points for a team.
	/// </summary>
	/// <param name="team">team that gets the points (0 or 1)</param>
	/// <param name="multiplier">points multiplier</param>
	public static void Score(int team, int multiplier = 1)
	{

		if (!Globals.playing) return;

		// add points, flash bar
		me.points[team] += multiplier;
		me.curWhiten[team] = 1f;

		// check if bars are clashing (max points sum reached)
		if (me.points[0] + me.points[1] > me.maxPoints)
		{
			// steal points from other team
			me.points[1 - team] -= me.points[0] + me.points[1] - me.maxPoints;

			// if other teams' points were decimated...
			if (me.points[1 - team] < 0)
			{
				// undo sub-zero points
				me.points[team] += me.points[1 - team];
				me.points[1 - team] = 0;

				// signal gameover
				me.auEnd.Play();
				Globals.me.MatchEnd();
			}
			// update other team's points text
			me.textPoints[1 - team].text = me.points[1 - team].ToString();
		}
		// update my team's points text
		me.textPoints[team].text = me.points[team].ToString();
	}
}
