using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System;
using System.Linq;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class WFCSimpleTiledModel : WFCModel
{
    
    [SerializeField, Tooltip("The json data file to read from")]
    private TextAsset _json;

    [SerializeField, Tooltip("The name of the subset to use of the xml file")]
    private string _subsetName;

    private List<string> _tiles = new List<string>();

    private WFCSampleData _sampleData;

    protected WFCSimpleTiledModel.Subset _currentSubset;
    
    [Serializable]
    public class WFCSampleData
    {
        public Constraint[] constraints;
        public Subset[] subsets;
        public Alias[] aliases;
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
    [Serializable]
    public class Alias
    {
        public string name;
        public string[] tiles;
    }

    protected override void GetConstraints()
    {
        // Read in the constraints from a json file
       _sampleData = JsonUtility.FromJson<WFCSampleData>(_json.text);

       _currentSubset = _sampleData.subsets.FirstOrDefault(s => s.name == _subsetName);
       if (_currentSubset == null)
       {
           Debug.Log("subset not found!");
           return;
       }

       foreach (var tile in _currentSubset.tiles)
       {
           _tiles.Add(tile);
       }
       _NbOfTiles = _tiles.Count;

       // Get the weight of each tile
       weights = new double[_NbOfTiles];
       for (int tile = 0; tile < _NbOfTiles; tile++)
       {
           weights[tile] = _sampleData.constraints[tile].weight;
       }
    }

    protected override void BuildPropagator()
    {
        // Set up propagator
        _propagator = new int[4][][];
        for (int dir = 0; dir < 4; dir++)
        {
            _propagator[dir] = new int[_NbOfTiles][];
        }

        // Fill in the constraints of each tile in the subset
        _correspondingPrefabTiles = new int[_NbOfTiles];
        for(int t = 0; t < _currentSubset.tiles.Length; t++)
        {
            Constraint currentConstraint = _sampleData.constraints.First(c => c.tile == _currentSubset.tiles[t]);
            _correspondingPrefabTiles[t] = Array.FindIndex(_sampleData.constraints, c => c == currentConstraint); 
            foreach (Direction direction in (Direction[])Enum.GetValues(typeof(Direction)))
            {
                List<int> allowedIndexes;
                switch (direction)
                {
                    case Direction.Left:
                        allowedIndexes = GetAllowedConstraints(currentConstraint.left);
                        break;
                    case Direction.Up:
                        allowedIndexes = GetAllowedConstraints(currentConstraint.up);
                        break;
                    case Direction.Right:
                        allowedIndexes = GetAllowedConstraints(currentConstraint.right);
                        break;
                    case Direction.Down:
                        allowedIndexes = GetAllowedConstraints(currentConstraint.down);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _propagator[(int)direction][t] = allowedIndexes.ToArray();

            }
        }
    }

    private List<int> GetAllowedConstraints(string[] constraintArray)
    {
        List<int> allowedIndexes = new List<int>();
        foreach (string s in constraintArray)
        {
            int allowedIdx = _tiles.FindIndex(tile => tile == s);
            if (allowedIdx < 0)
            {
                Alias foundAlias = _sampleData.aliases.FirstOrDefault(alias => alias.name == s);
                if (foundAlias == null)
                {
                    Debug.Log("Can't find tile " + s + " in tiles");
                    continue;
                }
                allowedIndexes.AddRange(GetAllowedConstraints(foundAlias.tiles)); 
                continue;
            }
            allowedIndexes.Add(allowedIdx);
        }
        return allowedIndexes;
    }

    protected override void Init()
    {
        _wave = new bool[_width * _height][];
        _compatible = new int[_wave.Length][][];
        for (int i = 0; i < _wave.Length; i++)
        {
            _wave[i] = new bool[_NbOfTiles];
            _compatible[i] = new int[_NbOfTiles][];
            for (int tile = 0; tile < _NbOfTiles; tile++)
                _compatible[i][tile] = new int[4];
        }

        // Calculate weights for entropy
        weightLogWeights = new double[_NbOfTiles];
        sumOfWeights = 0;
        sumOfWeightLogWeights = 0;

        for (int t = 0; t < _NbOfTiles; t++)
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

        _stack = new Tuple<int, int>[_wave.Length * _NbOfTiles];
        _stacksize = 0;
        _limit = _width * _height;
    }

    protected override bool OnBoundary(int x, int y)
    {
        return x < 0 || y < 0 || x >= _width || y >= _height;
    }

}