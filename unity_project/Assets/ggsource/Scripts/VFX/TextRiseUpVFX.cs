using UnityEngine;
using System.Collections;
using System.Collections.Generic; 

public class TextRiseUpVFX : MonoBehaviour
{
	public static readonly Color PositiveColor = CommonTools.ColorByteToFloat (93, 131, 62, 255);

	public static readonly Color NegativeColor = Color.red;

	public List<TextRiseUpSubVFX> textRiseUpSubVFXList = new List<TextRiseUpSubVFX> ();

	public int lastPlayedIndex = 0;

	public bool hasInit = false;

	// Use this for initialization
	public void Start ()
	{
		//---- 以下这块是生成的代码，不要手工改动！ ----
		if (LuaInject.HasPatch ("TextRiseUpVFX", "Start")) {
			object ret = LuaInject.CallPatch ("TextRiseUpVFX", "Start", new object[] {
				this
			});
			return;
		}
		//---- 以上这块是生成的代码，不要手工改动！ ----
		Init ();
	}

	public void Init ()
	{
		//---- 以下这块是生成的代码，不要手工改动！ ----
		if (LuaInject.HasPatch ("TextRiseUpVFX", "Init")) {
			object ret = LuaInject.CallPatch ("TextRiseUpVFX", "Init", new object[] {
				this
			});
			return;
		}
		//---- 以上这块是生成的代码，不要手工改动！ ----
		hasInit = true;
		for (int i = 0; i < this.textRiseUpSubVFXList.Count; i++) {
			if (this.textRiseUpSubVFXList [i] == null) {
				Debug.LogWarning ("TextRiseUpVFX : some elements is NULL in textRiseUpSubVFXList");
			}
			else {
				this.textRiseUpSubVFXList [i].Init ();
			}
		}
	}

	public void PlayTextAnimation (string text, Color color)
	{
		//---- 以下这块是生成的代码，不要手工改动！ ----
		if (LuaInject.HasPatch ("TextRiseUpVFX", "PlayTextAnimation")) {
			object ret = LuaInject.CallPatch ("TextRiseUpVFX", "PlayTextAnimation", new object[] {
				this,
				text,
				color
			});
			return;
		}
		//---- 以上这块是生成的代码，不要手工改动！ ----
		if (hasInit == false) {
			Init ();
		}
		bool findFree = false;
		for (int i = 0; i < this.textRiseUpSubVFXList.Count; i++) {
			if (this.textRiseUpSubVFXList [i].IsPlay == false) {
				this.textRiseUpSubVFXList [i].SetText (text);
				this.textRiseUpSubVFXList [i].SetTextColor (color);
				this.textRiseUpSubVFXList [i].PlayAnimation ();
				findFree = true;
				lastPlayedIndex = i;
				break;
			}
		}
		//当没有空闲subvfx的时候，拿取最早播放的subvfx，也就是lastindex的下一个index指向的subvfx，大概
		if (!findFree) {
			int playedIndex = lastPlayedIndex + 1;
			if (playedIndex >= this.textRiseUpSubVFXList.Count) {
				playedIndex = 0;
			}
			this.textRiseUpSubVFXList [playedIndex].SetText (text);
			this.textRiseUpSubVFXList [playedIndex].SetTextColor (color);
			this.textRiseUpSubVFXList [playedIndex].PlayAnimation ();
			lastPlayedIndex = playedIndex;
		}
	}
}
