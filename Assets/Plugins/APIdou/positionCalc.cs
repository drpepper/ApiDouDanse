using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class positionCalc {
	int[] accel, gyro;
	float oldTime;

	// Since Unity and the algorithm have two different frames of reference
	// we are using one internal quaternion and one exposed to other scripts
	Quaternion tmpQuat = Quaternion.identity;
	Quaternion madgwickQuat;
	public float Beta = 0.1f;

	// Use this for initialization
	void Start () {
		madgwickQuat = Quaternion.identity;
		accel = new int[3];
		gyro = new int[3];
	}


	public void updateData(int[] accelNew, int[] gyroNew)
	{
		accel = accelNew;
		gyro = gyroNew;

		madgwickFusion(Mathf.Deg2Rad * ((-1000 * gyro [1]) / 32768.0f),
			Mathf.Deg2Rad * ((1000 * gyro [2]) / 32768.0f),
			Mathf.Deg2Rad * ((-1000 * gyro [0]) / 32768.0f),
			-accel [1],
			accel [2],
			-accel [0],
			Time.time - oldTime);
		
		oldTime = Time.time;
	}

	public void reset()
	{
		tmpQuat = Quaternion.identity;
	}

	public Quaternion getQuaternion()
	{
		return madgwickQuat;
	}

	public void madgwickFusion(float gx, float gy, float gz, float ax, float ay, float az, float deltaTime)
	{
		float q1 = tmpQuat.x , q2 = tmpQuat.z, q3 = tmpQuat.y, q4 = tmpQuat.w;   // short name local variable for readability
		float norm;
		float s1, s2, s3, s4;
		float qDot1, qDot2, qDot3, qDot4;

		// Auxiliary variables to avoid repeated arithmetic
		float _2q1 = 2f * q1;
		float _2q2 = 2f * q2;
		float _2q3 = 2f * q3;
		float _2q4 = 2f * q4;
		float _4q1 = 4f * q1;
		float _4q2 = 4f * q2;
		float _4q3 = 4f * q3;
		float _8q2 = 8f * q2;
		float _8q3 = 8f * q3;
		float q1q1 = q1 * q1;
		float q2q2 = q2 * q2;
		float q3q3 = q3 * q3;
		float q4q4 = q4 * q4;

		// Normalise accelerometer measurement
		norm = Mathf.Sqrt(ax * ax + ay * ay + az * az);
		if (norm == 0f)
			return; // handle NaN
		norm = 1 / norm; // use reciprocal for division
		ax *= norm;
		ay *= norm;
		az *= norm;

		// Gradient decent algorithm corrective step
		s1 = _4q1 * q3q3 + _2q3 * ax + _4q1 * q2q2 - _2q2 * ay;
		s2 = _4q2 * q4q4 - _2q4 * ax + 4f * q1q1 * q2 - _2q1 * ay - _4q2 + _8q2 * q2q2 + _8q2 * q3q3 + _4q2 * az;
		s3 = 4f * q1q1 * q3 + _2q1 * ax + _4q3 * q4q4 - _2q4 * ay - _4q3 + _8q3 * q2q2 + _8q3 * q3q3 + _4q3 * az;
		s4 = 4f * q2q2 * q4 - _2q2 * ax + 4f * q3q3 * q4 - _2q3 * ay;
		norm = 1f / Mathf.Sqrt(s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4);    // normalise step magnitude
		s1 *= norm;
		s2 *= norm;
		s3 *= norm;
		s4 *= norm;

		// Compute rate of change of quaternion
		qDot1 = 0.5f * (-q2 * gx - q3 * gy - q4 * gz) - Beta * s1;
		qDot2 = 0.5f * (q1 * gx + q3 * gz - q4 * gy) - Beta * s2;
		qDot3 = 0.5f * (q1 * gy - q2 * gz + q4 * gx) - Beta * s3;
		qDot4 = 0.5f * (q1 * gz + q2 * gy - q3 * gx) - Beta * s4;

		// Integrate to yield quaternion
		q1 += qDot1 * deltaTime;
		q2 += qDot2 * deltaTime;
		q3 += qDot3 * deltaTime;
		q4 += qDot4 * deltaTime;
		norm = 1f / Mathf.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);    // normalise quaternion

		tmpQuat = new Quaternion (q1 * norm, q3 * norm, q2 * norm, q4 * norm);
		madgwickQuat = new Quaternion (tmpQuat.y, tmpQuat.x, -tmpQuat.z, tmpQuat.w);
	}


	public APIdou.Positions getPosition()
	{
		if (accel == null)
			return APIdou.Positions.Unknown;
		
		float magnitude;

		magnitude = Mathf.Sqrt(accel [0] * accel [0] + accel [1] * accel [1] + accel [2] * accel [2]) / 16384.0f;

		// if > 1.1g plush is moving, if < 0.5g plush is in free-fall (or in space)x²
		if (magnitude > 1.1f)
			return APIdou.Positions.Moving;
		else if (magnitude < 0.5f)
			return APIdou.Positions.Falling;

		float pitch, roll;

		pitch = Mathf.Atan2(-accel[0], accel[2]) * Mathf.Rad2Deg;
		roll = Mathf.Atan2(-accel[1], accel[2]) * Mathf.Rad2Deg;

		if (Mathf.Abs(pitch) < 30 && Mathf.Abs(roll) < 30)
			return APIdou.Positions.OnTheBack;
		else if (Mathf.Abs(pitch) > 145 && Mathf.Abs(roll) > 145)
			return APIdou.Positions.FacingDown;
		else if (pitch > 80)
			return APIdou.Positions.Standing;
		else if (pitch < 80)
			return APIdou.Positions.UpsideDown;
		else
			return APIdou.Positions.Unknown;
	}
}
