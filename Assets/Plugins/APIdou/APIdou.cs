using UnityEngine;
using System;

/// <summary>
/// The main class to use to connect your projects to an APIdou.
/// At the moment, just put this script on a GameObject and your game will connect
/// to the first APIdou it can find.
/// </summary>
public class APIdou : BLEDevice
{
	public const uint BLUE_FOOT = 1;
	public const uint ORANGE_FOOT = 2;
	public const uint BELLY = 4;
	public const uint RED_EAR = 8;
	public const uint YELLOW_EAR = 16;

	private const float GyroToDegrees = 1000.0f / 32768.0f;

	public enum Positions
	{
		Moving,
		Falling,
		OnTheBack,
		FacingDown,
		Standing,
		UpsideDown,
		Unknown
	}

	/*
	 * Global informations
	*/

	/// <summary>
	/// Gets the name of the currently connected APIdou (eg. APIdou_1)
	/// </summary>
	/// <returns>The name.</returns>
	public string getName()
	{
		return device_name;
	}

	/// <summary>
	/// Gets the current connexion status of the APIdou.
	/// You can use sensor values when this function returns "Receiving"
	/// </summary>
	/// <returns>The state.</returns>
	public BLEDevice.State getState()
	{
		return state;
	}

	/*
	 * Accelerometer/gyroscope datas
	*/

	/// <summary>
	/// Returns an human friendly position of the plush
	/// An easy way to check if the plush is standing, on its back, etc.
	/// </summary>
	/// <returns>The position.</returns>
	public Positions getPosition()
	{
		return posCalc.getPosition();
	}

	/// <summary>
	/// Gets the total acceleration (three axis combined) of the plush (in Gs).
	/// 1g: normal gravity, 0g: free-fall, other value usually means movement.
	/// </summary>
	/// <returns>The acceleration in Gs.</returns>
	public float getTotalAcceleration()
	{
		return Mathf.Sqrt(accel [0] * accel [0] + accel [1] * accel [1] + accel [2] * accel [2]) / 16384.0f;
	}

	/// <summary>
	/// Return the acceleration of each axis in Gs. Values can go from -2g to +2g
	/// </summary>
	/// <returns>The accelerometer.</returns>
	public float[] getAccelerometer()
	{
		float[] tab = new float[3];

		tab [0] = accel[0] / 16384.0f;
		tab [1] = accel[1] / 16384.0f;
		tab [2] = accel[2] / 16384.0f;

		return tab;
	}

	/// <summary>
	/// Return the rotational speed of each axis in degrees/s.
	/// Values goes from -1000°/s to +1000°/s.
	/// </summary>
	/// <returns>The gyroscope.</returns>
	public float[] getGyroscope()
	{
		float[] tab = new float[3];

		tab [0] = gyro[0] * GyroToDegrees;
		tab [1] = gyro[1] * GyroToDegrees;
		tab [2] = gyro[2] * GyroToDegrees;

		return tab;
	}

	/// <summary>
	/// Return a world oriented quaternion calculated from the fusion of the gyroscope and the accelerometer.
	/// The orientation will drift a bit over time, so use the resetQuat() function from time to time.
	/// It will also take several seconds to stabilize.
	/// </summary>
	/// <returns>A world oriented quaternion.</returns>
	public Quaternion getQuaternion()
	{
		return posCalc.getQuaternion ();
	}

	/// <summary>
	/// Resets the orientation quaternion
	/// </summary>
	public void resetQuat()
	{
		posCalc.reset ();
	}

	/*
	 * Tactile zones datas
	*/

	/// <summary>
	/// Return true if the tactile zone in parameter is touched, use binary operation to check multiple zones at the same time.
	/// For example, to check if both ears are touched, you can use isTouched(APIdou.LEFT_EAR & APIdou.RIGHT_EAR)
	/// </summary>
	/// <returns><c>true</c>, if all the zones passed in parameters are currently touched, <c>false</c> otherwise.</returns>
	/// <param name="val">The zones to check.</param>
	public bool isTouched(uint val)
	{
		if ((touch & val) != 0)
			return true;
		return false;
	}

	/// <summary>
	/// Gets the raw touch sensor value.
	/// An unsigned int where each bit represents one of the tactile zones
	/// </summary>
	public uint getTouch()
	{
		return touch;
	}

	/*
	 * Vibration motor
	*/

	/// <summary>
	/// Sets the vibration motor force.
	/// </summary>
	/// <returns>0 if the value was properly set.</returns>
	/// <param name="val">A value between 0 and 100.</param>
	new public int setVibration(uint val)
	{
		return setVibration (val);
	}

	/// <summary>
	/// Shorthand to set the vibration motor off or at full speed
	/// </summary>
	/// <returns>0 if the value was properly set.</returns>
	/// <param name="val">The desired state.</param>
	public int vibrate(bool val)
	{
		if (val == true)
			return setVibration (100);
		else
			return setVibration (0);
	}
}
