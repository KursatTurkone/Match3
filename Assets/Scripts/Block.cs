using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockData blockData;

    public void Setup(BlockData data)
    {
        blockData = data;
        
   
        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = data.blockSprite;
        sr.color = data.blockColor;
        

        if(data.blockSprite != null)
        {
    
            Vector2 spriteSize = data.blockSprite.bounds.size;
            
    
            float scaleX = data.blockSize.x / spriteSize.x;
            float scaleY = data.blockSize.y / spriteSize.y;
     
            transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {

            transform.localScale = Vector3.one;
        }
        

        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        if (data.blockSprite != null)
        {
            collider.size = data.blockSprite.bounds.size;
        }
        else
        {
         
            collider.size = data.blockSize;
        }
    }
}
