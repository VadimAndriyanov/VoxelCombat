﻿using System;
using System.Collections.Generic;
using UnityEngine;

#if SERVER
namespace UnityEngine
{
    public class Debug
    {
        public static void Assert(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }

       

        public static void LogError(string error)
        {
            System.Diagnostics.Debug.Write("[LogError]: " + error);
        }

        public static void LogWarningFormat(string warning, params object[] args)
        {
            System.Diagnostics.Debug.Write("[LogWarning]", string.Format(warning, args));
        }

        public static void LogWarning(string warning)
        {
            System.Diagnostics.Debug.Write("[LogWarning]: " + warning);
        }

        public static void Log(string message)
        {
            System.Diagnostics.Debug.Write("[Log]: " + message);
        }
    }

    public class Mathf
    {
        public static float Abs(float value)
        {
            return Math.Abs(value);
        }

        public static int RoundToInt(float value)
        {
            return (int)Math.Round(value);
        }

        public static float Pow(float x, float y)
        {
            return (float)Math.Pow(x, y);
        }

        public static float Max(float x, float y)
        {
            if (x > y)
            {
                return x;
            }
            return y;
        }
    }
}
#endif

namespace Battlehub.VoxelCombat
{
    public delegate void VoxelDataControllerEvent<T>(T data);


    public interface IVoxelDataController
    {
        Coordinate Coordinate
        {
            get;
        }

        VoxelData ControlledData
        {
            get;
        }

        MapRoot Map
        {
            get;
        }

        int MapSize
        {
            get;
        }

        VoxelAbilities Abilities
        {
            get;
        }

        Dictionary<int, VoxelAbilities>[] AllAbilities
        {
            get;
        }

        bool IsAlive
        {
            get;
        }

        int PlayerIndex
        {
            get;
        }

        bool IsCollapsedOrBlocked
        {
            get;
        }



        bool SetVoxelDataState(VoxelDataState state);

        VoxelDataState GetVoxelDataState();

        bool IsValidAndEmpty(Coordinate coord, bool considerIdleAsValid);

        
        bool Move(Coordinate to, bool isLastStep,
            Action<VoxelData, VoxelData, int, int> eatCallback, 
            Action<VoxelData, int> collapseCallback = null,
            Action<VoxelData> expandCallback = null,
            Action<VoxelData, int> explodeCallback = null);

       // MapCell GetCellFor(MapCell cell, VoxelData data, int weight);

        bool CanExplode(Coordinate to, VoxelData voxelData);

        bool Explode(Coordinate to, VoxelData explodeData,
            Action<VoxelData, VoxelData, int, int> eatCallback,
            Action<VoxelData> expandCallback = null,
            Action<VoxelData, int> explodeCallback = null);

        void RotateRight();

        void RotateLeft();

        bool? CanSplit();

        bool Split(out Coordinate[] coordinates, Action<VoxelData, VoxelData, int, int> eatCallback, Action<VoxelData, int> collapseCallback = null, Action<VoxelData> dieCallback = null);

        bool? CanSplit4();

        bool Split4(out Coordinate[] coordinates,  Action<VoxelData> expandCallback = null, Action<VoxelData> dieCallback = null);

        bool? CanGrow();

        bool Grow(Action<VoxelData, VoxelData, int, int> eatCallback, Action<VoxelData, int> collapseCallback = null);

        bool? CanDiminish();

        bool Diminish(Action<VoxelData> expandCallback = null);

        bool? CanConvert(int type);

        bool Convert(int type, Action<VoxelData> dieCallback = null);

        bool? CanPerformSpawnAction();

        bool PerformSpawnAction(out Coordinate[] coordinates);

        void SetHealth(int health, Action<VoxelData> dieCallback = null);

        IVoxelDataController Clone();
    }

    public class VoxelDataController : IVoxelDataController
    {
        private MapRoot m_map;
        private int m_mapSize;

        private VoxelAbilities m_abilities;
        private readonly VoxelData m_controlledData;
        private MapPos m_position;
        private int m_playerIndex;
        private Dictionary<int, VoxelAbilities>[] m_allAbilities;

        public Coordinate Coordinate
        {
            get { return new Coordinate(m_position, m_controlledData); }
        }

        public VoxelData ControlledData
        {
            get { return m_controlledData; }
        }

        public VoxelAbilities Abilities
        {
            get { return m_abilities; }
        }

        public Dictionary<int, VoxelAbilities>[] AllAbilities
        {
            get { return m_allAbilities; }
        }

        public MapRoot Map
        {
            get { return m_map; }
        }

        public int MapSize
        {
            get { return m_mapSize; }
        }

        public int PlayerIndex
        {
            get { return m_playerIndex; }
        }

        public bool IsAlive
        {
            get { return m_controlledData.IsAlive; }
        }

        public bool IsCollapsedOrBlocked
        {
            get { return m_controlledData.Height == 0 || m_controlledData.Next != null; }
        }

       
        public VoxelDataController(MapRoot map, Coordinate coordinate, int type, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            m_map = map;

            m_playerIndex = playerIndex;
            m_position = coordinate.MapPos;
            m_allAbilities = allAbilities;
            m_abilities = m_allAbilities[playerIndex][type];

            m_controlledData = m_map.Get(coordinate);
            if(m_controlledData.Unit == null)
            {
                m_controlledData.Unit = new VoxelUnitData();
            }

            if (m_controlledData == null)
            {
                throw new ArgumentException("No voxelData found at specified coordinate", "coordinate");
            }

            m_mapSize = map.GetMapSizeWith(m_controlledData.Weight);
        }

        private VoxelDataController(VoxelDataController dataController)
        {
            m_map = dataController.Map;

            m_playerIndex = dataController.m_playerIndex;
            m_position = dataController.m_position;
            m_allAbilities = dataController.m_allAbilities;
            m_abilities = dataController.m_abilities;

            m_controlledData = dataController.ControlledData;
            if (m_controlledData.Unit == null)
            {
                m_controlledData.Unit = new VoxelUnitData();
            }

            m_mapSize = dataController.MapSize;
        }



        public bool IsValidAndEmpty( Coordinate coord, bool considerIdleAsValid)
        {
            return IsValidAndEmpty(m_controlledData, m_map, coord, considerIdleAsValid);
        }

        public static bool IsValidAndEmpty(VoxelData controlledData, MapRoot map, Coordinate coord, bool considerIdleAsValid)
        {
            MapCell cell = map.Get(coord.Row, coord.Col, coord.Weight);
            if(cell.VoxelData == null)
            {
                return true;
            }

            VoxelData data = cell.VoxelData;
            while(data != null)
            {
                if(data != controlledData)
                {
                    if(VoxelData.IsControllableUnit(data.Type) && data.Owner == controlledData.Owner && (!considerIdleAsValid || data.Unit.State != VoxelDataState.Idle))
                    {
                        return false;
                    }
                }
                data = data.Next;
            }

            if(cell.HasDescendantsWithVoxelData(
                descendantData => VoxelData.IsControllableUnit(descendantData.Type) && descendantData.Owner == controlledData.Owner && (!considerIdleAsValid || descendantData.Unit.State != VoxelDataState.Idle)))
            {
                return false;
            }

            return true;
        }

        public bool SetVoxelDataState(VoxelDataState state)
        {
            ControlledData.Unit.State = state;
            return true;
        }

        public VoxelDataState GetVoxelDataState()
        {
            return ControlledData.Unit.State;
        }


        public static bool CanMove(VoxelData controlledData, VoxelAbilities abilities, MapRoot map, float mapSize, Coordinate from, Coordinate to, bool isLastStep, bool willDie, bool verbose, out MapCell targetCell)
        {
            targetCell = null;
           
            if (!abilities.CanMove)
            {
                DebugLog("!m_abilities.CanMove", verbose);
                return false;
            }

            if (from.Weight != to.Weight)
            {
                DebugLog("from.Weight != to.Weight", verbose);
                return false;
            }

            if (to.Row < 0 || to.Row >= mapSize || to.Col < 0 || to.Col >= mapSize)
            {
                DebugLog("Out of map bounds -> " + to.ToString(), verbose);
                return false;
            }

            int deltaRow = to.Row - from.Row;
            int deltaCol = to.Col - from.Col;
            if (deltaRow != 0 && deltaCol != 0)
            {
                DebugLog("Can't move in diagonal direction", verbose);
                return false;
            }

            if(deltaRow > abilities.MaxMoveDistance || deltaCol > abilities.MaxMoveDistance)
            {
#if !SERVER
                UnityEngine.Debug.LogError("Can't move max move distance reached");
#endif
                //DebugLog("Can't move max move distance reached", verbose)
                return false;
            }

            
            Coordinate newCoord = new Coordinate(from.Row + deltaRow, from.Col + deltaCol, from.Altitude, from.Weight);
            MapCell cell = map.Get(newCoord.Row, newCoord.Col, newCoord.Weight);

    
            int height = 1 << controlledData.Weight;
            int deltaAlt = to.Altitude - from.Altitude;

            float allowedJumpHeight = abilities.MaxJumpHeight * height;
            float allowedFallHeight = abilities.MaxFallHeight * height;


            if (deltaAlt > allowedJumpHeight)
            {
                DebugLog("deltaHeight > allowedJumpHeight", verbose);
                return false;
            }

            if (deltaAlt < allowedFallHeight)
            {
                DebugLog("deltaHeight > allowedJumpHeight", verbose);
                return false;
            }

            newCoord.Altitude += deltaAlt;

            //Do not allow move to cells with non-destroyable blocks of smaller weight
            //if (cell.HasDescendantsWithVoxelData(descendantVoxelData => descendantVoxelData.Weight >= controlledData.Weight))
            //{   
            //    DebugLog("Movement to cells with non-destroyable blocks of smaller weight is not allowed", verbose);
            //    return false;
            //}
    
            if(isLastStep)
            {
                if(!IsValidAndEmpty(controlledData, map, to, false))
                {
                    return false;
                }
            }

            targetCell = cell;
            return true;
        }


        private readonly List<VoxelData> m_collapseList = new List<VoxelData>();
        private readonly List<VoxelData> m_destroyList = new List<VoxelData>();

        private Coordinate CollapseOrDestroyEqualWeight(Coordinate coordinate, int type, int weight, int altitude, Action<VoxelData> destroyCallback, Action<VoxelData, int> collapseCallback)
        {
            MapCell cell = m_map.Get(coordinate.Row, coordinate.Col, coordinate.Weight);
            VoxelData voxelData = cell.VoxelData;
            if (voxelData == null)
            {
                return coordinate;
            }

            int deltaAltitude = 0;
            while (voxelData != null)
            {
                if(m_controlledData.Owner != voxelData.Owner && !voxelData.IsCollapsed && m_controlledData.IsExplodableBy(voxelData.Type, voxelData.Weight))
                {
                    m_destroyList.Add(voxelData);
                }
                else if (voxelData.IsCollapsableBy(type, weight))
                {
                    //if (voxelData.Owner != m_playerIndex)
                    //{
                    //    m_destroyList.Add(voxelData);
                    //}

                    if (!voxelData.IsCollapsed)
                    {
                        m_collapseList.Add(voxelData);
                    }

                    deltaAltitude += voxelData.Height;
                }

                voxelData = voxelData.Next;
            }

            for (int i = 0; i < m_collapseList.Count; ++i)
            {
                VoxelData collapseVoxelData = m_collapseList[i];

                collapseVoxelData.IsCollapsed = true;
                if (collapseCallback != null)
                {
                    collapseCallback(collapseVoxelData, coordinate.Altitude - altitude);
                }
            }

            for(int i = 0; i < m_destroyList.Count; ++i)
            {
                VoxelData destroyVoxelData = m_destroyList[i];

                if (destroyCallback != null)
                {
                    destroyCallback(destroyVoxelData);
                }
            }

            if (m_collapseList.Count > 0)
            {
                m_collapseList.Clear();
            }
            
            if(m_destroyList.Count > 0)
            {
                m_destroyList.Clear();
            }

            return coordinate;
        }

        private bool WillExplode(MapCell cell, VoxelData controlledData)
        {
            if(cell.VoxelData != null)
            {
                VoxelData next = cell.VoxelData;
                while(next != null)
                {
                    if(controlledData.IsExplodableBy(next.Type, next.Weight) && controlledData.Owner != next.Owner && !next.IsCollapsed)
                    {
                        return true;
                    }
                    next = next.Next;
                }
            }

            if (cell.Children != null)
            {
                for (int i = 0; i < cell.Children.Length; ++i)
                {
                    bool willExplode = WillExplode(cell.Children[i], controlledData);
                    if(willExplode)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool Move(Coordinate to, bool isLastStep,
            Action<VoxelData, VoxelData, int, int> eatCallback = null,
            Action<VoxelData, int> collapseCallback = null,
            Action<VoxelData> expandCallback = null,
            Action<VoxelData, int> explodeCallback = null)
        {
            Coordinate from = new Coordinate(m_position, m_controlledData);
            MapCell cell;
            if (IsCollapsedOrBlocked)
            {
                DebugLog("Movement is not allowed. height " + m_controlledData.Height + " next " + m_controlledData.Next + " unit index " + m_controlledData.UnitOrAssetIndex);
                return false;
            }

            if(to.Weight != m_controlledData.Weight)
            {
                DebugLog("to.Weight != m_controlledVoxelData.Weight");
                return false;
            }

            int type = m_controlledData.Type;
            int weight = m_controlledData.Weight;

            MapCell toCell = m_map.Get(to.Row, to.Col, to.Weight);

            bool canMove = false;
            if(isLastStep)
            {
                canMove = CanMove(m_controlledData, m_abilities, m_map, m_mapSize, from, to, isLastStep, false, true, out cell);
            }
            
            if(!canMove)
            {
                VoxelData target;
                VoxelData beneath = toCell.GetDefaultTargetFor(type, weight, m_playerIndex, false, out target);
                if(beneath == null)
                {
                    return false;
                }

                if(target != null && 
                   target != beneath) //<- target != beneath will prevent bomb from stucking on eaters
                {
                    if (!target.IsAttackableBy(m_controlledData) && !target.IsCollapsableBy(m_controlledData.Type, m_controlledData.Weight))
                    {
                        return false;
                    }
                }
                
                to.Altitude = beneath.Altitude + beneath.Height;
                canMove = CanMove(m_controlledData, m_abilities, m_map, m_mapSize, from, to, isLastStep, false, true, out cell);
            }

            if (canMove)
            {
                MapCell fromCell = m_map.Get(from.Row, from.Col, from.Weight);
                Remove(fromCell, m_controlledData);
      
                if(CanExpandDescendants(fromCell))
                {
                    ExpandDescendants(0, fromCell, expandCallback);
                }
                else
                {
                    ExpandCell(0, fromCell, expandCallback);
                }
                

                bool willExplode = WillExplode(toCell, m_controlledData);
                if(willExplode)
                {
                    int health = m_controlledData.Health;
                    m_controlledData.Health = 0;
                    if(explodeCallback != null)
                    {
                        explodeCallback(m_controlledData, health);
                    }
                }

                EatDestroyCollapseDescendants(m_controlledData, toCell, eatCallback, collapseCallback);

                to = CollapseOrDestroyEqualWeight(to, type, weight, m_controlledData.Altitude, destroyedVoxel =>
                {
                    if (eatCallback != null)
                    {
                        eatCallback(m_controlledData, destroyedVoxel, 0, destroyedVoxel.Health);
                    }

                    destroyedVoxel.Health = 0;
                    Remove(toCell, destroyedVoxel);
                },
                collapseCallback);

                if (m_controlledData.Health > 0)
                {
                    toCell.AppendVoxelData(m_controlledData);
                }
                else
                {
                    if (CanExpandDescendants(toCell))
                    {
                        ExpandDescendants(0, toCell, expandCallback);
                    }
                    else
                    {
                        ExpandCell(0, toCell, expandCallback);
                    }

                }

                m_controlledData.Altitude = to.Altitude;
                m_position = to.MapPos;

                return true;
            }

            return false;
        }

        public MapCell GetCellFor(MapCell cell, VoxelData data, int weight)
        {
            int deltaWeight = data.Weight - weight;
            if (deltaWeight < 0)
            {
                DebugLog("Can't GetCellFor. deltaWeight < 0");
                return null;
            }

            for (int i = 0; i < deltaWeight; ++i)
            {
                cell = cell.Parent;
            }

            return cell;
            
        }

        public bool CanExplode(Coordinate to, VoxelData explodeData)
        {
            const bool verbose = true;
            if(explodeData == null)
            {
                DebugLog("Can't explode. explodeData is null", verbose);
                return false;
            }

            if (explodeData.Health == 0)
            {
                DebugLog("Can't explode. explodeData.Health == 0", verbose);
                return false;
            }

            if (!explodeData.IsExplodableBy(m_controlledData.Type, m_controlledData.Weight))
            {
                DebugLog("Can't explode. explodeData is not explodable", verbose);
                return false;
            }
            MapCell cell = m_map.Get(to.Row, to.Col, to.Weight);

            cell = GetCellFor(cell, explodeData, to.Weight);

            if (cell == null)
            {
                DebugLog("Can't explode. cell == null", verbose);
                return false;
            }

            if (!cell.HasVoxelData(explodeData))
            {
                DebugLog("Can't explode. !cell.Has(explodeData)", verbose);
                return false;
            }

            return true;
        }

        public bool Explode(Coordinate to, VoxelData explodeData,
            Action<VoxelData, VoxelData, int, int> eatCallback = null,
            Action<VoxelData> expandCallback = null,
            Action<VoxelData, int> explodeCallback = null)
        {
            Coordinate from = new Coordinate(m_position, m_controlledData);
            MapCell cell;
            if (IsCollapsedOrBlocked)
            {
                DebugLog("Movement is not allowed. height " + m_controlledData.Height + " next " + m_controlledData.Next);
                return false;
            }

            if (to.Weight != m_controlledData.Weight)
            {
                DebugLog("to.Weight != m_controlledVoxelData.Weight");
                return false;
            }

            if(!CanExplode(to, explodeData))
            {
                return false;
            }

            if(from == to)
            {
                MapCell toCell = m_map.Get(to.Row, to.Col, to.Weight);
                Remove(toCell, m_controlledData);

                int health = m_controlledData.Health;
                int explodeDataHealth = explodeData.Health;

                m_controlledData.Health = 0;

                if(explodeData.Type == (int)KnownVoxelTypes.Ground)
                {
                    explodeData.Health--;
                    Debug.Assert(explodeData.Health >= 0);
                }
                else
                {
                    explodeData.Health = 0;
                }
                

                if(explodeCallback != null)
                {
                    explodeCallback(m_controlledData, health);
                    explodeCallback(explodeData, explodeDataHealth);
                }

                if (!explodeData.IsAlive)
                {
                    VoxelData next = explodeData.Next;
                    while (next != null)
                    {
                        next.Altitude -= explodeData.Height;
                        next = next.Next;
                    }

                    toCell = GetCellFor(toCell, explodeData, to.Weight);
                    Remove(toCell, explodeData);

                    if (CanExpandDescendants(toCell))
                    {
                        ExpandDescendants(0, toCell, expandCallback);
                    }
                    else
                    {
                        ExpandCell(0, toCell, expandCallback);
                    }
                }

                return true;
            }
            else
            {
                to.Altitude = explodeData.Altitude;

                bool willDie = true;
                bool dontCare = false;
                if (CanMove(m_controlledData, m_abilities, m_map, m_mapSize, from, to, dontCare, willDie, true, out cell))
                {
                    MapCell fromCell = m_map.Get(from.Row, from.Col, from.Weight);
                    Remove(fromCell, m_controlledData);
                    if (CanExpandDescendants(fromCell))
                    {
                        ExpandDescendants(0, fromCell, expandCallback);
                    }
                    else
                    {
                        ExpandCell(0, fromCell, expandCallback);
                    }

                    int health = m_controlledData.Health;
                    int explodeDataHealth = explodeData.Health;

                    m_controlledData.Health = 0;

                    if (explodeData.Type == (int)KnownVoxelTypes.Ground)
                    {
                        explodeData.Health--;
                        Debug.Assert(explodeData.Health >= 0);
                    }
                    else
                    {
                        explodeData.Health = 0;
                    }

                    if (explodeCallback != null)
                    {
                        explodeCallback(m_controlledData, health);
                        explodeCallback(explodeData, explodeDataHealth);
                    }

                    if (!explodeData.IsAlive)
                    {
                        VoxelData next = explodeData.Next;
                        while (next != null)
                        {
                            next.Altitude -= explodeData.Height;
                            next = next.Next;
                        }

                        MapCell toCell = m_map.Get(to.Row, to.Col, to.Weight);
                        toCell = GetCellFor(toCell, explodeData, to.Weight);
                        Remove(toCell, explodeData);
                        if (CanExpandDescendants(toCell))
                        {
                            ExpandDescendants(0, toCell, expandCallback);
                        }
                        else
                        {
                            ExpandCell(0, toCell, expandCallback);
                        }

                        EatDestroyCollapseDescendants(m_controlledData, toCell, eatCallback, (v, i) => { });
                    }
                    m_controlledData.Altitude = to.Altitude;
                    m_position = to.MapPos;

                    return true;
                }
            }

         

            return false;
        }


        public void RotateRight()
        {
            m_controlledData.Dir = VoxelData.RotateRight(m_controlledData.Dir);
        }

        public void RotateLeft()
        {
            m_controlledData.Dir = VoxelData.RotateLeft(m_controlledData.Dir);
        }

        private bool CanSplit(Coordinate coordinate)
        {
            if (IsCollapsedOrBlocked)
            {
                DebugLog("Can't split. not allowed");
                return false;
            }
            
            if (m_controlledData.Health != m_abilities.MaxHealth)
            {
                DebugLog("Can't split. m_controlledVoxelData.Health != m_abilities.MaxHealth " + coordinate);
                return false;
            }
            
            if(m_controlledData.Weight != coordinate.Weight)
            {
                DebugLog("Can't split. m_coordinate.Weight != targetCoordinate.Weight " + coordinate);
                return false;
            }

            float size = m_map.GetMapSizeWith(m_controlledData.Weight);
            if(coordinate.Row < 0 || coordinate.Col < 0 || coordinate.Row >= size || coordinate.Col >= size)
            {
                return false;
            }

            if(m_position.Row == coordinate.Row && m_position.Col == coordinate.Col)
            {
                DebugLog("Can't split. m_coordinate == coordinate " + coordinate); 
                return false;
            }

            if(m_position.Row != coordinate.Row && m_position.Col != coordinate.Col)
            {
                DebugLog("Can't split.  m_coordinate.Row != coordinate.Row && m_coordinate.Col != coordinate.Col " + coordinate);
                return false;
            }

            if(Mathf.Abs(m_position.Row - coordinate.Row) > 1 || Mathf.Abs(m_position.Col - coordinate.Col) > 1)
            {
                DebugLog("Can't split. target coordinate is too far away " + coordinate);
                return false;
            }
            

            MapCell cell = m_map.Get(coordinate.Row, coordinate.Col, coordinate.Weight);
            //if (cell.HasDescendantsWithVoxelData(descendantVoxelData => descendantVoxelData.Weight >= m_controlledData.Weight))
            //{
            //    DebugLog("Can't split. cells with non-destroyable blocks of smaller weight is not allowed " + coordinate);
            //    return false; 
            //}

            VoxelData target;
            VoxelData voxelData = cell.GetDefaultTargetFor(m_controlledData.Type, m_controlledData.Weight, m_playerIndex, false, out target);
            if(voxelData == null || target != null)
            {
                return false;
            }

            if(voxelData.IsCollapsableBy(m_controlledData.Type, m_controlledData.Weight))
            {
                DebugLog("Can't split. cells with collapsable blocks not allowed" + coordinate);
                return false;
            }

            if(voxelData.Altitude + voxelData.Height != coordinate.Altitude)
            {
                //DebugLog("Can't split. Wrong coordinate");
                return false;
            }

            return true;
        }

        private bool FindSplitCoorinate(out Coordinate result)
        {
            result = new Coordinate();
            for(int r = -1; r <= 1; ++r)
            {
                for(int c = -1; c <= 1; ++c)
                {
                    if(c == 0 || r == 0)
                    {
                        Coordinate coordinate = new Coordinate(m_position, m_controlledData);
                        coordinate.Row += r;
                        coordinate.Col += c;

                        if(CanSplit(coordinate))
                        {
                            result = coordinate;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool? CanSplit()
        {
            if (ControlledData.Type != (int)KnownVoxelTypes.Eater)
            {
                return null;
            }
            Coordinate coordinate;
            return FindSplitCoorinate(out coordinate);
        }

        public bool Split(out Coordinate[] coordinates, Action<VoxelData, VoxelData, int, int> eatOrDestroyCallback = null, Action<VoxelData, int> collapseCallback = null, Action<VoxelData> dieCallback = null)
        {
            coordinates = new Coordinate[2];
            Coordinate coordinate;
            if (!FindSplitCoorinate(out coordinate))
            {
                coordinates = new Coordinate[0];
                return false;
            }

            coordinates[0] = Coordinate;
            coordinates[1] = coordinate;

            MapCell controlledDataCell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
            Remove(controlledDataCell, m_controlledData);

            VoxelData cloneData = new VoxelData(m_controlledData);
            cloneData.Unit.State = VoxelDataState.Idle;
            cloneData.Health = m_abilities.DefaultHealth;

            VoxelData cloneData2 = new VoxelData(m_controlledData);
            cloneData.Unit.State = VoxelDataState.Idle;
            cloneData2.Altitude = coordinate.Altitude;
            cloneData2.Health = m_abilities.DefaultHealth;

            m_controlledData.Health = 0;

            if (dieCallback != null)
            {
                dieCallback(m_controlledData);
            }

            
            MapCell cell = m_map.Get(coordinate.Row, coordinate.Col, coordinate.Weight);
            VoxelData target;
            VoxelData voxelData = cell.GetDefaultTargetFor(m_controlledData.Type, m_controlledData.Weight, m_playerIndex, false, out target);
            Debug.Assert(voxelData.Altitude + voxelData.Height == coordinate.Altitude);

            EatDestroyCollapseDescendants(cloneData2, cell, eatOrDestroyCallback, collapseCallback);

            controlledDataCell.AppendVoxelData(cloneData);
            cell.AppendVoxelData(cloneData2);

            return true;
        }

        public bool? CanSplit4()
        {
            if (ControlledData.Type != (int)KnownVoxelTypes.Eater)
            {
                return null;
            }
            if (IsCollapsedOrBlocked)
            {
                DebugLog("Can't split 4. not allowed");
                return false;
            }

            if(m_controlledData.Weight == m_abilities.MinWeight)
            {
                DebugLog("Can't split 4. m_controlledData.Weight == m_abilities.MinWeight");
                return false;
            }

            MapCell cell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
            if(cell.VoxelData == null)
            {
                Debug.LogError(string.Format("cell.VoxelData == null at {0}. Something wrong. IsAlive? {1} ", m_position, IsAlive));
                return false;
            }

            VoxelData penultimate = cell.VoxelData.GetPenultimate();
            if(penultimate != null )
            {
                if(penultimate.IsCollapsed)
                {
                    DebugLog("Can't split 4. penultimate is collapsed");
                    return false;
                }

                if (!penultimate.IsBaseFor(m_controlledData.Type, m_controlledData.Weight - 1))
                {
                    DebugLog("Can't split 4. penultimate is not base for controlled data");
                    return false;
                }
            }

            return true;
        }

        public bool Split4(out Coordinate[] coordinates, Action<VoxelData> expandCallback = null, Action<VoxelData> dieCallback = null)
        {
            coordinates = new Coordinate[4];
            if (CanSplit4() != true)
            {
                return false;
            }
               

            MapCell parentCell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
            Remove(parentCell, m_controlledData);

            m_controlledData.Health = 0;

            if(dieCallback != null)
            {
                dieCallback(m_controlledData);
            }

            VoxelData[] childrenData = new VoxelData[4];
            for(int i = 0; i < childrenData.Length; ++i)
            {
                MapCell cell = parentCell.Children[i];

                VoxelData childData = new VoxelData(m_controlledData);
                childData.Unit.State = VoxelDataState.Idle;
                childData.Health = m_abilities.DefaultHealth;
                childData.Weight--;
                childData.Height /= 2;
                Debug.Assert(childData.Height > 0);

                cell.AppendVoxelData(childData);

                childrenData[i] = childData;
            }
            if(CanExpandDescendants(parentCell))
            {
                ExpandDescendants(0, parentCell, expandCallback);
            }
            else
            {
                ExpandCell(0, parentCell, expandCallback);
            }

            for (int i = 0; i < childrenData.Length; ++i)
            {
                VoxelData childData = childrenData[i];
                MapCell cell = parentCell.Children[i];

                int altitude = 0;
                VoxelData penultimate = cell.VoxelData.GetPenultimate();
                if (penultimate != null)
                {
                    altitude = penultimate.Altitude + penultimate.Height;
                }
                else
                {
                    altitude = cell.Parent.GetTotalHeight();
                }

                childData.Altitude = altitude;

                MapPos cellPosition = cell.GetPosition();
                coordinates[i] = new Coordinate(cellPosition, childData);
            }

            return true;
        }

        public bool? CanGrow()
        {
            if (ControlledData.Type != (int)KnownVoxelTypes.Eater)
            {
                return null;
            }
            if (m_controlledData.Health != m_abilities.MaxHealth)
            {
                DebugLog("Can't grow. m_controlledVoxelData.Health != m_abilities.MaxHealth");
                return false;
            }

            if (IsCollapsedOrBlocked)
            {
                DebugLog("Can't grow. not allowed");
                return false;
            }

            if (m_controlledData.Weight == m_abilities.MaxWeight)
            {
                DebugLog("Can't grow. m_controlledVoxelData.Weight == m_abilities.MaxWeight");
                return false;
            }

            MapCell cell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
            if(cell.Parent == null)
            {
                DebugLog("Can't grow. cell.Parent == null");
                return false;
            }

            cell = cell.Parent;
            if ((m_controlledData.Altitude - cell.GetTotalHeight())  > m_abilities.EvaluateHeight(m_controlledData.Weight + 1))
            {
                DebugLog("Can't grow. Too high");
                return false;
            }

            VoxelData target;
            VoxelData voxelData = cell.GetDefaultTargetFor(m_controlledData.Type, m_controlledData.Weight + 1, m_playerIndex, false, out target);
            if(voxelData == null)
            {
                DebugLog("Can't grow. Unable to find non-destoryable VoxelData");
                return false;
            }

            //if (cell.HasDescendantsWithVoxelData(descendantVoxelData => descendantVoxelData != m_controlledData && descendantVoxelData.Weight >= m_controlledData.Weight + 1))
            //{
            //    DebugLog("Can't grow. cells with non-destroyable blocks of smaller weight is not allowed ");
            //    return false;
            //}

            return true;
        }

        public bool Grow(Action<VoxelData, VoxelData, int, int> eatOrDestroyCallback = null, Action<VoxelData, int> collapseCallback = null)
        {
            if(CanGrow() != true)
            {
                return false;
            }

            MapCell cell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
            Remove(cell, m_controlledData);

            //After grow operation altitude change could be required (if growed up standing on other voxels

            cell = cell.Parent;
            m_controlledData.Weight++;
            m_controlledData.Health = m_abilities.DefaultHealth;

            VoxelData target;
            VoxelData voxelData = cell.GetDefaultTargetFor(m_controlledData.Type, m_controlledData.Weight, m_playerIndex, false, out target);
            //VoxelData voxelData = cell.GetLastNonDestroyableBy(m_controlledData.Type, m_controlledData.Weight);
            m_controlledData.Altitude = voxelData.Altitude + voxelData.Height;
            m_controlledData.Height *= 2;

            EatDestroyCollapseDescendants(m_controlledData, cell, eatOrDestroyCallback, collapseCallback);

            m_position = cell.GetPosition();
            
            cell.AppendVoxelData(m_controlledData);

            m_mapSize = m_map.GetMapSizeWith(m_controlledData.Weight);

            return true;
        }

        public bool? CanDiminish()
        {
            if (ControlledData.Type != (int)KnownVoxelTypes.Eater)
            {
                return null;
            }

            if (IsCollapsedOrBlocked)
            {
                DebugLog("Can't diminish. not allowed");
                return false;
            }

            if (m_controlledData.Weight == m_abilities.MinWeight)
            {
                DebugLog("Can't diminish. m_controlledVoxelData.Weight == m_abilities.MinWeight");
                return false;
            }

            Coordinate coordinate = new Coordinate(m_position, m_controlledData);
            coordinate.Weight--;
            if (IsValidLocationFor(Map, m_controlledData.Type, m_controlledData, coordinate))
            {
                return false;
            }

            return true;
        }

        public bool Diminish(Action<VoxelData> expandCallback = null)
        {
            if(CanDiminish() != true)
            {
                return false;
            }

            MapCell parentCell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
            Remove(parentCell, m_controlledData);

            MapCell cell = parentCell.Children[0];
            
            m_controlledData.Weight--;
            m_controlledData.Height /= 2;
            Debug.Assert(m_controlledData.Height > 0);

            m_position = cell.GetPosition();

            cell.AppendVoxelData(m_controlledData);

            if(CanExpandDescendants(parentCell))
            {
                ExpandDescendants(0, parentCell, expandCallback);
            }
            else
            {
                ExpandCell(0, parentCell, expandCallback);
            }

            int altitude = 0;
            VoxelData penultimate = cell.VoxelData.GetPenultimate();
            if(penultimate != null)
            {
                altitude = penultimate.Altitude + penultimate.Height;
            }
            else
            {
                altitude = cell.Parent.GetTotalHeight();
            }

            m_controlledData.Altitude = altitude;

            m_mapSize = m_map.GetMapSizeWith(m_controlledData.Weight);

            return true;
        }


        public bool? CanConvert(int type)
        {
            if(ControlledData.Type != (int)KnownVoxelTypes.Eater)
            {
                return null;
            }

            if (IsCollapsedOrBlocked)
            {
                DebugLog("Can't convert. not allowed");
                return false;
            }

            Coordinate coordinate = new Coordinate(m_position, m_controlledData);

            if(!IsValidLocationFor(Map, type, m_controlledData, coordinate))
            {
                return false;
            }

            MapCell cell = Map.Get(coordinate.Row, coordinate.Col, coordinate.Weight);
            if (type == (int)KnownVoxelTypes.Spawner)
            {
                if(cell.HasDescendantsWithVoxelData())
                {
                    return false;
                }
            }

            if (type == m_controlledData.Type)
            {
                DebugLog("Can't convert to the same type");
                return false;
            }

            return true;
        }

        private bool IsValidLocationFor(MapRoot map, int type, VoxelData controlledVoxelData, Coordinate coordinate)
        {
            MapCell cell = map.Get(coordinate.Row, coordinate.Col, coordinate.Weight);
            if (cell.VoxelData == null || cell.VoxelData == controlledVoxelData)
            {
                bool isAtTopOfNothing = true;
                MapCell parent = cell.Parent;
                while (parent != null)
                {
                    if (parent.VoxelData != null)
                    {
                        VoxelData target;
                        VoxelData previous = parent.GetDefaultTargetFor(type, controlledVoxelData.Weight, controlledVoxelData.Owner, false, out target);
                        if (previous != null)
                        {
                            isAtTopOfNothing = false;

                            bool prevIsBase = previous.IsBaseFor(type, coordinate.Weight);
                            bool isAtTopOfPrevious = previous.Altitude + previous.Height == controlledVoxelData.Altitude;

                            if (isAtTopOfPrevious)
                            {
                                if (!prevIsBase)
                                {
                                    DebugLog("Can't perform action on top of " + previous.Type);
                                    return false;
                                }
                            }
                        }
                    }
                    parent = parent.Parent;
                }

                if (isAtTopOfNothing)
                {
                    DebugLog("Can't perform action  on top of nothing");
                    return false;
                }
            }
            else
            {
                VoxelData previous = cell.VoxelData.GetPrevious(controlledVoxelData);
                if (!previous.IsBaseFor(type, coordinate.Weight))
                {
                    DebugLog("Can't perform action  on top of " + previous.Type);
                    return false;
                }
            }

            return true;
        }

        public bool Convert(int type, Action<VoxelData> dieCallback = null)
        {
            if(CanConvert(type) != true)
            {
                return false;
            }

            VoxelAbilities abilities = m_allAbilities[m_playerIndex][type];
            VoxelData voxelData = new VoxelData(m_controlledData);
            voxelData.Unit.State = VoxelDataState.Idle;
            voxelData.Type = type;
            voxelData.Height = abilities.VariableHeight ? 
                abilities.ClampHeight(voxelData.Height) :
                abilities.EvaluateHeight(voxelData.Weight, true);
            voxelData.Health = abilities.DefaultHealth;

            MapCell cell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
            if(cell.VoxelData == m_controlledData)
            {
                cell.VoxelData = voxelData;
            }
            else
            {
                VoxelData penultimate = cell.VoxelData.GetPenultimate();
                Debug.Assert(penultimate.Next == m_controlledData);
                penultimate.Next = voxelData;
            }

            m_controlledData.Health = 0;
            if(dieCallback != null)
            {
                dieCallback(m_controlledData);
            }

            return true;
        }

        public bool? CanPerformSpawnAction()
        {
            if(ControlledData.Type == (int)KnownVoxelTypes.Spawner)
            {
                if (IsCollapsedOrBlocked) //spawn and eat units
                {
                    DebugLog("Can't perform spawn action. not allowed");
                    return false;
                }

                MapCell cell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
                if(cell != null)
                {
                    for(int i = 0; i < cell.Children.Length; ++i)
                    {
                        MapCell childCell = cell.Children[i];
                       
                     
                        if (childCell.VoxelData == null)
                        {
                            int j = 3 - i;

                            MapCell targetCell = childCell.Children[j];
                            if(targetCell.VoxelData == null)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public bool PerformSpawnAction(out Coordinate[] coordinates)
        {
            coordinates = new Coordinate[0];
            if (CanPerformSpawnAction() != true)
            {
                return false;
            }

            if (ControlledData.Type == (int)KnownVoxelTypes.Spawner)
            {
                int index = 0;
                coordinates = new Coordinate[4];

                MapCell cell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
                for (int i = 0; i < cell.Children.Length; ++i)
                {
                    MapCell childCell = cell.Children[i];
                    if (childCell.VoxelData == null)
                    {
                        int j = 3 - i;
                        MapCell targetCell = childCell.Children[j];
                        if (targetCell.VoxelData == null)
                        {
                            int type = (int)KnownVoxelTypes.Eatable;

                            VoxelAbilities abilities = m_allAbilities[m_playerIndex][type];
                            VoxelData voxelData = new VoxelData();
                            
                            voxelData.Type = type;
                            voxelData.Owner = ControlledData.Owner;
                            voxelData.Dir = ControlledData.Dir;

                            voxelData.Weight = m_controlledData.Weight - 2;
                            Debug.Assert(voxelData.Weight >= abilities.MinWeight);

                            voxelData.Altitude = m_controlledData.Altitude + m_controlledData.Height;
                            voxelData.Height = abilities.EvaluateHeight(voxelData.Weight, true);
                            voxelData.Health = abilities.DefaultHealth;

                            targetCell.VoxelData = voxelData;

                            coordinates[index] = new Coordinate(targetCell.GetPosition(), voxelData);
                            index++;

                            break;
                        }
                    }
                }

                Array.Resize(ref coordinates, index);
            }

            return true;
        }

        public void SetHealth(int health, Action<VoxelData> dieCallback = null)
        {
            if (health < 0)
            {
                health = 0;
            }
            if (health > Abilities.MaxHealth)
            {
                health = Abilities.MaxHealth;
            }
            ControlledData.Health = health;

            if(health == 0)
            {
                MapCell cell = m_map.Get(m_position.Row, m_position.Col, m_controlledData.Weight);
                Remove(cell, m_controlledData);

                if(dieCallback != null)
                {
                    dieCallback(ControlledData);
                }
            }
        }

        private void Remove(MapCell cell, VoxelData dataToRemove)
        {
            if(cell.VoxelData == null)
            {
                return;
            }

            if(cell.VoxelData == dataToRemove)
            {
                cell.VoxelData = dataToRemove.Next;
            }
            else
            {
                VoxelData previous = cell.VoxelData;
                while(previous.Next != null)
                {
                    if(previous.Next == dataToRemove)
                    {
                        previous.Next = dataToRemove.Next;
                        break;
                    }

                    previous = previous.Next;
                }
            }
        }


        private bool CanExpandDescendants(MapCell cell)
        {
            if (cell.VoxelData != null)
            {
                VoxelData last = cell.VoxelData.GetLast();
                if(last.Type != (int)KnownVoxelTypes.Bomb && last.Type != (int)KnownVoxelTypes.Eater)
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        private void ExpandDescendants(int deltaAltitude, MapCell cell, Action<VoxelData> expandCallback)
        {
            deltaAltitude = ExpandCell(deltaAltitude, cell, expandCallback);

            if (cell.Children != null)
            {
                for (int i = 0; i < cell.Children.Length; ++i)
                {
                    ExpandDescendants(deltaAltitude, cell.Children[i], expandCallback);
                }
            }
        }

        private int ExpandCell(int deltaAltitude, MapCell cell, Action<VoxelData> expandCallback)
        {
            if (cell.VoxelData != null)
            {
                VoxelData data = cell.VoxelData;
                while (data != null)
                {
                    if(data.IsCollapsed)
                    {
                        if(data.Next == null || !data.IsCollapsableBy(data.Next.Type, data.Next.Weight))
                        {
                            data.IsCollapsed = false;
                        }

                        data.Altitude += deltaAltitude;
                        deltaAltitude += data.Height;

                        if (expandCallback != null)
                        {
                            expandCallback(data);
                        }
                    }
                    data = data.Next;
                }
            }

            return deltaAltitude;
        }

        private void EatDestroyCollapseDescendants(VoxelData destroyer,  MapCell cell, Action<VoxelData, VoxelData, int, int> eatOrDestroyCallback, Action<VoxelData, int> collapseCallback)
        {
            if (cell.Children != null)
            {
                for (int i = 0; i < cell.Children.Length; ++i)
                {
                    EatDestroyCollapseDescendants(0, destroyer, cell.Children[i], eatOrDestroyCallback, collapseCallback);
                }
            }
        }

        private void EatDestroyCollapseDescendants(int deltaAltitude, VoxelData destroyer, MapCell cell, Action<VoxelData, VoxelData, int, int> eatOrDestroyCallback, Action<VoxelData, int> collapseCallback)
        {
            if (cell.VoxelData != null)
            {
                VoxelData data = cell.VoxelData;

                if (data != null && data.Weight < destroyer.Weight)
                {
                    while (data != null)
                    {
                        bool isEatable = destroyer.Health < m_abilities.MaxHealth &&

                            (destroyer.Owner == data.Owner || data.IsNeutral) &&

                            data.IsEatableBy(destroyer.Type, destroyer.Weight, destroyer.Height, destroyer.Altitude, destroyer.Owner);
                        //&& destroyer.Health < m_allAbilities[destroyer.Owner][destroyer.Type].MaxHealth;

                        if (destroyer.Owner != data.Owner && !data.IsNeutral ||
                             isEatable ||
                             //following line meas that voxel will be always destroyed if it located at higher alitude.
                             //voxels with lower altitude will be temporarily collapsed
                             destroyer.Altitude + destroyer.Height <= data.Altitude)
                        {
                            int deltaHealth = 0;

                            if (isEatable && destroyer.Health > 0)
                            {
                                deltaHealth = EatVoxelData(destroyer, data);
                            }

                            if (eatOrDestroyCallback != null)
                            {
                                eatOrDestroyCallback(destroyer, data, deltaHealth, data.Health);
                            }

                            data.Health = 0; //Die
                            //data.VoxelRef = null; //voxel will should be returned to pool in eatVoxelCallback

                            VoxelData removeData = data;
                            Debug.Assert(data.Next == null || data.Altitude <= data.Next.Altitude);

                            deltaAltitude += data.Height;
                            data = data.Next;
                            Remove(cell, removeData);
                        }
                        else
                        {

                            if (destroyer.Health > 0)
#warning This if is temporary here (next time write 'why?')
                            {
                                data.Altitude -= deltaAltitude;
                                deltaAltitude += data.Height;
                                data.IsCollapsed = true;

                                if (collapseCallback != null)
                                {
                                    collapseCallback(data, data.Altitude - destroyer.Altitude);
                                }
                            }

                            data = data.Next;
                        }
                    }
                }
            }

            if(cell.Children != null)
            {
                for(int i = 0; i < cell.Children.Length; ++i)
                {
                    EatDestroyCollapseDescendants(deltaAltitude, destroyer, cell.Children[i], eatOrDestroyCallback, collapseCallback);
                }
            }
        }

        private int EatVoxelData(VoxelData eaterData, VoxelData voxelData)
        {
            //Apply simple calculations without Attack & Defense

            Debug.Assert(eaterData.Weight > voxelData.Weight);

            VoxelAbilities victimAbilities = m_allAbilities[voxelData.Owner][voxelData.Type];

            //Calculate health according to weight 
            int deltaWeight = eaterData.Weight - voxelData.Weight;
            int deltaHealth = 0; //if weight diff > 2 then voxel health have no effect
            if(deltaWeight == 2)
            {
                //if diff = 2 then 1 health will be added if voxel.Health > voxel.MaxHealth / 2;
                deltaHealth = Mathf.RoundToInt(((float)voxelData.Health) / victimAbilities.MaxHealth);
            }
            else if(deltaWeight == 1)
            {
                //if diff = 1 then health increase will be calculate using formula voxel.Health / voxel.MaxHealth * 8;
                deltaHealth = Mathf.RoundToInt((((float)voxelData.Health) / victimAbilities.MaxHealth) * 8);
            }

            eaterData.Health += deltaHealth;

            if(eaterData.Health > m_abilities.MaxHealth)
            {
                deltaHealth += (m_abilities.MaxHealth - eaterData.Health);

                eaterData.Health = m_abilities.MaxHealth;
            }
            else if(eaterData.Health < m_abilities.MinHealth)
            {
                deltaHealth += (m_abilities.MinHealth - eaterData.Health);

                eaterData.Health = m_abilities.MinHealth;
            }
            
            //DebugLog("Eating voxel " + voxelData.ToString());
            return deltaHealth;
        }


        private static void DebugLog(string message, bool verbose = false)
        {
#if UNITY_EDITOR

            if(verbose)
            {
                UnityEngine.Debug.Log(message);
            } 
#endif
        }

        public IVoxelDataController Clone()
        {
            return new VoxelDataController(this);
        }

  
    }
}

