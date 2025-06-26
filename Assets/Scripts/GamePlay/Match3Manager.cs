using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Match3Manager : MonoBehaviour
{
    public static Match3Manager Instance;

    [Header("Grid Ayarları")] public int width = 8;
    public int height = 8;
    public Vector2 blockSize = new Vector2(100, 100);
    [SerializeField] private Vector2 extraOffset = Vector2.zero;
    public Transform blockParent;
    public GameObject blockPrefab;
    public List<BlockData> normalBlocks;
    public float fallSpeed = 400f;

    private Block[,] grid;
    private Block selectedBlock;
    private bool isProcessing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (blockParent == null)
        {
            enabled = false;
            return;
        }

        if (blockParent.GetComponent<RectTransform>() == null)
        {
            enabled = false;
            return;
        }

        if (blockPrefab == null)
        {
            enabled = false;
            return;
        }

        if (ObjectPoolManager.Instance == null)
        {
            enabled = false;
            return;
        }

#if UNITY_EDITOR
        if (blockPrefab != null && blockPrefab.TryGetComponent(out RectTransform rtPrefab) &&
            blockSize != rtPrefab.sizeDelta)
        {
            Debug.LogWarning(
                "BlockSize ayarın, prefab boyutundan farklı. Prefab boyutuna uyum sağlamak için blockSize güncellendi. Bunu dilersen manuel olarak düzenleyebilir veya prefab boyutunu değiştirebilirsin.");
            blockSize = rtPrefab.sizeDelta;
        }
#endif
        GenerateGrid();
    }

    void GenerateGrid()
    {
        grid = new Block[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Block newBlock = CreateBlockGameObject(x, y);
                if (newBlock != null)
                {
                    grid[x, y] = newBlock;
                    newBlock.gridPos = new Vector2Int(x, y);

                    BlockData chosen = GetValidRandomData(x, y);
                    if (chosen != null)
                    {
                        newBlock.Setup(chosen);
                    }
                    else
                    {
                        newBlock.Setup(normalBlocks[0]);
                    }

                    RectTransform rt = newBlock.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = GetUIPosition(x, y);
                    }
                }
            }
        }

        isProcessing = true;
        StartCoroutine(InitialMatchCheck());
    }

    private Vector2 GetUIPosition(int x, int y)
    {
        return new Vector2(x * blockSize.x, -y * blockSize.y) + extraOffset;
    }

    Block CreateBlockGameObject(int x, int y, bool isFallingNewBlock = false, int emptyCountAbove = 0)
    {
        if (blockPrefab == null || ObjectPoolManager.Instance == null)
        {
            return null;
        }

        GameObject go = ObjectPoolManager.Instance.SpawnObject(
            blockPrefab,
            Vector3.zero,
            Quaternion.identity,
            blockParent
        );

        RectTransform rt = go.GetComponent<RectTransform>();

        if (rt == null)
        {
            ObjectPoolManager.Instance.DespawnObject(blockPrefab, go);
            return null;
        }

        rt.sizeDelta = blockSize;

        if (isFallingNewBlock)
        {
            float spawnYOffset = (emptyCountAbove + 1) * blockSize.y;
            rt.anchoredPosition = new Vector2(x * blockSize.x, spawnYOffset) + extraOffset;
        }
        else
        {
            rt.anchoredPosition = GetUIPosition(x, y);
        }

        Block block = go.GetComponent<Block>();
        if (block == null)
        {
            ObjectPoolManager.Instance.DespawnObject(blockPrefab, go);
            return null;
        }

        return block;
    }

    IEnumerator InitialMatchCheck()
    {
        yield return new WaitForEndOfFrame();

        bool hasMatches = true;
        while (hasMatches)
        {
            hasMatches = false;
            List<Block> allMatches = new List<Block>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y] != null)
                    {
                        List<Block> matches = FindMatchAt(new Vector2Int(x, y));
                        if (matches.Count >= 3)
                        {
                            allMatches.AddRange(matches);
                            hasMatches = true;
                        }
                    }
                }
            }

            if (hasMatches)
            {
                allMatches = allMatches.Distinct().ToList();
                yield return StartCoroutine(DestroyBlocks(allMatches));
                yield return StartCoroutine(FillBoard());
            }
        }

        isProcessing = false;
    }

    BlockData GetValidRandomData(int x, int y)
    {
        List<BlockData> candidates = new List<BlockData>(normalBlocks);
        int maxAttempts = 100;

        for (int i = 0; i < maxAttempts; i++)
        {
            if (candidates.Count == 0)
            {
                return normalBlocks[Random.Range(0, normalBlocks.Count)];
            }

            BlockData chosenData = candidates[Random.Range(0, candidates.Count)];
            bool isMatch = false;

            if (x >= 2)
            {
                Block blockBefore1 = grid[x - 1, y];
                Block blockBefore2 = grid[x - 2, y];

                if (blockBefore1 != null && blockBefore1.data == chosenData &&
                    blockBefore2 != null && blockBefore2.data == chosenData)
                {
                    isMatch = true;
                }
            }

            if (y >= 2)
            {
                Block blockAbove1 = grid[x, y - 1];
                Block blockAbove2 = grid[x, y - 2];

                if (blockAbove1 != null && blockAbove1.data == chosenData &&
                    blockAbove2 != null && blockAbove2.data == chosenData)
                {
                    isMatch = true;
                }
            }

            if (!isMatch)
            {
                return chosenData;
            }
            else
            {
                candidates.Remove(chosenData);
            }
        }

        return normalBlocks[Random.Range(0, normalBlocks.Count)];
    }


    public void OnBlockClicked(Block clicked)
    {
        if (isProcessing) return;

        if (selectedBlock == null)
        {
            selectedBlock = clicked;
            clicked.SetHighlight(true);
            return;
        }

        if (clicked == selectedBlock)
        {
            selectedBlock.SetHighlight(false);
            selectedBlock = null;
            return;
        }

        if (IsNeighbor(clicked.gridPos, selectedBlock.gridPos))
        {
            selectedBlock.SetHighlight(false);
            isProcessing = true;
            StartCoroutine(SwapAndCheck(clicked, selectedBlock));
            selectedBlock = null;
            return;
        }

        selectedBlock.SetHighlight(false);
        selectedBlock = clicked;
        clicked.SetHighlight(true);
    }

    bool IsNeighbor(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) == 1;
    }

    IEnumerator SwapAndCheck(Block a, Block b)
    {
        yield return StartCoroutine(SwapAnim(a, b));

        List<Block> matchA = FindMatchAt(a.gridPos);
        List<Block> matchB = FindMatchAt(b.gridPos);

        if (matchA.Count < 3 && matchB.Count < 3)
        {
            yield return StartCoroutine(SwapAnim(a, b));
            isProcessing = false;
            yield break;
        }

        yield return StartCoroutine(ProcessMatches(matchA, matchB));
        isProcessing = false;
    }

    IEnumerator SwapAnim(Block a, Block b)
    {
        RectTransform rtA = a.GetComponent<RectTransform>();
        RectTransform rtB = b.GetComponent<RectTransform>();

        if (rtA == null || rtB == null)
        {
            yield break;
        }

        Vector2 posA_Start = rtA.anchoredPosition;
        Vector2 posB_Start = rtB.anchoredPosition;

        grid[a.gridPos.x, a.gridPos.y] = b;
        grid[b.gridPos.x, b.gridPos.y] = a;
        (a.gridPos, b.gridPos) = (b.gridPos, a.gridPos);

        float t = 0f;
        float duration = 0.15f;

        while (t < duration)
        {
            if (rtA == null || rtB == null) yield break;

            t += Time.deltaTime;
            float lerpFactor = t / duration;
            rtA.anchoredPosition = Vector2.Lerp(posA_Start, posB_Start, lerpFactor);
            rtB.anchoredPosition = Vector2.Lerp(posB_Start, posA_Start, lerpFactor);
            yield return null;
        }

        if (rtA != null) rtA.anchoredPosition = posB_Start;
        if (rtB != null) rtB.anchoredPosition = posA_Start;
    }

    List<Block> FindMatchAt(Vector2Int pos)
    {
        Block center = grid[pos.x, pos.y];
        if (center == null) return new List<Block>();

        List<Block> matchedBlocks = new List<Block>();

        List<Block> horizontalMatches = new List<Block> { center };
        for (int x = pos.x - 1; x >= 0; x--)
        {
            if (grid[x, pos.y] != null && grid[x, pos.y].data == center.data)
            {
                horizontalMatches.Add(grid[x, pos.y]);
            }
            else
            {
                break;
            }
        }

        for (int x = pos.x + 1; x < width; x++)
        {
            if (grid[x, pos.y] != null && grid[x, pos.y].data == center.data)
            {
                horizontalMatches.Add(grid[x, pos.y]);
            }
            else
            {
                break;
            }
        }

        if (horizontalMatches.Count >= 3)
        {
            matchedBlocks.AddRange(horizontalMatches);
        }

        List<Block> verticalMatches = new List<Block> { center };
        for (int y = pos.y - 1; y >= 0; y--)
        {
            if (grid[pos.x, y] != null && grid[pos.x, y].data == center.data)
            {
                verticalMatches.Add(grid[pos.x, y]);
            }
            else
            {
                break;
            }
        }

        for (int y = pos.y + 1; y < height; y++)
        {
            if (grid[pos.x, y] != null && grid[pos.x, y].data == center.data)
            {
                verticalMatches.Add(grid[pos.x, y]);
            }
            else
            {
                break;
            }
        }

        if (verticalMatches.Count >= 3)
        {
            matchedBlocks.AddRange(verticalMatches);
        }

        return matchedBlocks.Distinct().ToList();
    }

    IEnumerator ProcessMatches(List<Block> initialMatchA, List<Block> initialMatchB)
    {
        List<Block> allMatches = new List<Block>();
        if (initialMatchA.Count >= 3) allMatches.AddRange(initialMatchA);
        if (initialMatchB.Count >= 3) allMatches.AddRange(initialMatchB);
        allMatches = allMatches.Distinct().ToList();

        bool hasMoreMatches = true;
        while (hasMoreMatches)
        {
            hasMoreMatches = false;

            if (allMatches.Any())
            {
                yield return StartCoroutine(DestroyBlocks(allMatches));
                yield return StartCoroutine(FillBoard());
            }

            List<Block> currentMatches = new List<Block>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y] != null)
                    {
                        List<Block> foundMatches = FindMatchAt(new Vector2Int(x, y));
                        if (foundMatches.Count >= 3)
                        {
                            currentMatches.AddRange(foundMatches);
                            hasMoreMatches = true;
                        }
                    }
                }
            }

            allMatches = currentMatches.Distinct().ToList();
        }
    }

    IEnumerator DestroyBlocks(List<Block> blocksToDestroy)
    {
        foreach (Block b in blocksToDestroy)
        {
            if (b != null)
            {
                if (b.gridPos.x >= 0 && b.gridPos.x < width && b.gridPos.y >= 0 && b.gridPos.y < height &&
                    grid[b.gridPos.x, b.gridPos.y] == b)
                {
                    grid[b.gridPos.x, b.gridPos.y] = null;
                }

                ObjectPoolManager.Instance.DespawnObject(blockPrefab, b.gameObject);
            }
        }

        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator FillBoard()
    {
        List<Coroutine> moveCoroutines = new List<Coroutine>();

        for (int x = 0; x < width; x++)
        {
            int emptyCount = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                if (grid[x, y] == null)
                {
                    emptyCount++;
                }
                else if (emptyCount > 0)
                {
                    Block blockToMove = grid[x, y];

                    grid[x, y + emptyCount] = blockToMove;
                    grid[x, y] = null;
                    blockToMove.gridPos = new Vector2Int(x, y + emptyCount);

                    RectTransform rt = blockToMove.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Vector2 targetPos = GetUIPosition(x, y + emptyCount);
                        moveCoroutines.Add(StartCoroutine(MoveBlockUI(rt, rt.anchoredPosition, targetPos, fallSpeed)));
                    }
                    else
                    {
                        ObjectPoolManager.Instance.DespawnObject(blockPrefab, blockToMove.gameObject);
                    }
                }
            }

            for (int i = 0; i < emptyCount; i++)
            {
                int targetY = i;
                Block newBlock = CreateBlockGameObject(x, targetY, true, emptyCount - i - 1);

                if (newBlock != null)
                {
                    grid[x, targetY] = newBlock;
                    newBlock.gridPos = new Vector2Int(x, targetY);

                    BlockData chosen = normalBlocks[Random.Range(0, normalBlocks.Count)];
                    if (chosen != null)
                    {
                        newBlock.Setup(chosen);
                    }
                    else
                    {
                        newBlock.Setup(normalBlocks[0]);
                    }

                    RectTransform rt = newBlock.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Vector2 targetPos = GetUIPosition(x, targetY);
                        moveCoroutines.Add(StartCoroutine(MoveBlockUI(rt, rt.anchoredPosition, targetPos, fallSpeed)));
                    }
                    else
                    {
                        ObjectPoolManager.Instance.DespawnObject(blockPrefab, newBlock.gameObject);
                    }
                }
            }
        }

        foreach (Coroutine coroutine in moveCoroutines)
        {
            if (coroutine != null)
            {
                yield return coroutine;
            }
        }
    }

    IEnumerator MoveBlockUI(RectTransform rt, Vector2 start, Vector2 end, float speed)
    {
        if (rt == null)
        {
            Debug.LogWarning("MoveBlockUI: RectTransform null, hareket edilemedi.");
            yield break;
        }

        float distance = Vector2.Distance(start, end);
        float duration = distance / speed;
        if (duration < 0.05f) duration = 0.05f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rt == null) yield break;

            rt.anchoredPosition = Vector2.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rt != null)
            rt.anchoredPosition = end;
    }
}