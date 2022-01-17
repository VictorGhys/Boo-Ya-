using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class WFCSimpleTiledModel : WFCModel
{
    
    [SerializeField, Tooltip("The json data file to read from")]
    private TextAsset _json;

    [SerializeField, Tooltip("The name of the subset to use of the xml file")]
    private string _subsetName;
    
    private WFCSampleData _sampleData;

    protected WFCSimpleTiledModel.Subset _currentSubset;
    
    [Serializable]
    public class WFCSampleData
    {
        public Constraint[] Constraints;
        public Subset[] Subsets;
        public Alias[] Aliases;
    }
    [Serializable]
    public class Constraint
    {
        public string Tile;
        public double Weight = 1;
        public string[] Left;
        public string[] Up;
        public string[] Right;
        public string[] Down;
    }
    [Serializable]
    public class Subset
    {
        public string Name;
        public string[] Tiles;
    }
    [Serializable]
    public class Alias
    {
        public string Name;
        public string[] Tiles;
    }

    protected override void PatternsFromSample()
    {
        // Read in the constraints from a json file
        _sampleData = JsonUtility.FromJson<WFCSampleData>(_json.text);

       _currentSubset = _sampleData.Subsets.FirstOrDefault(s => s.Name == _subsetName);
       if (_currentSubset == null)
       {
           Debug.Log("subset not found!");
           return;
       }

       _tiles.Clear();
       foreach (var tile in _currentSubset.Tiles)
       {
           _tiles.Add(tile);
       }
       _nbOfPatterns = _tiles.Count;
       _nbOfTiles = _tiles.Count;

       // Get the weight of each tile
       _weights = new double[_nbOfPatterns];
       for (int tile = 0; tile < _nbOfPatterns; tile++)
       {
           _weights[tile] = _sampleData.Constraints[tile].Weight;
       }
    }

    protected override void BuildPropagator()
    {
        // Set up propagator
        _propagator = new int[4][][];
        for (int dir = 0; dir < 4; dir++)
        {
            _propagator[dir] = new int[_nbOfPatterns][];
        }

        // Fill in the constraints of each tile in the subset
        _correspondingPrefabTiles = new int[_nbOfPatterns];
        for(int t = 0; t < _currentSubset.Tiles.Length; t++)
        {
            Constraint currentConstraint = _sampleData.Constraints.First(c => c.Tile == _currentSubset.Tiles[t]);
            _correspondingPrefabTiles[t] = Array.FindIndex(_sampleData.Constraints, c => c == currentConstraint); 
            foreach (Direction direction in (Direction[])Enum.GetValues(typeof(Direction)))
            {
                List<int> allowedIndexes;
                switch (direction)
                {
                    case Direction.Left:
                        allowedIndexes = GetAllowedConstraints(currentConstraint.Left);
                        break;
                    case Direction.Up:
                        allowedIndexes = GetAllowedConstraints(currentConstraint.Up);
                        break;
                    case Direction.Right:
                        allowedIndexes = GetAllowedConstraints(currentConstraint.Right);
                        break;
                    case Direction.Down:
                        allowedIndexes = GetAllowedConstraints(currentConstraint.Down);
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
                Alias foundAlias = _sampleData.Aliases.FirstOrDefault(alias => alias.Name == s);
                if (foundAlias == null)
                {
                    Debug.Log("Can't find tile " + s + " in tiles");
                    continue;
                }
                allowedIndexes.AddRange(GetAllowedConstraints(foundAlias.Tiles)); 
                continue;
            }
            allowedIndexes.Add(allowedIdx);
        }
        return allowedIndexes;
    }

    protected override bool OnBoundary(int x, int y)
    {
        return x < 0 || y < 0 || x >= _width || y >= _height;
    }

    protected override int Sample(int x, int y)
    {
        // Return the tile for position x y
        for (int t = 0; t < _nbOfTiles; t++)
        {
            if (_wave[x + y * _width][t])
            {
                string wantedTile = _tiles[t];
                int prefabIdx = _tilesPrefabs.FindIndex(pf => pf.name == wantedTile);
                if (prefabIdx < 0)
                {
                    Debug.Log("can't find tile: " + wantedTile);
                }
                return prefabIdx;
            }
        }
        return -1;
    }
}