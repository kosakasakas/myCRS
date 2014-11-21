using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class test : SetupLux {
	public int cubemapSize;
	public bool oneFacePerFrame = true;
	public bool useRealtimeReflect = false;
	private Vector3 cameraPos = new Vector3 (0, 0,  0);
	private Camera cam;
	private Cubemap rtex;
	private Cubemap diff;

	// for blur
	public int radius = 2;
	public int iterations = 0;
	private Texture2D tex;
	private float avgR = 0;
	private float avgG = 0;
	private float avgB = 0;
	private float avgA = 0;
	private float blurPixelCount = 0;


	// Use this for initialization
	public override void Start () {
		base.Start ();
		UpdateCubemap (63);
	}
	
	// Update is called once per frame
	public override void Update () {
		base.Update ();
	}

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
			//rtex = new RenderTexture(cubemapSize, cubemapSize, 16);
			rtex = new Cubemap(cubemapSize, TextureFormat.ARGB32, true);
			//rtex.isCubemap = true;
			rtex.filterMode = FilterMode.Trilinear;
			rtex.hideFlags = HideFlags.HideAndDontSave;
			//renderer.sharedMaterial.SetTexture ("_Cube", rtex);
			//this.specularCube = (Cubemap)rtex;
		}
		
		cam.transform.position = cameraPos;
		//cam.RenderToCubemap (this.specularCube, faceMask);
		 //this.diffuseCube.hideFlags = HideFlags.HideAndDontSave;
		cam.RenderToCubemap (rtex, faceMask);
		/*
		if (!diff) {
			diff = rtex;
		}
*/
		//if (Time.frameCount % 30 == 0) {
		diff = FastBlur (rtex, 4, radius, iterations);
		this.diffuseCube = rtex;
		//}
		//this.diffuseCube = rtex;
		this.specularCube = rtex;
	}
	/*
	Cubemap FastBlur(Cubemap image, int radius, int iterarions) {
		return (Cubemap)FastBlur ((Texture)image, radius, iterarions);
	}*/
	Cubemap FastBlur(Cubemap image, int faceMask, int radius, int iterations) {
		Cubemap tex = image;
		for(int i = 0; i < iterations; ++i) {
			tex = BlurImage(tex, faceMask, radius, true);
			tex = BlurImage(tex, faceMask, radius, false);
		}
		return tex;
	}

	Cubemap BlurImage(Cubemap image, int faceMask, int blurSize, bool horizontal) {
		//Cubemap blurred = new Cubemap (cubemapSize, TextureFormat.ARGB32, false);
		Cubemap blurred = image;
		int _W = cubemapSize;
		int _H = cubemapSize;
		int xx, yy, x, y;
		CubemapFace face = (CubemapFace)faceMask;
		if(horizontal) {
			for (yy = 0; yy < _H; ++ yy) {
				for( xx = 0; xx < _W; ++xx) {
					ResetPixel();

					//Right side of pixel
					for (x = xx; (x < xx + blurSize && x < _W); ++x) {
						AddPixel(image.GetPixel(face, x, yy));
					}

					//Left side of pixel
					for (x = xx; (x > xx - blurSize && x > 0); --x) {
						AddPixel(image.GetPixel( face, x, yy));
					}

					CalcPixel();

					for (x = xx; x < xx + blurSize && x < _W; ++x) {
						blurred.SetPixel(face, x, yy, new Color(avgR, avgG, avgB, 1.0f));
					}
				}
			}
		} else {
			for (xx = 0; xx < _W; ++xx) {
				for (yy = 0; yy < _H; ++yy) {
					ResetPixel();

					// Over pixel
					for (y = yy; (y < yy + blurSize && y < _H); ++y) {
						AddPixel(image.GetPixel(face, xx, y));
					}

					// Under pixel
					for (y = yy; (y < yy + blurSize && y < 0); --y) {
						AddPixel(image.GetPixel(face, xx, y));
					}

					CalcPixel();

					for (y = yy; (y < yy + blurSize && y < _H); ++y) {
						blurred.SetPixel( face, xx, y, new Color(avgR, avgG, avgB, 1.0f));
					}
				}
			}
		}
		blurred.Apply ();
		return blurred;
	}

	void AddPixel(Color pixel) {
		avgR += pixel.r;
		avgG += pixel.g;
		avgB += pixel.b;
		++blurPixelCount;
	}

	void ResetPixel() {
		avgR = 0.0f;
		avgG = 0.0f;
		avgB = 0.0f;
		blurPixelCount = 0;
	}

	void CalcPixel() {
		avgR /= blurPixelCount;
		avgG /= blurPixelCount;
		avgB /= blurPixelCount;
		/*
		avgR =0;
		avgG =0;
		avgB =0;
		*/
	}
}
