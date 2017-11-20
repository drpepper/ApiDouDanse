#define OLD_APIDOU

using UnityEngine;
using System;

public class BLEDevice : MonoBehaviour
{
	private BluetoothDeviceScript BlueScript;

	private string DeviceName = "APIdou_38";
	private string ServiceUUID = "5800";
	private string AccelUUID = "5801";
	private string GyroUUID = "5802";
	private string TouchUUID = "5803";
	private string VibUUID = "5806";

	protected State			state;

	private machineState	_state;
	private float			_timeout;
	private string			_deviceAddress;
	private bool			_foundAccel;
	private bool			_foundGyro;
	private bool			_foundTouch;

	protected positionCalc	posCalc;

	public string			device_name;

	protected int[]			accel;
	protected int[]			gyro;
	protected uint			touch;

	private enum machineState
	{
		None,
		Scan,
		Connect,
		Subscribe,
		Unsubscribe,
		Disconnect,
		Error
	}

	public enum State
	{
		NotInitialized,
		Scanning,
		DeviceFound,
		Connected,
		Receiving,
		Disconnected,
		Error
	}

	void reset ()
	{
		state = State.NotInitialized;
		_timeout = 0f;
		_state = machineState.None;
		_deviceAddress = null;
		_foundAccel = false;
		_foundGyro = false;
		_foundTouch = false;
	}

	void SetState (machineState newState, float timeout)
	{
		_state = newState;
		_timeout = timeout;
	}

	void StartProcess ()
	{
		reset ();
		// Init as Central BLE device
		BlueScript = BluetoothLEHardwareInterface.Initialize (true, false, () => {
			SetState (machineState.Scan, 0.1f);
			state = State.Scanning;

		}, (error) => {
			state = State.Error;
			BluetoothLEHardwareInterface.Log ("Error during initialize: " + error);
		});
		BlueScript.DisconnectedPeripheralAction += Disconnected;
	}

	// Use this for initialization
	void Start ()
	{
		accel = new int[3];
		gyro = new int[3];
		posCalc = new positionCalc();
		StartProcess ();
	}

	// Update is called once per frame
	void Update ()
	{
		if (_timeout > 0f)
		{
			_timeout -= Time.deltaTime;

			if (_timeout <= 0f)
			{
				_timeout = 0f;

				switch (_state)
				{
				case machineState.None:
					break;

				case machineState.Scan:
					BluetoothLEHardwareInterface.ScanForPeripheralsWithServices (null, (address, name) => {

						// if your device does not advertise the rssi and manufacturer specific data
						// then you must use this callback because the next callback only gets called
						// if you have manufacturer specific data
						Debug.Log("Found Device : " + address + " / Name : " + name);

						if (name.Contains (DeviceName))
						{
							BluetoothLEHardwareInterface.StopScan ();

							state = State.DeviceFound;
							device_name = name;

							// found a device with the name we want
							// this example does not deal with finding more than one
							_deviceAddress = address;
							SetState (machineState.Connect, 0.5f);
						}

					}, (address, name, rssi, bytes) =>
						{
							// APIdou does not broadcast manufacturer specific data
						}
					);
					break;

				case machineState.Connect:
					// set these flags
					_foundAccel = false;
					_foundGyro = false;
					_foundTouch = false;

					// note that the first parameter is the address, not the name. I have not fixed this because
					// of backwards compatiblity.
					// also note that I am note using the first 2 callbacks. If you are not looking for specific characteristics you can use one of
					// the first 2, but keep in mind that the device will enumerate everything and so you will want to have a timeout
					// large enough that it will be finished enumerating before you try to subscribe or do any other operations.
					BluetoothLEHardwareInterface.ConnectToPeripheral (_deviceAddress, null, null, (address, foundServiceUUID, foundCharUUID) => {

						if (IsEqual (foundServiceUUID, ServiceUUID))
						{
							_foundAccel = _foundAccel || IsEqual (foundCharUUID, AccelUUID);
							_foundGyro = _foundGyro || IsEqual (foundCharUUID, GyroUUID);
							_foundTouch = _foundTouch || IsEqual (foundCharUUID, TouchUUID);

							// if we have found all characteristics that we are waiting for
							// set the state. make sure there is enough timeout that if the
							// device is still enumerating other characteristics it finishes
							// before we try to subscribe
							if (_foundAccel && _foundGyro && _foundTouch)
							{
								Debug.Log("Connected");
								state = State.Connected;
								SetState (machineState.Subscribe, 3f);
							}
						}
					});
					break;

				case machineState.Subscribe:
					BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress (_deviceAddress, FullUUID (ServiceUUID), FullUUID (TouchUUID), null, onNotification);
					System.Threading.Thread.Sleep(500);
					BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress (_deviceAddress, FullUUID (ServiceUUID), FullUUID (AccelUUID), null, onNotification);
					System.Threading.Thread.Sleep(500);
					BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress (_deviceAddress, FullUUID (ServiceUUID), FullUUID (GyroUUID), null, onNotification);
					break;

				case machineState.Unsubscribe:
					BluetoothLEHardwareInterface.UnSubscribeCharacteristic (_deviceAddress, ServiceUUID, AccelUUID, null);
					SetState (machineState.Disconnect, 4f);
					break;

				case machineState.Disconnect:
					BluetoothLEHardwareInterface.DisconnectPeripheral (_deviceAddress, (address) => {
						BluetoothLEHardwareInterface.DeInitialize (() => {
							_state = machineState.None;
						});
					});
					break;
				}
			}
		}
	}

	void Disconnected(String uuid)
	{
		_state = machineState.Disconnect;
		state = State.Disconnected;
		Debug.Log ("Disconnected from " + uuid);
	}

	private int[] rawToIntTab(byte[] raw)
	{
		int i = 0;
		int[] data = new int[3];
		while (i < 3) {
			data[i] = raw [i * 2 + 1] << 8;
			data[i] |= raw [i * 2];
			if ((data[i] & 32768) != 0)
				data[i] = -1 - (data[i] ^ 0xFFFF);
			i++;
		}
		return data;
	}

	private void onNotification(String address, String uuid, byte[] raw)
	{
		// we don't have a great way to set the state other than waiting until we actually got
		// some data back.
		if (_state == machineState.Subscribe)
			_state = machineState.None;
		if (state == State.Connected)
			state = State.Receiving;
		
		Debug.Log ("UUID " + uuid);

		if (uuid == FullUUID (AccelUUID)) {
			int[] new_data = rawToIntTab (raw);

			// Quick LowPassFilter
			// + Correcting a bug on the firmware, X and Y axis are currently inverted
			accel [0] = (int)(new_data [1] * 0.3 + accel [0] * 0.7);
			accel [1] = (int)(new_data [0] * 0.3 + accel [1] * 0.7);
			accel [2] = (int)(new_data [2] * 0.3 + accel [2] * 0.7);

		} else if (uuid == FullUUID (GyroUUID)) {
			int[] new_data = rawToIntTab (raw);

			gyro = new_data;
		} else if (uuid == FullUUID (TouchUUID)) {
			#if OLD_APIDOU
			int new_touch;
			int tmp;

			new_touch = (int)raw [0];
			// Only the feets stayed at the same place, so wipe the rest
			// before adding them manually

			tmp = new_touch & 0x11;

			// hands to belly
			if (((new_touch & 4) != 0) || ((new_touch & 8) != 0))
				tmp |= 4;
			// Old ears to new ears
			if ((new_touch & 16) != 0)
				tmp |= 8;
			if ((new_touch & 32) != 0)
				tmp |= 16;
			touch = (uint)tmp;
			#else
			touch = (uint)raw [0];
			#endif
		}

		if (uuid == FullUUID(AccelUUID) || uuid == FullUUID(GyroUUID))
			posCalc.updateData (accel, gyro);
	}

	private int sendByte (byte value, string uuid)
	{
		byte[] data = new byte[] { value };
		int ret = -1;
		BluetoothLEHardwareInterface.WriteCharacteristic (
			_deviceAddress,
			FullUUID(ServiceUUID),
			FullUUID(uuid),
			data,
			data.Length,
			true, (characteristicUUID) => {
				BluetoothLEHardwareInterface.Log ("Write Succeeded");
				ret = 0;
			});
		return ret;
	}

	protected int setVibration(uint val)
	{
		return sendByte ((byte)val, VibUUID);
	}

	string FullUUID (string uuid)
	{
		return "aef9" + uuid + "-5027-41dd-a0ae-7b5f6045d4d3";
	}

	bool IsEqual(string uuid1, string uuid2)
	{
		if (uuid1.Length == 4)
			uuid1 = FullUUID (uuid1);
		if (uuid2.Length == 4)
			uuid2 = FullUUID (uuid2);
		return (uuid1.ToUpper().CompareTo(uuid2.ToUpper()) == 0);
	}
}