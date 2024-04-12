using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using SimplexNoise;
using Newtonsoft.Json;
using System.Diagnostics;
// using System.Numerics;

public struct NoiseInfo
{
	public Dimension dimension;//1D, 2D, 3D..
	public Vector3 dimensions;
	public float amplitude;
	public float scale;//lower values for larger smoother features
	public BasicNoiseType basicNoiseType;
	public int seed;
	public NoiseInfo(Dimension dimension, Vector3 dimensions, float amplitude, float scale, BasicNoiseType basicNoiseType, int seed)
	{
		this.dimension = dimension;
		this.dimensions = dimensions;//flip y z for godot
		this.amplitude = amplitude;
		this.scale = scale;
		this.basicNoiseType = basicNoiseType;
		this.seed = seed;
	}
}

public enum Dimension
{
	D1,
	D2,
	D3
}

public enum BasicNoiseType
{
	Wave,
	Simplex
}

enum TerrainType
{

}

enum Biome
{

}

public struct BiomeInfo
{
	//squashing factor and heightOffset are the only hard "result" values
	//temperature/humidity may influence shading/texturing?
	//otherwise there should be an indirect correlation between each value based on a lookup table?
	public static float ContinentalnessFactor = 100f;
	//"5" noises
	//2d unless caves
	public float Continentalness; //howto determine what layer of noises is this applied? how far down the noise tree does this apply?
	//just influences height really say c*100 c can be a negative #
	// public float ContinentalnessThresholdBottom;
	// public float ContinentalnessThresholdTop;
	public float squashingFactor;//erosion? mapped # ranges, ie 0-> -1 to .78, 6-> .55 to 1
	public float heightOffset;
	//last 2 should be soft coupled with prev 3
	public float temperature;
	public float humidity;
	//TODO create thresholds for categorization using tables
	public BiomeInfo(float Continentalness, 
		float squashingFactor, float heightOffset, float temperature, float humidity)
	{
		this.Continentalness = Continentalness;
		// this.ContinentalnessThresholdBottom = ContinentalnessThresholdBottom;
		// this.ContinentalnessThresholdTop = ContinentalnessThresholdTop;
		this.squashingFactor = squashingFactor;
		this.heightOffset = heightOffset;
		this.temperature = temperature;
		this.humidity = humidity;
	}
}

//for caves
//abs(3d) + abs(3d) for noodles
//sq(3D) + sq(3d) for caverns..
//combine both as well..

//++ add some filters and biome types

public class BlockGenerator : Spatial
{
	public bool _DEBUG = true;
	Dictionary<string, string> BlockTexturePaths;
	Dictionary<string, int> BlockIdMapping;
	Dictionary<int, string> BlockIdReverseMapping;

	public override void _Ready()
	{
		// Vector3 dimensions = new Vector3(2f, 2f, 2f);
		// Vector3 dimensions = new Vector3(5f, 5f, 5f);
		Vector3 dimensions = new Vector3(30f, 30f, 30f);
		FillWorld(dimensions);
	}

	public void FillWorld(Vector3 dimensions)
	{
		//TODO replace with whatever initialization of NoiseInfos you want..
		List<NoiseInfo> noiseInfos = new List<NoiseInfo>();
		List<float> noiseWeights = new List<float> { };

		// Gather noise data

		float amplitude = 1f;//should root amplitude be dominant? make sure normalization happens leaf to root?
		float scale = .1f;//inverse scale for larger smoother features
		NoiseInfo terrainNoise2DInfo = new NoiseInfo(Dimension.D2, dimensions, amplitude, scale, BasicNoiseType.Wave, 0);
		GeneralNoise terrainNoise2D = new GeneralNoise(terrainNoise2DInfo);
		terrainNoise2D.SetWeightedNoiseResult();

		// Do something with this data...
		SpatialMaterial basicBlockMaterial = new SpatialMaterial();
		basicBlockMaterial.ResourcePath = "res://BaseBlockTexture.png";
		// var texture_path = String.Format("res://BaseBlockTexture.png");
		// var texture = GD.Load<Texture>(texture_path);
		float[,] noiseData = terrainNoise2D.noise2D;
		//keep in mind y is up
		float dimY = dimensions.y;
		float halfdimY = dimY / 2f;

		float continentalness = 1f;
		float squashingFactor = .5f;//must b a value between 0 and 1 for proper results
		float heightOffset = 0f;//value between 0 and dimY
		BiomeInfo biomeInfo = new BiomeInfo(continentalness, squashingFactor, heightOffset, 1f, 1f);
		//BiomeInfo(Continentalness, 
		//squashingFactor, heightOffset, temperature, humidity);

		for (int x = 0; x < dimensions.x; x++)
		{
			for (int y = 0; y < dimensions.y; y++)
			{
				for (int z = 0; z < dimensions.z; z++)
				{
					if (_DEBUG)
					{
						GD.Print($"value for {x},{z} was {noiseData[x, z]}");
					}
					if (noiseData[x, z] * dimY * biomeInfo.squashingFactor + biomeInfo.heightOffset >= (float)y )
					// if(noiseData[x, z] <= .5f)
					{
						CSGBox csgbox = new CSGBox();
						// csgbox3d.Texture = texture;

						csgbox.Material = basicBlockMaterial;
						csgbox.Translation = new Vector3(x, y, z);
						AddChild(csgbox);
					}

				}
			}
		}
	}

	public float[,,] CaveNoiseValues(NoiseInfo noiseInfoA, NoiseInfo noiseInfoB, bool isNoodlyTunnels)
	{
		int xDim = (int)noiseInfoA.dimensions.x;
		int yDim = (int)noiseInfoA.dimensions.y;
		int zDim = (int)noiseInfoA.dimensions.z;
		if(noiseInfoA.dimensions != noiseInfoB.dimensions)
		{
			GD.Print("Warning, mismatched dimensions in CaveNoiseValue generation");
			Debug.Assert(false);
			return null;
		}
		GeneralNoise generalNoiseA = new GeneralNoise(noiseInfoA);
		GeneralNoise generalNoiseB = new GeneralNoise(noiseInfoB);
		float[,,] caveNoiseValues = new float[xDim, yDim, zDim];
		if(isNoodlyTunnels)
		{
			for(int x = 0; x < xDim; x++)
			{
				for(int y = 0; y < yDim; y++)
				{
					for(int z = 0; z < zDim; z++)
					{
						caveNoiseValues[x, y, z] = Math.Abs(generalNoiseA.noise3D[x, y, z]) + Math.Abs(generalNoiseB.noise3D[x, y, z]);
					}
				}
			}
			return caveNoiseValues;
		}

		for(int x = 0; x < xDim; x++)
			{
				for(int y = 0; y < yDim; y++)
				{
					for(int z = 0; z < zDim; z++)
					{
						caveNoiseValues[x, y, z] = (float)Math.Pow(generalNoiseB.noise3D[x, y, z], 2) + 
							(float)Math.Pow(generalNoiseB.noise3D[x, y, z], 2);
					}
				}
			}
			return caveNoiseValues;
	}

	public float[,,] ScalarFieldFromGeneralNoise(Vector3 generationDimensions, GeneralNoise generalNoise)
	{
		if (generalNoise.noiseInfo.dimension == Dimension.D1)
		{
			GD.Print("Cannot generate 3D scalar field from 1D rn");
			Debug.Assert(false);
		}
		if (generalNoise.noiseInfo.dimension == Dimension.D2)
		{
			if (generationDimensions.y != generalNoise.noiseInfo.dimensions.y ||
				generationDimensions.x != generalNoise.noiseInfo.dimensions.x)
			{
				GD.Print("Dimension mismatch, aborting 3D scalar field generation");
				Debug.Assert(false);
			}
		}
		if (generalNoise.noiseInfo.dimension == Dimension.D3 && generationDimensions != generalNoise.noiseInfo.dimensions)
		{
			GD.Print("Dimension mismatch, aborting 3D scalar field generation");
			Debug.Assert(false);
		}
		float[,,] scalarField = new float[(int)generationDimensions.x, (int)generationDimensions.y, (int)generationDimensions.z];

		float valueOverThreshold = 128f;
		float valueUnderThreshold = -128f;
		float[,] noiseData = generalNoise.noise2D;
		NoiseInfo noiseInfo = generalNoise.noiseInfo;
		//keep in mind y is up
		float dimY = generationDimensions.y;
		float halfdimY = dimY / 2f;
		float noiseValueReverseTwosComplement;//idk why i need this

		for (int x = 0; x < generationDimensions.x; x++)
		{
			for (int y = 0; y < generationDimensions.y; y++)
			{
				for (int z = 0; z < generationDimensions.z; z++)
				{
					if (generalNoise.noiseInfo.dimension == Dimension.D2)
					{
						noiseValueReverseTwosComplement = (noiseData[x, z] - 128f) / 32;
						if (_DEBUG)
						{
							GD.Print($"value for {x},{z} was {noiseValueReverseTwosComplement}");
						}
						if (noiseValueReverseTwosComplement * dimY >= (float)y - halfdimY)
						{
							scalarField[x, y, z] = valueOverThreshold;
						}
						else
						{
							scalarField[x, y, z] = valueUnderThreshold;
						}
					}
					if (generalNoise.noiseInfo.dimension == Dimension.D3)
					{
						scalarField[x, y, z] = generalNoise.resultNoise3D[x, y, z];
					}


				}
			}
		}
		return scalarField;
	}

	public void LoadContent()
	{
		string content;
		File file = new File();
		file.Open("res://Textures.json", File.ModeFlags.Read);
		content = file.GetAsText();
		file.Close();
		// string fileName = "C:\\Users\\NoSpacesForWSL\\OneDrive\\Documents\\GODOTProjs\\BasicBuilder\\Buildables.json";
		// BuildableInfoListWrapper buildableInfoListWrapper = JsonConvert.DeserializeObject<BuildableInfoListWrapper>(content);

	}

	public void LoadTextures()
	{
		// texture_path = String.Format("res://Textures/{0}/{1}{2}_size{3}.png", buildableInfo.path_name, buildableInfo.path_name, texture_path_prefix, medium_texture_path_suffix);
		// texture = GD.Load<Texture>(texture_path);

		// CSGBox3D csgbox3D = new CSGBox3D();

		// csgbox3D.Material.Resource = ;
	}
}
