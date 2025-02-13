using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockData blockData;

    public void Setup(BlockData data)
    {
        blockData = data;
        
        // SpriteRenderer ekleyip ayarlıyoruz
        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = data.blockSprite;
        sr.color = data.blockColor;
        
        // Eğer sprite tanımlıysa sprite'ın gerçek boyutunu alıp ölçek hesaplaması yapıyoruz
        if(data.blockSprite != null)
        {
            // Sprite'ın boyutları; import ayarlarında belirlenen Pixels Per Unit'a göre world unit cinsindedir.
            Vector2 spriteSize = data.blockSprite.bounds.size;
            
            // Eğer blok veri olarak istediğimiz boyutu tam olarak doldurmak istiyorsak,
            // x ve y için ayrı ölçek faktörleri hesaplayabiliriz.
            float scaleX = data.blockSize.x / spriteSize.x;
            float scaleY = data.blockSize.y / spriteSize.y;
            
            // Bu şekilde sprite, blockData'da belirlediğimiz boyuta tam oturur.
            transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            // Sprite yoksa varsayılan ölçek kullanılır
            transform.localScale = Vector3.one;
        }
        
        // BoxCollider2D ekliyoruz.
        // Collider'ın size'sını sprite'ın orijinal boyutuna göre ayarlıyoruz, çünkü
        // GameObject'in ölçeği (transform.localScale) collider'a da uygulanır.
        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        if (data.blockSprite != null)
        {
            collider.size = data.blockSprite.bounds.size;
        }
        else
        {
            // Sprite yoksa, collider'ı doğrudan blockSize'ya eşitleyebiliriz.
            collider.size = data.blockSize;
        }
    }
}