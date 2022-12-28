using UnityEngine;
using System.Collections;

public class TextRiseUpSubVFX : MonoBehaviour
{
	public UILabel label;

	public bool isPlay = false;

	public Vector3 initialPos;

	public float speedFactor = 1f;

	public float SpeedFactor {
		get {
			return speedFactor;
		}
		set {
			speedFactor = value;
		}
	}

	public bool IsPlay {
		get {
			return isPlay;
		}
		set {
			isPlay = value;
		}
	}

	public void Init ()
	{
		//---- 以下这块是生成的代码，不要手工改动！ ----
		if (LuaInject.HasPatch ("TextRiseUpSubVFX", "Init")) {
			object ret = LuaInject.CallPatch ("TextRiseUpSubVFX", "Init", new object[] {
				this
			});
			return;
		}
		//---- 以上这块是生成的代码，不要手工改动！ ----
		initialPos = this.transform.localPosition;
		this.gameObject.SetActive (false);
		this.label = this.GetComponent<UILabel> ();
	}

	public void PlayAnimation ()
	{
		//---- 以下这块是生成的代码，不要手工改动！ ----
		if (LuaInject.HasPatch ("TextRiseUpSubVFX", "PlayAnimation")) {
			object ret = LuaInject.CallPatch ("TextRiseUpSubVFX", "PlayAnimation", new object[] {
				this
			});
			return;
		}
		//---- 以上这块是生成的代码，不要手工改动！ ----
		SoundSystem.Instance.PlayAudio ("sfx_levelup");
		this.IsPlay = true;
		this.gameObject.SetActive (true);
		Vector3 finalPos = initialPos;
		finalPos.y += 200;
		TweenPosition tp = TweenPosition.Begin (this.gameObject, 0.8f * speedFactor, finalPos);
		tp.from = initialPos;
		tp.method = UITweener.Method.EaseOut;
		NGUIToolsAddon.SetAlphaRecursively (this.gameObject, 1);
		TweenAlpha ta = TweenAlpha.Begin (this.gameObject, 0.45f * speedFactor, 0);
		ta.from = 1;
		ta.delay = 0.5f;
		ta.method = UITweener.Method.EaseOut;
		EventDelegate.Add (ta.onFinished, FinishHandler, true);
	}

	public void SetText (string text)
	{
		//---- 以下这块是生成的代码，不要手工改动！ ----
		if (LuaInject.HasPatch ("TextRiseUpSubVFX", "SetText")) {
			object ret = LuaInject.CallPatch ("TextRiseUpSubVFX", "SetText", new object[] {
				this,
				text
			});
			return;
		}
		//---- 以上这块是生成的代码，不要手工改动！ ----
		label.text = text;
	}

	public void SetTextColor (Color color)
	{
		//---- 以下这块是生成的代码，不要手工改动！ ----
		if (LuaInject.HasPatch ("TextRiseUpSubVFX", "SetTextColor")) {
			object ret = LuaInject.CallPatch ("TextRiseUpSubVFX", "SetTextColor", new object[] {
				this,
				color
			});
			return;
		}
		//---- 以上这块是生成的代码，不要手工改动！ ----
		label.color = color;
	}

	public void FinishHandler ()
	{
		//---- 以下这块是生成的代码，不要手工改动！ ----
		if (LuaInject.HasPatch ("TextRiseUpSubVFX", "FinishHandler")) {
			object ret = LuaInject.CallPatch ("TextRiseUpSubVFX", "FinishHandler", new object[] {
				this
			});
			return;
		}
		//---- 以上这块是生成的代码，不要手工改动！ ----
		this.gameObject.SetActive (false);
		this.IsPlay = false;
	}
}
