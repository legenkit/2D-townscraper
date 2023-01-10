using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GridManager : MonoBehaviour
{

    public List<GridLocation> direction = new List<GridLocation>
    {
        new GridLocation(-1,0),
        new GridLocation(1, 0),
        new GridLocation(0,-1),
        new GridLocation(0, 1)
    };

    #region VAR
    [Header("Diamensions")]
    public int Width;
    public int Height;
    public float TileSize;

    [Header("Reference")]
    [SerializeField] GameObject Grid;
    [SerializeField] Transform TileHolder;

    [Header("ModularTile")]
    public GameObject[] Tiles;

    #region Private VAR
    Grid[,] _gridArray;
    byte[,] _gridData;

    Grid _selectedGrid;
    #endregion
    #endregion
    void Awake()
    {
        InitailizeGrid();
    }

    void InitailizeGrid()
    {
        _gridArray = new Grid[Width, Height];
        _gridData = new byte[Width, Height];
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                _gridData[i, j] = 0;
                Vector3 pos = new Vector3(i * TileSize - Width / 2, j * TileSize - Height / 2, 0);
                GameObject tile = Instantiate(Grid, pos, Quaternion.identity, this.transform);
                _gridArray[i, j] = tile.GetComponent<Grid>();
                _gridArray[i, j].Tilevalue = -1;
                tile.GetComponent<Grid>().Cordinates = new GridLocation(i, j);
            }
        }
    }

    void Update()
    {
        MyInput();
    }

    void MyInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (_selectedGrid != null) _selectedGrid.DeselectThisGrid();

            Vector2 origin = new Vector2(Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
                                          Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
            RaycastHit2D mouseRay = Physics2D.Raycast(origin, Vector2.zero, 0f);
            if (mouseRay && mouseRay.collider.CompareTag("Grid"))
            {
                _selectedGrid = mouseRay.collider.GetComponent<Grid>();
                DoScaleTween(_selectedGrid.transform.transform, 0.45f);

                if (Input.GetMouseButtonDown(0))
                {
                    DrawTile(_selectedGrid.Cordinates);
                    //Updating Neighbouring Tile
                    UpdateNeighourTile(_selectedGrid.Cordinates);
                }
                else
                {
                    DestroyTile(_selectedGrid.Cordinates);
                }

            }
        }
    }

    void DrawTile(GridLocation pos)
    {
        DestroyTile(pos);
        GenerateTile(pos);
    }

    void GenerateTile(GridLocation pos)
    {
        // Updating GridData
        _gridData[pos.x, pos.y] = 1;

        // Instantiating The Tile
        _gridArray[pos.x, pos.y].Tile = Instantiate(Tiles[_gridArray[pos.x, pos.y].Tilevalue = TileToGenerate(_gridArray[pos.x, pos.y].Cordinates)],
            _gridArray[pos.x, pos.y].transform.position, Quaternion.identity, TileHolder);

        // Checking condition to Flip the Instantiated Tile
        if (_gridData[_gridArray[pos.x, pos.y].Cordinates.x + 1, _gridArray[pos.x, pos.y].Cordinates.y] == 1)
        {
            _gridArray[pos.x, pos.y].FlipTileHor();
        }

        // Making Grid Sprite Disappear
        _gridArray[pos.x, pos.y].transform.GetComponent<SpriteRenderer>().enabled = false;
        _gridArray[pos.x, pos.y].SelectThisGrid();

        DoScaleTween(_gridArray[pos.x, pos.y].Tile.transform, 1);
    }

    void DestroyTile(GridLocation pos)
    {
        // Updating GridData
        _gridData[_gridArray[pos.x, pos.y].Cordinates.x, _gridArray[pos.x, pos.y].Cordinates.y] = 0;

        // Destroying Tile
        if (_gridArray[pos.x, pos.y].Tile != null) Destroy(_gridArray[pos.x, pos.y].Tile);

        // Clearing Reference from GRID script
        _gridArray[pos.x, pos.y].Tilevalue = -1;
        _gridArray[pos.x, pos.y].Tile = null;


        _gridArray[pos.x, pos.y].transform.GetComponent<SpriteRenderer>().enabled = true;
        _gridArray[pos.x, pos.y].SelectThisGrid();
    }

    int TileToGenerate(GridLocation pos)
    {
        if (pos.y == 0)
        {
            if (CountHorNeighbour(pos) == 1)
            {
                return 1;
            }
            return 0;
        }
        return Random.Range(2, Tiles.Length);
    }

    void UpdateNeighourTile(GridLocation pos)
    {
        if (pos.x < Width - 1 && _gridData[pos.x + 1, pos.y] == 1) 
            DrawTile(new GridLocation(pos.x + 1, pos.y));

        if (pos.x > 0 && _gridData[pos.x - 1, pos.y] == 1) 
            DrawTile(new GridLocation(pos.x - 1, pos.y));

        if (pos.y < Height - 1 && _gridData[pos.x, pos.y + 1] == 1) 
            DrawTile(new GridLocation(pos.x, pos.y + 1));

        if (pos.y > 0 && _gridData[pos.x, pos.y - 1] == 1) 
            DrawTile(new GridLocation(pos.x, pos.y - 1));
    }

    bool SearchGridData(GridLocation pos, byte[] Pattern)
    {
        int count = 0;
        int ind = 0;
        for (int y = -1; y < 2; y++)
        {
            for (int x = -1; x < 2; x++)
            {
                if (Pattern[ind] == _gridData[pos.x + x, pos.y + y] || Pattern[ind] == 5)
                    count++;
                ind++;
            }
        }
        return count == 9;
    }

    public int CountHorNeighbour(GridLocation pos)
    {
        int count = 0;
        if (pos.x <= 0 || pos.x >= Width - 1) return 5;
        if (_gridData[pos.x + 1, pos.y] == 1) count++;
        if (_gridData[pos.x - 1, pos.y] == 1) count++;
        return count;
    }
    public int CountSquareNeighbours(GridLocation pos)
    {
        int count = 0;
        if (pos.x <= 0 || pos.x >= Width - 1 || pos.y <= 0 || pos.y >= Height - 1) return 5;
        if (_gridData[pos.x, pos.y - 1] == 1) count++;
        if (_gridData[pos.x + 1, pos.y] == 1) count++;
        if (_gridData[pos.x - 1, pos.y] == 1) count++;
        if (_gridData[pos.x, pos.y - 1] == 1) count++;
        return count;
    }
    public int CountDiagonalNeighbours(GridLocation pos)
    {
        int count = 0;
        if (pos.x <= 0 || pos.x >= Width - 1 || pos.y <= 0 || pos.y >= Height - 1) return 5;
        if (_gridData[pos.x - 1, pos.y - 1] == 1) count++;
        if (_gridData[pos.x + 1, pos.y + 1] == 1) count++;
        if (_gridData[pos.x - 1, pos.y + 1] == 1) count++;
        if (_gridData[pos.x + 1, pos.y - 1] == 1) count++;
        return count;
    }
    protected int CountNeighbour(GridLocation Pos)
    {
        return CountSquareNeighbours(Pos) + CountDiagonalNeighbours(Pos);
    }

    public void DoScaleTween(Transform obj, float scale)
    {
        Sequence s = DOTween.Sequence();
        s.Append(obj.DOScale(scale * 1.4f, .1f).SetEase(Ease.InBack));
        s.Append(obj.DOScale(scale, .2f).SetEase(Ease.OutBack));
    }
}

[System.Serializable]
public class GridLocation
{
    public int x;
    public int y;
    public GridLocation(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

