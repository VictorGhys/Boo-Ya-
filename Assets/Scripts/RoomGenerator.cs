using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomGenerator : MonoBehaviour
{
    [SerializeField] private int _seed;
    [SerializeField] private bool _generate = false;

    [Header("parameters")]
    [Header("hallway")]
    [SerializeField]
    private int _minHallwayLength = 2;

    [SerializeField]
    private int _maxHallwayLength = 8;

    [SerializeField]
    private int _minHallwayWidth = 2;

    [SerializeField]
    private int _maxHallwayWidth = 3;

    [SerializeField]
    private int _minHallwayPieces = 2;

    [SerializeField]
    private int _maxHallwayPieces = 3;

    [SerializeField]
    private int _hallwayOffsetChancePercentage = 40;

    [SerializeField]
    private int _minHallwayOffset = 4;

    [SerializeField]
    private int _maxHallwayOffset = 8;

    [SerializeField]
    private int _minHallwayFreeSpaces = 4;

    [SerializeField]
    private int _maxHallwayFreeSpaces = 8;

    [Header("rooms")]
    [SerializeField]
    private int _minRoomLength = 2;

    [SerializeField]
    private int _maxRoomLength = 5;

    [SerializeField]
    private int _minRoomWidth = 2;

    [SerializeField]
    private int _maxRoomWidth = 5;

    [SerializeField]
    private int _minRooms = 2;

    [SerializeField]
    private int _maxRooms = 3;

    [Header("prefabs")]
    [SerializeField] private Transform _pfHallwayFloor;

    [SerializeField] private Transform _pfHallwayWall;
    [SerializeField] private Transform _pfHallwayInnerCorner;
    [SerializeField] private Transform _pfHallwayOuterCorner;
    [SerializeField] private Transform _pfHallwayDoorFrame;
    [SerializeField] private Transform _pfHallwayWindow;

    [SerializeField] private Transform _pfRoomFloor;
    [SerializeField] private Transform _pfRoomWall;
    [SerializeField] private Transform _pfRoomInnerCorner;
    [SerializeField] private Transform _pfRoomOuterCorner;
    [SerializeField] private Transform _pfRoomDoorFrame;
    [SerializeField] private Transform _pfRoomWindow;

    [SerializeField] private Transform _pfBathroomFloor;
    [SerializeField] private Transform _pfBathroomWall;
    [SerializeField] private Transform _pfBathroomInnerCorner;
    [SerializeField] private Transform _pfBathroomOuterCorner;
    [SerializeField] private Transform _pfBathroomDoorFrame;
    [SerializeField] private Transform _pfBathroomWindow;

    [SerializeField] private Transform _pfElevatorFloor;
    [SerializeField] private Transform _pfElevatorWall;
    [SerializeField] private Transform _pfElevatorInnerCorner;
    [SerializeField] private Transform _pfElevatorOuterCorner;
    [SerializeField] private Transform _pfElevatorDoorFrame;
    [SerializeField] private Transform _pfElevatorWindow;

    [SerializeField] private Transform _pfDoor;
    [SerializeField] private Transform _pfCloset;
    [SerializeField] private float doorOffset = 0.5f;

    [SerializeField] private List<Transform> _bathroomPrefabs;
    [SerializeField] private List<Vector2Int> _bathroomPrefabsWidthHeight;

    private const int _tileSize = 2;
    private Dictionary<Vector3Int, GameObject> _generatedFloors = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> _generatedHallwayFloors = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> _generatedRoomFloors = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> _generatedTopBottomWalls = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> _generatedLeftRightWalls = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> _generatedHallwayDoorFrames = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> _generatedRoomDoorFrames = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> _generatedDoors = new Dictionary<Vector3Int, GameObject>();
    private List<GameObject> _generatedCornersList = new List<GameObject>();

    private void Awake()
    {
        Random.InitState(_seed);
    }

    private void Start()
    {
        GenerateLevel();
    }

    private void GenerateLevel()
    {
        GenerateHallWay();
        GenerateRooms();
        PlaceBathroomPrefabs();
    }

    private void GenerateHallWay()
    {
        Vector3Int dir = Vector3Int.forward;
        Vector3Int prevDir = dir;
        int hallwayLength = 1;
        int hallwayWidth = 1;
        int hallwayPieces = Random.Range(_minHallwayPieces, _maxHallwayPieces + 1);
        Vector3Int pos = Vector3Int.FloorToInt(transform.position);

        bool rotatedLeft = true;
        bool prevRotatedLeft = rotatedLeft;
        int prevHallwayWidth = hallwayWidth;
        int prevHallwayLength = hallwayLength;

        for (int i = 0; i < hallwayPieces; i++)
        {
            hallwayLength = Random.Range(_minHallwayLength, _maxHallwayLength + 1);
            hallwayWidth = Random.Range(_minHallwayWidth, _maxHallwayWidth + 1);
            // reposition pos to the left bottom corner of the next hallway
            if (rotatedLeft)
            {
                // left
                pos += prevDir * _tileSize;
            }
            else
            {
                // right
                pos -= dir * _tileSize * (prevHallwayWidth - 1);// move left
                pos += prevDir * _tileSize * hallwayWidth;// move up
            }
            // offset the pos
            if (Random.Range(0, 100) < _hallwayOffsetChancePercentage)
            {
                pos -= dir * _tileSize * Random.Range(_minHallwayOffset, Math.Min(hallwayLength - 1, _maxHallwayOffset));
            }
            pos = CreateRoom(hallwayLength, hallwayWidth, pos, dir, _pfHallwayFloor, ref _generatedHallwayFloors);
            prevDir = new Vector3Int(dir.x, dir.y, dir.z);
            prevRotatedLeft = rotatedLeft;
            rotatedLeft = RotateLeftRight(ref dir);
            prevHallwayWidth = hallwayWidth;
            prevHallwayLength = hallwayLength;
            // make sure you don't put hallways next to each other by turning the same twice
            if (prevRotatedLeft == rotatedLeft && prevHallwayLength <= prevHallwayWidth * 2)
            {
                rotatedLeft = !rotatedLeft;
                //dir = Quaternion.Euler(0, 180, 0) * dir;
                GetRightOfDir(GetRightOfDir(dir));
            }
        }
    }

    private void GenerateRooms()
    {
        var freeSpaces = FindFreeSpacesAroundHallway(_generatedHallwayFloors);
        int randIndex = 0;
        int roomLength = 0;
        int roomWidth = 0;
        int roomOffset = 0;
        int amountOfFreeSpaces = Random.Range(_minHallwayFreeSpaces, _maxHallwayFreeSpaces + 1);
        while (freeSpaces.Keys.Count > amountOfFreeSpaces)
        {
            randIndex = Random.Range(0, freeSpaces.Count);
            roomLength = Random.Range(_minRoomLength, _maxRoomLength + 1);
            roomWidth = Random.Range(_minRoomWidth, _maxRoomWidth + 1);
            roomOffset = Random.Range(0, roomWidth - 1);
            Vector3Int randPos = freeSpaces.Keys.ElementAt(randIndex);
            Vector3Int randDir = freeSpaces.Values.ElementAt(randIndex);
            Vector3Int randDirRight = GetRightOfDir(randDir);
            Vector3Int startRoomPos = randPos - roomOffset * randDirRight * _tileSize;
            // do check if the room fits
            if (!DoRoomFitCheck(roomLength, roomWidth, startRoomPos, randDir) || !DoDoorFitCheck(randPos, randDir))
            {
                freeSpaces.Remove(randPos);
                continue;
            }
            // create a door frames at the start of the room
            var hallwayDoorFrame = Instantiate(_pfHallwayDoorFrame,
                randPos - randDir * _tileSize / 2 + randDirRight * _tileSize / 2,
                Quaternion.LookRotation(randDir), transform).gameObject;
            _generatedHallwayDoorFrames.Add(randPos, hallwayDoorFrame);

            var roomDoorFrame = Instantiate(_pfRoomDoorFrame,
                randPos - randDir * _tileSize / 2 + randDirRight * _tileSize / 2,
                Quaternion.LookRotation(randDir) * Quaternion.Euler(0, 180, 0), transform).gameObject;
            _generatedRoomDoorFrames.Add(randPos, roomDoorFrame);
            // create door
            Vector3 doorPos = randPos - randDir * _tileSize / 2;
            doorPos += new Vector3(randDirRight.x, randDirRight.y, randDirRight.z) * doorOffset;
            _generatedDoors.Add(randPos,
                Instantiate(_pfDoor, doorPos, Quaternion.LookRotation(randDir) * Quaternion.Euler(0, 180, 0), transform).gameObject);
            // create the room
            CreateRoomWithWalls(roomLength, roomWidth, startRoomPos, randDir, roomOffset, _pfRoomFloor, ref _generatedRoomFloors);

            freeSpaces = FindFreeSpacesAroundHallway(_generatedHallwayFloors);
        }
    }

    private void PlaceBathroomPrefabs()
    {
        var freeSpaces = FindFreeSpacesAroundHallway(_generatedHallwayFloors);
        foreach (var freePos in freeSpaces)
        {
            int count = 0;
            var widthHeight = _bathroomPrefabsWidthHeight[count];
            if (DoRoomFitCheck(widthHeight.y, widthHeight.x, freePos.Key, freePos.Value))
            {
                //place bathroom
                var rightOfDir = GetRightOfDir(freePos.Value);

                var bathroomPos = freePos.Key + rightOfDir * (widthHeight.x - 1) * _tileSize;
                Instantiate(_bathroomPrefabs[0], bathroomPos, Quaternion.LookRotation(freePos.Value) * Quaternion.Euler(0, -90, 0));
            }

            count++;
        }
    }

    private Vector3Int CreateRoom(int length, int width, Vector3Int pos, Vector3Int dir, Transform floor, ref Dictionary<Vector3Int, GameObject> dictionary)
    {
        Vector3Int currPos = pos;
        Vector3Int rightOfDir = GetRightOfDir(dir);
        for (int l = 0; l < length; l++)
        {
            for (int w = 0; w < width; w++)
            {
                currPos = pos + l * dir * _tileSize + w * _tileSize * rightOfDir;
                if (!_generatedFloors.ContainsKey(currPos))
                {
                    dictionary.Add(currPos, Instantiate(floor, currPos, Quaternion.identity, transform).gameObject);
                    _generatedFloors.Add(currPos, Instantiate(floor, currPos, Quaternion.identity, transform).gameObject);
                }
            }
        }

        return currPos;
    }

    private Vector3Int CreateRoomWithWalls(int length, int width, Vector3Int pos, Vector3Int dir, int offset, Transform floor, ref Dictionary<Vector3Int, GameObject> dictionary)
    {
        Vector3Int currPos = pos;
        Vector3Int rightOfDir = GetRightOfDir(dir);
        for (int l = 0; l < length; l++)
        {
            for (int w = 0; w < width; w++)
            {
                currPos = pos + l * dir * _tileSize + w * _tileSize * rightOfDir;
                if (!_generatedFloors.ContainsKey(currPos))
                {
                    // floor
                    dictionary.Add(currPos, Instantiate(floor, currPos, Quaternion.identity, transform).gameObject);
                    _generatedFloors.Add(currPos, Instantiate(floor, currPos, Quaternion.identity, transform).gameObject);
                    //left wall
                    if (w == 0)
                    {
                        Vector3Int leftWallPos = currPos - rightOfDir * (_tileSize / 2) + dir * _tileSize / 2;
                        if (l != length - 1)
                        {
                            // regular left wall
                            Quaternion rot = Quaternion.LookRotation(-rightOfDir);
                            _generatedLeftRightWalls.Add(currPos, Instantiate(_pfRoomWall, leftWallPos, rot, transform).gameObject);
                        }
                        else
                        {
                            // inner top left corner
                            Quaternion rot = Quaternion.LookRotation(rightOfDir) * Quaternion.Euler(0, 180, 0);
                            _generatedCornersList.Add(Instantiate(_pfRoomInnerCorner, leftWallPos, rot, transform).gameObject);
                        }
                    }
                    // bottom wall
                    if (l == 0)
                    {
                        Vector3Int wallPos = currPos - dir * (_tileSize / 2) + GetRightOfDir(dir) * _tileSize / 2;
                        // bottom left inner corner
                        if (w == 0)
                        {
                            Quaternion leftRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180, 0);
                            Vector3 leftInnerPos = currPos - dir * (_tileSize / 2) - GetRightOfDir(dir) * _tileSize / 2;
                            _generatedCornersList.Add(Instantiate(_pfRoomInnerCorner, leftInnerPos, leftRot, transform).gameObject);
                        }
                        // bottom right inner corner
                        if (w == width - 1)
                        {
                            Quaternion rightRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 90, 0);
                            _generatedCornersList.Add(Instantiate(_pfRoomInnerCorner, wallPos, rightRot, transform).gameObject);
                            continue;
                        }

                        // regular wall
                        if (!_generatedTopBottomWalls.ContainsKey(currPos) && !_generatedRoomDoorFrames.ContainsKey(currPos))
                        {
                            Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180, 0);
                            _generatedTopBottomWalls.Add(currPos, Instantiate(_pfRoomWall, wallPos, rot, transform).gameObject);
                        }
                        else
                        {
                            Debug.Log("can't place wall");
                        }
                    }
                    // top wall
                    if (l == length - 1 && w != width - 1)
                    {
                        // regular wall
                        if (!_generatedTopBottomWalls.ContainsKey(currPos))
                        {
                            Vector3Int wallPos = currPos + dir * (_tileSize / 2) + GetRightOfDir(dir) * _tileSize / 2;
                            Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, 0);
                            _generatedTopBottomWalls.Add(currPos, Instantiate(_pfRoomWall, wallPos, rot, transform).gameObject);
                        }
                    }
                }
            }
            //right wall
            if (!_generatedLeftRightWalls.ContainsKey(currPos))
            {
                Vector3Int rightWallPos = currPos + rightOfDir * (_tileSize / 2) + dir * _tileSize / 2;
                if (l != length - 1)
                {
                    // regular right wall
                    Quaternion rot = Quaternion.LookRotation(rightOfDir);
                    _generatedLeftRightWalls.Add(currPos, Instantiate(_pfRoomWall, rightWallPos, rot, transform).gameObject);
                }
                else
                {
                    // inner top right corner
                    Quaternion rot = Quaternion.LookRotation(rightOfDir) * Quaternion.Euler(0, -90, 0);
                    _generatedCornersList.Add(Instantiate(_pfRoomInnerCorner, rightWallPos, rot, transform).gameObject);
                }
            }
        }

        return currPos;
    }

    private Vector3Int GetRightOfDir(Vector3Int dir)
    {
        if (dir.x > 0)
        {
            return Vector3Int.back;
        }
        if (dir.x < 0)
        {
            return Vector3Int.forward;
        }
        if (dir.z > 0)
        {
            return Vector3Int.right;
        }
        if (dir.z < 0)
        {
            return Vector3Int.left;
        }
        throw new Exception("Could not find right of" + dir);
    }

    private Vector3Int GetLeftOfDir(Vector3Int dir)
    {
        if (dir.x > 0)
        {
            return Vector3Int.forward;
        }
        if (dir.x < 0)
        {
            return Vector3Int.back;
        }
        if (dir.z > 0)
        {
            return Vector3Int.left;
        }
        if (dir.z < 0)
        {
            return Vector3Int.right;
        }
        throw new Exception("Could not find left of" + dir);
    }

    private Dictionary<Vector3Int, Vector3Int> FindFreeSpacesAroundHallway(Dictionary<Vector3Int, GameObject> hallway)
    {
        Dictionary<Vector3Int, Vector3Int> freeSpaces = new Dictionary<Vector3Int, Vector3Int>();
        foreach (var hallwayPos in hallway)
        {
            // get the neighbor positions of this tile
            var neighborPositions = FindNeighbors(hallwayPos.Key);
            // add the neighbor pos if it is free
            foreach (var neighborPosition in neighborPositions)
            {
                if (!_generatedFloors.ContainsKey(neighborPosition.Key) && !freeSpaces.ContainsKey(neighborPosition.Key))
                {
                    freeSpaces.Add(neighborPosition.Key, neighborPosition.Value);
                }
            }
        }

        return freeSpaces;
    }

    private Dictionary<Vector3Int, Vector3Int> FindNeighbors(Vector3Int hallwayPos)
    {
        Dictionary<Vector3Int, Vector3Int> directions = new Dictionary<Vector3Int, Vector3Int>()
        {
            {hallwayPos + Vector3Int.forward * _tileSize, Vector3Int.forward},
            {hallwayPos + Vector3Int.right * _tileSize, Vector3Int.right},
            {hallwayPos + Vector3Int.back * _tileSize, Vector3Int.back},
            {hallwayPos + Vector3Int.left * _tileSize, Vector3Int.left},
        };
        return directions;
    }

    private bool RotateLeftRight(ref Vector3Int dir)
    {
        if (Random.Range(0, 2) == 0)
        {
            // rotate left
            dir = GetLeftOfDir(dir);
            return true;
        }
        else
        {
            // rotate right
            dir = GetRightOfDir(dir);
            return false;
        }
    }

    private bool DoDoorFitCheck(Vector3Int pos, Vector3Int dir)
    {
        // check if the hallway piece from where the room starts has a hallway piece to the right so the door fits
        Vector3Int rightOfDir = GetRightOfDir(dir);
        Vector3Int checkPos = pos - dir * _tileSize + _tileSize * rightOfDir;
        if (!_generatedHallwayFloors.ContainsKey(checkPos))
        {
            return false;
        }

        return true;
    }

    private bool DoRoomFitCheck(int length, int width, Vector3Int pos, Vector3Int dir)
    {
        Vector3Int currPos = pos;
        Vector3Int rightOfDir = GetRightOfDir(dir);
        for (int l = 0; l < length; l++)
        {
            for (int w = 0; w < width; w++)
            {
                currPos = pos + l * dir * _tileSize + w * _tileSize * rightOfDir;
                if (_generatedFloors.ContainsKey(currPos))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void Update()
    {
        if (_generate)
        {
            _generate = false;
            // Create new seed
            _seed = Random.Range(0, 10000);
            Random.InitState(_seed);
            // Delete old generated gameobjects
            DestroyGeneratedObjects();
            // Generate
            GenerateLevel();
        }
    }

    private void DestroyGeneratedObjects()
    {
        foreach (var floor in _generatedFloors)
        {
            Destroy(floor.Value);
        }
        _generatedFloors.Clear();
        foreach (var floor in _generatedHallwayFloors)
        {
            Destroy(floor.Value);
        }
        _generatedHallwayFloors.Clear();
        foreach (var floor in _generatedRoomFloors)
        {
            Destroy(floor.Value);
        }
        _generatedRoomFloors.Clear();

        foreach (var wall in _generatedLeftRightWalls)
        {
            Destroy(wall.Value);
        }
        _generatedLeftRightWalls.Clear();
        foreach (var wall in _generatedTopBottomWalls)
        {
            Destroy(wall.Value);
        }
        _generatedTopBottomWalls.Clear();

        foreach (var doorFrame in _generatedHallwayDoorFrames)
        {
            Destroy(doorFrame.Value);
        }
        _generatedHallwayDoorFrames.Clear();
        foreach (var doorFrame in _generatedRoomDoorFrames)
        {
            Destroy(doorFrame.Value);
        }
        _generatedRoomDoorFrames.Clear();
        foreach (var door in _generatedDoors)
        {
            Destroy(door.Value);
        }
        _generatedDoors.Clear();
        foreach (var corner in _generatedCornersList)
        {
            Destroy(corner);
        }
        _generatedCornersList.Clear();
    }
}