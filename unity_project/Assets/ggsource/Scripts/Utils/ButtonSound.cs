using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSound : MonoBehaviour
{
   public enum SoundSelect
   {
      None,
      sfx_button_press,
      sfx_button_click
   }
   
   public SoundSelect selectSound = SoundSelect.sfx_button_press;
   public void OnClick()
   {
      if (selectSound == SoundSelect.None)
         return;
      SoundSystem.Instance.PlayAudio(selectSound.ToString());
   }
}
