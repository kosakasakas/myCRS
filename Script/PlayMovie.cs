using UnityEngine;
using System.Collections;

public class PlayMovie : MonoBehaviour {

	// Use this for initialization
	void Start () {
		MovieTexture tex = (MovieTexture)renderer.material.mainTexture;
		tex.loop = true;
		tex.Play ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
