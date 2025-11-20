using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace FairyGUI
{
	public class TimersEngine : MonoBehaviour
	{
		void Update ()
		{
			Timers.inst.Update();
		}
	}
}