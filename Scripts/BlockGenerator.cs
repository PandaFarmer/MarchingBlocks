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
	public Dimension dimension;
	public Vector3 dimensions;
	public float amplitude;
	public float scale;//
	public Vector3 baseOffsets;
	public BasicNoiseType basicNoiseType;
	public int seed;
	public NoiseInfo(Dimension dimension, Vector3 dimensions, float amplitude, float scale, Vector3 baseOffsets, BasicNoiseType basicNoiseType, int seed)
	{
		this.dimension = dimension;
		this.dimensions = dimensions;//flip y z for godot
		this.amplitude = amplitude;
		this.scale = scale;
		this.baseOffsets = baseOffsets;
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
	//"5" noises
	//2d unless caves
	public float Continentalness; //howto determine what layer of noises is this applied? how far down the noise tree does this apply?
	public float ContinentalnessThresholdBottom;
	public float ContinentalnessThresholdTop;
	public float squashingFactor;//erosion? mapped # ranges, ie 0-> -1 to .78, 6-> .55 to 1
	public float heightOffset;
	//last 2 should be soft coupled with prev 3
	public float temperature;
	public float humidity;
	//TODO create thresholds for categorization using tables
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
		Vector3 dimensions = new Vector3(30f, 10f, 30f);
		FillWorld(dimensions);
	}

	public void FillWorld(Vector3 dimensions)
	{
		//TODO replace with whatever initialization of NoiseInfos you want..
		List<NoiseInfo> noiseInfos = new List<NoiseInfo>();
		List<float> noiseWeights = new List<float> { };

		// Gather noise data

		float amplitude = 1f;
		float scale = 5f;
		NoiseInfo terrainNoise2DInfo = new NoiseInfo(Dimension.D2, dimensions, amplitude, scale, Vector3.Zero, BasicNoiseType.Wave, 0);
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
		float noiseValueReverseTwosComplement;//idk why i need this

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
					if (noiseData[x, z] * dimY >= (float)y - halfdimY)
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
