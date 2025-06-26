using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Block : MonoBehaviour, IPointerClickHandler
{
    public BlockData data;
    public Vector2Int gridPos;

    [SerializeField] private GameObject selectedHighlight;

    public void Setup(BlockData newData)
    {
        data = newData;
        GetComponent<Image>().sprite = data.sprite;
        GetComponent<Image>().color = data.color;
        SetHighlight(false);
    }

    public void SetHighlight(bool on)
    {
        if (selectedHighlight != null)
            selectedHighlight.SetActive(on);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Match3Manager.Instance.OnBlockClicked(this);
    }
}

