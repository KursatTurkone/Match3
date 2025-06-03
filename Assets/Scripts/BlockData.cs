using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockData", menuName = "Match3/Block Data", order = 51)]
public class BlockData : ScriptableObject
{
    public Sprite blockSprite;
    public Color blockColor;
    public Vector2 blockSize = new Vector2(1f, 1f);

}
