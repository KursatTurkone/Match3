using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockData", menuName = "Match3/Block Data", order = 51)]
public class BlockData : ScriptableObject
{
    public Sprite blockSprite;
    public Color blockColor;
    public Vector2 blockSize = new Vector2(1f, 1f);
    // İstersen buraya blok tipi, özel efektler ya da puan değerleri gibi ek alanlar da ekleyebilirsin.
}
