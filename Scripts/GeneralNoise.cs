using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using SimplexNoise;
using System.Linq;
using System.Diagnostics;

public class GeneralNoise
{
	public bool _DEBUG = true;
	//TODO add compatible stuff- and wave
	public NoiseInfo noiseInfo;
	public List<GeneralNoise> weightedNoises;
	public List<float> noiseWeights;
	public float[] noise1D;
	public float[,] noise2D;
	public float[,,] noise3D;
	public float[] resultNoise1D;
	public float[,] resultNoise2D;
	public float[,,] resultNoise3D;
	bool computed = false;
	//TODO decide where to include squashing factor+height offset

	public GeneralNoise(NoiseInfo noiseInfo)
	{
		if (_DEBUG)
		{
			GD.Print($"Noise dimension is: {noiseInfo.dimension}\nNoise type is {noiseInfo.basicNoiseType}\nNoise amplitude is {noiseInfo.amplitude}");
		}
		this.noiseInfo = noiseInfo;
		weightedNoises = new List<GeneralNoise>();
		noiseWeights = new List<float>();
		// if (noiseInfo.basicNoiseType == BasicNoiseType.FastNoiseLite)
		// {
		// 	//TODO use set fns for FastNoiseLites
		// }
		Noise.Seed = noiseInfo.seed;
		if (noiseInfo.dimension == Dimension.D1)
		{
			noise1D = Noise.Calc1D((int)noiseInfo.dimensions.x, noiseInfo.scale);
		}
		else if (noiseInfo.dimension == Dimension.D2)
		{
			// if(noiseInfo.basicNoiseType == BasicNoiseType.Wave)
			// {
			// 	SetWaveNoise2D();
			// }
			noise2D = Noise.Calc2D((int)noiseInfo.dimensions.x, (int)noiseInfo.dimensions.z, noiseInfo.scale);
		}
		else if (noiseInfo.dimension == Dimension.D3)
		{
			noise3D = Noise.Calc3D((int)noiseInfo.dimensions.x, (int)noiseInfo.dimensions.z, (int)noiseInfo.dimensions.y, noiseInfo.scale);
		}

		FlattenData();
		
		//reverses two's complement interpretation
		for (int i = 0; i < noise1D.Length; i++)
		{
			noise1D[i] = (noise1D[i] - 128f) / 256;
		}

		UnflattenData();

		TestNoise();
	}

	private void TestNoise()
	{
		bool testCaseNormalization = true;
		foreach (float noiseValue in noise1D)
		{
			if (noiseValue > noiseInfo.amplitude || noiseValue < -noiseInfo.amplitude)
			{
				testCaseNormalization = false;
				break;
			}
		}
		GD.Print($"Values Normalized: {testCaseNormalization}");
		GD.Print($"Values ranged from {noise1D.Min()} to {noise1D.Max()}");
		Debug.Assert(testCaseNormalization);
	}

	private void FlattenData()
	{
		if (noiseInfo.dimension == Dimension.D1)
		{
			return;
		}
		else if (noiseInfo.dimension == Dimension.D2)
		{
			noise1D = new float[(int)(noiseInfo.dimensions.x * noiseInfo.dimensions.z)];
			for (int x = 0; x < noiseInfo.dimensions.x; x++)
			{
				for (int z = 0; z < noiseInfo.dimensions.z; z++)
				{
					noise1D[(int)(z * noiseInfo.dimensions.x + x)] = noise2D[x, z];
				}
			}
		}
		else if (noiseInfo.dimension == Dimension.D3)
		{
			noise1D = new float[(int)(noiseInfo.dimensions.x * noiseInfo.dimensions.y * noiseInfo.dimensions.z)];
			for (int x = 0; x < noiseInfo.dimensions.x; x++)
			{
				for (int z = 0; z < noiseInfo.dimensions.z; z++)
				{
					for (int y = 0; y < noiseInfo.dimensions.y; y++)
					{
						noise1D[(int)(y * noiseInfo.dimensions.x * noiseInfo.dimensions.z +
							z * noiseInfo.dimensions.x +
							x)] = noise2D[x, z];
					}
				}
			}
		}
	}

	private void UnflattenData()
	{
		int xDim = (int)noiseInfo.dimensions.x;
		int yDim = (int)noiseInfo.dimensions.y;
		int zDim = (int)noiseInfo.dimensions.z;
		if (noiseInfo.dimension == Dimension.D1)
		{
			return;
		}
		else if (noiseInfo.dimension == Dimension.D2)
		{
			for (int i = 0; i < noise1D.Length; i++)
			{
				noise2D[i % xDim, i / xDim] = noise1D[i];
			}
		}
		else if (noiseInfo.dimension == Dimension.D3)
		{
			for (int i = 0; i < noise1D.Length; i++)
			{
				noise3D[i % xDim % zDim, i % xDim / zDim, i / xDim / zDim] = noise1D[i];
			}
		}
	}

	private void NormalizeData(IEnumerable<double> data, int min, int max)
	{
		double dataMax = data.Max();
		double dataMin = data.Min();
		double range = dataMax - dataMin;
		//https://stackoverflow.com/questions/5383937/array-data-normalization
		noise1D = noise1D.Select(d => (d - dataMin) / range)
		.Select(n => (float)((1 - n) * min + n * max))
		.ToArray();

		//Unflatten:
		if (noiseInfo.dimension == Dimension.D1)
		{
			return;
		}
		else if (noiseInfo.dimension == Dimension.D2)
		{
			for (int x = 0; x < noiseInfo.dimensions.x; x++)
			{
				for (int z = 0; z < noiseInfo.dimensions.z; z++)
				{
					noise2D[x, z] = noise1D[(int)(x * noiseInfo.dimensions.x + z)];
				}
			}
		}
		else if (noiseInfo.dimension == Dimension.D3)
		{
			for (int x = 0; x < noiseInfo.dimensions.x; x++)
			{
				for (int z = 0; z < noiseInfo.dimensions.z; z++)
				{
					for (int y = 0; y < noiseInfo.dimensions.y; y++)
					{
						noise2D[x, z] = noise1D[(int)(x * noiseInfo.dimensions.x * noiseInfo.dimensions.z +
							z * noiseInfo.dimensions.z +
							y)];
					}
				}
			}
		}
	}

	public void AddNoise(GeneralNoise generalNoise, float weight)
	{
		computed = false;
		weightedNoises.Add(generalNoise);
		noiseWeights.Add(weight);
	}

	public void SetWeightedNoiseResult()
	{
		if (computed)
		{
			return;
		}
		if (noiseInfo.dimension == Dimension.D1)
		{
			resultNoise1D = noise1D;
			SetWeightedNoiseResult1D();
		}
		else if (noiseInfo.dimension == Dimension.D2)
		{
			resultNoise2D = noise2D;
			SetWeightedNoiseResult2D();
		}
		else if (noiseInfo.dimension == Dimension.D3)
		{
			resultNoise3D = noise3D;
			SetWeightedNoiseResult3D();
		}
		computed = true;
	}

	private void SetWeightedNoiseResult1D()
	{
		for (int i = 0; i < weightedNoises.Count; i++)
		{
			weightedNoises[i].SetWeightedNoiseResult();
			for (int x = 0; x < noiseInfo.dimensions.x; x++)
			{
				resultNoise1D[x] += noiseWeights[i] * weightedNoises[i].resultNoise1D[x];
			}
		}
	}

	private void SetWeightedNoiseResult2D()
	{
		for (int i = 0; i < weightedNoises.Count; i++)
		{
			weightedNoises[i].SetWeightedNoiseResult();
			for (int x = 0; x < noiseInfo.dimensions.x; x++)
			{
				for (int z = 0; z < noiseInfo.dimensions.z; z++)
				{
					resultNoise2D[x, z] += noiseWeights[i] * weightedNoises[i].resultNoise2D[x, z];
				}
			}
		}
	}

	private void SetWeightedNoiseResult3D()
	{
		for (int i = 0; i < weightedNoises.Count; i++)
		{
			weightedNoises[i].SetWeightedNoiseResult();
			for (int x = 0; x < noiseInfo.dimensions.x; x++)
			{
				for (int z = 0; z < noiseInfo.dimensions.z; z++)
				{
					for (int y = 0; y < noiseInfo.dimensions.y; y++)
					{
						resultNoise3D[x, z, y] += noiseWeights[i] * weightedNoises[i].resultNoise3D[x, z, y];
					}
				}
			}
		}
	}

	private void SetWaveNoise2D()
	{
		float amplitude = noiseInfo.amplitude;
		float frequency = 1 / noiseInfo.scale;
		int x_ = (int)noiseInfo.dimensions.x;
		int z_ = (int)noiseInfo.dimensions.z;
		noise2D = new float[x_, z_];

		for (int x = 0; x < x_; x++)
		{
			for (int z = 0; z < z_; z++)
			{
				noise2D[x, z] = (float)(amplitude * (Math.Sin(x * frequency) + Math.Sin(z * frequency)) / 2f);
				if (_DEBUG)
				{
					GD.Print($"generated value at {x}, {z}: {noise2D[x, z]}");
					if (noise2D[x, z] > amplitude)
					{
						GD.Print("WARNING: wave noise amplitude exceeded on generation?");
					}

				}
			}
		}
	}
}
