using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class test : SetupLux {
	public int cubemapSize = 16;
	public GameObject targetObj;
	public float gamma =  1.15f;
	private Vector3 cameraPos = new Vector3 (10, 0, 0);
	private Camera cam;
	private Cubemap diff;
	private Cubemap spec;
	private int bias = 8;

	private int interval = 8;

	// for blur
	public int radius = 8;
	public int iterations = 1;
	private float avgR = 0;
	private float avgG = 0;
	private float avgB = 0;
	private float avgA = 0;
	private float blurPixelCount = 0;
	
	// Use this for initialization
	public override void Start () {
		base.Start ();
		updateCameraPosition ();
		UpdateCubemap (63);
	}
	
	// Update is called once per frame
	public override void Update () {
		base.Update ();
		updateCameraPosition ();
	}

	void updateCameraPosition() {
		if (targetObj != null) {
			cameraPos = targetObj.transform.localPosition + new Vector3(10, 0, 0);
		}
		print (cameraPos.x);
	}

	void LateUpdate(){
		int count = Time.frameCount % interval;
		if (count == 0) {
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
		
		if (!spec && !diff) {
			spec = new Cubemap(cubemapSize, TextureFormat.ARGB32, true);
			diff = new Cubemap(cubemapSize, TextureFormat.ARGB32, true);
			spec.filterMode = FilterMode.Trilinear;
			diff.filterMode = FilterMode.Trilinear;
			spec.hideFlags = HideFlags.HideAndDontSave;
			diff.hideFlags = HideFlags.HideAndDontSave;
			Shader.SetGlobalTexture("_SpecCubeIBL", spec);
			Shader.SetGlobalTexture("_DiffCubeIBL", diff);
		}
		
		cam.transform.position = cameraPos;
		cam.RenderToCubemap(spec, faceMask);

		if (faceToRender >= 0) {
			faceToRender = faceToRender % 6;
		} else {
			for (int i = 0; i < 6; ++i) {
				CubemapFace face = (CubemapFace) i;
				diff.SetPixels(GammaCorrection(FastBlur ( spec, face, radius, iterations), cubemapSize, cubemapSize, gamma), face);
			}
			diff.Apply();
		}
	}

	Color[] GammaCorrection(Color[] input, int width, int height, float gamma) {
		Color[] output = new Color[width * height];
		for (int w = 0; w < width; ++w) {
			for (int h = 0; h < height; ++h) {
				output[width * w + h].r = 255.0f * Mathf.Pow(1.0f / 255.0f * input[width * w + h].r, 1.0f / gamma);
				output[width * w + h].g = 255.0f * Mathf.Pow(1.0f / 255.0f * input[width * w + h].g, 1.0f / gamma);
				output[width * w + h].b = 255.0f * Mathf.Pow(1.0f / 255.0f * input[width * w + h].b, 1.0f / gamma);
				output[width * w + h].a = 255.0f;
			}
		}
		return output;
	}
	
	Color[] FastBlur(Cubemap input, CubemapFace face, int radius, int iterations) {
		Cubemap output = new Cubemap (input.height, TextureFormat.ARGB32, true);
		// copy cubemap
		for (int i = 0; i < 6; ++i) {
			CubemapFace targetFace = (CubemapFace) i;
			output.SetPixels(input.GetPixels(targetFace),targetFace);
		}
		output.Apply();

		for(int i = 0; i < iterations; ++i) {
			// x 
			output.SetPixels(BlurImage( output, face, radius, true), face);
			output.Apply();
			// y
			output.SetPixels(BlurImage( output, face, radius, false), face);
			output.Apply();
		}

		return output.GetPixels(face);
	}

	Color[] BlurImage(Cubemap image, CubemapFace face, int blurSize, bool horizontal) {
		Cubemap blurred = new Cubemap (image.height, TextureFormat.ARGB32, true);
		int _W = cubemapSize;
		int _H = cubemapSize;
		int xx, yy, x, y;
		float colBias = 1.0f;
		if(horizontal) {
			for (yy = 0; yy < _H; ++ yy) {
				for( xx = 0; xx < _W; ++xx) {
					ResetPixel();

					//Right side of pixel
					for (x = xx; (x < xx + blurSize); ++x) {
						if (x < _W) {
							AddPixel(image.GetPixel(face,  x, yy));
						} else { 
							int index_x = x - _W;
							int index_y = yy;
							Color col = getOverRightPixel(image, face, index_x, index_y);
							AddPixel(col);
						}
					}

					//Left side of pixel
					for (x = xx; (x > xx - blurSize); --x) {
						if (x <= 0) {
							int index_x = 0 - x;
							int index_y = yy;
							Color col = getOverLeftPixel(image, face, index_x, index_y);
							AddPixel(col);
						} else {
							AddPixel(image.GetPixel( face, x, yy));
						}
					}

					CalcPixel();

					blurred.SetPixel(face, xx, yy, new Color(colBias*avgR, colBias*avgG, colBias*avgB, 1.0f));
				}
			}
		} else {
			for (xx = 0; xx < _W; ++xx) {
				for (yy = 0; yy < _H; ++yy) {
					ResetPixel();

					// Over pixel
					for (y = yy; (y < yy + blurSize); ++y) {
						if (y <= _H) {
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

					blurred.SetPixel( face, xx, yy, new Color(colBias* avgR, colBias*avgG,colBias* avgB, 1.0f));
				}
			}
		}
		blurred.Apply ();
		return blurred.GetPixels(face);
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
	}

	Color getOverRightPixel(Cubemap image, CubemapFace face, int index_x, int index_y) {
		Color pixCol;
		CubemapFace targetFace = CubemapFace.NegativeX;
		int x = 0, y = 0;
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
		int x = 0, y = 0;
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
		return pixCol;
	}
	Color getOverBottomPixel(Cubemap image, CubemapFace face, int index_x, int index_y) {
		Color pixCol;
		CubemapFace targetFace = CubemapFace.NegativeX;
		int x = 0, y = 0;
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
		return pixCol;
	}
	Color getOverTopPixel(Cubemap image, CubemapFace face, int index_x, int index_y) {
		Color pixCol;
		CubemapFace targetFace = CubemapFace.NegativeX;
		int x = 0, y = 0;
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
		return pixCol;
	}
}
