using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class WFCModel: MonoBehaviour
{
    [SerializeField,
     Tooltip("The prefabs of the tiles to place in the level in the same order as defined in data.json")]
    protected List<GameObject> _tilesPrefabs;

    [SerializeField, Tooltip("The gameobject containing the output")]
    protected GameObject _output;

    [SerializeField, Tooltip("The width of the grid to generate on")]
    protected int _width = 10;

    [SerializeField, Tooltip("The height of the grid to generate on")]
    protected int _height = 10;

    [SerializeField, Tooltip("The seed for random generation")]
    protected int _seed = 0;

    protected int _limit = 100;
    // Holds the possibilities of tiles of every position on the grid. Indexes in order: Index, Tile
    protected bool[][] _wave;

    // Holds the constraint rules of which tiles can be next to a tile in every direction. Indexes in order: Direction, Tile, Options
    protected int[][][] _propagator;

    // Holds the amount of tiles that are still compatible with this one. Indexes in order: Index, Tile, Direction
    protected int[][][] _compatible;

    // The random generator for the noise in entropy
    protected System.Random _random;

    // The number of tiles that are possible at a given index
    protected int[] _sumOfPossibilities;

    // Weights
    protected double[] weights;
    protected double[] weightLogWeights;
    protected double sumOfWeights, sumOfWeightLogWeights, startingEntropy;

    protected double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

    // Stack in order: index, tile
    protected Tuple<int, int>[] _stack;
    protected int _stacksize;

    protected int _NbOfTiles;
    protected bool Stop = false;
    protected int[] _correspondingPrefabTiles;

    // Direction and offsets for quick convertion to neighbours
    protected enum Direction : int
    {
        Left = 0, Up = 1, Right = 2, Down = 3
    }
    static int[] XDirectionOffsets = { -1, 0, 1, 0 };
    static int[] YDirectionOffsets = { 0, 1, 0, -1 };
    static readonly int[] OppositeDirection = { 2, 3, 0, 1 };

    protected abstract void Init();
    protected abstract void GetConstraints();
    protected abstract void BuildPropagator();
    protected abstract bool OnBoundary(int x, int y);

    void Start()
    {
        GetConstraints();
        BuildPropagator();
        Init();
        Clear();
        if (Run(_seed, _limit))
        {
            OutputObservations();
            Debug.Log("WFC finished successfully");
        }
        else
            Debug.Log("WFC failed");
    }


    bool Run(int seed, int limit)
    {
        if (seed == 0)
        {
            _random = new System.Random();
        }
        else
        {
            _random = new System.Random(seed);
        }

        for (int l = 0; l < limit || limit == 0; l++)
        {
            bool? result = Observe();
            if (result != null)
                return (bool)result;
            else
                Propagate();

        }

        return true;
    }

    bool? Observe()
    {
        int? node = FindLowestEntropy();
        // If FindLowestEntropy returns null the algorithm failed
        if (node == null)
            return false;

        // If FindLowestEntropy returns -1 the algorithm finished
        if (node == -1)
            return true;

        bool[] w = _wave[(int)node];
        // Create a weighted distribution
        double[] distribution = new double[_NbOfTiles];
        for (int tile = 0; tile < _NbOfTiles; tile++)
        {
            if (w[tile])
            {
                distribution[tile] = weights[tile];
            }
            else
            {
                distribution[tile] = 0;
            }
        }

        int chosenPattern = GetWeightedRandom(distribution, _random.NextDouble());
        // Remove all the other possibilities except the chosen pattern
        for (int tile = 0; tile < _NbOfTiles; tile++)
            if (w[tile] != (tile == chosenPattern))
                Ban((int)node, tile);
        //Debug.Log("position: " + node + " is tile " + chosenPattern + " " + _tiles[chosenPattern]);
        return null;
    }

    int? FindLowestEntropy()
    {
        double min = 1E+3;
        int lowestEntropyIdx = -1;

        for (int i = 0; i < _wave.Length; i++)
        {
            if (OnBoundary(i % _width, i / _width))
                continue;

            int amount = _sumOfPossibilities[i];
            if (amount == 0)
                return null;

            double entropy = entropies[i];
            if (amount > 1 && entropy <= min)
            {
                double noise = 1E-6 * _random.NextDouble();
                if (entropy + noise < min)
                {
                    min = entropy + noise;
                    lowestEntropyIdx = i;
                }
            }
        }
        return lowestEntropyIdx;
    }


    void Propagate()
    {
        // Go over every possibility that has been removed
        while (_stacksize > 0)
        {
            var e1 = _stack[_stacksize - 1];
            //Debug.Log("Last propagation idx: " + e1.Item1 + " tile: " + e1.Item2 + " " + _tiles[e1.Item2]);
            _stacksize--;

            int idx = e1.Item1;
            int x = idx % _width;
            int y = idx / _width;
            bool[] w1 = _wave[idx];

            // Go over all the neighbors
            for (int d = 0; d < 4; d++)
            {
                int xOffset = XDirectionOffsets[d];
                int yOffset = YDirectionOffsets[d];
                int xPropagation = x + xOffset;
                int yPropagation = y + yOffset;
                if (OnBoundary(xPropagation, yPropagation))
                    continue;

                if (xPropagation < 0)
                    xPropagation += _width;
                else if (xPropagation >= _width)
                    xPropagation -= _width;
                if (yPropagation < 0)
                    yPropagation += _height;
                else if (yPropagation >= _height)
                    yPropagation -= _height;

                int idxPropagation = xPropagation + yPropagation * _width;
                int[] p = _propagator[d][e1.Item2];
                int[][] compat = _compatible[idxPropagation];

                // Go over all their potentially valid tiles
                for (int l = 0; l < p.Length; l++)
                {
                    int t2 = p[l];
                    int[] comp = compat[t2];

                    // Remove one compatibility, because this tile possibility has been banned
                    comp[d]--;
                    // Ban the tile if it no longer is compatible with the surrounding tiles
                    if (comp[d] == 0)
                        Ban(idxPropagation, t2);
                    if (Stop)
                        return;
                }
            }
        }
    }
    protected void Ban(int i, int t)
    {
        // Remove this possibility
        _wave[i][t] = false;

        int[] comp = _compatible[i][t];
        for (int d = 0; d < 4; d++)
            comp[d] = 0;

        // Add it to the stack to get the neighbors updated
        _stack[_stacksize] = new Tuple<int, int>(i, t);
        _stacksize++;

        double sum = sumsOfWeights[i];
        entropies[i] += sumsOfWeightLogWeights[i] / sum - Math.Log(sum);

        _sumOfPossibilities[i] -= 1;
        if (_sumOfPossibilities[i] == 0 && !Stop)
        {
            Stop = true;
            DumpWave(i, t);
        }
        sumsOfWeights[i] -= weights[t];
        sumsOfWeightLogWeights[i] -= weightLogWeights[t];

        sum = sumsOfWeights[i];
        entropies[i] -= sumsOfWeightLogWeights[i] / sum - Math.Log(sum);
    }

    void Clear()
    {
        for (int i = 0; i < _wave.Length; i++)
        {
            for (int tile = 0; tile < _NbOfTiles; tile++)
            {
                _wave[i][tile] = true;
                for (int dir = 0; dir < 4; dir++)
                    _compatible[i][tile][dir] = _propagator[OppositeDirection[dir]][tile].Length;
            }
            _sumOfPossibilities[i] = _NbOfTiles;
            sumsOfWeights[i] = sumOfWeights;
            sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
            entropies[i] = startingEntropy;
        }

    }


    public static int GetWeightedRandom(double[] a, double r)
    {
        double sum = a.Sum();

        // Make sure you will not divide later by 0
        if (sum == 0)
        {
            for (int i = 0; i < a.Count(); i++)
                a[i] = 1;
            sum = a.Sum();
        }
        // Weight the array on normalized scale 0 to 1
        for (int i = 0; i < a.Count(); i++)
            a[i] /= sum;

        double partialSum = 0;
        // Get the random index
        for (int idx = 0; idx < a.Count(); idx++)
        {
            partialSum += a[idx];
            if (r <= partialSum)
                return idx;
        }

        return 0;
    }

    void OutputObservations()
    {
        // Create output if it doesn't exist
        if (!_output)
            _output = new GameObject("OutputWFC");

        // Destroy previous output
        for (int i = 0; i < _output.transform.childCount; i++)
        {
            GameObject go = _output.transform.GetChild(i).gameObject;
            Destroy(go);
        }

        // Add new tile prefabs to output
        GameObject[] observed = new GameObject[_width * _height];
        for (int i = 0; i < _wave.Length; i++)
        for (int t = 0; t < _NbOfTiles; t++)
            if (_wave[i][t] && _correspondingPrefabTiles[t] != 0)
            {
                float x = i % _width;
                float z = i / _width;
                observed[i] = Instantiate(_tilesPrefabs[_correspondingPrefabTiles[t]], _output.transform.position + new Vector3(x, 0, z),
                    Quaternion.identity, _output.transform);
                break;
            }
    }


    private void DumpWave(int idxNoPossibilities, int tile)
    {
        for (int idx = 0; idx < _wave.Length; idx++)
        {
            if (idx == idxNoPossibilities)
            {
                Debug.Log("problem sum of index: " + idxNoPossibilities + " is zero. Was tile " + tile);
                float x = idxNoPossibilities % _width;
                float z = idxNoPossibilities / _width;
                Instantiate(_tilesPrefabs[_correspondingPrefabTiles[tile]],
                    _output.transform.position + new Vector3(x, 0, z),
                    Quaternion.identity, _output.transform);
                continue;
            }
            int counter = 0;
            for (int t = 0; t < _NbOfTiles; t++)
            {
                if (!_wave[idx][t] && _correspondingPrefabTiles[t] != 0)
                {
                    float x = idx % _width;
                    float z = idx / _width;
                    Instantiate(_tilesPrefabs[_correspondingPrefabTiles[t]],
                        _output.transform.position + new Vector3(x, (counter + 1) * 2, z),
                        Quaternion.identity, _output.transform);
                    counter++;
                    //break;
                }
            }
        }
    }

}
