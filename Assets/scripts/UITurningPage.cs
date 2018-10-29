using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class UITurningPage : MonoBehaviour
{
    #region Const
    private const string TplPage = "tplPage";
    private float FLIP_RATIO = 0.33f;   //Temp页侧边中点达到底边的FLIP_RATIO，松开就翻页，否则还原
    private int FLIP_DELTA_THRESHOLD = 2000;    //翻书速度阈值
    private int TBL_DELTA_COUNT = 5;    //delta表存储的元素最大个数
    private Vector2 TWEEN_TIME_SPAN = new Vector2(0.1f, 1);
    #endregion

    #region UI
    private RectTransform _bookRect = null; //uiTurningPage结点；书页
    private RectTransform CurrMask = null;  //Curr遮罩
    private RectTransform CurrPage = null;  //Curr页
    private RectTransform NextPage = null;  //Next页
    private RectTransform TempPage = null;  //Temp页
    private UIPageItem _tplPage = null; //UIPageItem模板
    private UIPageItem CurrPageItem = null;
    private UIPageItem NextPageItem = null;
    private UIPageItem TempPageItem = null;

    private RectTransform _tranTween = null;
    private RectTransform _shadow = null;   //阴影
    private RectTransform _shadowParent = null; //阴影父节点
    private Text _txtPageNum = null;    //页码
    private Button _btnBF = null;   //播放按钮
    private Button _btnZT = null;   //暂停按钮
    private Button _btnBack = null; //返回按钮
    #endregion

    #region Data
    private FlipMode _flipMode = FlipMode.Next;    //当前翻书模式

    private Vector2 _pointLB = Vector2.zero;    //左下角
    private Vector2 _pointRB = Vector2.zero;    //右下角
    private Vector2 _pointLT = Vector2.zero;    //左上角
    private Vector2 _pointRT = Vector2.zero;    //右上角
    private Vector2 _pointST = Vector2.zero;    //书脊顶坐标
    private Vector2 _pointSB = Vector2.zero;    //书脊底坐标

    private Vector2 _pointTouch = Vector2.zero; //Touch点
    private Vector2 _pointProjection = Vector2.zero;    //投影点
    private Vector2 _pointTweenTarget = Vector2.zero;   //Tween的目标点
    private Vector2 _pointBezier = Vector2.zero;    //贝塞尔点
    private Vector2 _pointTmp = Vector2.zero;   //_pointProjection的对称点
    private Vector2 _pointPivotTemp = Vector2.zero; //Temp页轴点
    private Vector2 _pointPivotMask = Vector2.zero; //Mask页轴点
    private Vector2 _pointT1 = Vector2.zero;
    private Vector2 _pointT2 = Vector2.zero;
    private Vector2 _pointCenter = Vector2.zero;    //当前书页左右两边的中点
    private Vector2 _pointTempCorner = Vector2.zero;    //临时页角点
    private Vector3 _maskPos = Vector3.zero;    //遮罩位置
    private Quaternion _maskQuarternion = Quaternion.identity;  //遮罩旋转
    private Vector3 _tempPos = Vector3.zero;
    private Quaternion _tempQuarternion = Quaternion.identity;

    private Vector2 _bookSize = Vector2.zero;

    internal static bool isLandScape = false;
    private Quaternion _rotQuaternion = Quaternion.identity;
    private Vector3 _globalZeroPos = Vector3.zero;

    private int _timeNum = 0;
    private bool _isReachRatio = false;

    private float _sx = 0;  //书脊x坐标
    private float _lx = 0;  //左边界x坐标
    private float _rx = 0;  //右边界x坐标
    private float _ty = 0;  //上边界y坐标
    private float _by = 0;  //下边界y坐标
    private float _radius1 = 0;
    private float _radius2 = 0;

    private List<DeltaTime> _deltaList = null;

    private bool _canDrag = false;

    [SerializeField]
    private bool IsAutoPlay = false;    //是否自动播放
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
        CurrMask.pivot = new Vector2(1, 0.5f);

        _tplPage = _bookRect.Find(TplPage).gameObject.AddComponent<UIPageItem>(); 
        _tplPage.gameObject.SetActive(false);

        CurrPage = SetPage("curr", out CurrPageItem);
        NextPage = SetPage("next", out NextPageItem);
        TempPage = SetPage("temp", out TempPageItem);
        ActiveGOSome(false);
        ActiveGOTemp(false);

        _tranTween = _bookRect.Find("goTween").GetComponent<RectTransform>();
        _txtPageNum = transform.Find("Adapter/txtPageNum").GetComponent<Text>();
        _btnBF = transform.Find("Adapter/btn/btnBF").GetComponent<Button>();
        _btnBF.onClick.AddListener(OnClickBtnBF);
        _btnZT = transform.Find("Adapter/btn/btnZT").GetComponent<Button>();
        _btnZT.onClick.AddListener(OnClickBtnZT);
        _btnBack = transform.Find("Adapter/btnBack").GetComponent<Button>();
        _btnBack.onClick.AddListener(OnClickBtnBC);
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
        //Tween
        _tweenTime = 0.5f;  //需计算的Tween时间
        _isTweening = false;
        _isReachRatio = false;

        FlushBtnAutoPlay(IsAutoPlay);
        LoadTextures();
        Init();
    }

    private void Start()
    {
        SetCurPageNum(1);
    }
    private void Update()
    {
        Debug.DrawLine(Local2Global(_pointTouch), Local2Global(_pointST), Color.yellow);
        Debug.DrawLine(Local2Global(_pointTouch), Local2Global(_pointSB), Color.yellow);

        Debug.DrawLine(Local2Global(_pointProjection), Local2Global(_pointST), Color.magenta);
        Debug.DrawLine(Local2Global(_pointProjection), Local2Global(_pointSB), Color.cyan);

        Debug.DrawLine(Local2Global(_pointLB), Local2Global(_pointRB), Color.black, 0, false);
        Debug.DrawLine(Local2Global(_pointLT), Local2Global(_pointRT), Color.black, 0, false);

        Debug.DrawLine(Local2Global(_pointT2), Local2Global(_pointT1), Color.red);
        Debug.DrawLine(Local2Global(_pointPivotMask), Local2Global(_pointTmp), Color.blue);
        Debug.DrawLine(Local2Global(_pointPivotMask), Local2Global(_pointProjection), Color.green);
        Debug.DrawLine(Local2Global(_pointTmp), Local2Global(_pointBezier), Color.white);
        Debug.DrawLine(Local2Global(_pointProjection), Local2Global(_pointBezier), Color.white);
    }
    #endregion

    private void Init()
    {
        //初始化书页各点
        float halfWidth = _bookSize.x / 2;
        _sx = 0;
        _lx = _sx - halfWidth;
        _rx = _sx + halfWidth;
        float halfHeight = _bookSize.y / 2;
        _ty = halfHeight;
        _by = -halfHeight;
        _pointLB = new Vector2(_lx, _by);
        _pointRB = new Vector2(_rx, _by);
        _pointLT = new Vector2(_lx, _ty);
        _pointRT = new Vector2(_rx, _ty);
        _pointST = new Vector2(_sx, _ty);
        _pointSB = new Vector2(_sx, _by);
        //将Mask设置为边长=书页对角线长度的2倍的正方形
        float diagonal = Vector2.Distance(_pointLT, _pointRB);  //书页对角线
        Vector2 size = new Vector2(2 * diagonal, 2 * diagonal);
        CurrMask.sizeDelta = size;
        //翻书时的阴影效果
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
    {//加载每页的纹理
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

    private RectTransform SetPage(string pageName, out UIPageItem pageItem)
    {
        RectTransform rectPage = _bookRect.Find(pageName).GetComponent<RectTransform>();
        pageItem = Instantiate(_tplPage, rectPage.transform);
        pageItem.gameObject.SetActive(true);
        pageItem.name = TplPage;
        return rectPage;
    }
    private void ActiveGOSome(bool isActive)
    {
        CurrMask.gameObject.SetActive(isActive);
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
        if (_flipMode == FlipMode.Next)
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
    private void UpdateCurrPageData()
    {//更新页面图片;
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
    
    private Vector3 Local2Global(Vector3 localVal)
    {//将本地坐标转换为世界坐标
        return _bookRect.TransformPoint(localVal);
    }
    private Vector2 Screen2Local(Vector2 screenPos, Camera cam)
    {//将屏幕坐标映射到transform的本地坐标; screen范围是：左下角[0, 0]，右上角[分辨率宽, 分辨率高]
        Vector2 pos = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_bookRect, screenPos, cam, out pos);
        return pos;
    }
    private bool IsFromUp()
    {//是否从上部开始拖动
        return _pointProjection.y > 0;
    }
    private bool IsFromRight()
    {//是否从右部开始拖动
        return _pointProjection.x > 0;
    }
    private Vector2 CurC()
    {//当前角点
        if (_flipMode == FlipMode.Next)
        {
            if (IsFromUp())
            {
                return _pointRT;
            }
            else
            {
                return _pointRB;
            }
        }
        else
        {
            if (IsFromUp())
            {
                return _pointLT;
            }
            else
            {
                return _pointLB;
            }
        }
    }
    private Vector2 CurS()
    {//当前书脊点
        if (IsFromUp())
        {
            return _pointST;
        }
        else
        {
            return _pointSB;
        }
    }
    private Vector2 CurOtherS()
    {//当前书脊点
        if (IsFromUp())
        {
            return _pointSB;
        }
        else
        {
            return _pointST;
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
    private float CurOtherRadius()
    {
        if (IsFromUp())
        {
            return _radius1;
        }
        else
        {
            return _radius2;
        }
    }

    //private float NormalizeT1X(float t1X)
    //{
    //    Vector2 curS = CurS();
    //    float limitT1X = 0;
    //    if (IsFromUp())
    //    {
    //        limitT1X = _pointST.x - (_pointST.y - _pointSB.y) * (_pointST.x - _pointPivotMask.x) / (_pointPivotMask.y - _pointSB.y);
    //    }
    //    else
    //    {
    //        limitT1X = _pointSB.x + (_pointST.y - _pointSB.y) * (_pointPivotMask.x - _pointSB.x) / (_pointST.y - _pointPivotMask.y);
    //    }
    //    if (IsFromRight())
    //    {
    //        return Mathf.Min(Mathf.Max(t1X, curS.x), limitT1X);
    //    }
    //    else
    //    {
    //        return Mathf.Max(Mathf.Min(t1X, curS.x), limitT1X);
    //    }
    //}
    private Vector2 CalSymmetryPoint(Vector2 linePoint1, Vector2 linePoint2, Vector2 point)
    {//求point关于由linePoint1和linePoint2确定的直线的对称点;
        Vector3 vP1P2 = linePoint1 - linePoint2;
        Quaternion q = Quaternion.FromToRotation(vP1P2.normalized, Vector3.up);
        Quaternion p = Quaternion.FromToRotation(Vector3.up, vP1P2.normalized);
        Vector2 vPP1 = point - linePoint1;
        vPP1 = q * vPP1;
        vPP1.x = Vector3.up.x - vPP1.x;
        vPP1 = p * vPP1;
        vPP1 = vPP1 + linePoint1;
        return vPP1;
    }

    //private void Calc()
    //{
    //    Vector2 curS = CurS();
    //    Vector2 curC = CurC();
    //    float curRadius = CurRadius();
    //    float angleMask, angleTemp;
    //    //求MaskPivot
    //    //float dTS = Vector2.Distance(_pointTouch, curS);
    //    float sqrTS = (_pointTouch - curS).sqrMagnitude;    //替代Vector2.Distance()方法，以避免开方操作
    //    if (sqrTS <= curRadius * curRadius)
    //    {
    //        _pointTmp = _pointTouch;
    //    }
    //    else
    //    {
    //        Vector2 vST = _pointTouch - curS;
    //        _pointTmp = vST.normalized * curRadius + curS;  //书脊端点curS到Touch点的向量，其单位向量乘以curRadius，就是curS指向_pointTmp点的向量，加上curS就是对curS点做平移，得到的点就是_pointTmp点
    //    }
    //    _pointPivotMask = (_pointTmp + _pointProjection) / 2;
    //    Vector2 vT0P = _pointProjection - _pointPivotMask;
    //    float anglePT0H = Mathf.Atan2(vT0P.y, vT0P.x);
    //    float xT1 = _pointPivotMask.x + (_pointPivotMask.y - curC.y) * Mathf.Tan(anglePT0H);
    //    xT1 = NormalizeT1X(xT1);
    //    float xT2 = xT1 + (curS.y + curC.y) * Mathf.Tan(anglePT0H);
    //    if (IsFromRight())
    //    {
    //        xT2 = Mathf.Max(xT2, curS.x);
    //    }
    //    else
    //    {
    //        xT2 = Mathf.Min(xT2, curS.x);
    //    }
    //    _pointT1 = new Vector2(xT1, curS.y);
    //    _pointT2 = new Vector2(xT2, -curS.y);
    //    //求Mask角
    //    Vector2 vT0T2 = _pointT2 - _pointPivotMask;
    //    angleMask = Mathf.Atan2(vT0T2.y, vT0T2.x);
    //    if (IsFromUp())
    //    {
    //        angleMask = angleMask + Mathf.PI / 2;
    //    }
    //    else
    //    {
    //        angleMask = angleMask - Mathf.PI / 2;
    //    }
    //    if (!IsFromRight())
    //    {
    //        angleMask = angleMask + Mathf.PI;
    //    }
    //    angleMask = angleMask * Mathf.Rad2Deg;  //弧度变角度
    //    //求TempPivot
    //    _pointPivotTemp = CalSymmetryPoint(_pointPivotMask, _pointT2, _pointCenter);   // Temp的pivot点坐标;
    //    _pointTempCorner = CalSymmetryPoint(_pointPivotMask, _pointT2, curC);   // Temp页距书脊中点最近的角点坐标;
    //    //求Temp角
    //    Vector2 vPTC = _pointPivotTemp - _pointTempCorner;
    //    angleTemp = Mathf.Atan2(vPTC.y, vPTC.x);
    //    if (IsFromUp())
    //    {
    //        angleTemp = angleTemp + Mathf.PI / 2;
    //    }
    //    else
    //    {
    //        angleTemp = angleTemp - Mathf.PI / 2;
    //    }
    //    angleTemp = angleTemp * Mathf.Rad2Deg;
    //    //result
    //    _maskPos = Local2Global(_pointPivotMask);   //Mask position
    //    _maskQuarternion = Quaternion.Euler(0, 0, angleMask) * _rotQuaternion;  //Mask旋转角
    //    _tempPos = Local2Global(_pointPivotTemp);   //Temp position
    //    _tempQuarternion = Quaternion.Euler(0, 0, angleTemp) * _rotQuaternion;  //Temp旋转角
    //}
    private void Calc()
    {
        //求_pointTmp
        Vector2 curS1 = CurS();
        Vector2 curS2 = CurOtherS();
        float curR1 = CurRadius();
        float curR2 = CurOtherRadius();
        float sqrTS1 = (_pointTouch - curS1).sqrMagnitude;    //替代Vector2.Distance()方法，以避免开方操作
        float sqrTS2 = (_pointTouch - curS2).sqrMagnitude;
        bool b1 = sqrTS1 <= curR1 * curR1;
        bool b2 = sqrTS2 <= curR2 * curR2;

        if (b1 && b2)
        {
            _pointTmp = _pointTouch;
        }
        //else if (!b1 && b2)
        else if (b2 && !b1)
        {
            _pointTmp = (_pointTouch - curS1).normalized * curR1 + curS1; //书脊端点curS到Touch点的向量，其单位向量乘以curRadius，就是curS指向_pointTmp点的向量，加上curS就是对curS点做平移，得到的点就是_pointTmp点
        }
        else if (b1 && !b2)
        {
            _pointTmp = (_pointTouch - curS2).normalized * curR2 + curS2;
        }
        else
        {
            Debug.LogWarning(string.Format("### {0}{1}", b1, b2));
            //if (Mathf.Abs(_pointTouch.y) > Mathf.Abs(_pointProjection.y))
            //{
            //    _pointTmp = (_pointTouch - curS2).normalized * curR2 + curS2;
            //}
            //else
            //{
            //    _pointTmp = (_pointTouch - curS1).normalized * curR1 + curS1;
            //}
        }


        //if (Mathf.Abs(_pointTouch.y) > Mathf.Abs(_pointProjection.y))
        //{
        //    if(b1)
        //    {
        //        _pointTmp = _pointTouch;
        //    }
        //    else
        //    {
        //        _pointTmp = (_pointTouch - curS1).normalized * curR1 + curS1;
        //    }
        //}
        //else
        //{
        //    if (b2)
        //    {
        //        _pointTmp = _pointTouch;
        //    }
        //    else
        //    {
        //        _pointTmp = (_pointTouch - curS2).normalized * curR2 + curS2;
        //    }
        //}


        //求MaskPivot
        _pointPivotMask = (_pointTmp + _pointProjection) / 2;
        //据_pointProjection和_pointTmp之间的向量，与_pointBezier和_pointPivotMask之间的向量，互相垂直，故点积为0，来求_pointBezier的x坐标，_pointBezier的y坐标即为_pointProjection的y坐标
        Vector2 vTP = _pointProjection - _pointTmp;
        Vector2 pointBezier = new Vector2(_pointPivotMask.x - (_pointProjection.y - _pointPivotMask.y) * vTP.y / vTP.x, _pointProjection.y);
        //求TempPivot
        _pointPivotTemp = CalSymmetryPoint(_pointPivotMask, pointBezier, _pointCenter);   //Temp的pivot点坐标;
        //求角度
        Vector2 vTB = IsFromRight() ? pointBezier - _pointTmp : _pointTmp - pointBezier;
        Vector2 vMP = _pointProjection - _pointPivotMask;
        _maskPos = Local2Global(_pointPivotMask);   //Mask position
        _maskQuarternion = Quaternion.FromToRotation(Vector2.right, vMP) * _rotQuaternion;  //Mask旋转角
        _tempPos = Local2Global(_pointPivotTemp);   //Temp position
        _tempQuarternion = Quaternion.FromToRotation(Vector2.right, vTB) * _rotQuaternion;  //Temp旋转角
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
        SetPositionAndRotation(_shadow, _maskPos, _maskQuarternion);    //shadow
        SetPositionAndRotation(CurrPage, _globalZeroPos, _rotQuaternion);
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
            Vector3[] v3Arr = new Vector3[count+1];
            for (int i = 0; i <= count; i++)
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
        CurrPage.SetParent(_bookRect, true);
        TempPage.SetParent(_bookRect, true);
        TempPage.transform.localRotation = Quaternion.identity;
        ActiveGOSome(false);
        ActiveGOTemp(false);

        bool isBack = _pointTweenTarget.x == _pointProjection.x;    //是否未翻页，而Tween回原样
        if (!isBack)
        {
            if (_flipMode == FlipMode.Next)
            {
                SetCurPageNum(_curPageNum + 1);
            }
            else
            {
                SetCurPageNum(_curPageNum - 1);
            }
        }
        _isTweening = false;
        StartCoroutine(CoAutoPlay());
    }

    private void InitPointProjection()
    {//根据touch点初始化投影点
        // 刚开始拖动时拖拽点在左右边界上的投影点，只在开始拖动时计算一次; 用于计算radius1;
        _pointProjection.x = _pointTouch.x;
        _pointProjection.y = _pointTouch.y;
        if (IsFromRight())
        {
            _pointProjection.x = _rx;
            _flipMode = FlipMode.Next;
        }
        else
        {
            _pointProjection.x = _lx;
            _flipMode = FlipMode.Prev;
        }
    }
    private void BeginDragInit()
    {
        _pointCenter = new Vector2(CurC().x, (_ty + _by) / 2);
        _radius1 = Vector2.Distance(_pointProjection, _pointSB);
        _radius2 = Vector2.Distance(_pointProjection, _pointST);
        UpdateNextPageData();   //不能删，否则会出现往回翻时Temp页显示的是下一页的内容，而非上一页的内容
        CurrPage.SetParent(CurrMask, true);
        TempPage.SetParent(CurrMask, true);
        _shadowParent.SetAsLastSibling();
        if (_flipMode == FlipMode.Next)
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

    private bool IsPointerIdValid(int pointerId)
    {//触控点是否合法，用于过滤多点触控；pointerId - 0第一个触控点，1第二个触控点...;-1鼠标左键-2鼠标右键-3鼠标中键
        return pointerId == 0 || pointerId == -1;
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
        {//正在做Tween时不允许操作
            _canDrag = false;
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
        InitPointProjection();
        //判断往左划还是往右划
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
        {//到最后一页，不允许往下翻
            _canDrag = false;
            return;
        }
        if (isFirstPage && !isNext)
        {//到第一页，不允许往上翻
            _canDrag = false;
            return;
        }
        if (isFromRight && !isNext)
        {//从右半页，不允许往下翻
            _canDrag = false;
            return;
        }
        if (!isFromRight && isNext)
        {//从左半页，不允许往上翻
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

    #region AutoPlay
    private void AutoFlip()
    {//自动翻页只有翻下一页的情况
        _pointTouch.x = _pointRT.x;
        _pointTouch.y = (_ty + _by) / 2;
        InitPointProjection();
        BeginDragInit();
        _tweenTime = 1;
        _pointTweenTarget.x = -_pointProjection.x;  //左边界或上边界的中点
        _pointTweenTarget.y = _pointProjection.y;
        _pointBezier.x = _sx;   //贝塞尔点是书脊顶点
        _pointBezier.y = _ty;
        _tranTween.localPosition = new Vector3(_pointProjection.x, _pointProjection.y, 0);
        SetIsTweening(true);
    }
    private void OnClickBtnBF()
    {
        FlushBtnAutoPlay(true);
    }
    private void OnClickBtnZT()
    {
        FlushBtnAutoPlay(false);
    }
    private void FlushBtnAutoPlay(bool isAutoPlay)
    {
        IsAutoPlay = isAutoPlay;
        if (IsAutoPlay)
        {
            StartCoroutine(CoAutoPlay());
        }
        else
        {
            StopCoroutine(CoAutoPlay());
        }
        _btnBF.gameObject.SetActive(!IsAutoPlay);
        _btnZT.gameObject.SetActive(IsAutoPlay);
    }
    private IEnumerator CoAutoPlay()
    {
        if(IsLastPage())
        {
            FlushBtnAutoPlay(false);
            yield break;
        }
        if (IsAutoPlay)
        {
            yield return new WaitForSeconds(2);
            AutoFlip();
        }
    }
    #endregion

    private void OnClickBtnBC()
    {
        SceneManager.UnloadSceneAsync("Main");
        SceneManager.LoadScene("Start");
    }
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