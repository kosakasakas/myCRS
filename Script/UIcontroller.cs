using UnityEngine;
using System.Collections;

public class UIcontroller : MonoBehaviour
{
	public bool autoMusicPlay;
	// Prefabs.
	public GameObject musicPlayerPrefab;

	bool  musicPlaying;
	
	// Objects to be controlled.
	GameObject musicPlayer;
	Animator animator;

	void Awake()
	{
		// Instantiate the prefabs.
		musicPlayer = (GameObject)Instantiate(musicPlayerPrefab);
	}

	void Start ()
	{
		if (autoMusicPlay) {
			StartCoroutine(WaitAndPlayMusic(2.1f));
			//StartDance();
		}
	}

	IEnumerator WaitAndPlayMusic(float sec) {
		yield return new WaitForSeconds(sec);//ここでは2秒待ってから以下を行う
		StartMusic ();
	}

	void Update() {
		bool canPlay = false;
		if (Input.GetKeyDown (KeyCode.Space)) {
			//canPlay = true;
		}
		GetComponent<Animator>().SetBool("PlayPool",canPlay);
	}

	void StartMusic () {
		foreach (var source in musicPlayer.GetComponentsInChildren<AudioSource>())
			source.Play();
		musicPlaying = true;
	}

	void StartDance () {
		animator.Play(" 003_NOT01_Final");
	}
	
	void OnGUI ()
	{
		if (!musicPlaying && GUI.Button (new Rect (0, 0, 200, 50), "Start Music"))
		{
			StartMusic();
			Debug.Log ("Click Music Play button.");
		}
	}
}