using System.Collections.Generic;
using UnityEngine;

//Keeps a set of pre-instantiated CellObjects that are recycled with SetActive instead of Instantiate/Destroy
public class CellObjectPool<T> where T : CellObject
{
    private T m_Prefab;
    private Transform m_Parent;
    private Stack<T> m_Available = new Stack<T>();

    public CellObjectPool(T prefab, int initialSize, Transform parent)
    {
        m_Prefab = prefab;
        m_Parent = parent;

        for (int i = 0; i < initialSize; ++i)
        {
            m_Available.Push(CreateObject());
        }
    }

    public T Get()
    {
        //pool is empty, grow it rather than failing
        T obj = m_Available.Count > 0 ? m_Available.Pop() : CreateObject();
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        obj.gameObject.SetActive(false);
        m_Available.Push(obj);
    }

    T CreateObject()
    {
        T obj = Object.Instantiate(m_Prefab, m_Parent);
        obj.gameObject.SetActive(false);
        return obj;
    }
}
