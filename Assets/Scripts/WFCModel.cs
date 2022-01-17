using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
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

    [SerializeField, Tooltip("The amount of times to retry if wfc fails")]
    private int _retries = 3;

    [SerializeField, Tooltip("Whether or not to have an empty border in the output to prevent open rooms at the border")]
    private bool _emptyBorder = true;

    protected int _limit = 100;
    
    // Hold the names of the tiles
    protected List<string> _tiles = new List<string>();

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

    // Weights and Entropy
    protected double[] _weights;
    protected double[] _weightLogWeights;
    protected double _sumOfWeights, _sumOfWeightLogWeights, _startingEntropy;

    protected double[] _sumsOfWeights, _sumsOfWeightLogWeights, _entropies;

    // Stack in order: index, tile
    protected Tuple<int, int>[] _stack;
    protected int _stacksize;

    protected int _nbOfPatterns;
    protected int _nbOfTiles;
    protected bool _stop = false;
    protected int[] _correspondingPrefabTiles;

    // Direction and offsets for quick convertion to neighbours
    protected enum Direction : int
    {
        Left = 0, Up = 1, Right = 2, Down = 3
    }
    protected static int[] XDirectionOffsets = { -1, 0, 1, 0 };
    protected static int[] YDirectionOffsets = { 0, 1, 0, -1 };
    static readonly int[] OppositeDirection = { 2, 3, 0, 1 };

    protected abstract void PatternsFromSample();
    protected abstract void BuildPropagator();
    protected abstract bool OnBoundary(int x, int y);
    protected abstract int Sample(int x, int y);

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        PatternsFromSample();
        BuildPropagator();
        Init();
        int r = _retries;
        while (r > 0)
        {
            r--;
            Clear();
            if (Run(_seed, _limit))
            {
                OutputObservations();
                Debug.Log("WFC finished successfully!");
                break;
            }
            else
            {
                Debug.Log("WFC failed! :(");
            }
        }
    }

    private void Init()
    {
        _wave = new bool[_width * _height][];
        _compatible = new int[_wave.Length][][];
        for (int i = 0; i < _wave.Length; i++)
        {
            _wave[i] = new bool[_nbOfPatterns];
            _compatible[i] = new int[_nbOfPatterns][];
            for (int tile = 0; tile < _nbOfPatterns; tile++)
                _compatible[i][tile] = new int[4];
        }

        // Calculate weights for entropy
        _weightLogWeights = new double[_nbOfPatterns];
        _sumOfWeights = 0;
        _sumOfWeightLogWeights = 0;

        for (int t = 0; t < _nbOfPatterns; t++)
        {
            _weightLogWeights[t] = _weights[t] * Math.Log(_weights[t]);
            _sumOfWeights += _weights[t];
            _sumOfWeightLogWeights += _weightLogWeights[t];
        }

        _startingEntropy = Math.Log(_sumOfWeights) - _sumOfWeightLogWeights / _sumOfWeights;

        _sumOfPossibilities = new int[_width * _height];
        _sumsOfWeights = new double[_width * _height];
        _sumsOfWeightLogWeights = new double[_width * _height];
        _entropies = new double[_width * _height];

        _stack = new Tuple<int, int>[_wave.Length * _nbOfPatterns];
        _stacksize = 0;
        _limit = _width * _height;
    }

    bool Run(int seed, int limit)
    {
        if (seed == 0)
            _random = new System.Random();
        else
            _random = new System.Random(seed);

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
        double[] distribution = new double[_nbOfPatterns];
        for (int tile = 0; tile < _nbOfPatterns; tile++)
        {
            if (w[tile])
            {
                distribution[tile] = _weights[tile];
            }
            else
            {
                distribution[tile] = 0;
            }
        }

        int chosenPattern = GetWeightedRandom(distribution, _random.NextDouble());
        // Remove all the other possibilities except the chosen pattern
        for (int tile = 0; tile < _nbOfPatterns; tile++)
            if (w[tile] != (tile == chosenPattern))
                Ban((int)node, tile);
        Debug.Log("position: " + node + " is tile " + chosenPattern + " " + _tiles[chosenPattern]);
        
        // Show the tiles that is placed below
        //float x = (int)node % _width;
        //float z = (int)node / _width;
        //if (_sumOfPossibilities[(int)node] == 1)
        //{
        //    Instantiate(_tilesPrefabs[_correspondingPrefabTiles[chosenPattern]],
        //        _output.transform.position + new Vector3(x, -2, z),
        //        Quaternion.identity, _output.transform);
        //}

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

            double entropy = _entropies[i];
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
                    if (_stop)
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

        double sum = _sumsOfWeights[i];
        _entropies[i] += _sumsOfWeightLogWeights[i] / sum - Math.Log(sum);

        _sumOfPossibilities[i] -= 1;

        // Stop and show debug info in algorithm fails
        if (_sumOfPossibilities[i] == 0 && !_stop)
        {
            _stop = true;
            DumpWave(i, t);
        }

        _sumsOfWeights[i] -= _weights[t];
        _sumsOfWeightLogWeights[i] -= _weightLogWeights[t];

        sum = _sumsOfWeights[i];
        _entropies[i] -= _sumsOfWeightLogWeights[i] / sum - Math.Log(sum);
    }

    public void Clear()
    {
        _stop = false;

        // Create output if it doesn't exist
        if (!_output)
            _output = new GameObject("OutputWFC");

        // Destroy previous output
        GameObject[] allChildrenToDestroy = new GameObject[_output.transform.childCount];
        for (int i = 0; i < _output.transform.childCount; i++)
        {
            allChildrenToDestroy[i] = _output.transform.GetChild(i).gameObject;
        }
        
        foreach (GameObject child in allChildrenToDestroy)
        {
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        // Reset wave, compatible, weights and entropies
        for (int i = 0; i < _wave.Length; i++)
        {
            for (int tile = 0; tile < _nbOfPatterns; tile++)
            {
                _wave[i][tile] = true;
                for (int dir = 0; dir < 4; dir++)
                {
                    int oppositeDirection = OppositeDirection[dir];
                    int temp = _propagator[oppositeDirection][tile].Length;
                    _compatible[i][tile][dir] = temp;
                }
            }
            _sumOfPossibilities[i] = _nbOfPatterns;
            _sumsOfWeights[i] = _sumOfWeights;
            _sumsOfWeightLogWeights[i] = _sumOfWeightLogWeights;
            _entropies[i] = _startingEntropy;
        }

        // Create empty border in the output to prevent open rooms at the border
        if (_emptyBorder)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (x == 0 || y == 0 || x == _width - 1 || y == _height - 1)
                    {
                        int chosenPattern = 0;
                        int node = x + y * _width;
                        bool[] w = _wave[node];
                        // Remove all the other possibilities except the chosen pattern
                        for (int tile = 0; tile < _nbOfPatterns; tile++)
                            if (w[tile] != (tile == chosenPattern))
                                Ban(node, tile);
                    }
                }
            }
            Propagate();
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
        // Add new tile prefabs to output
        GameObject[] observed = new GameObject[_width * _height];
        for (int i = 0; i < _wave.Length; i++)
        {
            float x = i % _width;
            float z = i / _width;
            int tilePrefabIdx = Sample((int)x, (int)z);
            if (tilePrefabIdx != -1)
            {
                observed[i] = Instantiate(_tilesPrefabs[tilePrefabIdx],
                    _output.transform.position + new Vector3(x, 0, z),
                    Quaternion.identity, _output.transform);
            }
        }
    }


    private void DumpWave(int idxNoPossibilities, int tile)
    {
        // Overlapping WFC can't have debug visualization
        if (_nbOfPatterns != _nbOfTiles)
        {
            return;
        }

        for (int idx = 0; idx < _wave.Length; idx++)
        {
            float x = idx % _width;
            float z = idx / _width;
            // Show the tile that caused the 0 possibilities
            if (idx == idxNoPossibilities)
            {
                Debug.Log("problem sum of index: " + idxNoPossibilities + " is zero. Was tile " + tile + " " + _tiles[tile]);
                Instantiate(_tilesPrefabs[_correspondingPrefabTiles[tile]],
                    _output.transform.position + new Vector3(x, 0, z),
                    Quaternion.identity, _output.transform);
                continue;
            }
            int counter = 0;
            for (int t = 0; t < _nbOfTiles; t++)
            {
                // Show banned possibilities above the output
                if (!_wave[idx][t] && _correspondingPrefabTiles[t] != 0)
                {
                    
                    Instantiate(_tilesPrefabs[_correspondingPrefabTiles[t]],
                        _output.transform.position + new Vector3(x, (counter + 1) * 2, z),
                        Quaternion.identity, _output.transform);
                    counter++;
                }
            }
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(WFCModel), true)]
public class WFCEditor : Editor
{
    private WFCModel me;

    public void Awake()
    {
        me = (WFCModel)target;
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("GENERATE"))
        {
            me.Generate();
        }
        
        DrawDefaultInspector();
    }
}
#endif