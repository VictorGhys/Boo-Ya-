using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

public class WFCOverlapModel : WFCModel
{
    [SerializeField, Tooltip("The size of the sample pattern")]
    private int _sampleSize = 2;
    [SerializeField, Tooltip("The size of one grid block")]
    private int _gridSize = 1;
    [SerializeField, Tooltip("The width of the grid to sample from")]
    private int _sampleAreaWidth = 5;
    [SerializeField, Tooltip("The height of the grid to sample from")]
    private int _sampleAreaHeight = 5;
    [SerializeField, Tooltip("The height of the grid to sample from")]
    private double _customWeightEmpty = 0;
    
    [SerializeField, Tooltip("The kind of symmetry that is used on the patterns derived from the input")]
    private Symmetry _symmetry = Symmetry.AllSymmetry;
    [SerializeField, Tooltip("The input to train this model")]
    private GameObject _input;

    enum Symmetry
    {
        NoSymmetry = 1, Reflect = 2, AllSymmetry = 8
    }

    // Holds the indexes of tiles for every position on the grid x,z
    private byte[,] _sample;
    // The patterns of tiles, Indexes in order: Index, Tile index in pattern
    private byte[][] _patterns;
    private long _nbOfTilesPow;
    protected static int[] _reflectWallOffsets = { 0, 2, 0, -2 };
    protected static int[] _reflectCornerOffsets = { 1, -1, 1, -1 };
    //protected static int[] _rotateOffsets = { 1, 1, 1, -3 };
    protected static int[] _rotateOffsets = { 3, -1, -1, -1 };
    private int[] _reflectedTileIndices;
    private int[] _rotatedTileIndices;

    protected override void PatternsFromSample()
    {
        // Read in the tiles from the input
        Dictionary<string, byte> _tileDictionary = new Dictionary<string, byte>();
        _sample = new byte[_sampleAreaWidth, _sampleAreaHeight];
        List<int> correspondingPrefabTilesList = new List<int>();
        
        if (_input == null)
        {
            Debug.Log("No input defined");
            return;
        }

        // Add an empty tile if not every space is used
        _tiles.Clear();
        if (_input.transform.childCount < _sampleAreaWidth * _sampleAreaHeight)
        {
            //Debug.Log("Add empty tile because input is not completely filled");
            _tiles.Add("Empty");
            _tileDictionary.Add("Empty", 0);
            correspondingPrefabTilesList.Add(0);
        }

        for (int i = 0; i < _input.transform.childCount; i++)
        {
            GameObject tile = _input.transform.GetChild(i).gameObject;
            Vector3 tilepos = tile.transform.localPosition;
            if (tilepos.x >= 0 && tilepos.x < _sampleAreaWidth * _gridSize &&
                tilepos.z >= 0 && tilepos.z < _sampleAreaHeight * _gridSize)
            {
                // Get the corresponding tiles prefabs
                string regexPattern = @"\s*\(\d*\)";
                //Remove the regex pattern from tile.name
                string nameWithoutIndexInBrackets = Regex.Replace(tile.name, regexPattern, "");
                int prefabIdx = _tilesPrefabs.FindIndex(tilePf => tilePf.name == nameWithoutIndexInBrackets);
                if (prefabIdx == -1)
                {
                    Debug.Log("could not find " + nameWithoutIndexInBrackets + " in prefabs");
                }
                tile.name = nameWithoutIndexInBrackets;
                
                int x = (int)(tilepos.x) / _gridSize;
                int z = (int)(tilepos.z) / _gridSize;
                if (!_tileDictionary.ContainsKey(tile.name))
                {
                    int index = _tileDictionary.Count;
                    _tileDictionary.Add(tile.name, (byte)index);
                    _tiles.Add(tile.name);
                    correspondingPrefabTilesList.Add(prefabIdx);
                }
                _sample[x, z] = _tileDictionary[tile.name];
            }
        }
        _correspondingPrefabTiles = correspondingPrefabTilesList.ToArray();

        int CreateNewTile(int newPrefabIdx)
        {
            _tiles.Add(_tilesPrefabs[newPrefabIdx].name);
            int newReflectTileIdx = _tiles.Count - 1;
            int prefabIdx = _tilesPrefabs.FindIndex(tilePf => tilePf.name == _tilesPrefabs[newPrefabIdx].name);
            correspondingPrefabTilesList.Add(prefabIdx);
            return newReflectTileIdx;
        }

        // Pre calculate reflected and rotated versions of tiles
        _reflectedTileIndices = new int[_tilesPrefabs.Count];
        _rotatedTileIndices = new int[_tilesPrefabs.Count];
        for (int i = 0; i < _tilesPrefabs.Count; i++)
        {
            string tile = _tilesPrefabs[i].name;
            int tileIdx = _tiles.FindIndex(t => t == tile);
            if (tileIdx < 0)
            {
                tileIdx = CreateNewTile(i);
            }

            void MakeReflectRotateIndices(string regexPattern, int[] reflectOffsets)
            {
                var results = Regex.Matches(tile, regexPattern).Cast<Match>().
                    Select(x => int.Parse(x.Groups[1].Value)).ToList();
                int rotationIdx = 0;
                if (results.Count > 0)
                    rotationIdx = results[0];

                int newReflectPrefabIdx = i + reflectOffsets[rotationIdx];
                int newRotatePrefabIdx = i + _rotateOffsets[rotationIdx];
                int newReflectTileIdx = _tiles.FindIndex(t => t == _tilesPrefabs[newReflectPrefabIdx].name);
                int newRotateTileIdx = _tiles.FindIndex(t => t == _tilesPrefabs[newRotatePrefabIdx].name);

                if (newReflectTileIdx < 0)
                {
                    newReflectTileIdx = CreateNewTile(newReflectPrefabIdx);
                }

                if (newRotateTileIdx < 0)
                {
                    newRotateTileIdx = CreateNewTile(newRotatePrefabIdx);
                }

                _reflectedTileIndices[tileIdx] = newReflectTileIdx;
                _rotatedTileIndices[tileIdx] = newRotateTileIdx;
            }

            if (Regex.IsMatch(tile, @".*Wall.*"))
            {
                MakeReflectRotateIndices(@".*Wall.*([0-3]+)", _reflectWallOffsets);
            }
            else if(Regex.IsMatch(tile, @".*Corner.*"))
            {
                MakeReflectRotateIndices(@".*Corner.*([0-3]+)", _reflectCornerOffsets);
            }
            else if(Regex.IsMatch(tile, @".*(Door|Window).*"))
            {
                var results = Regex.Matches(tile, @".*(Door|Window).*([0-3]+)").Cast<Match>().Select(x => int.Parse(x.Groups[2].Value)).ToList();
                int rotationIdx = 0;
                if (results.Count > 0)
                    rotationIdx = results[0];

                int newReflectPrefabIdx = i + _reflectWallOffsets[rotationIdx];
                int newRotatePrefabIdx = i + _rotateOffsets[rotationIdx];

                string leftPattern = @".*Left.*";
                string rightPattern = @".*Right.*";
                string newReflectTile = "";
                if (Regex.IsMatch(_tilesPrefabs[newReflectPrefabIdx].name, leftPattern))
                {
                    newReflectTile = Regex.Replace(_tilesPrefabs[newReflectPrefabIdx].name, "Left", "Right");
                }
                else if (Regex.IsMatch(_tilesPrefabs[newReflectPrefabIdx].name, rightPattern))
                {
                    newReflectTile = Regex.Replace(_tilesPrefabs[newReflectPrefabIdx].name, "Right", "Left");
                }

                int newReflectTileIdx = _tiles.FindIndex(t => t == newReflectTile);
                int newRotateTileIdx = _tiles.FindIndex(t => t == _tilesPrefabs[newRotatePrefabIdx].name);
                
                if (newReflectTileIdx < 0)
                {
                    _tiles.Add(newReflectTile);
                    newReflectTileIdx = _tiles.Count - 1;
                    int prefabIdx = _tilesPrefabs.FindIndex(tilePf => tilePf.name == newReflectTile);
                    correspondingPrefabTilesList.Add(prefabIdx);
                }
                if (newRotateTileIdx < 0)
                {
                    newRotateTileIdx = CreateNewTile(newRotatePrefabIdx);
                }

                _reflectedTileIndices[tileIdx] = newReflectTileIdx;
                _rotatedTileIndices[tileIdx] = newRotateTileIdx;
            }
            else
            {
                _reflectedTileIndices[tileIdx] = tileIdx;
                _rotatedTileIndices[tileIdx] = tileIdx;
            }
        }
        _correspondingPrefabTiles = correspondingPrefabTilesList.ToArray();
    }
    
    protected override void BuildPropagator()
    {
        // Look at the allowed neighours of every tile
        _nbOfTiles = _tiles.Count;
        _nbOfTilesPow = (long)Math.Pow(_nbOfTiles, _sampleSize * _sampleSize);

        Dictionary<long, int> weights = new Dictionary<long, int>();
        List<long> ordering = new List<long>();

        for (int y = 0; y < _sampleAreaHeight - _sampleSize + 1; y++)
        {
            for (int x = 0; x < _sampleAreaWidth - _sampleSize + 1; x++)
            {
                byte[][] ps = new byte[8][];

                ps[0] = PatternFromSample(x, y);
                ps[1] = ReflectRemapTiles(Reflect(ps[0]));
                ps[2] = RotateRemapTiles(Rotate(ps[0]));
                ps[3] = ReflectRemapTiles(Reflect(ps[2]));
                ps[4] = RotateRemapTiles(Rotate(ps[2]));
                ps[5] = ReflectRemapTiles(Reflect(ps[4]));
                ps[6] = RotateRemapTiles(Rotate(ps[4]));
                ps[7] = ReflectRemapTiles(Reflect(ps[6]));
                //ps[1] = Reflect(ps[0]);
                //ps[2] = Rotate(ps[0]);
                //ps[3] = Reflect(ps[2]);
                //ps[4] = Rotate(ps[2]);
                //ps[5] = Reflect(ps[4]);
                //ps[6] = Rotate(ps[4]);
                //ps[7] = Reflect(ps[6]);

                for (int k = 0; k < (int) _symmetry; k++)
                {
                    long ind = Index(ps[k]);
                    if (weights.ContainsKey(ind))
                        weights[ind]++;
                    else
                    {
                        weights.Add(ind, 1);
                        ordering.Add(ind);
                    }
                }
            }
        }
        _nbOfPatterns = weights.Count;

        _patterns = new byte[_nbOfPatterns][];
        _weights = new double[_nbOfPatterns];

        int counter = 0;
        foreach (long w in ordering)
        {
            _patterns[counter] = PatternFromIndex(w);
            _weights[counter] = weights[w];
            counter++;
        }

        // Set the weight of the empty pattern to a custom one
        if (_customWeightEmpty != 0)
        {
            _weights[0] = _customWeightEmpty;
        }

        // Set up propagator
        _propagator = new int[4][][];
        for (int d = 0; d < 4; d++)
        {
            _propagator[d] = new int[_nbOfPatterns][];
            for (int t = 0; t < _nbOfPatterns; t++)
            {
                List<int> list = new List<int>();
                for (int t2 = 0; t2 < _nbOfPatterns; t2++)
                    if (Agrees(_patterns[t], _patterns[t2], XDirectionOffsets[d], YDirectionOffsets[d]))
                        list.Add(t2);
                _propagator[d][t] = new int[list.Count];
                if (list.Count == 0)
                {
                    Debug.LogWarning("doomed pattern");
                }
                for (int c = 0; c < list.Count; c++)
                    _propagator[d][t][c] = list[c];
            }
        }
    }

    byte[] Pattern(Func<int,int,byte> f)
    {
        byte[] result = new byte[_sampleSize * _sampleSize];
        for (int y = 0; y < _sampleSize; y++) 
            for (int x = 0; x < _sampleSize; x++) 
                result[x + y * _sampleSize] = f(x, y);
        return result;
    }

    byte[] PatternFromSample(int x, int y)
    {
        return Pattern((dx, dy) => _sample[(x + dx) % _sampleAreaWidth, (y + dy) % _sampleAreaHeight]);
    }

    // Rotate the pattern clockwise
    byte[] Rotate(byte[] p)
    {
        return Pattern((x, y) => p[_sampleSize - 1 - y + x * _sampleSize]);
    }

    // Reflect the pattern horizontally
    byte[] Reflect(byte[] p)
    {
        return Pattern((x, y) => p[_sampleSize - 1 - x + y * _sampleSize]);
    }

    long Index(byte[] pattern)
    {
        long result = 0;
        long power = 1;
        for (int i = 0; i < pattern.Length; i++)
        {
            result += pattern[pattern.Length - 1 - i] * power;
            power *= _nbOfTiles;
        }
        return result;
    }

    byte[] PatternFromIndex(long ind)
    {
        long residue = ind;
        long power = _nbOfTilesPow;
        byte[] result = new byte[_sampleSize * _sampleSize];

        for (int i = 0; i < result.Length; i++)
        {
            power /= _nbOfTiles;
            int count = 0;

            while (residue >= power)
            {
                residue -= power;
                count++;
            }

            result[i] = (byte)count;
        }
        return result;
    }

    bool Agrees(byte[] pattern1, byte[] pattern2, int xOffset, int yOffset)
    {
        int xmin = xOffset < 0 ? 0 : xOffset;
        int xmax = xOffset < 0 ? xOffset + _sampleSize : _sampleSize;
        int ymin = yOffset < 0 ? 0 : yOffset;
        int ymax = yOffset < 0 ? yOffset + _sampleSize : _sampleSize;

        for (int y = ymin; y < ymax; y++)
            for (int x = xmin; x < xmax; x++)
                if (pattern1[x + _sampleSize * y] != pattern2[x - xOffset + _sampleSize * (y - yOffset)])
                    return false;
        return true;
    }

    byte[] ReflectRemapTiles(byte[] p)
    {
        byte[] r = new byte[p.Length];
        for (int i = 0; i < _sampleSize * _sampleSize; i++)
        {
            int reflectedIdx = _reflectedTileIndices[(int) p[i]];
            if (reflectedIdx < 0)
                return p;
            r[i] = (byte) reflectedIdx;
        }

        return r;
    }

    byte[] RotateRemapTiles(byte[] p)
    {
        byte[] r = new byte[p.Length];
        for (int i = 0; i < _sampleSize * _sampleSize; i++)
        {
            int rotatedIdx = _rotatedTileIndices[(int)p[i]];
            if (rotatedIdx < 0)
                return p;
            r[i] = (byte) rotatedIdx;
        }

        return r;
    }
    
    protected override bool OnBoundary(int x, int y)
    {
        return x + _sampleSize > _width || y + _sampleSize > _height || x < 0 || y < 0;
    }
    
    protected override int GetTileIndex(int t)
    {
        return _patterns[t][0];
    }

    protected override int GetPatternIndex(int t, int x, int y)
    {
        List<int> validIndices = new List<int>();
        // Get a random pattern index with t in it
        for (int i = 0; i < _patterns.Length; i++)
        {
            if(_patterns[i][0] == t && _wave[x + y * _width][i])
                validIndices.Add(i);
        }

        if (validIndices.Count <= 0)
            return -1;

        int randIdx = _random.Next(validIndices.Count);
        return validIndices[randIdx];
    }

    protected override void CreateEmptyBorderPiece(int x, int y)
    {
        if (x == 0 || x > _width - _sampleSize || y == 0 || y > _height - _sampleSize)
        {
            CreatePiece(x, y, 0);
        }
    }
    
    public void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.matrix = _input.transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(_sampleAreaWidth / 2f, 0f, _sampleAreaHeight / 2f), new Vector3(_sampleAreaWidth, 1, _sampleAreaHeight));
    }
}
