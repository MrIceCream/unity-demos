using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfitiScrollView : MonoBehaviour
{
    //测试数据
    List<string> TestDataList = new List<string>();
    List<float> TestSize = new List<float>();

    //可见区域cell列表（当前激活的Cell）
    List<Cell> cellDatas = new List<Cell>();
    //缓冲Cell
    Queue<Cell> cellDatasCache = new Queue<Cell>();


    //当前定位坐标(非滚动轴实时坐标)
    private float currentPos;

    //第一个和最后一个物体
    private Cell firstCell;
    private Cell LastCell;

    //第一个和最后一个缓冲物体
    private Cell firstCacheCell;
    private Cell LastCacheCell;

    //要实例化的预制体
    public GameObject Prefab;

    public UIScrollBar ScrollBar;

    private UIScrollView scrollView;
    //目前只判定Horizontal和Vertical
    private bool posXorY;
    private float ScrollPos
    {
        get
        {
            if (posXorY)
                return scrollView.transform.localPosition.x;
            else
                return scrollView.transform.localPosition.y;
        }
    }

    //物体坐标
    private Vector3 _cellSizeVector3 = Vector3.zero;
    private  float SetCellSize
    {
        set
        {
            if (posXorY)
                _cellSizeVector3.x = value;
            else
                _cellSizeVector3.y = value;
        }
    }

    //UIPanel初始偏移
    private float initoffset;
    //ScrollView初始坐标
    private float initTransform;
    //UIPanel当前滚动区域大小
    private float scrollClipArea;
    //滚动区域总大小
    private float sizeCount;

    //更新Cell
    private Action<Cell> _updataCallBack;
    //根据index获取大小
    private Func<int, float> _getDataSizeCallBack;
    void Start()
    {
        //初始化测试数据
        for(int i=0;i<20;i++)
        {
            TestDataList.Add(i.ToString());
            TestSize.Add(i % 2 == 0 ? 150 : 250);
        }
        
        init();
    }
    private void initScrollBar()
    {
        float sizeSum = 0;
        foreach(var size in TestSize)
        {
            sizeSum += size;
        }
        sizeCount = sizeSum;
        ScrollBar.barSize = initoffset / sizeCount;
        ScrollBar.value = 0;
        ScrollBar.onChange.Add(new EventDelegate(()=> 
        {
            SetScrollBar();
        }));
    }
    private void UpdataScrollBar()
    {
        float currentMoveArea =  Math.Abs(scrollView.panel.clipOffset.y)- initoffset;
        ScrollBar.value = currentMoveArea / (sizeCount - initoffset*2);
    }
    public void SetScrollBar()
    {
        if (scrollView.isDragging)
            return;
        float moveDistance = ScrollBar.value * (sizeCount - initoffset * 2);
        scrollView.panel.clipOffset = new Vector2(0, -moveDistance - initoffset);
        transform.localPosition = new Vector2(transform.localPosition.x, moveDistance + initTransform);
    }
    //初始化后刷新一下
    private bool isLoad = false;
    private void LateUpdate()
    {
        if(!isLoad)
        {
            scrollView.ResetPosition();
            isLoad = true;
            initoffset = posXorY ? scrollView.panel.clipOffset.x : scrollView.panel.clipOffset.y;
            initoffset = Math.Abs(initoffset);
            initScrollBar();
            initTransform = transform.localPosition.y;
        }
        
    }
    public void UpdataTest(Cell cell)
    {
        CellData celldata = cell.Cellprefab.GetComponent<CellData>();
        celldata.SetData(cell.dataIndex.ToString());
    }
    public float SetSizeTest(int index)
    {
        return TestSize[index];
    }
    public void ReLoadData()
    {
        scrollView.ResetPosition();
        for (int i=0;i<cellDatas.Count;i++)
        {
            cellDatas[i].Cellprefab.SetActive(false);
            cellDatasCache.Enqueue(cellDatas[i]);
        }
        cellDatas.Clear();
        initLoad();
        scrollView.UpdatePosition();
        currentPos = ScrollPos;
    }
    public void JumpTo()
    {
        int index = 10;
       // scrollView.ResetPosition();
        for (int i = 0; i < cellDatas.Count; i++)
        {
            cellDatas[i].Cellprefab.SetActive(false);
            cellDatasCache.Enqueue(cellDatas[i]);
        }
        cellDatas.Clear();
        if(firstCacheCell!=null)
            cellDatasCache.Enqueue(firstCacheCell);
        else
            firstCacheCell = null;
        if(LastCacheCell!=null)
            cellDatasCache.Enqueue(LastCacheCell);
        LastCacheCell = null;

        int Index = index;
        //初始化区域总长度
        float AreaCellSizeCount = 1900f;
        //初始化生成数量
        int CreateCount = 0;
        scrollClipArea = posXorY ? scrollView.panel.finalClipRegion.z : scrollView.panel.finalClipRegion.w;
        while (AreaCellSizeCount < 1900+scrollClipArea && Index < TestDataList.Count)
        {

            GameObject prefab = null;
            Cell cell = null;
            if (cellDatasCache.Count == 0)
            {
                prefab = Instantiate(Prefab, scrollView.transform);
                cell = new Cell(prefab, Index++);
            }
            else
            {
                cell = cellDatasCache.Dequeue();
                cell.dataIndex = Index++;
                
                prefab = cell.Cellprefab;
                prefab.SetActive(true);
            }
            cellDatas.Add(cell);

            //坐标运算
            SetCellSize = posXorY ? AreaCellSizeCount : -AreaCellSizeCount;
            prefab.transform.localPosition = _cellSizeVector3;
            AreaCellSizeCount += _getDataSizeCallBack(CreateCount);
            cell.SetSize(posXorY, (int)_getDataSizeCallBack(CreateCount));

            CreateCount++;
            _updataCallBack?.Invoke(cell);
        }
        firstCell = cellDatas[0];
        LastCell = cellDatas.Count > 1 ? cellDatas[cellDatas.Count - 1] : firstCell;
    }
    public void init(Action<Cell> updataCallBack = null,Func<int,float> getCellSizeCallback = null)
    {
        _updataCallBack = UpdataTest;
        _getDataSizeCallBack = SetSizeTest;
        scrollView = GetComponent<UIScrollView>();
        posXorY = scrollView.movement == UIScrollView.Movement.Horizontal;
        Prefab.GetComponent<UIWidget>().pivot = posXorY ? UIWidget.Pivot.Left : UIWidget.Pivot.Top;
        initLoad();
        currentPos = ScrollPos;
    }
    //获取缓冲Cell
    private Cell GetFreeCell()
    {
        Cell cellCache = null;
        
        if(cellDatasCache.Count==0)
        {
            GameObject prefab = Instantiate(Prefab, scrollView.transform);
            cellCache = new Cell(prefab, 0);
        }
        else
        {
            cellCache = cellDatasCache.Dequeue();
        }
        return cellCache;
    }
    
    private void initLoad()
    {
        int Index = 0;
        //初始化区域总长度
        float AreaCellSizeCount = 0;
        //初始化生成数量
        int CreateCount = 0;
        scrollClipArea = posXorY ? scrollView.panel.finalClipRegion.z : scrollView.panel.finalClipRegion.w;
        while (AreaCellSizeCount < scrollClipArea && CreateCount < TestDataList.Count)
        {

            GameObject prefab = null;
            Cell cell = null;
            if (cellDatasCache.Count==0)
            {
                 prefab = Instantiate(Prefab, scrollView.transform);
                 cell = new Cell(prefab, Index++);
            }else
            {
                cell = cellDatasCache.Dequeue();
                prefab = cell.Cellprefab;
            }
            cellDatas.Add(cell);

            //坐标运算
            SetCellSize = posXorY? AreaCellSizeCount : -AreaCellSizeCount;
            prefab.transform.localPosition = _cellSizeVector3;
            AreaCellSizeCount += _getDataSizeCallBack(CreateCount);
            cell.SetSize(posXorY, (int)_getDataSizeCallBack(CreateCount));

            CreateCount++;
            _updataCallBack?.Invoke(cell);
        }
        firstCell = cellDatas[0];
        LastCell = cellDatas.Count > 1 ? cellDatas[cellDatas.Count - 1] : firstCell;
        //默认加载4个缓冲cell
        if (CreateCount < TestDataList.Count)
        {
            int cacheCount = cellDatasCache.Count;
            for (int i = cacheCount; i < 2; i++)
            {
                GameObject prefab = Instantiate(Prefab, scrollView.transform);
                Cell cell = new Cell(prefab, 0);
                cellDatasCache.Enqueue(cell);
                prefab.gameObject.SetActive(false);
            }
        }
    }
    private float LastScrollPos;
    void Update()
    {
        //有滑动
        if (currentPos!= ScrollPos&& LastScrollPos!= ScrollPos)
        {
            LastScrollPos = ScrollPos;
            if(scrollView.isDragging)
            {
                UpdataScrollBar();
            }
            //水平滑动
            if (posXorY)
            {
                //鼠标向→滑动
                if (currentPos < ScrollPos)
                {
                    LoadFirstCell();
                }
                //鼠标向←滑动
                else
                {
                    LoadLastCell();
                }
            }
            else
            {
                //鼠标向↑滑动
                if (currentPos < ScrollPos)
                {
                    LoadLastCell();
                }
                //鼠标向↓滑动
                else
                {
                    LoadFirstCell();
                }
            }
            
        }
    }
    private void LoadFirstCell()
    {
        //回收最后一个
        if (!LastCell.IsSee() && firstCell.dataIndex != 0)
        {
            LastCell.Cellprefab.gameObject.SetActive(false);
            cellDatasCache.Enqueue(LastCell);
            cellDatas.Remove(LastCell);
            for (int i = 0; i < cellDatas.Count; i++)
            {
                if (LastCell.dataIndex == cellDatas[i].dataIndex + 1)
                {
                    LastCell = cellDatas[i];
                    break;
                }
            }
        }
        //左侧加载完全（自身加载80%）
        float moveDistance = posXorY ? ScrollPos - currentPos : currentPos - ScrollPos;
        if (firstCacheCell != null && moveDistance > _getDataSizeCallBack(firstCell.dataIndex) * 0.8)
        {
            firstCacheCell = null;
            currentPos = ScrollPos;
        }
        if (firstCell.dataIndex != 0 && firstCacheCell == null)
        {
            firstCacheCell = GetFreeCell();

            int dataindex = firstCell.dataIndex - 1;
            firstCacheCell.Cellprefab.SetActive(true);


            SetCellSize = _getDataSizeCallBack(dataindex);
            if (posXorY)
                firstCacheCell.Cellprefab.transform.localPosition = firstCell.Cellprefab.transform.localPosition - _cellSizeVector3;
            else
                firstCacheCell.Cellprefab.transform.localPosition = firstCell.Cellprefab.transform.localPosition + _cellSizeVector3;
            firstCacheCell.SetSize(posXorY, (int)_getDataSizeCallBack(dataindex));

            firstCacheCell.dataIndex = dataindex;
            firstCell = firstCacheCell;
            cellDatas.Add(firstCacheCell);
            _updataCallBack?.Invoke(firstCell);
        }
        
        
    }
    private void LoadLastCell()
    {
        if (!firstCell.IsSee() && LastCell.dataIndex != TestDataList.Count - 1)
        {
            firstCell.Cellprefab.gameObject.SetActive(false);
            cellDatasCache.Enqueue(firstCell);
            cellDatas.Remove(firstCell);
            for (int i = 0; i < cellDatas.Count; i++)
            {
                if (firstCell.dataIndex == cellDatas[i].dataIndex - 1)
                {
                    firstCell = cellDatas[i];
                    break;
                }
            }
        }
        float moveDistance = posXorY ? currentPos - ScrollPos : ScrollPos - currentPos;
        if (LastCacheCell != null && moveDistance > TestSize[LastCacheCell.dataIndex] * 0.8)
        {
            LastCacheCell = null;
            currentPos = ScrollPos;
        }
        if (LastCell.dataIndex != TestDataList.Count - 1 && LastCacheCell == null)
        {
            LastCacheCell = GetFreeCell();
            int dataindex = LastCell.dataIndex + 1;
            LastCacheCell.Cellprefab.gameObject.SetActive(true);


            SetCellSize = _getDataSizeCallBack(LastCell.dataIndex);
            if (posXorY)
                LastCacheCell.Cellprefab.transform.localPosition = LastCell.Cellprefab.transform.localPosition + _cellSizeVector3;
            else
                LastCacheCell.Cellprefab.transform.localPosition = LastCell.Cellprefab.transform.localPosition - _cellSizeVector3;
            LastCacheCell.SetSize(posXorY, (int)_getDataSizeCallBack(dataindex));


            LastCacheCell.dataIndex = dataindex;
            LastCell = LastCacheCell;
            cellDatas.Add(LastCacheCell);
            _updataCallBack?.Invoke(LastCell);
        }
       
    }
}

public class Cell
{
    //要生成的预制体
    public GameObject Cellprefab;
    //数据下标索引
    public int dataIndex;
    public Cell(GameObject cellPrefab, int index)
    {
        Cellprefab = cellPrefab;
        dataIndex = index;
    }
    public void SetSize(bool isX,int size)
    {
        if (isX)
            Cellprefab.GetComponent<UIWidget>().width = size;
        else
            Cellprefab.GetComponent<UIWidget>().height = size;
    }
    public bool IsSee()
    {
        return Cellprefab.GetComponent<UIWidget>().isVisible;
    }
}