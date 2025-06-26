using UnityEngine;

public enum BlockType 
{ 
    Normal, 
    StripedHorizontal, 
    StripedVertical, 
    Wrapped, 
    ColorBomb,
    Fish
}

[CreateAssetMenu(menuName = "Match3/Block Data")]
public class BlockData : ScriptableObject
{
    public Sprite sprite;
    public Color color; 
    public BlockType type = BlockType.Normal;
}