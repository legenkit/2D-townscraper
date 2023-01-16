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
    [SerializeField] GameObject CursorIcon;

    [Header("ModularTile")]
    public GameObject[] Tiles;

    #region Private VAR
    Grid[,] _gridArray;
    byte[,] _gridData;

    Grid _selectedGrid;

    Vector3 CameraPos;
    Vector3 MouseCurrentPos = Vector3.zero;
    Vector3 MouseLastPos = Vector3.zero;

    #region Cursor VAR

    #endregion
    Vector2 origin;
    RaycastHit2D mouseRay;

    GameObject CurrentGrid = null;
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
                Vector3 pos = new Vector3(i * TileSize - Width / 2, j * TileSize - 4, 0);
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
        //Cursor
        {
            origin = new Vector2(Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
                                          Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
            mouseRay = Physics2D.Raycast(origin, Vector2.zero, 0f);
            if (mouseRay && mouseRay.collider.CompareTag("Grid"))
            {
                if (CurrentGrid != null && CurrentGrid != mouseRay.collider.gameObject)
                {
                    CurrentGrid = mouseRay.collider.gameObject;
                    CursorIcon.transform.DOMove(CurrentGrid.transform.position, .1f).SetEase(Ease.OutBack);
                }
                CurrentGrid = mouseRay.collider.gameObject;
            }
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (mouseRay && mouseRay.collider.CompareTag("Grid"))
            {
                _selectedGrid = mouseRay.collider.GetComponent<Grid>();
                DoScaleTween(CursorIcon.transform, 1);

                if (Input.GetMouseButtonDown(0))
                {
                    DrawTile(_selectedGrid.Cordinates);
                    //Updating Neighbouring Tile
                    UpdateNeighourTile(_selectedGrid.Cordinates);
                }
                else
                {
                    DestroyTile(_selectedGrid.Cordinates);

                    UpdateNeighourTile(_selectedGrid.Cordinates);

                    //Updating Supporing Pillers if the structure needs piller
                    bool canUpdatePillers = _gridData[_selectedGrid.Cordinates.x, _selectedGrid.Cordinates.y + 1] == 2 ||
                                            (_gridData[_selectedGrid.Cordinates.x, _selectedGrid.Cordinates.y - 1] == 2 &&
                                            _gridData[_selectedGrid.Cordinates.x, _selectedGrid.Cordinates.y + 1] == 1);

                    DestroyPillerSupport(new GridLocation(_selectedGrid.Cordinates.x, _selectedGrid.Cordinates.y - 1));
                    if (canUpdatePillers) AddPillerSupport(_selectedGrid.Cordinates, true);

                }

            }
        }

        // Map Movement
        {
            if (Input.GetMouseButtonDown(2))
            {
                MouseLastPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
            if (Input.GetMouseButton(2))
            {
                MouseCurrentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                CameraPos = Camera.main.transform.position;
                CameraPos += MouseLastPos - MouseCurrentPos;
                CameraPos.z = Camera.main.transform.position.z;
                Camera.main.transform.position = CameraPos;
                MouseLastPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        // ZoomIn and ZoomOut
        {
            if (Input.mouseScrollDelta.y > 0 && Camera.main.orthographicSize > 3)
            {
                Camera.main.DOOrthoSize(Camera.main.orthographicSize * 0.9f, .2f);
            }
            else if (Input.mouseScrollDelta.y < 0 && Camera.main.orthographicSize < 12)
            {
                Camera.main.DOOrthoSize(Camera.main.orthographicSize * 1.1f, .2f);
            }
        }
    }


    void DrawTile(GridLocation pos)
    {

        if (pos.y > 0) DestroyPillerSupport(new GridLocation(pos.x, pos.y - 1));
        DestroyTile(pos);
        GenerateTile(pos, true);
        if (pos.y > 0) AddPillerSupport(new GridLocation(pos.x, pos.y - 1), true);
    }

    void GenerateTile(GridLocation pos, bool main)
    {
        // Updating GridData
        if (main) _gridData[pos.x, pos.y] = 1;

        // Instantiating The Tile
        Tile tile = TileToGenerate(_gridArray[pos.x, pos.y].Cordinates);
        _gridArray[pos.x, pos.y].Tile = Instantiate(Tiles[tile.ind], _gridArray[pos.x, pos.y].transform.position, Quaternion.identity, TileHolder);

        // Checking condition to Flip the Instantiated Tile
        if ((tile.canflip == -1 && _gridData[_gridArray[pos.x, pos.y].Cordinates.x + 1, _gridArray[pos.x, pos.y].Cordinates.y] == 1) || tile.canflip == 1)
        {
            _gridArray[pos.x, pos.y].FlipTileHor();
        }

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
    }

    Tile TileToGenerate(GridLocation pos)
    {
        //Pillers
        if (pos.y == 0)
        {
            if (CountHorNeighbour(pos) == 1)
            {
                return new Tile(1, -1);
            }
            return new Tile(0);
        }

        //House
        if (SearchGridData(pos, new byte[] { 5, 0, 5, 4, 1, 4, 5, 5, 5 }))
        {
            return new Tile(2);
        }

        //House Wall
        if (SearchGridData(pos, new byte[] { 5, 0, 5, 1, 1, 4, 5, 5, 5 }))
        {
            return new Tile(3, 1);
        }
        if (SearchGridData(pos, new byte[] { 5, 0, 5, 4, 1, 1, 5, 5, 5 }))
        {
            return new Tile(3);
        }

        //House Top
        if (SearchGridData(pos, new byte[] { 5, 0, 5, 1, 1, 1, 5, 5, 5 }))
        {
            return new Tile(4);
        }

        // House Piller
        if (SearchGridData(pos, new byte[] { 5, 2, 5, 4, 1, 4, 5, 5, 5 }))
        {
            return new Tile(5);
        }

        // House Wall Piller
        if (SearchGridData(pos, new byte[] { 5, 2, 5, 1, 1, 4, 5, 5, 5 }))
        {
            return new Tile(6, 1);
        }
        if (SearchGridData(pos, new byte[] { 5, 2, 5, 4, 1, 1, 5, 5, 5 }))
        {
            return new Tile(6);
        }

        // House Piller
        if (SearchGridData(pos, new byte[] { 5, 2, 5, 1, 1, 1, 5, 5, 5 }))
        {
            return new Tile(7);
        }

        // wall
        if (SearchGridData(pos, new byte[] { 5, 1, 4, 1, 1, 4, 5, 5, 5 }))
        {
            return new Tile(8, 1);
        }
        if (SearchGridData(pos, new byte[] { 4, 1, 5, 4, 1, 1, 5, 5, 5 }))
        {
            return new Tile(8);
        }

        // wall TR
        if (SearchGridData(pos, new byte[] { 5, 1, 1, 1, 1, 4, 5, 5, 5 }))
        {
            return new Tile(9, 1);
        }
        if (SearchGridData(pos, new byte[] { 1, 1, 5, 4, 1, 1, 5, 5, 5 }))
        {
            return new Tile(9);
        }

        // Box
        if (SearchGridData(pos, new byte[] { 1, 1, 1, 1, 1, 1, 5, 5, 5 }))
        {
            return new Tile(10);
        }

        // Box TR
        if (SearchGridData(pos, new byte[] { 4, 1, 1, 1, 1, 1, 5, 5, 5 }))
        {
            return new Tile(11);
        }
        if (SearchGridData(pos, new byte[] { 1, 1, 4, 1, 1, 1, 5, 5, 5 }))
        {
            return new Tile(11, 1);
        }

        // Box TLR
        if (SearchGridData(pos, new byte[] { 4, 5, 4, 1, 1, 1, 5, 5, 5 }))
        {
            return new Tile(12);
        }

        // House Support
        if (SearchGridData(pos, new byte[] { 4, 1, 4, 4, 1, 4, 5, 5, 5 }))
        {
            return new Tile(13);
        }

        // House Support TR
        if (SearchGridData(pos, new byte[] { 4, 1, 1, 4, 1, 4, 5, 5, 5 }))
        {
            return new Tile(14);
        }
        if (SearchGridData(pos, new byte[] { 1, 1, 4, 4, 1, 4, 5, 5, 5 }))
        {
            return new Tile(14, 1);
        }

        // House Support TLR
        if (SearchGridData(pos, new byte[] { 1, 1, 1, 4, 1, 4, 5, 5, 5 }))
        {
            return new Tile(15);
        }

        //Piller
        if (_gridData[pos.x, pos.y + 1] == 1)
        {
            return new Tile(16);
        }
        return new Tile(17);
    }

    bool SearchGridData(GridLocation pos, byte[] Pattern)
    {
        int count = 0;
        int ind = 0;
        for (int y = 1; y >= -1; y--)
        {
            for (int x = 1; x >= -1; x--)
            {
                if (Pattern[ind] == _gridData[pos.x + x, pos.y + y] || Pattern[ind] == 5 || (Pattern[ind] == 4 && _gridData[pos.x + x, pos.y + y] != 1))
                {
                    count++;
                }
                ind++;
            }
        }
        return count == 9;
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

    void AddPillerSupport(GridLocation pos, bool canterminate)
    {
        bool AllowedToBuild = canterminate && ((_gridData[pos.x + 1, pos.y] == 2 && _gridData[pos.x + 1, pos.y + 1] == 1) ||
                              (_gridData[pos.x - 1, pos.y] == 2 && _gridData[pos.x - 1, pos.y + 1] == 1));

        if (pos.y == -1 || _gridData[pos.x, pos.y] == 1 || AllowedToBuild)
        {
            if (pos.y > 0 && _gridData[pos.x, pos.y] == 1)
            {
                DrawTile(pos);
            }
            return;
        }

        if (pos.y == 0)
        {
            DrawTile(pos);
            UpdateNeighourTile(pos);
        }
        else
        {
            _gridData[pos.x, pos.y] = 2;
            GenerateTile(pos, false);
        }
        AddPillerSupport(new GridLocation(pos.x, pos.y - 1), false);
    }
    void DestroyPillerSupport(GridLocation pos)
    {
        if (pos.y == -1 || _gridData[pos.x, pos.y] != 2)
        {
            return;
        }
        _gridData[pos.x, pos.y] = 0;
        DestroyTile(pos);
        DestroyPillerSupport(new GridLocation(pos.x, pos.y - 1));
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

public class Tile
{
    public int ind;
    public int canflip;

    public Tile(int index, int flip = 0)
    {
        ind = index;
        canflip = flip;
    }
}

