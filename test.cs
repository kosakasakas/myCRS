using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class test : SetupLux {
	public int cubemapSize;
	public bool oneFacePerFrame = false;
	public bool useRealtimeReflect = false;
	private Vector3 cameraPos = new Vector3 (0, 0, 0);
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
			UpdateCubemap (faceMask, faceToRender);
		} else {
			UpdateCubemap (63); // all six faces
		}
	}

	void UpdateCubemap(int faceMask, int faceToRender = -1) {
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
			Shader.SetGlobalTexture("_DiffCubeIBL", rtex);
			Shader.SetGlobalTexture("_SpecCubeIBL", rtex);
		}
		
		cam.transform.position = cameraPos;
		//cam.RenderToCubemap (this.specularCube, faceMask);
		 //this.diffuseCube.hideFlags = HideFlags.HideAndDontSave;
		cam.RenderToCubemap (rtex, faceMask);
		//rtex.mipMapBias = 3.5f;
		//rtex.SmoothEdges (8);
		/*
		if (!diff) {
			diff = rtex;
		}
*/
		if (faceToRender >= 0) {
			faceToRender = faceToRender % 6;
			rtex = FastBlur (rtex, faceToRender, radius, iterations);
		//this.diffuseCube = rtex;
		} else {
			for (int i = 0; i < 6; ++i) {
				rtex = FastBlur (rtex, i, radius, iterations);
			}
		}
		//this.diffuseCube = rtex;
		//this.specularCube = rtex;
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
					for (x = xx; (x < xx + blurSize); ++x) {
						if (x < _W) {
							AddPixel(image.GetPixel(face, x, yy));
						} else { // over right pixel
							int index_x = x - _W;
							int index_y = yy;
							Color col = getOverRightPixel(image, face, index_x, index_y);
							//Color col = new Color(0,0,255);
							AddPixel(col);
						}
					}

					//Left side of pixel
					for (x = xx; (x > xx - blurSize); --x) {
						if (x < 0) {
							int index_x = 0 - x;
							int index_y = yy;
							Color col = getOverLeftPixel(image, face, index_x, index_y);
							AddPixel(col);
						} else {// over left pixel
							AddPixel(image.GetPixel( face, x, yy));
						}
					}

					CalcPixel();

					//for (x = xx; x < xx + blurSize && x < _W; ++x) {
						blurred.SetPixel(face, xx, yy, new Color(avgR, avgG, avgB, 1.0f));
					//}
				}
			}
		} else {
			for (xx = 0; xx < _W; ++xx) {
				for (yy = 0; yy < _H; ++yy) {
					ResetPixel();

					// Over pixel
					for (y = yy; (y < yy + blurSize); ++y) {
						if (y < _H) {
							AddPixel(image.GetPixel(face, xx, y));
						} else {
							int index_x = xx;
							int index_y = y - _H;
							Color col = getOverBottomPixel(image, face, index_x, index_y);
							AddPixel(col);
						}
					}

					// Under pixel
					for (y = yy; (y > yy - blurSize); --y) {
						if (y < 0) {
							int index_x = xx;
							int index_y = 0 - y;
							Color col = getOverTopPixel(image, face, index_x, index_y);
							AddPixel(col);
						} else {
							AddPixel(image.GetPixel(face, xx, y));
						}
					}

					CalcPixel();

					//for (y = yy; (y < yy + blurSize && y < _H); ++y) {
						blurred.SetPixel( face, xx, yy, new Color(avgR, avgG, avgB, 1.0f));
					//}
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

	Color getOverRightPixel(Cubemap image, CubemapFace face, int index_x, int index_y) {
		Color pixCol;
		CubemapFace targetFace = CubemapFace.NegativeX;
		int x=0, y=0, axis;  // axis= +-1(x) +-2(y)
		if (face == CubemapFace.PositiveX) {
			targetFace = CubemapFace.NegativeZ;
			x = index_x;
			y = index_y;
		} else if (face == CubemapFace.PositiveZ) {
			targetFace = CubemapFace.PositiveX;
			x = index_x;
			y = index_y;
		} else if (face == CubemapFace.NegativeZ) {
			targetFace = CubemapFace.NegativeX;
			x = index_x;
			y = index_y;
		} else if (face == CubemapFace.NegativeX) {
			targetFace = CubemapFace.PositiveZ;
			x = index_x;
			y = index_y;
		} else if (face == CubemapFace.PositiveY) {
			targetFace = CubemapFace.PositiveX;
			x = cubemapSize - index_y;
			y = index_x;
		} else if (face == CubemapFace.NegativeY) {
			targetFace = CubemapFace.PositiveX;
			x = index_y;
			y = cubemapSize - index_x;
		} 
		pixCol = image.GetPixel (targetFace, x, y);
		return pixCol;
	}
	Color getOverLeftPixel(Cubemap image, CubemapFace face, int index_x, int index_y) {
		Color pixCol;
		CubemapFace targetFace = CubemapFace.NegativeX;
		int x=0, y=0, axis;  // axis= +-1(x) +-2(y)
		if (face == CubemapFace.PositiveX) {
			targetFace = CubemapFace.PositiveZ;
			x = cubemapSize - index_x;
			y = index_y;
		} else if (face == CubemapFace.PositiveZ) {
			targetFace = CubemapFace.NegativeX;
			x = cubemapSize - index_x;
			y = index_y;
		} else if (face == CubemapFace.NegativeZ) {
			targetFace = CubemapFace.PositiveX;
			x = cubemapSize - index_x;
			y = index_y;
		} else if (face == CubemapFace.NegativeX) {
			targetFace = CubemapFace.NegativeZ;
			x = cubemapSize - index_x;
			y = index_y;
		} else if (face == CubemapFace.PositiveY) {
			targetFace = CubemapFace.NegativeX;
			x = index_y;
			y = index_x;
		} else if (face == CubemapFace.NegativeY) {
			targetFace = CubemapFace.NegativeX;
			x = index_y;
			y = cubemapSize - index_x;
		} 
		pixCol = image.GetPixel (targetFace, x, y);
		//pixCol = new Color (0, 0, 200);
		return pixCol;
	}
	Color getOverBottomPixel(Cubemap image, CubemapFace face, int index_x, int index_y) {
		Color pixCol;
		CubemapFace targetFace = CubemapFace.NegativeX;
		int x=0, y=0, axis;  // axis= +-1(x) +-2(y)
		if (face == CubemapFace.PositiveX) {
			targetFace = CubemapFace.NegativeY;
			x = cubemapSize - index_y;
			y = index_x;
		} else if (face == CubemapFace.PositiveZ) {
			targetFace = CubemapFace.NegativeY;
			x = index_x;
			y = index_y;
		} else if (face == CubemapFace.NegativeZ) {
			targetFace = CubemapFace.NegativeY;
			x = cubemapSize - index_x;
			y = cubemapSize - index_y;
		} else if (face == CubemapFace.NegativeX) {
			targetFace = CubemapFace.NegativeY;
			x = index_y;
			y = cubemapSize - index_x;
		} else if (face == CubemapFace.PositiveY) {
			targetFace = CubemapFace.PositiveZ;
			x = index_x;
			y = cubemapSize - index_y;
		} else if (face == CubemapFace.NegativeY) {
			targetFace = CubemapFace.NegativeZ;
			x = cubemapSize - index_x;
			y = index_y;
		} 
		pixCol = image.GetPixel (targetFace, x, y);
		//pixCol = new Color (0, 0, 200);
		return pixCol;
	}
	Color getOverTopPixel(Cubemap image, CubemapFace face, int index_x, int index_y) {
		Color pixCol;
		CubemapFace targetFace = CubemapFace.NegativeX;
		int x=0, y=0, axis;  // axis= +-1(x) +-2(y)
		if (face == CubemapFace.PositiveX) {
			targetFace = CubemapFace.PositiveY;
			x = cubemapSize - index_y;
			y = index_x;
		} else if (face == CubemapFace.PositiveZ) {
			targetFace = CubemapFace.PositiveY;
			x = index_x;
			y = cubemapSize - index_y;
		} else if (face == CubemapFace.NegativeZ) {
			targetFace = CubemapFace.PositiveY;
			x = cubemapSize - index_x;
			y = cubemapSize - index_y;
		} else if (face == CubemapFace.NegativeX) {
			targetFace = CubemapFace.PositiveY;
			x = index_y;
			y = index_x;
		} else if (face == CubemapFace.PositiveY) {
			targetFace = CubemapFace.NegativeZ;
			x = cubemapSize - index_x;
			y = cubemapSize - index_y;
		} else if (face == CubemapFace.NegativeY) {
			targetFace = CubemapFace.PositiveZ;
			x = index_x;
			y = index_y;
		} 
		pixCol = image.GetPixel (targetFace, x, y);
		//pixCol = new Color (0, 0, 200);
		return pixCol;
	}
}
