using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定义一个可被四叉树存储的对象的接口。
/// 任何需要被四叉树索引的对象都必须实现此接口。
/// </summary>
public interface IQuadtreeStorable
{
    /// <summary>
    /// 对象在二维空间中的位置。
    /// </summary>
    Vector2 Position { get; }
}


/// <summary>
/// 代表四叉树中的一个节点。
/// 这是一个通用的四叉树实现，用于在二维空间中高效地查询对象。
/// (已修改为泛型类，以支持任何实现了 IQuadtreeStorable 接口的类型)
/// </summary>
public class Quadtree<T> where T : IQuadtreeStorable
{
    // --- 核心属性 ---

    /// <summary>
    /// 此节点在二维空间中所代表的矩形边界。
    /// </summary>
    private Rect boundary;
    public Rect Boundary => boundary; // 公共访问器

    /// <summary>
    /// 一个节点在分裂成子节点之前可以容纳的最大对象数。
    /// </summary>
    private int capacity;

    /// <summary>
    /// 存储在此节点中的对象。
    /// (已修改为泛型列表)
    /// </summary>
    private List<T> objects = new();

    /// <summary>
    /// 标记此节点是否已经被分裂成四个子节点。
    /// </summary>
    private bool divided = false;
    public bool Divided => divided; // 公共访问器，用于调试绘制

    // --- 子节点引用 ---

    // (已修改为泛型)
    private Quadtree<T> northwest;
    public Quadtree<T> Northwest => northwest; 

    private Quadtree<T> northeast;
    public Quadtree<T> Northeast => northeast; 

    private Quadtree<T> southwest;
    public Quadtree<T> Southwest => southwest; 

    private Quadtree<T> southeast;
    public Quadtree<T> Southeast => southeast; 

    /// <summary>
    /// 构造函数，创建一个新的四叉树节点。
    /// </summary>
    /// <param name="boundary">此节点所代表的矩形区域。</param>
    /// <param name="capacity">此节点的最大容量。</param>
    public Quadtree(Rect boundary, int capacity)
    {
        this.boundary = boundary;
        this.capacity = capacity;
    }

    /// <summary>
    /// 尝试将一个对象插入到四叉树中。
    /// </summary>
    /// <param name="obj">要插入的、实现了IQuadtreeStorable接口的对象。</param>
    /// <returns>如果对象成功插入，则返回true。</returns>
    public bool Insert(T obj)
    {
        // 1. 检查对象是否在此节点的边界内。如果不在，则无法插入。
        // (已修改为使用接口的Position属性)
        if (!boundary.Contains(obj.Position))
        {
            return false;
        }

        // 2. 如果当前节点还有容量，并且还未分裂，则直接将对象添加到列表中。
        if (objects.Count < capacity && !divided)
        {
            objects.Add(obj);
            return true;
        }
        
        // 3. 如果节点已满且尚未分裂，则执行分裂操作。
        if (!divided)
        {
            Subdivide();
        }

        // 4. 分裂后，将对象尝试插入到对应的子节点中。
        if (northwest.Insert(obj)) return true;
        if (northeast.Insert(obj)) return true;
        if (southwest.Insert(obj)) return true;
        if (southeast.Insert(obj)) return true;

        // 如果由于某种原因（例如，对象正好在边界线上），所有子节点都无法接收，
        // 则插入失败。在实践中这种情况很少见。
        return false;
    }

    /// <summary>
    /// 将当前节点分裂成四个大小相等的子节点。
    /// </summary>
    private void Subdivide()
    {
        // 计算子节点的尺寸和位置
        float x = boundary.x;
        float y = boundary.y;
        float w = boundary.width / 2;
        float h = boundary.height / 2;

        // 创建四个子节点的矩形边界
        Rect nw = new Rect(x, y + h, w, h);
        Rect ne = new Rect(x + w, y + h, w, h);
        Rect sw = new Rect(x, y, w, h);
        Rect se = new Rect(x + w, y, w, h);

        // 实例化四个子节点 (已修改为泛型)
        northwest = new Quadtree<T>(nw, capacity);
        northeast = new Quadtree<T>(ne, capacity);
        southwest = new Quadtree<T>(sw, capacity);
        southeast = new Quadtree<T>(se, capacity);

        // 标记当前节点为已分裂
        divided = true;

        // 将分裂前存储在本节点的对象，重新分配到新的子节点中去。
        foreach (var obj in objects)
        {
            if (northwest.Insert(obj)) continue;
            if (northeast.Insert(obj)) continue;
            if (southwest.Insert(obj)) continue;
            if (southeast.Insert(obj)) continue;
        }

        // 清空本节点的对象列表，因为所有对象都已下沉到子节点。
        objects.Clear();
    }

    /// <summary>
    /// 查询并返回在指定矩形范围内的所有对象。
    /// </summary>
    /// <param name="range">要查询的矩形区域。</param>
    /// <param name="found">用于收集结果的泛型列表。</param>
    public void Query(Rect range, List<T> found)
    {
        // 1. 如果查询范围与此节点的边界完全不相交，则此节点及其所有子节点都不可能包含目标对象，直接返回。
        // 这是四叉树实现高效查询的核心。
        if (!boundary.Overlaps(range))
        {
            return;
        }

        // 2. 如果节点尚未分裂，则遍历本节点存储的所有对象。
        if (!divided)
        {
            foreach (var obj in objects)
            {
                // 检查对象的位置是否在查询范围内 (已修改为使用接口的Position属性)
                if (range.Contains(obj.Position))
                {
                    found.Add(obj);
                }
            }
            return; // 因为未分裂，所以没有子节点，直接返回。
        }

        // 3. 如果节点已经分裂，则递归地在所有子节点中执行查询。
        northwest.Query(range, found);
        northeast.Query(range, found);
        southwest.Query(range, found);
        southeast.Query(range, found);
    }
    
    /// <summary>
    /// 清空整个四叉树及其所有子节点，为下一帧的重建做准备。
    /// </summary>
    public void Clear()
    {
        // 清空当前节点的对象列表
        objects.Clear();

        // 如果已经分裂，则递归地清空所有子节点
        if (divided)
        {
            northwest.Clear();
            northeast.Clear();
            southwest.Clear();
            southeast.Clear();
        }

        // 重置分裂状态，但不再将子节点引用设为null，以便复用节点对象
        divided = false;
    }
}