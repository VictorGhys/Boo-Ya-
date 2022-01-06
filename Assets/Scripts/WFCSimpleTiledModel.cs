using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System;
using System.Linq;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class WFCSimpleTiledModel : MonoBehaviour
{
    [SerializeField, Tooltip("The prefabs of the tiles to place in the level in the same order as defined in data.json")]
    private List<GameObject> _tilesPrefabs;

    [SerializeField, Tooltip("The gameobject containing the output")]
    private GameObject _output;

    [SerializeField, Tooltip("The json data file to read from")]
    private TextAsset _json;

    [SerializeField, Tooltip("The name of the subset to use of the xml file")]
    private string _subsetName;

    [SerializeField, Tooltip("The width of the grid to generate on")]
    private int _width = 10;
    [SerializeField, Tooltip("The height of the grid to generate on")]
    private int _height = 10;
    [SerializeField, Tooltip("The seed for random generation")]
    private int _seed = 0;
    [SerializeField, Tooltip("The limit of iterations to run the algorithm, should be width*height")]
    private int _limit = 100;

    private List<string> _tiles = new List<string>();
    private WFCSampleData _sampleData;
    // Holds the possibilities of tiles of every position on the grid. Indexes in order: Index, Tile
    private bool[][] _wave;
    // Holds the constraint rules of which tiles can be next to a tile in every direction. Indexes in order: Direction, Tile, Options
    private int[][][] _propagator;
    // Indexes in order: Index, Tile, Direction
    private int[][][] _compatible;

    // The random generator for the noise in entropy
    private System.Random _random;
    // The number of tiles that are possible at a given index
    private int[] _sumOfPossibilities;
    // Weights
    private double[] weights;
    private double[] weightLogWeights;
    private double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    private double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;
    // Stack in order: index, tile
    private Tuple<int, int>[] _stack;
    private int _stacksize;

    private Subset _currentSubset;
    private int[] _correspondingPrefabTiles;

    static int[] XDirectionOffsets = { -1, 0, 1, 0 };
    static int[] YDirectionOffsets = { 0, 1, 0, -1 };
    static readonly int[] OppositeDirection = { 2, 3, 0, 1 };

    enum Direction : int
    {
        Left = 0, Up = 1, Right = 2, Down = 3
    }

    void Start()
    {
        PatternsFromJson();
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

    void Init()
    { 
        _wave = new bool[_width * _height][];
        _compatible = new int[_wave.Length][][];
        for (int i = 0; i < _wave.Length; i++)
        {
            _wave[i] = new bool[_tiles.Count];
            _compatible[i] = new int[_tiles.Count][];
            for (int tile = 0; tile < _tiles.Count; tile++)
                _compatible[i][tile] = new int[4];
        }

        // Calculate weights for entropy
        weightLogWeights = new double[_tiles.Count];
        sumOfWeights = 0;
        sumOfWeightLogWeights = 0;

        for (int t = 0; t < _tiles.Count; t++)
        {
            weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
            sumOfWeights += weights[t];
            sumOfWeightLogWeights += weightLogWeights[t];
        }

        startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

        _sumOfPossibilities = new int[_width * _height];
        sumsOfWeights = new double[_width * _height];
        sumsOfWeightLogWeights = new double[_width * _height];
        entropies = new double[_width * _height];

        _stack = new Tuple<int, int>[_wave.Length * _tiles.Count];
        _stacksize = 0;
    }

    void Update()
    {
        
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
                return (bool) result;
            else
                Propagate();

        }

        return true;
    }

    [Serializable]
    public class WFCSampleData
    {
        public Constraint[] constraints;
        public Subset[] subsets;
    }
    [Serializable]
    public class Constraint
    {
        public string tile;
        public double weight = 1;
        public string[] left;
        public string[] up;
        public string[] right;
        public string[] down;
    }
    [Serializable]
    public class Subset
    {
        public string name;
        public string[] tiles;
    }
    void PatternsFromJson()
    {
       _sampleData = JsonUtility.FromJson<WFCSampleData>(_json.text);

       //foreach (var constraint in _sampleData.constraints)
       //{
       //    _tiles.Add(constraint.tile);
       //}

       _currentSubset = _sampleData.subsets.First(s => s.name == _subsetName);
       if (_currentSubset == null)
       {
           Debug.Log("subset not found!");
           return;
       }

       foreach (var tile in _currentSubset.tiles)
       {
           _tiles.Add(tile);
       }

       // Get the weight of each tile
       weights = new double[_tiles.Count];
       for (int tile = 0; tile < _tiles.Count; tile++)
       {
           weights[tile] = _sampleData.constraints[tile].weight;
       }
    }

    void PaternsFromXML()
    {
        var xdoc = new XmlDocument();
        xdoc.LoadXml(name);
        XmlNode xnode = xdoc.FirstChild;
        xnode = xnode.FirstChild;
        // Read subset
        List<string> subsetTiles = null;
        if (_subsetName != "")
        {
            subsetTiles = new List<string>();
            foreach (XmlNode xsubset in xnode.NextSibling.NextSibling.ChildNodes)
            {
                XmlElement subsetElement = xsubset["name"];
                if (subsetElement == null)
                    continue;

                if (xsubset.NodeType != XmlNodeType.Comment && subsetElement.Value == _subsetName)
                    foreach (XmlNode tile in xsubset.ChildNodes)
                        if(tile["name"] != null)
                            subsetTiles.Add(tile["name"].Value);
            }
        }
        // Read Symmetry and tiles
        foreach (XmlNode xtile in xnode.ChildNodes)
        {
            if(xtile["name"] == null)
                continue;

            string tilename = xtile["name"].Value;
            if (subsetTiles != null && !subsetTiles.Contains(tilename))
                continue;

            _tiles.Add("0" + tilename);
        }
    }


    void BuildPropagator()
    {
        // Set up propagator
        _propagator = new int[4][][];
        for (int dir = 0; dir < 4; dir++)
        {
            _propagator[dir] = new int[_tiles.Count][];
        }
        
        _correspondingPrefabTiles = new int[_tiles.Count];
        for(int t = 0; t < _currentSubset.tiles.Length; t++)
        {
            Constraint currentConstraint = _sampleData.constraints.First(c => c.tile == _currentSubset.tiles[t]);
            _correspondingPrefabTiles[t] = Array.FindIndex(_sampleData.constraints, c => c == currentConstraint); 
            foreach (Direction direction in (Direction[])Enum.GetValues(typeof(Direction)))
            {
                switch (direction)
                {
                    case Direction.Left:
                        SetAllowedConstraints(currentConstraint.left, direction, t);
                        break;
                    case Direction.Up:
                        SetAllowedConstraints(currentConstraint.up, direction, t);
                        break;
                    case Direction.Right:
                        SetAllowedConstraints(currentConstraint.right, direction, t);
                        break;
                    case Direction.Down:
                        SetAllowedConstraints(currentConstraint.down, direction, t);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
        }
    }

    private void SetAllowedConstraints(string[] constraintArray, Direction direction, int currentIdx)
    {
        List<int> allowedIndexes = new List<int>();
        foreach (string s in constraintArray)
        {
            int allowedIdx = _tiles.FindIndex(tile => tile == s);
            if (allowedIdx < 0)
            {
                Debug.Log("Can't find tile " + s + " in tiles");
                continue;
            }
            allowedIndexes.Add(allowedIdx);
        }

        _propagator[(int) direction][currentIdx] = allowedIndexes.ToArray();
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
        double[] distribution = new double[_tiles.Count];
        for (int tile = 0; tile < _tiles.Count; tile++)
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
        for (int tile = 0; tile < _tiles.Count; tile++)
            if (w[tile] != (tile == chosenPattern))
                Ban((int)node, tile);
        return null;
    }

    int? FindLowestEntropy()
    {
        double min = 1E+3;
        int lowestEntropyIdx = -1;

        for (int i = 0; i < _wave.Length; i++)
        {
            if (OnBoundary(i % _width, i / _height))
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
    private bool OnBoundary(int x, int y)
    {
        return x < 0 || y < 0 || x >= _width || y >= _height;
    }

    void Propagate()
    {
        // Propagate over every cell that needs to be updated
        while (_stacksize > 0)
        {
            var e1 = _stack[_stacksize - 1];
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

                // Go over all the potentially valid patterns
                for (int l = 0; l < p.Length; l++)
                {
                    int t2 = p[l];
                    int[] comp = compat[t2];

                    comp[d]--;
                    // Remove the pattern if it no longer matches
                    if (comp[d] == 0)
                        Ban(idxPropagation, t2);
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
        sumsOfWeights[i] -= weights[t];
        sumsOfWeightLogWeights[i] -= weightLogWeights[t];

        sum = sumsOfWeights[i];
        entropies[i] -= sumsOfWeightLogWeights[i] / sum - Math.Log(sum);
    }
    
    void Clear()
    {
        for (int i = 0; i < _wave.Length; i++)
        {
            for (int tile = 0; tile < _tiles.Count; tile++)
            {
                _wave[i][tile] = true;
                for (int dir = 0; dir < 4; dir++)
                    _compatible[i][tile][dir] = _propagator[OppositeDirection[dir]][tile].Length;
            }
            _sumOfPossibilities[i] = _tiles.Count;
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
            for (int t = 0; t < _tiles.Count; t++)
                if (_wave[i][t] && _correspondingPrefabTiles[t] != 0)
                {
                    float x = i % _width;
                    float z = i / _height;
                    observed[i] = Instantiate(_tilesPrefabs[_correspondingPrefabTiles[t]], _output.transform.position + new Vector3(x, 0, z),
                        Quaternion.identity, _output.transform);
                    break;
                }
    }
}