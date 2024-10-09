﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Events;
using Extensions.DoTween;
using Extensions.System;
using Extensions.Unity;
using Settings;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Components
{
    public partial class GridManager : SerializedMonoBehaviour, ITweenContainerBind
    {
        [Inject] private InputEvents InputEvents{get;set;}
        [Inject] private GridEvents GridEvents{get;set;}
        [Inject] private ProjectSettings ProjectSettings{get;set;}

        [BoxGroup(Order = 999)]
#if UNITY_EDITOR
        [TableMatrix(SquareCells = true, DrawElementMethod = nameof(DrawTile))]  
#endif
        [OdinSerialize]
        private Tile[,] _grid;
        [SerializeField] private int _gridSizeX;
        [SerializeField] private int _gridSizeY;
        [SerializeField] private Bounds _gridBounds;
        [SerializeField] private Transform _transform;
        [SerializeField] private List<GameObject> _tileBGs = new();
        [SerializeField] private List<GameObject> _gridBorders = new();
        [SerializeField] private Transform _bGTrans;
        [SerializeField] private Transform _borderTrans;
        private Tile _selectedTile;
        private Vector3 _mouseDownPos;
        private Vector3 _mouseUpPos;
        private List<MonoPool> _tilePoolsByPrefabID;
        private MonoPool _tilePool0;
        private MonoPool _tilePool1;
        private MonoPool _tilePool2;
        private MonoPool _tilePool3;
        private Tile[,] _tilesToMove;
        [OdinSerialize] private List<List<Tile>> _lastMatches;
        private Tile _hintTile;
        private GridDir _hintDir;
        private Sequence _hintTween;
        private Coroutine _destroyRoutine;
        public ITweenContainer TweenContainer{get;set;}
        private Coroutine _hintRoutine;
        [SerializeField]private int _scoreMulti;
        private Settings _mySettings;
        
        //
        private Dictionary<int, MonoPool> _powerupPoolsByPrefabID;
        

        private bool _horizontalPowerupPresent = false;
        private bool _verticalPowerupPresent = false;
        private bool _bombPowerupPresent = false;
        //

        private void Awake()
        {
            _mySettings = ProjectSettings.GridManagerSettings;
            _tilePoolsByPrefabID = new List<MonoPool>();
            _powerupPoolsByPrefabID = new Dictionary<int, MonoPool>();
            
            for(int prefabId = 0; prefabId < _mySettings.PrefabIds.Count; prefabId ++)
            {
                MonoPool tilePool = new
                (
                    new MonoPoolData
                    (
                        _mySettings.TilePrefabs[prefabId],
                        10,
                        _transform
                    )
                );
                
                _tilePoolsByPrefabID.Add(tilePool);
            }
            
            //
            
            for(int i = 0; i < _mySettings.PowerupPrefabIds.Count; i++)
            {
                int powerUpID = _mySettings.TilePrefabs.Count + i;
                MonoPool powerupPool = new MonoPool(
                    new MonoPoolData(
                        _mySettings.TilePowerupPrefabs[i],
                        5,
                        _transform
                    )
                );
                _powerupPoolsByPrefabID.Add(powerUpID, powerupPool);
            }
            //
            
            TweenContainer = TweenContain.Install(this);
        }

        private void Start()
        {
            for(int x = 0; x < _grid.GetLength(0); x ++)
            for(int y = 0; y < _grid.GetLength(1); y ++)
            {
                Tile tile = _grid[x, y];

                SpawnTile(tile.ID, _grid.CoordsToWorld(_transform, tile.Coords), tile.Coords);
                tile.gameObject.Destroy();
            }

            IsGameOver(out _hintTile, out _hintDir);
            GridEvents.GridLoaded?.Invoke(_gridBounds);
            GridEvents.InputStart?.Invoke();
        }

        private void OnEnable() {RegisterEvents();}

        private void OnDisable()
        {
            UnRegisterEvents();
            TweenContainer.Clear();
        }

        private bool CanMove(Vector2Int tileMoveCoord) => _grid.IsInsideGrid(tileMoveCoord);

        // private bool HasMatch(Tile fromTile, Tile toTile, out List<List<Tile>> matches)
        // {
        //     matches = new List<List<Tile>>();
        //     bool hasMatches = false;
        //
        //     List<Tile> matchesAll = _grid.GetMatchesYAll(toTile);
        //     matchesAll.AddRange(_grid.GetMatchesXAll(toTile));
        //
        //     if(matchesAll.Count > 0)
        //     {
        //         matches.Add(matchesAll);
        //     }
        //
        //     matchesAll = _grid.GetMatchesYAll(fromTile);
        //     matchesAll.AddRange(_grid.GetMatchesXAll(fromTile));
        //
        //     if(matchesAll.Count > 0)
        //     {
        //         matches.Add(matchesAll);
        //     }
        //     
        //     if(matches.Count > 0) hasMatches = true;
        //
        //     return hasMatches;
        // }

        private bool HasAnyMatches(out List<List<Tile>> matches)
        {
            matches = new List<List<Tile>>();
            
            foreach(Tile tile in _grid)
            {
                List<Tile> matchesAll = _grid.GetMatchesXAll(tile);
                matchesAll.AddRange(_grid.GetMatchesYAll(tile));
                
                if(matchesAll.Count > 0)
                {
                    matches.Add(matchesAll);
                }
            }

            matches = matches.OrderByDescending(e => e.Count).ToList();

            for(int i = 0; i < matches.Count; i ++)
            {
                List<Tile> match = matches[i];
                
                matches[i] = match.Where(e => e.ToBeDestroyed == false).DoToAll(e => e.ToBeDestroyed = true).ToList();
            }
            
            const int matchIndex = 2;
            matches = matches.Where(e => { return e.Count > matchIndex; }).ToList();

            return matches.Count > 0;
        }

        private bool IsGameOver(out Tile hintTile, out GridDir hintDir)
        {
            hintDir = GridDir.Null;
            hintTile = null;
            
            List<Tile> matches = new();
            
            foreach(Tile fromTile in _grid)
            {
                hintTile = fromTile;

                Vector2Int thisCoord = fromTile.Coords;

                Vector2Int leftCoord = thisCoord + Vector2Int.left;
                Vector2Int topCoord = thisCoord + Vector2Int.up;
                Vector2Int rightCoord = thisCoord + Vector2Int.right;
                Vector2Int botCoord = thisCoord + Vector2Int.down;

                if(_grid.IsInsideGrid(leftCoord))
                {
                    Tile toTile = _grid.Get(leftCoord);

                    _grid.Swap(fromTile, toTile);

                    matches = _grid.GetMatchesX(fromTile);
                    matches.AddRange(_grid.GetMatchesY(fromTile));

                    _grid.Swap(toTile, fromTile);

                    if(matches.Count > 0)
                    {
                        hintDir = GridDir.Left;
                        return false;
                    }
                }
                
                if(_grid.IsInsideGrid(topCoord))
                {
                    Tile toTile = _grid.Get(topCoord);
                    _grid.Swap(fromTile, toTile);

                    matches = _grid.GetMatchesX(fromTile);
                    matches.AddRange(_grid.GetMatchesY(fromTile));
                    
                    _grid.Swap(toTile, fromTile);
                    
                    if(matches.Count > 0)
                    {
                        hintDir = GridDir.Up;
                        return false;
                    }
                }
                
                if(_grid.IsInsideGrid(rightCoord))
                {
                    Tile toTile = _grid.Get(rightCoord);
                    _grid.Swap(fromTile, toTile);

                    matches = _grid.GetMatchesX(fromTile);
                    matches.AddRange(_grid.GetMatchesY(fromTile));
                    
                    _grid.Swap(toTile, fromTile);
                    
                    if(matches.Count > 0)
                    {
                        hintDir = GridDir.Right;
                        return false;
                    }
                }
                
                if(_grid.IsInsideGrid(botCoord))
                {
                    Tile toTile = _grid.Get(botCoord);
                    _grid.Swap(fromTile, toTile);

                    matches = _grid.GetMatchesX(fromTile);
                    matches.AddRange(_grid.GetMatchesY(fromTile));
                    
                    _grid.Swap(toTile, fromTile);
                    
                    if(matches.Count > 0)
                    {
                        hintDir = GridDir.Down;
                        return false;
                    }
                }
            }

            return matches.Count == 0;
        }

        private void SpawnAndAllocateTiles()
        {
            _tilesToMove = new Tile[_gridSizeX,_gridSizeY];

            for(int y = 0; y < _gridSizeY; y ++)
            {
                int spawnStartY = 0;
                
                for(int x = 0; x < _gridSizeX; x++)
                {
                    Vector2Int thisCoord = new(x, y);
                    Tile thisTile = _grid.Get(thisCoord);

                    if(thisTile) continue;

                    int spawnPoint = _gridSizeY;

                    for(int y1 = y; y1 <= spawnPoint; y1 ++)
                    {
                        if(y1 == spawnPoint)
                        {
                            if(spawnStartY == 0)
                            {
                                spawnStartY = thisCoord.y;
                            }
                        
                            //MonoPool randomPool = _tilePoolsByPrefabID.Random();
                            
                            //
                            Tile newTile = SpawnRegularOrPowerupTile(new Vector2Int(x, spawnPoint), thisCoord);
                            //
                            
                            // Tile newTile = SpawnTile
                            // (
                            //     randomPool, 
                            //     _grid.CoordsToWorld(_transform, new Vector2Int(x, spawnPoint)),
                            //     thisCoord
                            // );
                        
                            _tilesToMove[thisCoord.x, thisCoord.y] = newTile;
                            break;
                        }

                        Vector2Int emptyCoords = new(x, y1);

                        Tile mostTopTile = _grid.Get(emptyCoords);

                        if(mostTopTile)
                        {
                            _grid.Set(null, mostTopTile.Coords);
                            _grid.Set(mostTopTile, thisCoord);
                        
                            _tilesToMove[thisCoord.x, thisCoord.y] = mostTopTile;

                            break;
                        }
                    }
                }
            }

            StartCoroutine(RainDownRoutine());
        }

        private Tile SpawnTile(MonoPool randomPool, Vector3 spawnWorldPos, Vector2Int spawnCoords)
        {
            Tile newTile = randomPool.Request<Tile>();

            newTile.Teleport(spawnWorldPos);
            
            _grid.Set(newTile, spawnCoords);

            return newTile;
        }

        private Tile SpawnTile(int id, Vector3 worldPos, Vector2Int coords) => SpawnTile(_tilePoolsByPrefabID[id], worldPos, coords);
        
        
        private IEnumerator RainDownRoutine()
        {
            int longestDistY = 0;
            Tween longestTween = null;
            
            for(int y = 0; y < _gridSizeY; y ++) // TODO: Should start from first tile that we are moving
            {
                bool shouldWait = false;
                
                for(int x = 0; x < _gridSizeX; x ++)
                {
                    Tile thisTile = _tilesToMove[x, y];

                    if(thisTile == false) continue;

                    Tween thisTween = thisTile.DoMove(_grid.CoordsToWorld(_transform, thisTile.Coords));

                    shouldWait = true;

                    if(longestDistY < y)
                    {
                        longestDistY = y;
                        longestTween = thisTween;
                    }
                }

                if(shouldWait)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            if(longestTween != null)
            {
                longestTween.onComplete += delegate
                {
                    if(HasAnyMatches(out _lastMatches))
                    {
                        StartDestroyRoutine();
                    }
                    else
                    {
                        IsGameOver(out _hintTile, out _hintDir);
                        GridEvents.InputStart?.Invoke();
                    }
                };
            }
            else
            {
                Debug.LogWarning("This should not have happened!");
                GridEvents.InputStart?.Invoke();
            }
        }

        private void StartDestroyRoutine()
        {
            if(_destroyRoutine != null)
            {
                StopCoroutine(_destroyRoutine);
            }
            
            _destroyRoutine = StartCoroutine(DestroyRoutine());
        }
        
        private IEnumerator DestroyRoutine()
        {
            foreach(List<Tile> matches in _lastMatches)
            {
                IncScoreMulti();
                matches.DoToAll(DespawnTile);
                
                //TODO: Show score multi text in ui as PunchScale
                
                GridEvents.MatchGroupDespawn?.Invoke(matches.Count * _scoreMulti);
    
                yield return new WaitForSeconds(0.1f);
            }
            
            SpawnAndAllocateTiles();
        }

        private void DespawnTile(Tile tile) //Tile e idi burası
        {
            //_grid.Set(null, e.Coords);
            //_tilePoolsByPrefabID[e.ID].DeSpawn(e);
            
            //
            _grid.Set(null, tile.Coords);
            if (_mySettings.PowerupPrefabIds.Contains(tile.ID))
            {
                if (tile.ID == _mySettings.PowerupPrefabIds[0])
                {
                    _horizontalPowerupPresent = false;
                }
                else if (tile.ID == _mySettings.PowerupPrefabIds[1])
                {
                    _verticalPowerupPresent = false;
                }
                else if (tile.ID == _mySettings.PowerupPrefabIds[2])
                {
                    _bombPowerupPresent = false;
                }

                _powerupPoolsByPrefabID[tile.ID].DeSpawn(tile);
                return;
            }
            _tilePoolsByPrefabID[tile.ID].DeSpawn(tile);
            //
            
        }
        

        private void DoTileMoveAnim(Tile fromTile, Tile toTile, TweenCallback onComplete = null)
        {
            Vector3 fromTileWorldPos = _grid.CoordsToWorld(_transform, fromTile.Coords);
            fromTile.DoMove(fromTileWorldPos);
            Vector3 toTileWorldPos = _grid.CoordsToWorld(_transform, toTile.Coords);
            toTile.DoMove(toTileWorldPos, onComplete);
        }

        private void StartHintRoutine()
        {
            if(_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
            }

            _hintRoutine = StartCoroutine(HintRoutineUpdate());
        }
        
        private void StopHintRoutine()
        {
            if(_hintTile)
            {
                _hintTile.Teleport(_grid.CoordsToWorld(_transform, _hintTile.Coords));
            }
            
            if(_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }
        }
        
        private IEnumerator HintRoutineUpdate()
        {
            while(true)
            {
                yield return new WaitForSeconds(3f);
                TryShowHint();
            }
        }
        private void TryShowHint()
        {
            if(_hintTile)
            {
                Vector2Int gridMoveDir = _hintDir.ToVector();

                Vector3 moveCoords = _grid.CoordsToWorld(_transform, _hintTile.Coords + gridMoveDir);
                
                _hintTween = _hintTile.DoHint(moveCoords);
            }
        }

        private void ResetScoreMulti()
        {
            _scoreMulti = 0;
        }

        private void IncScoreMulti()
        {
            _scoreMulti ++;
        }

        private void RegisterEvents()
        {
            InputEvents.MouseDownGrid += OnMouseDownGrid;
            InputEvents.MouseUpGrid += OnMouseUpGrid;
            GridEvents.InputStart += OnInputStart;
            GridEvents.InputStop += OnInputStop;
        }

        private void OnInputStop()
        {
            StopHintRoutine();
        }

        private void OnInputStart()
        {
            StartHintRoutine();
            ResetScoreMulti();
        }

        private void OnMouseDownGrid(Tile clickedTile, Vector3 dirVector)
        {
            _selectedTile = clickedTile;
            _mouseDownPos = dirVector;

            if(_hintTween.IsActive())
            {
                _hintTween.Complete();
            }
        }
        
        //
        private Tile SpawnRegularOrPowerupTile(Vector2Int spawnPoint, Vector2Int targetCoord)
        {
            if (ShouldSpawnPowerup(out int powerupId))
            {
                return SpawnTile(_powerupPoolsByPrefabID[powerupId], _grid.CoordsToWorld(_transform, spawnPoint), targetCoord);
            }
            else
            {
                MonoPool randomPool = _tilePoolsByPrefabID.Random();
                return SpawnTile(randomPool, _grid.CoordsToWorld(_transform, spawnPoint), targetCoord);
            }
        }

        private bool ShouldSpawnPowerup(out int powerupId)
        {
            powerupId = -1;

            if (_scoreMulti == EnvVar.bombPowerupThreshold + 1 && !_bombPowerupPresent)
            {
                powerupId = _mySettings.PowerupPrefabIds[2];
                _bombPowerupPresent = true;
                return true;
            }
            if (_scoreMulti == EnvVar.horizontalPowerupThreshold + 1 && !_horizontalPowerupPresent)
            {
                Debug.Log("scoremulti: " + _scoreMulti);
                powerupId = _mySettings.PowerupPrefabIds[0];
                _horizontalPowerupPresent = true;
                return true;
            }
            if (_scoreMulti == EnvVar.verticalPowerupThreshold + 1 && !_verticalPowerupPresent)
            {
                powerupId = _mySettings.PowerupPrefabIds[1]; 
                _verticalPowerupPresent = true;
                return true;
            }
            return false;
        }
        //

        private void OnMouseUpGrid(Vector3 mouseUpPos)
        {
            _mouseUpPos = mouseUpPos;

            Vector3 dirVector = mouseUpPos - _mouseDownPos;

            if(_selectedTile)
            {
                //
                if (IsPowerupTile(_selectedTile))
                {
                    ActivatePowerup(_selectedTile);
                    return;
                }
                
                if(!HasAnyMatches(out _lastMatches))
                {
                    ResetScoreMulti();
                }
                //
                
                Vector2Int tileMoveCoord = _selectedTile.Coords + GridF.GetGridDirVector(dirVector);

                if(! CanMove(tileMoveCoord)) return;

                Tile toTile = _grid.Get(tileMoveCoord);

                _grid.Swap(_selectedTile, toTile);

                if(! HasAnyMatches(out _lastMatches))
                {
                    GridEvents.InputStop?.Invoke();

                    DoTileMoveAnim(_selectedTile, toTile,
                        delegate
                        {
                            _grid.Swap(toTile, _selectedTile);
                            
                            DoTileMoveAnim(_selectedTile, toTile,
                                delegate
                                {
                                    GridEvents.InputStart?.Invoke();
                                });
                        });
                }
                else
                {
                    GridEvents.InputStop?.Invoke();

                    DoTileMoveAnim
                    (
                        _selectedTile,
                        toTile,
                        StartDestroyRoutine
                    );
                }
            }
        }
        
        //
        private bool IsPowerupTile(Tile tile)
        {
            return _mySettings.PowerupPrefabIds.Contains(tile.ID);
        }

        private void ActivatePowerup(Tile powerupTile)
        {
            
            List<Tile> tilesToDestroy = new List<Tile>();

            if (powerupTile.ID == _mySettings.PowerupPrefabIds[0])
            {
                for (int x = 0; x < _gridSizeX; x++)
                {
                    tilesToDestroy.Add(_grid[x, powerupTile.Coords.y]);
                }
                _horizontalPowerupPresent = false;
            }
            else if (powerupTile.ID == _mySettings.PowerupPrefabIds[1]) 
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    tilesToDestroy.Add(_grid[powerupTile.Coords.x, y]);
                }
                _verticalPowerupPresent = false;
            }
            else if (powerupTile.ID == _mySettings.PowerupPrefabIds[2]) 
            {
                Vector2Int[] adjacentDirections = new Vector2Int[]
                {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                };

                foreach (Vector2Int dir in adjacentDirections)
                {
                    Vector2Int adjacentCoord = powerupTile.Coords + dir;
                    if (_grid.IsInsideGrid(adjacentCoord))
                    {
                        tilesToDestroy.Add(_grid[adjacentCoord.x, adjacentCoord.y]);
                    }
                }
                _bombPowerupPresent = false;
            }
            
            foreach (Tile tile in tilesToDestroy)
            {
                DespawnTile(tile);
            }
            DespawnTile(powerupTile);
            
            //GridEvents.MatchGroupDespawn?.Invoke(tilesToDestroy.Count * _scoreMulti);
            
            if (powerupTile.ID == _mySettings.PowerupPrefabIds[0]) _bombPowerupPresent = false;
            else if (powerupTile.ID == _mySettings.PowerupPrefabIds[1]) _horizontalPowerupPresent = false;
            else if (powerupTile.ID == _mySettings.PowerupPrefabIds[2]) _verticalPowerupPresent = false;

            _scoreMulti = 1;
            int score = tilesToDestroy.Count * _scoreMulti;
            Debug.Log($"Powerup score: {score}");
            GridEvents.MatchGroupDespawn?.Invoke(score);
            
            SpawnAndAllocateTiles();
        }
        //
        
        
        private void UnRegisterEvents()
        {
            InputEvents.MouseDownGrid -= OnMouseDownGrid;
            InputEvents.MouseUpGrid -= OnMouseUpGrid;
            GridEvents.InputStart -= OnInputStart;
            GridEvents.InputStop -= OnInputStop;
        }
        
        [Serializable]
        public class Settings
        {
            public List<GameObject> TilePrefabs => _tilePrefabs;
            [SerializeField] private List<GameObject> _tilePrefabs;
            
            public GameObject TileBGPrefab => _tileBGPrefab;
            [SerializeField] private GameObject _tileBGPrefab;
            
            public List<int> PrefabIds => _prefabIds;
            [SerializeField] private List<int> _prefabIds;
            
            public List<GameObject> TilePowerupPrefabs => _tilePowerupPrefabs;
            [SerializeField] private List<GameObject> _tilePowerupPrefabs;
            
            public List<int> PowerupPrefabIds => _powerupPrefabIds;
            [SerializeField] private List<int> _powerupPrefabIds;
            
            [SerializeField] private GameObject _borderTopLeft;
            [SerializeField] private GameObject _borderTopRight;
            [SerializeField] private GameObject _borderBotLeft;
            [SerializeField] private GameObject _borderBotRight;
            [SerializeField] private GameObject _borderLeft;
            [SerializeField] private GameObject _borderRight;
            [SerializeField] private GameObject _borderTop;
            [SerializeField] private GameObject _borderBot;

            public GameObject BorderTopRight => _borderTopRight;
            public GameObject BorderBotLeft => _borderBotLeft;
            public GameObject BorderTopLeft => _borderTopLeft;
            public GameObject BorderBotRight => _borderBotRight;
            public GameObject BorderLeft => _borderLeft;
            public GameObject BorderRight => _borderRight;
            public GameObject BorderTop => _borderTop;
            public GameObject BorderBot => _borderBot;

        }
    }
} 