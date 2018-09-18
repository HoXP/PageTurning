﻿using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

class UITurningPage : MonoBehaviour
{
    #region Const
    private const string lclTplPage = "tplPage";
    private float FLIP_RATIO = 0.33f;
    private int FLIP_DELTA_THRESHOLD = 2000;    //翻书速度阈值
    private int TBL_DELTA_COUNT = 5;    //delta表存储的元素最大个数
    private Vector2 TWEEN_TIME_SPAN = new Vector2(0.1f, 1);
    #endregion

    #region UI
    private RectTransform _bookRect = null;
    private RectTransform CurrMask = null;
    private RectTransform NextMask = null;

    private UIPageItem _tplPage = null;
    private RectTransform CurrPage = null;
    private RectTransform NextPage = null;
    private RectTransform TempPage = null;
    private UIPageItem CurrPageItem = null;
    private UIPageItem NextPageItem = null;
    private UIPageItem TempPageItem = null;

    private RectTransform _tranTween = null;
    private RectTransform _shadow = null;
    private RectTransform _shadowParent = null;
    private Text _txtPageNum = null;
    #endregion

    #region Data
    private FlipMode lclFlipMode = FlipMode.Next;    //当前翻书模式

    private Vector2 lclPointElb = Vector2.zero;
    private Vector2 lclPointErb = Vector2.zero;
    private Vector2 lclPointElt = Vector2.zero;
    private Vector2 lclPointErt = Vector2.zero;
    private Vector2 lclPointSt = Vector2.zero;
    private Vector2 lclPointSb = Vector2.zero;

    private Vector2 _pointTouch = Vector2.zero;
    private Vector2 _pointProjection = Vector2.zero;
    private Vector2 _pointTweenTarget = Vector2.zero;
    private Vector2 _pointBezier = Vector2.zero;
    private Vector2 _pointTmp = Vector2.zero;
    private Vector2 _pointPivotTemp = Vector2.zero;
    private Vector2 _pointPivotMask = Vector2.zero;
    private Vector2 _pointT1 = Vector2.zero;
    private Vector2 _pointT2 = Vector2.zero;
    private Vector2 _pointECenter = Vector2.zero;
    private Vector2 _pointTempCorner = Vector2.zero;
    private Vector3 _maskPos = Vector3.zero;
    private Quaternion _maskQuarternion = Quaternion.identity;
    private Vector3 _tempPos = Vector3.zero;
    private Quaternion _tempQuarternion = Quaternion.identity;

    private Vector2 _bookSize = Vector2.zero;

    [SerializeField]
    private bool isLandScape = false;
    private Quaternion _rotQuaternion = Quaternion.identity;
    private Vector3 _globalZeroPos = Vector3.zero;

    private int _timeNum = 0;
    private bool _isReachRatio = false;

    private float _sx = 0;
    private float _lx = 0;
    private float _rx = 0;
    private float _ty = 0;
    private float _by = 0;
    private float _radius1 = 0;
    private float _radius2 = 0;

    private List<DeltaTime> _deltaList = null;

    private bool _canDrag = false;
    #endregion

    #region Tween
    private Tweener _tweener = null;
    private bool _isTweening = false;
    private float _tweenTime = 0.5f;    //需计算的Tween时间
    #endregion

    #region PageData
    private int _curPageNum = 0; //当前页码
    private int _maxPageNum = 0; //最大页码
    private Texture[] _texList = null;  //纹理资源列表
    #endregion

    #region Sys
    private void Awake()
    {
        _bookRect = transform.Find("Adapter/uiTurningPage").GetComponent<RectTransform>();
        #region EventTrigger
        EventTrigger et = _bookRect.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = null;
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.BeginDrag;
        entry.callback.AddListener(OnBeginDragBook);
        et.triggers.Add(entry);
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Drag;
        entry.callback.AddListener(OnDragBook);
        et.triggers.Add(entry);
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.EndDrag;
        entry.callback.AddListener(OnEndDragBook);
        et.triggers.Add(entry);
        #endregion

        CurrMask = _bookRect.Find("maskC").GetComponent<RectTransform>();
        NextMask = _bookRect.Find("maskN").GetComponent<RectTransform>();
        CurrMask.pivot = new Vector2(1, 0.5f);
        NextMask.pivot = new Vector2(0, 0.5f);

        _tplPage = _bookRect.Find(lclTplPage).gameObject.AddComponent<UIPageItem>(); ;
        _tplPage.gameObject.SetActive(false);

        CurrPage = SetPage("curr", PageType.Curr, out CurrPageItem);
        NextPage = SetPage("next", PageType.Next, out NextPageItem);
        TempPage = SetPage("temp", PageType.Temp, out TempPageItem);
        ActiveGOSome(false);
        ActiveGOTemp(false);

        _tranTween = _bookRect.Find("goTween").GetComponent<RectTransform>();
        _txtPageNum = transform.Find("Adapter/txtPageNum").GetComponent<Text>();

        //Data
        if (isLandScape)
        {
            AdapterUtils.Instance.SetHorizontal(transform);
            _rotQuaternion = Quaternion.identity * Quaternion.Euler(0, 0, -90);
        }
        else
        {
            AdapterUtils.Instance.SetVertical(transform);
            _rotQuaternion = Quaternion.identity;
        }
        _bookSize = _bookRect.rect.size;
        _globalZeroPos = Local2Global(Vector3.zero);
        _timeNum = 0;
        // Tween
        _tweenTime = 0.5f;  //需计算的Tween时间
        _isTweening = false;
        _isReachRatio = false;

        LoadTextures();
        Init();
    }
    private void Start()
    {
        SetCurPageNum(1);
    }
    private void Update()
    {
        Debug.DrawLine(Local2Global(lclPointElb), Local2Global(lclPointErb), Color.black, 0, false);
        Debug.DrawLine(Local2Global(lclPointElt), Local2Global(lclPointErt), Color.black, 0, false);
        Debug.DrawLine(Local2Global(lclPointSt), Local2Global(lclPointSb), Color.black, 0, false);

        Debug.DrawLine(Local2Global(_pointT2), Local2Global(_pointT1), Color.red);
        Debug.DrawLine(Local2Global(_pointPivotMask), Local2Global(_pointTmp), Color.blue);
        Debug.DrawLine(Local2Global(_pointPivotMask), Local2Global(_pointProjection), Color.green);
        Debug.DrawLine(Local2Global(_pointTmp), Local2Global(_pointBezier), Color.white);
        Debug.DrawLine(Local2Global(_pointProjection), Local2Global(_pointBezier), Color.white);
    }
    #endregion

    private void Init()
    {
        Canvas cvs = transform.GetComponentInParent<Canvas>();
        cvs = cvs.rootCanvas;
        Vector2 sizeData = cvs.GetComponent<RectTransform>().sizeDelta;
        float halfWidth = _bookSize.x / 2;
        //书脊在中间
        _sx = 0;
        _lx = _sx - halfWidth;
        _rx = _sx + halfWidth;
        //书脊在左边
        // if isLandScape then
        //     _sx = 0
        //     _lx = _sx - halfWidth
        //     _rx = _sx + halfWidth
        // else
        //_sx = -halfWidth
        //     _lx = -halfWidth
        //     _rx = halfWidth
        // end

        float halfHeight = _bookSize.y / 2;
        _ty = halfHeight;
        _by = -halfHeight;
        //
        lclPointElb = new Vector2(_lx, _by);    //_bookRect左下顶点;
        lclPointErb = new Vector2(_rx, _by);    //_bookRect右下顶点;
        lclPointElt = new Vector2(_lx, _ty);    //_bookRect左上顶点;
        lclPointErt = new Vector2(_rx, _ty);    //_bookRect右上顶点;
        lclPointSt = new Vector2(_sx, _ty); //书脊上顶点;
        lclPointSb = new Vector2(_sx, _by); //书脊下顶点;
        //设置Mask大小
        float diagonal = Vector2.Distance(lclPointElt, lclPointErb);
        Vector2 size = new Vector2(2 * diagonal, 2 * diagonal);
        CurrMask.sizeDelta = size;
        NextMask.sizeDelta = size;
        //shadow
        _shadow = transform.Find("Adapter/mskShadow/imgShadow").GetComponent<RectTransform>();
        _shadowParent = _shadow.transform.parent.GetComponent<RectTransform>();
        _shadowParent.transform.SetParent(TempPage, true);
        _shadowParent.anchoredPosition = Vector2.zero;
        _shadowParent.sizeDelta = Vector2.zero;
        _shadowParent.transform.localScale = Vector3.one;
        _shadowParent.transform.localRotation = Quaternion.identity;
        _shadow.sizeDelta = new Vector2(_shadow.sizeDelta.x, size.y);
    }
    private void LoadTextures()
    {
        string path = "Textures/";
        if(isLandScape)
        {
            path = string.Format("{0}H", path);
        }
        else
        {
            path = string.Format("{0}V", path);
        }
        _texList = Resources.LoadAll<Texture>(path);
        if(_texList == null)
        {
            Debug.LogError("加载资源失败");
            return;
        }
        _curPageNum = 1;
        _maxPageNum = _texList.Length;
    }

    private RectTransform SetPage(string pageName, PageType pageType, out UIPageItem pageItem)
    {
        RectTransform rectPage = _bookRect.Find(pageName).GetComponent<RectTransform>();
        pageItem = GameObject.Instantiate<UIPageItem>(_tplPage, rectPage.transform);
        pageItem.gameObject.SetActive(true);
        pageItem.name = lclTplPage;
        return rectPage;
    }
    private void ActiveGOSome(bool isActive)
    {
        CurrMask.gameObject.SetActive(isActive);
        NextMask.gameObject.SetActive(isActive);
        NextPage.gameObject.SetActive(isActive);
    }
    private void ActiveGOTemp(bool isActive)
    {
        TempPage.gameObject.SetActive(isActive);
    }

    private void FlushPage(UIPageItem item, int pNum)
    {
        if (0 < pNum && pNum <= _maxPageNum)    //MaxPageNum是最后一页，所以pNum < MaxPageNum条件不能刷新最后一页，需要 <=
        {
            item.Flush(_texList[pNum - 1]);
        }
    }
    private void UpdateNextPageData()
    {
        int nextNum = _curPageNum;
        if (lclFlipMode == FlipMode.Next)
        {
            nextNum = nextNum + 1;
        }
        else
        {
            nextNum = nextNum - 1;
        }
        FlushPage(NextPageItem, nextNum);
        FlushPage(TempPageItem, nextNum);
    }
    private void UpdateCurrPageData()   //更新页面数据，比如页面图片;//CurrPage 总是保持当前图片，且总是在NextPage之上;
    {
        FlushPage(CurrPageItem, _curPageNum);
    }

    private void SetCurPageNum(int curPageNum)
    {
        if (curPageNum < 1 || curPageNum > _maxPageNum)
        {
            return;
        }
        _curPageNum = curPageNum;
        _txtPageNum.text = string.Format("{0}/{1}", _curPageNum, _maxPageNum);
        UpdateCurrPageData();
    }

    private Vector3 v3Local2Global = Vector3.zero;
    private Vector3 Local2Global(Vector3 localVal)
    {
        v3Local2Global.x = localVal.x;
        v3Local2Global.y = localVal.y;
        Vector3 ret = _bookRect.TransformPoint(v3Local2Global);
        return ret;
    }
    private Vector2 Screen2Local(Vector2 screenPos, Camera cam)    //将屏幕坐标映射到transform的本地坐标; screen范围是：左下角[0, 0]，右上角[分辨率宽, 分辨率高]
    {
        Vector2 pos = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_bookRect, screenPos, cam, out pos);
        return pos;
    }
    private bool IsFromUp()   //是否从上部开始拖动
    {
        return _pointProjection.y > 0;
    }
    private bool IsFromRight()    //是否从右部开始拖动
    {
        return _pointProjection.x > 0;
    }
    private Vector2 CurE()
    {
        if (lclFlipMode == FlipMode.Next)
        {
            if (IsFromUp())
            {
                return lclPointErt;
            }
            else
            {
                return lclPointErb;
            }
        }
        else
        {
            if (IsFromUp())
            {
                return lclPointElt;
            }
            else
            {
                return lclPointElb;
            }
        }
    }
    private Vector2 CurS()
    {
        if (IsFromUp())
        {
            return lclPointSt;
        }
        else
        {
            return lclPointSb;
        }
    }
    private float CurRadius()
    {
        if (IsFromUp())
        {
            return _radius2;
        }
        else
        {
            return _radius1;
        }
    }

    private float NormalizeT1X(float t1X)
    {
        Vector2 curS = CurS();
        float limitT1X = 0;
        if (IsFromUp())
        {
            limitT1X = lclPointSt.x - (lclPointSt.y - lclPointSb.y) * (lclPointSt.x - _pointPivotMask.x) / (_pointPivotMask.y - lclPointSb.y);
        }
        else
        {
            limitT1X = lclPointSb.x + (lclPointSt.y - lclPointSb.y) * (_pointPivotMask.x - lclPointSb.x) / (lclPointSt.y - _pointPivotMask.y);
        }
        if (IsFromRight())
        {
            return Mathf.Min(Mathf.Max(t1X, curS.x), limitT1X);
        }
        else
        {
            return Mathf.Max(Mathf.Min(t1X, curS.x), limitT1X);
        }
    }
    private Vector3 vP1P2 = Vector3.zero;
    private Vector3 vY = new Vector3(0, 1, 0);
    private Vector3 vPP1 = Vector3.zero;
    private Vector2 v2CalSymmetryPoint = Vector2.zero;
    private Vector2 CalSymmetryPoint(Vector2 linePoint1, Vector2 linePoint2, Vector2 point) //求point关于由linePoint1和linePoint2确定的直线的对称点;
    {
        vP1P2.x = linePoint1.x - linePoint2.x;
        vP1P2.y = linePoint1.y - linePoint2.y;
        Quaternion q = Quaternion.FromToRotation(vP1P2.normalized, vY);
        Quaternion p = Quaternion.FromToRotation(vY, vP1P2.normalized);
        vPP1.x = point.x - linePoint1.x;
        vPP1.y = point.y - linePoint1.y;
        vPP1 = q * vPP1;
        vPP1.x = vY.x - vPP1.x;
        vPP1 = p * vPP1;
        v2CalSymmetryPoint.x = vPP1.x;
        v2CalSymmetryPoint.y = vPP1.y;
        v2CalSymmetryPoint = v2CalSymmetryPoint + linePoint1;
        return v2CalSymmetryPoint;
    }

    private void Calc()
    {
        Vector2 curS = CurS();
        Vector2 curE = CurE();
        float curRadius = CurRadius();
        //
        float angleMask, angleTemp;
        //求MaskPivot
        Vector2 vST = _pointTouch - curS;   //点S到点T的向量
        float angleTSH = Mathf.Atan2(vST.y, vST.x); //根据向量用反正切算出该向量与右向量之间的夹角;
        Vector2 pointR1 = new Vector2(curRadius * Mathf.Cos(angleTSH), curRadius * Mathf.Sin(angleTSH)) + curS;
        float dTS = Vector2.Distance(_pointTouch, curS);
        if (dTS < curRadius)
        {
            _pointTmp.x = _pointTouch.x;
            _pointTmp.y = _pointTouch.y;
        }
        else
        {
            _pointTmp.x = pointR1.x;
            _pointTmp.y = pointR1.y;
        }
        _pointPivotMask = (_pointTmp + _pointProjection) / 2;
        Vector2 vT0P = _pointProjection - _pointPivotMask;
        float anglePT0H = Mathf.Atan2(vT0P.y, vT0P.x);

        float xT1 = _pointPivotMask.x + (_pointPivotMask.y - curE.y) * Mathf.Tan(anglePT0H);
        xT1 = NormalizeT1X(xT1);
        float xT2 = xT1 + (curS.y + curE.y) * Mathf.Tan(anglePT0H);
        if (IsFromRight())
        {
            xT2 = Mathf.Max(xT2, curS.x);
        }
        else
        {
            xT2 = Mathf.Min(xT2, curS.x);
        }
        _pointT1 = new Vector2(xT1, curS.y);
        _pointT2 = new Vector2(xT2, -curS.y);
        //求Mask角
        Vector2 vT0T2 = _pointT2 - _pointPivotMask;
        angleMask = Mathf.Atan2(vT0T2.y, vT0T2.x);
        if (IsFromUp())
        {
            angleMask = angleMask + Mathf.PI / 2;
        }
        else
        {
            angleMask = angleMask - Mathf.PI / 2;
        }
        if (!IsFromRight())
        {
            angleMask = angleMask + Mathf.PI;
        }
        angleMask = angleMask * Mathf.Rad2Deg;  //弧度变角度
        //求TempPivot
        _pointPivotTemp = CalSymmetryPoint(_pointPivotMask, _pointT2, _pointECenter);   // Temp的pivot点坐标;
        _pointTempCorner = CalSymmetryPoint(_pointPivotMask, _pointT2, curE);   // Temp页距书脊中点最近的角点坐标;
        //求Temp角
        Vector2 vPTC = _pointPivotTemp - _pointTempCorner;
        angleTemp = Mathf.Atan2(vPTC.y, vPTC.x);
        if (IsFromUp())
        {
            angleTemp = angleTemp + Mathf.PI / 2;
        }
        else
        {
            angleTemp = angleTemp - Mathf.PI / 2;
        }
        angleTemp = angleTemp * Mathf.Rad2Deg;
        //result
        _maskPos = Local2Global(_pointPivotMask);   //Mask position
        _maskQuarternion = Quaternion.Euler(0, 0, angleMask) * _rotQuaternion;  //Mask旋转角
        _tempPos = Local2Global(_pointPivotTemp);   //Temp position
        _tempQuarternion = Quaternion.Euler(0, 0, angleTemp) * _rotQuaternion;  //Temp旋转角
    }

    private void SetPositionAndRotation(Transform trans, Vector3 v3Pos, Quaternion qtnRot)
    {
        if (trans == null)
        {
            return;
        }
        trans.SetPositionAndRotation(v3Pos, qtnRot);
    }

    private void UpdateBookToPoint()
    {
        Calc();

        SetPositionAndRotation(CurrMask, _maskPos, _maskQuarternion);
        SetPositionAndRotation(NextMask, _maskPos, _maskQuarternion);
        SetPositionAndRotation(_shadow, _maskPos, _maskQuarternion);    //shadow

        SetPositionAndRotation(CurrPage, _globalZeroPos, _rotQuaternion);
        SetPositionAndRotation(NextPage, _globalZeroPos, _rotQuaternion);

        SetPositionAndRotation(TempPage, _tempPos, _tempQuarternion);

        _timeNum = _timeNum - 1;
        if (_timeNum == 0)
        {
            ActiveGOTemp(true);
        }
    }

    private Vector2 GetQuadraticBezierPoint(Vector2 pointA, Vector2 pointB, Vector2 pointC, float t, bool isLocal)  //获取二次贝塞尔点，t∈[0,1]
    {
        Vector2 P = (1 - t) * (1 - t) * pointA + 2 * (1 - t) * t * pointB + t * t * pointC;
        if (!isLocal)
        {
            P = Local2Global(P);
        }
        return P;
    }

    private void SetIsTweening(bool isTween)
    {
        _isTweening = isTween;
        if (_isTweening)
        {
            int count = 16;
            Vector3[] v3Arr = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                v3Arr[i] = GetQuadraticBezierPoint(_pointTouch, _pointBezier, _pointTweenTarget, i * 1.0f / count, false);
            }
            _tweener = _tranTween.DOPath(v3Arr, _tweenTime, PathType.Linear, PathMode.Full3D, 1, Color.green)
                .OnUpdate(delegate ()
                {
                    _pointTouch.x = _tranTween.localPosition.x;
                    _pointTouch.y = _tranTween.localPosition.y;
                    UpdateBookToPoint();
                })
                .OnComplete(delegate ()
                {
                    CompleteTween();
                })
                .OnKill(delegate ()
                {
                    _isReachRatio = false;
                });
        }
        else
        {
            if (_tweener != null)
            {
                _tweener.Kill(true);
            }
        }
    }
    private void CompleteTween()
    {
        NextPage.SetParent(_bookRect, true);
        CurrPage.SetParent(_bookRect, true);
        TempPage.SetParent(_bookRect, true);
        TempPage.transform.localRotation = Quaternion.identity;
        ActiveGOSome(false);
        ActiveGOTemp(false);

        bool isBack = _pointTweenTarget.x == _pointProjection.x;   //是否未翻页，而Tween回原样
        if (!isBack)
        {
            if (lclFlipMode == FlipMode.Next)
            {
                SetCurPageNum(_curPageNum + 1);
            }
            else
            {
                SetCurPageNum(_curPageNum - 1);
            }
        }
        _isTweening = false;
    }

    private void BeginDragInit()
    {
        _pointTouch.x = _pointProjection.x;
        _pointTouch.y = _pointProjection.y;
        if (IsFromRight())
        {
            _pointProjection.x = _rx;
            lclFlipMode = FlipMode.Next;
        }
        else
        {
            _pointProjection.x = _lx;
            lclFlipMode = FlipMode.Prev;
        }
        _pointECenter = new Vector2(CurE().x, (_ty + _by) / 2);
        _radius1 = Vector2.Distance(_pointProjection, lclPointSb);
        _radius2 = Vector2.Distance(_pointProjection, lclPointSt);
        UpdateNextPageData();   //不能删，否则会出现往回翻时Temp页显示的是下一页的内容，而非上一页的内容
        //开始拖动回调，用于初始化一些数据，比如更新图片;
        CurrPage.SetParent(CurrMask, true);
        NextPage.SetParent(NextMask, true);
        TempPage.SetParent(CurrMask, true);
        _shadowParent.SetAsLastSibling();
        if (lclFlipMode == FlipMode.Next)
        {
            TempPage.pivot = new Vector2(0, 0.5f);
        }
        else
        {
            TempPage.pivot = new Vector2(1, 0.5f);
        }
        ActiveGOSome(true);
        _timeNum = 2;
    }

    private bool IsPointerIdValid(int pointerId)    //触控点是否合法，用于过滤多点触控
    {
        return pointerId == 0 || pointerId == -1;   //pointerId - 0第一个触控点，1第二个触控点...;-1鼠标左键-2鼠标右键-3鼠标中键
    }

    private void UpdateTblDelta(float curPosX, string fromFlag)
    {
        if (_deltaList == null)
        {
            return;
        }
        DeltaTime tblDeltaTime = new DeltaTime() { pos = curPosX, time = Time.time, flag = fromFlag };
        if (_deltaList.Count < TBL_DELTA_COUNT)
        {
            _deltaList.Add(tblDeltaTime);
        }
        else
        {
            _deltaList.RemoveAt(0);
            _deltaList.Add(tblDeltaTime);
        }
    }
    private bool IsFirstPage()
    {
        return _curPageNum <= 1;
    }
    private bool IsLastPage()
    {
        return _curPageNum >= _maxPageNum;
    }

    #region Drag callback
    private void OnBeginDragBook(BaseEventData arg0)
    {
        _canDrag = true;
        if (_isTweening)
        {
            _canDrag = false;
            return;
        }
        SetIsTweening(false);
        if (arg0 == null)
        {
            return;
        }
        PointerEventData ped = arg0 as PointerEventData;
        if (!IsPointerIdValid(ped.pointerId))
        {
            return;
        }
        _pointProjection = Screen2Local(ped.position, ped.pressEventCamera);  // 刚开始拖动时拖拽点在左右边界上的投影点，只在开始拖动时计算一次; 用于计算radius1;
        bool isNext = false;
        if (isLandScape)
        {
            isNext = ped.delta.y > 0;
        }
        else
        {
            isNext = ped.delta.x < 0;
        }
        bool isLastPage = IsLastPage();
        bool isFirstPage = IsFirstPage();
        bool isFromRight = IsFromRight();
        if (isLastPage && isNext)
        {
            _canDrag = false;
            return;
        }
        if (isFirstPage && !isNext)
        {
            _canDrag = false;
            return;
        }
        if (isFromRight && !isNext)
        {
            _canDrag = false;
            return;
        }
        if (!isFromRight && isNext)
        {
            _canDrag = false;
            return;
        }
        BeginDragInit();
        if (_deltaList == null)
        {
            _deltaList = new List<DeltaTime>();
        }
        else
        {
            _deltaList.Clear();
        }
    }
    private void OnDragBook(BaseEventData arg0)
    {
        if (!_canDrag)
        {
            return;
        }
        if (arg0 == null)
        {
            return;
        }
        PointerEventData ped = arg0 as PointerEventData;
        if (!IsPointerIdValid(ped.pointerId))
        {
            return;
        }
        _pointTouch = Screen2Local(ped.position, ped.pressEventCamera);
        UpdateTblDelta(_pointTouch.x, "OnDragBook");
        UpdateBookToPoint();
    }
    private void OnEndDragBook(BaseEventData arg0)
    {
        if (!_canDrag)
        {
            return;
        }
        if (arg0 == null)
        {
            return;
        }
        PointerEventData ped = arg0 as PointerEventData;
        if (!IsPointerIdValid(ped.pointerId))
        {
            return;
        }
        if (_deltaList == null)
        {
            return;
        }
        _pointTouch = Screen2Local(ped.position, ped.pressEventCamera);
        UpdateTblDelta(_pointTouch.x, "OnEndDragBook");

        _pointTweenTarget.x = _pointProjection.x;
        _pointTweenTarget.y = _pointProjection.y;
        bool isFlipOver = false;
        int count = _deltaList.Count - 1;
        float speed = 0;
        int CALC_NUM = Mathf.Min(1, count - 1); //可取值1 至 count - 1
        float lastDelta, lastDeltaTime;
        lastDelta = _deltaList[count].pos - _deltaList[count - CALC_NUM].pos;
        lastDeltaTime = _deltaList[count].time - _deltaList[count - CALC_NUM].time;
        speed = lastDelta / lastDeltaTime;
        //根据速度和书页翻的程度做处理
        float absSpeed = Mathf.Abs(speed);
        if (absSpeed > FLIP_DELTA_THRESHOLD)//是否达到翻页速度的阈值。速度优先
        {
            if (IsFromRight())
            {
                isFlipOver = speed < 0;
            }
            else
            {
                isFlipOver = speed > 0;
            }
        }
        else
        {
            //求Temp的pivot是否到边界比例
            _isReachRatio = false;
            if (IsFromRight())
            {
                if (_pointPivotTemp.x < _sx + FLIP_RATIO / 2 * _bookSize.x)
                {
                    _isReachRatio = true;
                }
            }
            else
            {
                if (_pointPivotTemp.x > _sx - FLIP_RATIO / 2 * _bookSize.x)
                {
                    _isReachRatio = true;
                }
            }
            if (_isReachRatio)
            {
                isFlipOver = true;
            }
            else
            {
                isFlipOver = false;
            }
        }
        //根据是否翻成功计算贝塞尔点
        if (isFlipOver)
        {
            _pointTweenTarget.x = _sx - _pointTweenTarget.x;
            //将贝塞尔点设为另外两点的中点，这时构造出的曲线其实就是这个线段
            _pointBezier.x = (_pointTweenTarget.x + _pointTmp.x) / 2;
            _pointBezier.y = (_pointTweenTarget.y + _pointTmp.y) / 2;
        }
        else
        {
            //求Bezier点(运用相互垂直的两向量点积为0的原理求贝塞尔点的x坐标)
            _pointBezier.x = (_pointProjection.y - _pointPivotMask.y) * (_pointProjection.y - _pointTmp.y) / (_pointTmp.x - _pointProjection.x) + _pointPivotMask.x;
            _pointBezier.y = _pointProjection.y;
        }
        //求根据速度求Tween时间
        float s = Mathf.Abs(_pointTweenTarget.x - _pointTmp.x);
        float sRatio = s / _bookSize.x; //[0, 1]
        if (absSpeed > FLIP_DELTA_THRESHOLD)
        {
            _tweenTime = s / absSpeed * 5;  // * N矫正用
            if (_tweenTime > TWEEN_TIME_SPAN.y)
            {
                _tweenTime = TWEEN_TIME_SPAN.y;
            }
        }
        else
        {
            _tweenTime = 1;
        }
        _tweenTime = _tweenTime * sRatio;
        if (_tweenTime < TWEEN_TIME_SPAN.x)
        {
            _tweenTime = TWEEN_TIME_SPAN.x;
        }
        //tween go位置
        _tranTween.localPosition = new Vector3(_pointTmp.x, _pointTmp.y, 0);
        SetIsTweening(true);
    }
    #endregion

    private void AutoFlip()   //自动翻页只有翻下一页的情况
    {
        _pointProjection.x = lclPointErt.x;
        _pointProjection.y = (_ty + _by) / 2;
        BeginDragInit();
        _pointTweenTarget.x = -_pointProjection.x;  //左边界或上边界的中点
        _pointTweenTarget.y = _pointProjection.y;
        _pointBezier.x = _sx;   //贝塞尔点是书脊顶点
        _pointBezier.y = _ty;
        _tranTween.localPosition = new Vector3(_pointProjection.x, _pointProjection.y, 0);
        SetIsTweening(true);
    }
}

enum PageType
{//书页类型
    Curr = 0,
    Next = 1,
    Temp = 2
}
enum FlipMode
{//翻书模式枚举 - Next = 下一页, Prev = 上一页
    Next = 0,
    Prev = 1
}

class DeltaTime
{
    internal float pos = 0;
    internal float time = 0;
    internal string flag = string.Empty;
}