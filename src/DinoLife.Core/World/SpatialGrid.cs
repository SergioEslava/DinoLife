using System;
using System.Collections.Generic;
using DinoLife.Core.Utils;

namespace DinoLife.Core.World;

/// <summary>
/// Uniform spatial grid for fast radius queries.
/// </summary>
public sealed class SpatialGrid
{
    private Vector2 _worldSize;
    private float _cellSize;
    private int _columns;
    private int _rows;
    private List<int>[] _cells;

    public SpatialGrid(Vector2 worldSize, float cellSize)
    {
        _worldSize = worldSize;
        _cellSize = MathF.Max(1f, cellSize);
        (_columns, _rows) = ComputeGridSize(_worldSize, _cellSize);
        _cells = CreateCells(_columns * _rows);
    }

    /// <summary>
    /// Size of each cell in world units.
    /// </summary>
    public float CellSize => _cellSize;

    /// <summary>
    /// Number of columns in the grid.
    /// </summary>
    public int Columns => _columns;

    /// <summary>
    /// Number of rows in the grid.
    /// </summary>
    public int Rows => _rows;

    /// <summary>
    /// Recreate the grid when world size changes.
    /// </summary>
    public void Resize(Vector2 worldSize)
    {
        _worldSize = worldSize;
        (_columns, _rows) = ComputeGridSize(_worldSize, _cellSize);
        _cells = CreateCells(_columns * _rows);
    }

    /// <summary>
    /// Change cell size and rebuild the grid.
    /// </summary>
    public void SetCellSize(float cellSize)
    {
        _cellSize = MathF.Max(1f, cellSize);
        Resize(_worldSize);
    }

    /// <summary>
    /// Clear all cells for a new tick.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i].Clear();
        }
    }

    /// <summary>
    /// Insert an entity index at the provided position.
    /// </summary>
    public void Insert(int entityIndex, Vector2 position)
    {
        int cellIndex = ToCellIndex(position);
        _cells[cellIndex].Add(entityIndex);
    }

    /// <summary>
    /// Query entity indices within radius. Results list is cleared and filled.
    /// </summary>
    public void QueryRadius(Vector2 position, float radius, List<int> results)
    {
        results.Clear();
        if (_cells.Length == 0) { return; }

        float r = MathF.Max(0f, radius);
        int minX = (int)MathF.Floor((position.X - r) / _cellSize);
        int maxX = (int)MathF.Floor((position.X + r) / _cellSize);
        int minY = (int)MathF.Floor((position.Y - r) / _cellSize);
        int maxY = (int)MathF.Floor((position.Y + r) / _cellSize);

        minX = Math.Clamp(minX, 0, _columns - 1);
        maxX = Math.Clamp(maxX, 0, _columns - 1);
        minY = Math.Clamp(minY, 0, _rows - 1);
        maxY = Math.Clamp(maxY, 0, _rows - 1);

        for (int y = minY; y <= maxY; y++)
        {
            int rowStart = y * _columns;
            for (int x = minX; x <= maxX; x++)
            {
                List<int> cell = _cells[rowStart + x];
                for (int i = 0; i < cell.Count; i++)
                {
                    results.Add(cell[i]);
                }
            }
        }
    }

    private int ToCellIndex(Vector2 position)
    {
        int x = (int)MathF.Floor(position.X / _cellSize);
        int y = (int)MathF.Floor(position.Y / _cellSize);

        x = Math.Clamp(x, 0, _columns - 1);
        y = Math.Clamp(y, 0, _rows - 1);

        return (y * _columns) + x;
    }

    private static (int columns, int rows) ComputeGridSize(Vector2 worldSize, float cellSize)
    {
        int columns = Math.Max(1, (int)MathF.Ceiling(worldSize.X / cellSize));
        int rows = Math.Max(1, (int)MathF.Ceiling(worldSize.Y / cellSize));
        return (columns, rows);
    }

    private static List<int>[] CreateCells(int count)
    {
        var cells = new List<int>[count];
        for (int i = 0; i < count; i++)
        {
            cells[i] = new List<int>(capacity: 16);
        }
        return cells;
    }
}
