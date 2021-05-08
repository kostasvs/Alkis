using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothMenu : MonoBehaviour
{

	/// <summary>
	/// fade in/out duration
	/// </summary>
	public const float fadeDur = .15f;

	/// <summary>
	/// scale during fade transition
	/// </summary>
	public const float fadeScale = 1.1f;

	[Tooltip("canvasGroup to use for alpha (taken from current gameObject if null)")]
	public CanvasGroup cg;
	[Tooltip("rectTransform to use for scaling (optional)")]
	public RectTransform rt;

	//private System.Action doneHide;
	//private System.Action doneShow;

	public bool shown = false;

	/// <summary>
	/// LeanTween hiding animation descriptor
	/// </summary>
	public LTDescr hider;
	/// <summary>
	/// LeanTween showing animation descriptor
	/// </summary>
	public LTDescr shower;

	private bool initialized;

	private void Awake()
	{
		Init();
	}

	private void Init()
	{
		if (initialized) return;
		initialized = true;
		if (cg == null) cg = GetComponent<CanvasGroup>();
	}

	private void OnEnable()
	{
		shown = true;
	}

	private void OnDisable()
	{
		shown = false;
	}

	public void Hide()
	{
		Init();

		shown = false;
		cg.interactable = false;
		cg.alpha = 1f;
		hider = LeanTween.alphaCanvas(cg, 0f, fadeDur).setOnComplete(() =>
		{
			gameObject.SetActive(false);
		});
		
		if (rt)
		{
			rt.localScale = Vector3.one;
			LeanTween.scale(rt, Vector3.one * fadeScale, fadeDur);
		}
	}

	public void Show()
	{
		Init();

		if (!gameObject.activeSelf) gameObject.SetActive(true);
		
		cg.interactable = true;
		cg.alpha = 0f;
		shower = LeanTween.alphaCanvas(cg, 1f, fadeDur);
		
		if (rt)
		{
			rt.localScale = Vector3.one * fadeScale;
			LeanTween.scale(rt, Vector3.one, fadeDur);
		}
	}
}
