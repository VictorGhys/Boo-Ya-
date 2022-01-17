using System;
using System.Collections.Generic;
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
    // The patterns of tiles
    private byte[][] _patterns;
    private long nbOfTilesPow;

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
        if (_input.transform.childCount < _sampleAreaWidth * _sampleAreaHeight)
        {
            Debug.Log("Add empty tile because input is not completely filled");
            _tiles.Add(null);
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
    }
    
    protected override void BuildPropagator()
    {
        // Look at the allowed neighours of every tile
        _nbOfTiles = _tiles.Count;
        nbOfTilesPow = (long)Math.Pow(_nbOfTiles, _sampleSize * _sampleSize);

        Dictionary<long, int> weights = new Dictionary<long, int>();
        List<long> ordering = new List<long>();

        for (int y = 0; y < _sampleAreaHeight - _sampleSize + 1; y++)
        {
            for (int x = 0; x < _sampleAreaWidth - _sampleSize + 1; x++)
            {
                byte[][] ps = new byte[8][];

                ps[0] = PatternFromSample(x, y);
                ps[1] = Reflect(ps[0]);
                ps[2] = Rotate(ps[0]);
                ps[3] = Reflect(ps[2]);
                ps[4] = Rotate(ps[2]);
                ps[5] = Reflect(ps[4]);
                ps[6] = Rotate(ps[4]);
                ps[7] = Reflect(ps[6]);

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

    byte[] Rotate(byte[] p)
    {
        return Pattern((x, y) => p[_sampleSize - 1 - y + x * _sampleSize]);
    }

    byte[] Reflect(byte[] p)
    {
        return Pattern((x, y) => p[_sampleSize - 1 - x + y * _sampleSize]);
    }

    long Index(byte[] pattern)
    {
        long result = 0, power = 1;
        for (int i = 0; i<pattern.Length; i++)
        {
            result += pattern[pattern.Length - 1 - i] * power;
            power *= _nbOfTiles;
        }
        return result;
    }

    byte[] PatternFromIndex(long ind)
    {
        long residue = ind;
        long power = nbOfTilesPow;
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

    protected override bool OnBoundary(int x, int y)
    {
        return x + _sampleSize > _width || y + _sampleSize > _height || x < 0 || y < 0;
    }

    protected override int Sample(int x, int y)
    {
        // Return the tile for position x y
        for (int t = 0; t < _nbOfPatterns; t++)
        {
            if (_wave[x + y * _width][t])
            {
                // Find the wanted tile in the tile prefabs
                int patternIdx = _patterns[t][0];
                string wantedTile = _tiles[patternIdx];
                int prefabIdx = _tilesPrefabs.FindIndex(pf => pf.name == wantedTile);
                return prefabIdx;
            }
        }
        return -1;
    }
}
