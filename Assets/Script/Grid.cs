using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Grid : MonoBehaviour
{
    public GameObject Tile;
    public GridLocation Cordinates;
    public int Tilevalue = -1;

    public void SelectThisGrid()
    {

    }
    public void DeselectThisGrid()
    {

    }

    public void FlipTileHor()
    {
        Tile.transform.rotation = Quaternion.Euler(0, 180, 0);
    }
}
