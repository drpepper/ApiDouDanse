using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour {

	APIdou apidou;

	public AudioSource m_musicSource;
	public AudioSource m_fxSource;
	public Text m_stateText;


	public AudioClip m_debout;
	public AudioClip m_surLeDos;
	public AudioClip m_surLeVentre;
	public AudioClip m_teteEnBas;
	public AudioClip m_continue;
	public AudioClip m_start;
	

	enum State { Connecting, Playing, WaitingForStop, WaitingForPlay };
	State m_state = State.Connecting;

	float m_nextBreak;
	float m_lastMovementTime = 0;

	APIdou.Positions[] m_positions = {APIdou.Positions.Standing, APIdou.Positions.OnTheBack, APIdou.Positions.FacingDown, APIdou.Positions.UpsideDown};
	APIdou.Positions m_nextPosition;

	// Use this for initialization
	void Start () 
	{
		apidou = GameObject.Find ("Main Camera").GetComponent<APIdou> ();
	}
	
	// Update is called once per frame
	void Update () 
	{
		bool isMoving = Mathf.Abs(apidou.getTotalAcceleration() - 1.0f) > 0.2f;
		if(isMoving) 
		{
			m_lastMovementTime = Time.time;
		}

		if(m_state == State.Connecting)
		{
			if (apidou.getState () == APIdou.State.Receiving) 
			{
				m_state = State.WaitingForPlay;
				m_fxSource.PlayOneShot(m_start);
			}
		}
		else if(m_state == State.WaitingForPlay) 
		{
			if(Time.time - m_lastMovementTime < 1f) 
			{
				m_state = State.Playing;
				m_stateText.text = "Playing";

				m_musicSource.Play();

				m_nextBreak = Time.time	+ Random.RandomRange(5f, 10f);
				m_nextPosition = m_positions[Random.Range(0, m_positions.Length)];
			} 
		}
		else if(m_state == State.Playing) 
		{
			if(Time.time >= m_nextBreak) 
			{
				m_state = State.WaitingForStop;
				m_stateText.text = "WaitingForStop";

				m_musicSource.Pause();
				
				PlayPositionClip(m_nextPosition);
			}
		}
		else if(m_state == State.WaitingForStop)
		{
			if(Time.time - m_lastMovementTime > 1f && apidou.getPosition() == m_nextPosition) 
			{
				m_state = State.WaitingForPlay;
				m_stateText.text = "WaitingForPlay";

				m_fxSource.PlayOneShot(m_continue);		
			}			
		}

		/*if(m_isPlaying) 
		{
			if(Time.time >= m_nextBreak) 
			{
				m_isPlaying = false;
				m_musicSource.Pause();
				m_shouldDanse.SetActive(false);			
				
				PlayPositionClip(m_nextPosition);
			}
			else
			{
				if(Time.time - m_lastMovementTime > 1f && Time.time - m_lastBreak > 4f)  
				{
					m_shouldDanse.SetActive(true);
				}
				else
				{
					m_shouldDanse.SetActive(false);
				}
			}
		} else {
			if(Time.time >= m_nextBreak + 3f) 
			{
				m_isPlaying = true;
				m_musicSource.Play();

				m_lastBreak = m_nextBreak;
				m_nextBreak = Time.time	+ Random.RandomRange(5f, 10f);
				m_nextPosition = m_positions[Random.Range(0, m_positions.Length)];

				m_shouldStop.SetActive(false);
				m_wrongPosition.SetActive(false);
			}
			else
			{
				if(Time.time - m_lastMovementTime > 1f && apidou.getPosition() != m_nextPosition) 
				{
					m_nextBreak = 
				}
				else
				{
					m_shouldStop.SetActive(false);
				}
			}
		}*/
	}


	void PlayPositionClip(APIdou.Positions position) 
	{	
		switch(position)
		{
			case APIdou.Positions.Standing:
				m_fxSource.PlayOneShot(m_debout);
				break;
			case APIdou.Positions.OnTheBack:
				m_fxSource.PlayOneShot(m_surLeDos);
				break;
			case APIdou.Positions.FacingDown:
				m_fxSource.PlayOneShot(m_surLeVentre);
				break;
			case APIdou.Positions.UpsideDown:
				m_fxSource.PlayOneShot(m_teteEnBas);
				break;
		}
	}
}
