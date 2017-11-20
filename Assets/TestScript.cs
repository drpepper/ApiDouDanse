using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestScript : MonoBehaviour {
	APIdou apidou;
	Text txt;
	// Use this for initialization
	void Start () {
		apidou = GameObject.Find ("Main Camera").GetComponent<APIdou> ();
		txt = GetComponent<Text> ();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (apidou.getState () == APIdou.State.Receiving) {
			txt.text = "Name: " + apidou.getName () +
			"\nTotal acceleration: " + apidou.getTotalAcceleration () + "g";

			if (apidou.isTouched (APIdou.BELLY))
				txt.text += "\nBelly is touched !";
		
			txt.text += "\n" + apidou.getPosition ();

			if (apidou.getPosition () == APIdou.Positions.Standing)
				txt.text += "\nAPIdou is standing";

			bool isMoving = Mathf.Abs(apidou.getTotalAcceleration() - 1.0f) > 0.2f;
			txt.text += "\n\n";
			txt.text += isMoving ? "IS MOVING" : "not moving";
		} else
			loader ();
	}

	void loader()
	{
		APIdou.State state = apidou.getState();

		if (state == APIdou.State.NotInitialized || state == APIdou.State.Scanning)
			txt.text = "Scanning";
		else if (state == APIdou.State.DeviceFound)
			txt.text = "APIdou found";
		else if (state == APIdou.State.Connected)
			txt.text = "Connected !";
		else
			txt.text = "Error ! Check that your device supports BLE and that no other app is using Bluetooth"; // Error
	}
}
