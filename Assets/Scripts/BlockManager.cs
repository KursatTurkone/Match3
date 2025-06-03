using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    [Header("Grid Ayarları")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float blockSpacing = 1.1f;  
    public float fallSpeed = 5f;     
    public float rowDelay = 0.2f;      

    [Header("Blok Verileri")]
    public List<BlockData> blockDataList;


    private Block[,] blocks;

    private void Start()
    {
        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
     
        yield return StartCoroutine(CreateGridRowByRow());
        yield return StartCoroutine(WaitForBlocksToSettle());
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(ProcessMatches());
    }

    #region Grid Oluşturma ve Yerleşme

    IEnumerator CreateGridRowByRow()
    {
        blocks = new Block[gridWidth, gridHeight];

     
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector3 spawnPos = new Vector3(x * blockSpacing, gridHeight * blockSpacing, 0);
                GameObject blockGO = new GameObject("Block_" + x + "_" + y);
                blockGO.transform.position = spawnPos;
                blockGO.transform.parent = transform;

                Block blockComponent = blockGO.AddComponent<Block>();
             
                BlockData validData = GetValidBlockData(x, y);
                blockComponent.Setup(validData);

                blocks[x, y] = blockComponent;

           
                Vector3 targetPos = new Vector3(x * blockSpacing, y * blockSpacing, 0);
                StartCoroutine(MoveBlock(blockComponent, spawnPos, targetPos, fallSpeed));
            }
         
            yield return new WaitForSeconds(rowDelay);
        }
    }


    IEnumerator WaitForBlocksToSettle()
    {
        yield return new WaitUntil(() => AllBlocksSettled());
    }

    bool AllBlocksSettled()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (blocks[x, y] != null)
                {
                    Vector3 targetPos = new Vector3(x * blockSpacing, y * blockSpacing, 0);
                    if (Vector3.Distance(blocks[x, y].transform.position, targetPos) > 0.05f)
                        return false;
                }
            }
        }
        return true;
    }

    #endregion

    #region Match Önleyici Yerleştirme

    BlockData GetValidBlockData(int x, int y)
    {
        for (int i = 0; i < 100; i++)
        {
            BlockData candidate = blockDataList[Random.Range(0, blockDataList.Count)];
            if (IsValidPlacement(x, y, candidate))
                return candidate;
        }
        return blockDataList[0];
    }


    bool IsValidPlacement(int x, int y, BlockData candidate)
    {
 
        if (x > 0 && blocks[x - 1, y] != null)
        {
            if (blocks[x - 1, y].blockData.blockColor == candidate.blockColor)
                return false;
        }
      
        if (x < gridWidth - 1 && blocks[x + 1, y] != null)
        {
            if (blocks[x + 1, y].blockData.blockColor == candidate.blockColor)
                return false;
        }
    
        if (y > 0 && blocks[x, y - 1] != null)
        {
            if (blocks[x, y - 1].blockData.blockColor == candidate.blockColor)
                return false;
        }
        return true;
    }

    #endregion

    #region Eşleşme Tespiti (Flood Fill)


    List<Block> GetMatches()
    {
        List<Block> matchedBlocks = new List<Block>();
        bool[,] visited = new bool[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (blocks[x, y] != null && !visited[x, y])
                {
                    List<Vector2Int> connectedCells = new List<Vector2Int>();
                    Color targetColor = blocks[x, y].blockData.blockColor;
                    FloodFill(x, y, targetColor, visited, connectedCells);
                    if (connectedCells.Count >= 3)
                    {
                        foreach (Vector2Int cell in connectedCells)
                        {
                            Block b = blocks[cell.x, cell.y];
                            if (b != null && !matchedBlocks.Contains(b))
                                matchedBlocks.Add(b);
                        }
                    }
                }
            }
        }
        return matchedBlocks;
    }


    void FloodFill(int x, int y, Color targetColor, bool[,] visited, List<Vector2Int> connectedCells)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            return;
        if (visited[x, y])
            return;
        if (blocks[x, y] == null)
            return;
        if (blocks[x, y].blockData.blockColor != targetColor)
            return;

        visited[x, y] = true;
        connectedCells.Add(new Vector2Int(x, y));

        FloodFill(x + 1, y, targetColor, visited, connectedCells);
        FloodFill(x - 1, y, targetColor, visited, connectedCells);
        FloodFill(x, y + 1, targetColor, visited, connectedCells);
        FloodFill(x, y - 1, targetColor, visited, connectedCells);
    }

    #endregion

    #region İşlem Sırası: Patlama, Aşağı İnme, Yeni Blok Spawn

    IEnumerator ProcessMatches()
    {
        bool matchesFound;
        do
        {
            yield return StartCoroutine(WaitForBlocksToSettle());
            List<Block> matches = GetMatches();
            if (matches.Count > 0)
            {
               
                yield return StartCoroutine(RemoveMatches(matches));
                yield return new WaitForSeconds(0.2f);

             
                yield return StartCoroutine(FallDownBlocksConcurrently());

           
                yield return StartCoroutine(SpawnNewBlocksRowByRow());

                yield return StartCoroutine(WaitForBlocksToSettle());
                yield return new WaitForSeconds(0.2f);
                matchesFound = true;
            }
            else
            {
                matchesFound = false;
            }
        } while (matchesFound);
    }


    IEnumerator RemoveMatches(List<Block> matchedBlocks)
    {
        float duration = 0.2f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0f, timer / duration);
            foreach (Block block in matchedBlocks)
            {
                if (block != null)
                    block.transform.localScale = new Vector3(scale, scale, 1f);
            }
            yield return null;
        }
        foreach (Block block in matchedBlocks)
        {
            if (block != null)
            {
                // Grid dizisinde bu bloğun referansını temizle.
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        if (blocks[x, y] == block)
                        {
                            blocks[x, y] = null;
                            break;
                        }
                    }
                }
                Destroy(block.gameObject);
            }
        }
        matchedBlocks.RemoveAll(item => item == null);
    }

    IEnumerator FallDownBlocksConcurrently()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            List<Block> columnBlocks = new List<Block>();
          
            for (int y = 0; y < gridHeight; y++)
            {
                if (blocks[x, y] != null)
                    columnBlocks.Add(blocks[x, y]);
            }
          
            for (int i = 0; i < columnBlocks.Count; i++)
            {
                Block block = columnBlocks[i];
                Vector3 targetPos = new Vector3(x * blockSpacing, i * blockSpacing, 0);
                blocks[x, i] = block;
                StartCoroutine(MoveBlock(block, block.transform.position, targetPos, fallSpeed));
            }
    
            for (int y = columnBlocks.Count; y < gridHeight; y++)
            {
                blocks[x, y] = null;
            }
        }

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(WaitForBlocksToSettle());
    }


    IEnumerator SpawnNewBlocksRowByRow()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            bool rowHasEmpty = false;
            for (int x = 0; x < gridWidth; x++)
            {
                if (blocks[x, y] == null)
                {
                    rowHasEmpty = true;
                    Vector3 spawnPos = new Vector3(x * blockSpacing, gridHeight * blockSpacing, 0);
                    GameObject blockGO = new GameObject("Block_" + x + "_" + y);
                    blockGO.transform.position = spawnPos;
                    blockGO.transform.parent = transform;

                    Block blockComponent = blockGO.AddComponent<Block>();
                    // Yeni blok spawn edilirken, komşulara bakıp tamamen farklı renk seç.
                    BlockData validData = GetValidBlockData(x, y);
                    blockComponent.Setup(validData);

                    blocks[x, y] = blockComponent;
                    Vector3 targetPos = new Vector3(x * blockSpacing, y * blockSpacing, 0);
                    StartCoroutine(MoveBlock(blockComponent, spawnPos, targetPos, fallSpeed));
                }
            }
            if (rowHasEmpty)
                yield return new WaitForSeconds(rowDelay);
        }
    }

    #endregion

    #region Hareket (MoveBlock)


    IEnumerator MoveBlock(Block block, Vector3 startPos, Vector3 targetPos, float speed)
    {
        float elapsed = 0f;
        float duration = Vector3.Distance(startPos, targetPos) / speed;
        while (elapsed < duration)
        {
            if (block == null)
                yield break;
            block.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (block != null)
            block.transform.position = targetPos;
    }

    #endregion
}
