using UnityEngine;
using System.Collections;

public class RealTimeRenderCubeMapTexture : SetupLux {
	/*
	public int cubemapSize = 128;
	public bool oneFacePerFrame = true;
	public bool useRealtimeReflect = false;
	private Vector3 cameraPos = new Vector3 (0, 1, 0);
	private Camera cam;
	private RenderTexture rtex;
	*/

	// Use this for initialization
	//void Start () {
		/*
		// render all six faces at startup
		oneFacePerFrame = true;
		useRealtimeReflect = true;
		if (useRealtimeReflect) {
			//this.specularCube = null;
			UpdateCubemap (63);
		}
		*/
	//}

	//void Update () {
	//}


	/*
	void LateUpdate(){
		if (!useRealtimeReflect) {
			return;
		}
		if (oneFacePerFrame) {
			int faceToRender = Time.frameCount % 6;
			int faceMask = 1 << faceToRender;
			UpdateCubemap (faceMask);
		} else {
			UpdateCubemap (63); // all six faces
		}
	}

	void UpdateCubemap(int faceMask) {
		if (!cam) {
			GameObject go = new GameObject ("CubemapCamera", typeof(Camera));
			go.hideFlags = HideFlags.HideAndDontSave;
			go.transform.position = cameraPos;
			go.transform.rotation = Quaternion.identity;
			cam = go.camera;
			cam.farClipPlane = 100; // don't render very far into cubemap
			cam.enabled = false;
		}

		if (!rtex) {
			rtex = new RenderTexture(cubemapSize, cubemapSize, 16);
			rtex.isCubemap = true;
			rtex.hideFlags = HideFlags.HideAndDontSave;
			renderer.sharedMaterial.SetTexture ("_Cube", rtex);
			//this.specularCube = (Cubemap)rtex;
		}
		
		cam.transform.position = transform.position;
		//cam.RenderToCubemap (this.specularCube, faceMask);
		cam.RenderToCubemap (rtex, faceMask);
	}

	void OnDisable () {
		DestroyImmediate (cam);
		DestroyImmediate (rtex);
	}
	*/
}