using System.Collections.Generic;
using UnityEngine;

// generic object pool so we're not calling Instantiate/Destroy every frame
// create one in Awake/Start, use Get() to grab an object, Return() when done with it
public class ObjectPool
{
    private readonly GameObject        prefab;
    private readonly Transform         parent;
    private readonly Queue<GameObject> available = new Queue<GameObject>();

    public ObjectPool(GameObject prefab, Transform parent = null, int initialSize = 5)
    {
        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNew();
            obj.SetActive(false);
            available.Enqueue(obj);
        }
    }

    // grabs an idle object from the pool, or makes a new one if there are none left
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = available.Count > 0 ? available.Dequeue() : CreateNew();
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    // call this instead of Destroy() — just hides the object and puts it back
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        available.Enqueue(obj);
    }

    public int AvailableCount => available.Count;

    private GameObject CreateNew()
    {
        GameObject obj = Object.Instantiate(prefab, parent);
        PooledObject pooledObj = obj.AddComponent<PooledObject>();
        pooledObj.Pool = this;
        return obj;
    }
}

// gets added to every pooled object automatically so it knows how to return itself
public class PooledObject : MonoBehaviour
{
    public ObjectPool Pool { get; set; }

    public void ReturnToPool()
    {
        if (Pool != null)
            Pool.Return(gameObject);
        else
            Destroy(gameObject); // fallback if somehow the pool reference got lost
    }

    public void ReturnToPoolAfter(float delay)
    {
        Invoke(nameof(ReturnToPool), delay);
    }
}
