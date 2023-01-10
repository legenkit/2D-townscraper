using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GridManager : MonoBehaviour
{



    #region VAR
    [Header("Diamensions")]
    public int Width;
    public int Height;
    public float TileSize;

    [Header("Reference")]
    [SerializeField] GameObject Grid;
    [SerializeField] Transform TileHolder;
    [SerializeField] GameObject Tile;

    #region Private VAR
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
        _gridData = new byte[Width, Height];
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                _gridData[i, j] = 0;
                Vector3 pos = new Vector3(i * TileSize - Width / 2, j * TileSize - Height / 2, 0);
                GameObject tile = Instantiate(Grid, pos, Quaternion.identity, this.transform);
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
                DoScaleTween(_selectedGrid.transform.transform , 0.45f);

                if (Input.GetMouseButtonDown(0)) 
                { 
                    GenerateTile();
                }
                else
                {
                    DestroyTile();
                }
            }
        }
    }

    void GenerateTile()
    {
        _gridData[_selectedGrid.Cordinates.x, _selectedGrid.Cordinates.y] = 1;

        _selectedGrid.Tile = Instantiate(Tile, _selectedGrid.transform.position, Quaternion.identity, TileHolder);

        _selectedGrid.transform.GetComponent<SpriteRenderer>().enabled = false;
        _selectedGrid.SelectThisGrid();

        DoScaleTween(_selectedGrid.Tile.transform , 1);
    }
    void DestroyTile()
    {
        _gridData[_selectedGrid.Cordinates.x, _selectedGrid.Cordinates.y] = 0;

        if(_selectedGrid.Tile != null) Destroy(_selectedGrid.Tile);
        _selectedGrid.transform.GetComponent<SpriteRenderer>().enabled = true;
        _selectedGrid.SelectThisGrid();
    }

    public void DoScaleTween(Transform obj , float scale)
    {
        Sequence s = DOTween.Sequence();
        s.Append(obj.DOScale(scale*1.4f, .1f).SetEase(Ease.InBack));
        s.Append(obj.DOScale(scale, .2f).SetEase(Ease.OutBack));
    }
}

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