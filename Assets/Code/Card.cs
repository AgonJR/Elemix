using UnityEngine;

public class Card : MonoBehaviour
{
    [Header("Element")]
    [SerializeField] private ScriptableElement _elementData;
    [SerializeField] private string _elementName = "X";
    [SerializeField] private TextMesh _elementTxt;

    [Header("Movement")]
    [HideInInspector] 
    [SerializeField] private Vector3 _idlePosition;
    [SerializeField] private float _idleLerpSpeed = 1;
    [SerializeField] private float _mouseFollowSpeed = 3;

    [Space]
    [Header("References")]
    [Space]
    [SerializeField] private Animator _meshAnimator;
    [SerializeField] private Transform _meshTransform;
    [SerializeField] private MeshRenderer _rendererBody;
    [SerializeField] private MeshRenderer _rendererFace;
    [Space]
    [Header("Materials")]
    [Space]
    [SerializeField] private Material _matDefault;
    [SerializeField] private Material _matSelected;
    [SerializeField] private Material _matOverQueued;
    [Space]
    [SerializeField] private AudioSource _sFXQ;
    [SerializeField] private AudioSource _sFXDeQ;
    [SerializeField] private AudioSource _sFXOverQ;

    // - - - Internal - - -

    private int  _queued = 0;
    private bool _selected;
    private bool _hovered;
    private bool _moving;
    private bool _held;

    private Vector3 _posOnPress;
    private Vector3 _posMouseTarget;
    private Vector3 _posOnRelease;

    // - - -

    void Awake()
    {
        _idlePosition = transform.localPosition; // test
    }

    void FixedUpdate()
    {
        if ( _held )
        {
            if ( _moving )
            {
                _posMouseTarget = GameBoard.GetMouseBoardPosition();
                float speed = _mouseFollowSpeed * Mathf.Max(Vector3.Distance(_posMouseTarget, transform.localPosition), 1.0f);
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, _posMouseTarget, speed * Time.deltaTime);
            }
            else
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                _moving = !(Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform);
            }
        }
        else if ( transform.localPosition != _idlePosition )
        {
            float speed = _idleLerpSpeed * Mathf.Max(Vector3.Distance(_idlePosition, transform.localPosition), 1.0f);
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, _idlePosition, speed * Time.deltaTime);
        }
    }

    void OnMouseEnter() { _hovered = true; RefreshName(); }
    void OnMouseExit() { _hovered = false; RefreshName(); }

    void OnMouseDown() { Press(); }
    void OnMouseUp() { Release(); }

    private void Press() 
    { 
        _posOnPress = _idlePosition;

        GameBoard.Instance.PickUpCard(_idlePosition.x, _idlePosition.y); 

        // _sFXPress?.Play();
        _held = true;
        _meshAnimator.enabled = false;
        _meshTransform.localPosition = new Vector3(0,0,-0.5f);
    }

    private void Release() 
    { 
        GameBoard.Instance.PlaceCard(this); 

        bool was    = _selected;
        _selected   = (_posOnPress == _idlePosition && !_moving) ? !_selected : _selected;
        
        _queued     = (_queued != 0 && !_selected &&  was) ? Mixer.Instance.DeQueue(this)     : 
                      (_queued == 0 &&  _selected && !was) ? Mixer.Instance.QueueForMix(this) : _queued;

        _moving     = false;
        _held       = false; 

        if ( _selected && !was ) 
        {   
            if ( _queued ==  1)  _sFXQ.Play(); 
            if ( _queued == -1) _sFXOverQ.Play(); 
        }
        if ( was && !_selected ) _sFXDeQ.Play();

        _posOnRelease = transform.localPosition;
        _meshTransform.localPosition = Vector3.zero;

        _meshAnimator.enabled = true;
        UpdateMaterials();
        RefreshName();
    }

    private void UpdateMaterials()
    {
        if (_selected && _queued != 0)
        {
            _rendererBody.material = _queued == 1 ? _matSelected : _matOverQueued;
        }
        else
        {
            _rendererBody.material = _matDefault;
        }
    }

    public void ResetSelection()
    {
        _queued = 0;
        _selected = false;
        UpdateMaterials();
        RefreshName();
    }
    
    public void SetQueued(int q)
    {
        _queued = q;
        UpdateMaterials();
    }

    public void RefreshName()
    {
        gameObject.name = $" [ {IdleX, 3}, {IdleY,3} ] {_elementName} ";
        _elementTxt.text = $" {_elementName} ";
        _elementTxt.gameObject.SetActive(_hovered);
    }

    public void SetElement(string elementName)
    {
        _elementName = elementName;
        RefreshName();
    }

    public string GetElement()
    {
        return _elementName;
    }

    public void SetFace(Material m)
    {
        _rendererFace.material = m;
    }

    public void SetIdlePosition(Vector3 idlePos)
    {
        _idlePosition = idlePos;
    }

    public void SetElement(ScriptableElement elData) {AssignElementData(elData);}
    public void AssignElementData(ScriptableElement elData)
    {
        _elementData = elData;
        _elementName = elData.Name;
        _rendererFace.material = elData.ColorMat;
        RefreshName();
    }


    public int MixHash => _elementData.MixHash();
    public int EleHash => _elementData.GetHashCode();

    public float IdleX => _idlePosition.x;
    public float IdleY => _idlePosition.y;

    // ---

}