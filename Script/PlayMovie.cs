using UnityEngine;
using System.Collections;

public class PlayMovie : MonoBehaviour {

	// Use this for initialization
	void Start () {
		transform.localScale = new Vector3( -transform.localScale.x,
		                                   transform.localScale.y,
		                                   transform.localScale.z );
		MovieTexture tex = (MovieTexture)GetComponent<Renderer>().material.mainTexture;
		tex.loop = true;
		tex.Play ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
