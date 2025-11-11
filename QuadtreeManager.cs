using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 这是一个通用的包装类，用于将任何类型的对象适配到IQuadtreeStorable接口。
/// 这使得我们的四叉树可以存储任意类型的对象，只要我们能为它提供一个位置。
/// </summary>
public class QuadtreeObjectWrapper : IQuadtreeStorable
{
    /// <summary>
    /// 实际存储的对象引用。
    /// </summary>
    public object WrappedObject { get; set; }
    
    /// <summary>
    /// 实现接口所需的位置属性。
    /// </summary>
    public Vector2 Position { get; set; }

    // 无参构造函数，用于对象池创建实例
    public QuadtreeObjectWrapper() {}

    public QuadtreeObjectWrapper(object obj, Vector2 position)
    {
        WrappedObject = obj;
        Position = position;
    }

    // 用于从对象池中取出后重置状态的方法
    public void Reset(object obj, Vector2 position)
    {
        WrappedObject = obj;
        Position = position;
    }
}


/// <summary>
/// 全局单例管理器，负责维护和操作游戏世界中的主四叉树。
/// (已修改为使用泛型化的Quadtree)
/// </summary>
public class QuadtreeManager
{
    /// <summary>
    /// 主四叉树实例。(已修改为存储包装类)
    /// </summary>
    private Quadtree<QuadtreeObjectWrapper> quadtree;
 
    /// <summary>
    /// 存储所有需要被四叉树管理的游戏对象的列表。(已修改为存储包装类)
    /// </summary>
    private readonly List<QuadtreeObjectWrapper> registeredObjects = new List<QuadtreeObjectWrapper>();
 
    /// <summary>
    /// 用于缓存查询结果的列表。(已修改为存储包装类)
    /// </summary>
    private readonly List<QuadtreeObjectWrapper> _queryResultCache = new List<QuadtreeObjectWrapper>();

    /// <summary>
    /// QuadtreeObjectWrapper的对象池，用于避免运行时GC。
    /// </summary>
    private readonly Stack<QuadtreeObjectWrapper> _wrapperPool = new Stack<QuadtreeObjectWrapper>(1000); // 初始容量设为1000

    /// <summary>
    /// 构造函数，允许在创建实例时定义四叉树的边界和容量。
    /// </summary>
    /// <param name="bounds">四叉树的世界边界</param>
    /// <param name="capacity">每个节点的容量</param>
    public QuadtreeManager(Rect bounds, int capacity = 4)
    {
        quadtree = new Quadtree<QuadtreeObjectWrapper>(bounds, capacity);
    }
 
    /// <summary>
    /// 手动重建四叉树。这提供了更灵活的控制，允许我们在服务器或客户端的特定时间点进行重建。
    /// </summary>
    public void RebuildTree()
    {
        if (quadtree == null) return;
        
        quadtree.Clear();
        foreach (var wrapper in registeredObjects)
        {
            // 这里我们假设包装器内的位置信息是最新的。
            // 对于客户端对象，我们需要在注册前更新其包装器的位置。
            // 对于服务器对象，其位置在逻辑Tick中更新，是天然最新的。
            quadtree.Insert(wrapper);
        }
    }
 
    /// <summary>
    /// 清空所有已注册的对象和四叉树。
    /// </summary>
    public void ClearAll()
    {
        // 将所有使用中的包装器归还到对象池
        foreach (var wrapper in registeredObjects)
        {
            _wrapperPool.Push(wrapper);
        }
        registeredObjects.Clear();

        if (quadtree != null)
        {
            quadtree.Clear();
        }
    }
 
    /// <summary>
    /// 向管理器注册一个对象。(已修改为接受通用对象和位置)
    /// </summary>
    /// <param name="obj">要注册的任何类型的对象。</param>
    /// <param name="position">该对象当前的位置。</param>
    public void RegisterObject(object obj, Vector2 position)
    {
        if (obj != null)
        {
            // 从对象池获取或创建一个新的包装器实例，而不是每次都new
            QuadtreeObjectWrapper wrapper;
            if (_wrapperPool.Count > 0)
            {
                wrapper = _wrapperPool.Pop();
                wrapper.Reset(obj, position);
            }
            else
            {
                wrapper = new QuadtreeObjectWrapper(obj, position);
            }
            registeredObjects.Add(wrapper);
        }
    }
 
    /// <summary>
    /// 从管理器中注销一个对象。(已修改为接受通用对象)
    /// </summary>
    /// <param name="obj">要注销的对象。</param>
    public void UnregisterObject(object obj)
    {
        if (obj != null)
        {
            // 找到包装器，将其归还到池中，然后从列表中移除
            int index = registeredObjects.FindIndex(wrapper => wrapper.WrappedObject == obj);
            if (index != -1)
            {
                _wrapperPool.Push(registeredObjects[index]);
                registeredObjects.RemoveAt(index);
            }
        }
    }
    
    /// <summary>
    /// 从管理器中注销一个对象的所有实例。
    /// 这对于一个对象被多次注册的情况很有用。
    /// </summary>
    /// <param name="obj">要注销的对象。</param>
    public void UnregisterAllInstances(object obj)
    {
        if (obj == null) return;

        // 从后向前遍历以安全地在循环中移除元素
        for (int i = registeredObjects.Count - 1; i >= 0; i--)
        {
            if (registeredObjects[i].WrappedObject == obj)
            {
                _wrapperPool.Push(registeredObjects[i]);
                registeredObjects.RemoveAt(i);
            }
        }
    }
 
    /// <summary>
    /// 查询在指定矩形范围内的所有对象。(已修改为返回包装类列表)
    /// </summary>
    /// <param name="range">要查询的矩形区域。</param>
    /// <returns>一个包含所有在范围内的对象的包装器列表。</returns>
    public List<QuadtreeObjectWrapper> Query(Rect range)
    {
        // 使用缓存的列表来接收结果
        _queryResultCache.Clear();
        if (quadtree != null)
        {
            quadtree.Query(range, _queryResultCache);
        }
        return _queryResultCache;
    }
}