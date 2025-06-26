using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    }
    
    public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {

        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary.Add(prefab, new Queue<GameObject>());
            Debug.Log($"Dynamic pool created for prefab: {prefab.name}");
        }

        Queue<GameObject> objectPool = poolDictionary[prefab];

        GameObject objectToSpawn;
        if (objectPool.Count > 0)
        {
            objectToSpawn = objectPool.Dequeue();
        }
        else
        {
            objectToSpawn = Instantiate(prefab); 
            objectToSpawn.transform.SetParent(transform, false); 
            Debug.Log($"New instance created for prefab: {prefab.name} (Pool was empty)");
        }
        objectToSpawn.SetActive(true);
        RectTransform rt = objectToSpawn.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero; 
            rt.rotation = Quaternion.identity; 
            rt.localScale = Vector3.one;
        }
        else
        {
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
        }
        
        if (parent != null)
        {
            objectToSpawn.transform.SetParent(parent, false); 
        }

        return objectToSpawn;
    }
    
    public void DespawnObject(GameObject prefab, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            Debug.LogWarning($"Attempted to despawn object {obj.name} (from {prefab.name}), but no pool exists for this prefab. Destroying object instead.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false); 
        obj.transform.SetParent(transform); 
        poolDictionary[prefab].Enqueue(obj); 
    }
}