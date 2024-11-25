using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rive;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Renderer = UnityEngine.Renderer;

public class RiveRender : MonoBehaviour
{
    //Rect transform height and width must match the art board size that it is in Rive
    //_xSize and _ySize must match the screen resolution (it's the quality it will render the texture at)
    
    protected enum ArtBoardLoadType
    {
        Index, Name
    }
    protected enum DimensionSpecification
    {
        Type2D, Type3D
    }
    
    [BoxGroup("Rive File Settings")][Tooltip("2D rendering requires a RawImage, 3D requires a renderer component")]
    [SerializeField] protected DimensionSpecification _dimensionSpecification = DimensionSpecification.Type3D;
    [BoxGroup("Rive File Settings")][Required][SerializeField] protected Asset _asset;
    [BoxGroup("Rive File Settings")][Tooltip("Will load Art Board at index 0")]
    [SerializeField] protected bool _defaultArtBoard = true;
    [BoxGroup("Rive File Settings")][ShowIf("@this._defaultArtBoard == false")]
    [SerializeField] protected ArtBoardLoadType _artBoardLoadType = ArtBoardLoadType.Index;
    [BoxGroup("Rive File Settings")][ShowIf("@this._artBoardLoadType == ArtBoardLoadType.Index && _defaultArtBoard == false")]
    [SerializeField] protected uint _artBoardIndex = 0;
    [BoxGroup("Rive File Settings")][ShowIf("@this._artBoardLoadType == ArtBoardLoadType.Name && _defaultArtBoard == false")]
    [SerializeField] protected string _artBoardName;
    [BoxGroup("Rive Texture Settings")][SerializeField] protected Fit _fit = Fit.fill;
    [BoxGroup("Rive Texture Settings")][SerializeField] protected int _xSize = 512;
    [BoxGroup("Rive Texture Settings")][SerializeField] protected int _ySize = 512;
    [BoxGroup("Rive Queue Settings")][SerializeField] protected float _queueTriggerDelay = 1f;
    [Tooltip("When variables get triggered from external scripts, debug the call")] 
    [BoxGroup("Rive Debug Options")][SerializeField] private bool _debugTriggeredVariables = false;
    [Tooltip("On start Unity will write to the console all the inputs the loaded art board contains")] 
    [BoxGroup("Rive Debug Options")][SerializeField] private bool _debugAllInputs = false;
    [Tooltip("When an event is reported from the art board, Unity will write the name of the event to the console")] 
    [BoxGroup("Rive Debug Options")][SerializeField] private bool _debugReportedEvents = false;
    [Tooltip("Tied to the button on this script")] 
    [BoxGroup("Rive Debug Options")][InlineButton("RunOnRequest", "Run On Request")] 
    [SerializeField] private ResetEvent[] _runAtRequest;
    [BoxGroup("Mouse Input")][SerializeField] private bool _assignCameraRuntime;
    [BoxGroup("Mouse Input")][ShowIf("@this._assignCameraRuntime == false")]
    [Required][SerializeField] private Camera _camera;
    [BoxGroup("Mouse Input")][SerializeField] private bool _allowMouseTracking;
    [BoxGroup("Mouse Input")][SerializeField] protected bool _allowClicking;
    [BoxGroup("Mouse Input")][SerializeField][Range(0, 100)] private float _percentageInfluence;
    [BoxGroup("Mouse Input")][SerializeField] private RectTransform[] _objectsToMatch; 
    [BoxGroup("Run At Start")][SerializeField] private TextRuns[] _staticTextRuns;
    [Tooltip("Unity events that must happen when player interaction happens with the art board")] 
    [BoxGroup("Run At Start")][SerializeField] private ResetEvent[] _variablesSetAtStart;
    [Tooltip("Unity events that must happen when player interaction happens with the art board")] 
    [BoxGroup("Events")][SerializeField]private InteractionEvent[] _interactionEvents;
    [Tooltip("Unity events that must happen when events are reported by the art board")]
    [BoxGroup("Events")][SerializeField] private ArtBoardEvent[] _artBoardEvents;
    [Tooltip("Variables to reset on the art board")]
    [BoxGroup("Events")][SerializeField] private ResetEvent[] _resetEvents;
    
    protected StateMachine MStateMachine;
    private bool _canRunRiveFile;
    private Artboard _mArtBoard;
    private RenderTexture _mRenderTexture;
    private Rive.RenderQueue _mRenderQueue;
    private Rive.Renderer _mRiveRenderer;
    private CommandBuffer _mCommandBuffer;
    private File _mFile;
    private RawImage _rawImage;
    private Renderer _componentRenderer;
    private Camera _mCamera;
    private Vector2 _mLastMousePosition;
    private bool _mWasMouseDown;
    private Queue<string> _triggersInWaiting;
    private Coroutine _triggerCoroutine;

    private bool _hasPreview;

    #region Structs

    protected enum ValueTypes {Boolean, Trigger, Number}

    [Serializable]
    private struct TextRuns
    {
        [SerializeField] private string _propertyName;
        [SerializeField] private string _newText;

        public void SetText(Artboard artBoard)
        {
            artBoard.SetTextRun(_propertyName, _newText);
        }
    }

    [Serializable]
    private struct ResetEvent
    {
        [SerializeField] private string _variableName;
        [SerializeField] private ValueTypes _valueType;
        [ShowIf("@this._valueType == ValueTypes.Boolean")] 
        [SerializeField] private bool _resetBoolean;
        [ShowIf("@this._valueType == ValueTypes.Number")] 
        [SerializeField] private float _resetNumber;
        
        public void ManageEvent(StateMachine stateMachine)
        {
            switch (_valueType)
            {
                case ValueTypes.Boolean:
                    var boolToReturn = stateMachine.GetBool(_variableName);
                    boolToReturn.Value = _resetBoolean;
                    //Debug.Log($"{boolToReturn.Name} set to {boolToReturn.Value}");
                    break;
                case ValueTypes.Number:
                    var numberToReturn = stateMachine.GetNumber(_variableName);
                    numberToReturn.Value = _resetNumber;
                    stateMachine.Advance(Time.deltaTime);
                    //Debug.Log($"{numberToReturn.Name} set to {numberToReturn.Value}");
                    break;
                case ValueTypes.Trigger:
                    var trigger = stateMachine.GetTrigger(_variableName);
                    trigger.Fire();
                    stateMachine.Advance(Time.deltaTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    private struct InteractionEvent
    {
        [SerializeField] private string _eventName;
        [SerializeField] private ValueTypes _valueType;
        [ShowIf("@this._valueType == ValueTypes.Boolean")] 
        [SerializeField] private UnityEvent<bool> _eventBool;
        [ShowIf("@this._valueType == ValueTypes.Trigger")]
        [SerializeField]private UnityEvent _eventTrigger;
        [ShowIf("@this._valueType == ValueTypes.Number")]
        [SerializeField]private UnityEvent<float> _eventNumber;

        public void ManageEvent(StateMachine stateMachine)
        {
            switch (_valueType)
            {
                case ValueTypes.Boolean:
                    var boolToReturn = stateMachine.GetBool(_eventName);
                    _eventBool?.Invoke(boolToReturn.Value);
                    break;
                case ValueTypes.Trigger:
                    var triggerToReturn = stateMachine.GetTrigger(_eventName);
                    if (triggerToReturn.IsTrigger)
                        _eventTrigger?.Invoke();
                    break;
                case ValueTypes.Number:
                    var numberToReturn = stateMachine.GetNumber(_eventName);
                    _eventNumber?.Invoke(numberToReturn.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    [Serializable]
    private struct ArtBoardEvent
    {
        [SerializeField] private string _eventName;
        [SerializeField] private UnityEvent _eventToTrigger;

        public void ManageEvent(ReportedEvent reportedEvent)
        {
            if (string.CompareOrdinal(reportedEvent.Name, _eventName) == 0) 
                _eventToTrigger?.Invoke();
        }
    }

    #endregion

    [Button("Preview")]
    protected void OnValidate()
    {
        if (!_hasPreview)
        {
            Awake();
            Start();
            Update();
            _hasPreview = true;
        }
    }

    protected virtual void Awake()
    {
        _canRunRiveFile = false;
        LoadRiveFile();
    }

    protected virtual void Start()
    {
        _triggersInWaiting = new Queue<string>();
        SetStaticElements();
        DebugInputs();

        foreach (SMIInput input in MStateMachine.Inputs())
        {
            Debug.Log($"StateMachine has input: {input.Name}. as {((input.IsTrigger)?"Trigger":(input.IsBoolean)?"Boolean":"Number")}");
        }
    }

    protected virtual void Update()
    {
        if (!_canRunRiveFile) return;
        RiveMouseInput();
        UpdateStateMachine();
    }

    #region Debug Methods
    private void RunOnRequest()
    {
        foreach (var variable in _runAtRequest)
        {
            variable.ManageEvent(MStateMachine);
        }
    }

    private void DebugInputs()
    {
        if (!_debugAllInputs) return;
        foreach (var variable in MStateMachine.Inputs())
        {
            var type = string.Empty;
            if (variable.IsTrigger)
                type = "Trigger";
            else if (variable.IsBoolean)
                type = "Boolean";
            else if (variable.IsNumber)
                type = "Number";
            Debug.Log($"{_asset.name} :: {type} :: {variable.Name}");
        }
    }

    #endregion

    #region Art board Rendering
    
    private void LoadRiveFile()
    {
        if (_assignCameraRuntime)
            _camera = Camera.main;
        
        _mRenderTexture = new RenderTexture(TextureHelper.Descriptor(_xSize, _ySize));
        _mRenderTexture.Create();
        
        switch (_dimensionSpecification)
        {
            case DimensionSpecification.Type2D:
                if (TryGetComponent(out RawImage rawImage))
                {
                    _canRunRiveFile = true;
                    _rawImage = rawImage;
                    _rawImage.texture = _mRenderTexture;
                    if (FlipY())
                    {
                        var parent = transform.parent;
                        var parentChildIndex = transform.GetSiblingIndex();
                        if (parent && parent.childCount > 1)
                        {
                            var newParent = new GameObject($"{gameObject.name} (Parent)");
                            newParent.AddComponent<RectTransform>();
                            newParent.transform.SetParent(parent);
                            newParent.GetComponent<RectTransform>().position =
                                gameObject.GetComponent<RectTransform>().position;
                            gameObject.transform.SetParent(newParent.transform);
                            newParent.transform.SetSiblingIndex(parentChildIndex);
                        }
                        
                        var rectTransform = _rawImage.GetComponent<RectTransform>();
                        var currentLocalScale = rectTransform.localScale;
                        if (currentLocalScale.y > 0)
                        {
                            rectTransform.localScale = new Vector3(currentLocalScale.x, 
                                currentLocalScale.y * -1, currentLocalScale.z);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"No RawImage component attached to {gameObject.name}", this);
                }
                break;
            case DimensionSpecification.Type3D:
                if (TryGetComponent(out Renderer render))
                {
                    _canRunRiveFile = true;
                    _componentRenderer = render;
                    var material = _componentRenderer.material;
                    material.mainTexture = _mRenderTexture;
                    if (FlipY())
                    {
                        var currentLocalScale = transform.localScale;
                        transform.localScale = 
                            new Vector3(currentLocalScale.x, currentLocalScale.y*-1, 
                                currentLocalScale.z);
                    }
                }
                else
                {
                    Debug.LogError($"No Renderer component attached to {gameObject.name}",this);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (!_canRunRiveFile) return;
        _mRenderQueue = new Rive.RenderQueue(_mRenderTexture);
        _mRiveRenderer = _mRenderQueue.Renderer();
        if (_asset != null)
        {
            _mFile = File.Load(_asset);
            if (_defaultArtBoard)
            {
                _mArtBoard = _mFile.Artboard(0);
            }
            else
            {
                _mArtBoard = _artBoardLoadType switch
                {
                    ArtBoardLoadType.Index => _mFile.Artboard(_artBoardIndex),
                    ArtBoardLoadType.Name => _mFile.Artboard(_artBoardName),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            MStateMachine = _mArtBoard?.StateMachine();
        }

        if (_mArtBoard != null && _mRenderTexture != null)
        {
            _mRiveRenderer.Align(_fit, Alignment.Center, _mArtBoard);
            _mRiveRenderer.Draw(_mArtBoard);

            _mCommandBuffer = new CommandBuffer();
            _mCommandBuffer.SetRenderTarget(_mRenderTexture);
            _mCommandBuffer.ClearRenderTarget(true, true, UnityEngine.Color.clear, 0.0f);
            _mRiveRenderer.AddToCommandBuffer(_mCommandBuffer);
            
            _mCamera = Camera.main;
            if (_mCamera != null)
                _mCamera.AddCommandBuffer(CameraEvent.AfterEverything, _mCommandBuffer);
        }
    }
    
    private static bool FlipY()
    {
        switch (SystemInfo.graphicsDeviceType)
        {
            case GraphicsDeviceType.Metal:
            case GraphicsDeviceType.Direct3D11:
                return true;
            default:
                return false;
        }
    }

    private void SetStaticElements()
    {
        foreach (var variable in _staticTextRuns)
        {
            variable.SetText(_mArtBoard);
        }
        foreach (var variable in _variablesSetAtStart)
        {
            variable.ManageEvent(MStateMachine);
        }
    }
    
    private void UpdateStateMachine()
    {
        _mRiveRenderer.Submit();
        GL.InvalidateState();
        MStateMachine?.Advance(Time.deltaTime);
    }

    #region Mouse Input
    private void RiveMouseInput()
    {
        if (_camera != null)
        {
            var mousePos = _camera.ScreenToViewportPoint(Input.mousePosition);
            var mouseRiveScreenPos = new Vector2(
                mousePos.x * _camera.pixelWidth,
                (1 - mousePos.y) * _camera.pixelHeight
            );
            if (_mArtBoard != null)
            {
                var v2 = new Vector2(_camera.pixelWidth, _camera.pixelHeight);
                if (_allowMouseTracking)
                {
                    if (_mLastMousePosition != mouseRiveScreenPos)
                    {
                        var local = _mArtBoard.LocalCoordinate(
                            mouseRiveScreenPos,
                            new Rect(0, 0, v2.x, v2.y),
                            _fit,
                            Alignment.Center
                        );
                        MStateMachine?.PointerMove(local);
                        _mLastMousePosition = mouseRiveScreenPos;
                        TrackAdditionalObjects(mouseRiveScreenPos);
                    }
                }
                if (_allowClicking)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        var local = _mArtBoard.LocalCoordinate(
                            mouseRiveScreenPos,
                            new Rect(0, 0, v2.x, v2.y),
                            _fit,
                            Alignment.Center
                        );
                        MStateMachine?.PointerDown(local);
                        foreach (var variable in _interactionEvents)
                            variable.ManageEvent(MStateMachine);
                        _mWasMouseDown = true;
                    }
                    else if (_mWasMouseDown)
                    {
                        _mWasMouseDown = false;
                        var local = _mArtBoard.LocalCoordinate(
                            mouseRiveScreenPos,
                            new Rect(0, 0, v2.x, v2.y),
                            _fit,
                            Alignment.Center
                        );
                        MStateMachine?.PointerUp(local);
                    }
                }
            }
        }

        foreach (var reportedEvent in MStateMachine?.ReportedEvents() ?? Enumerable.Empty<ReportedEvent>())
        {
            if (_debugReportedEvents)
            {
                Debug.Log($"Event received, name: \"{reportedEvent.Name}\", " +
                          $"secondsDelay: {reportedEvent.SecondsDelay}");
            }
            foreach (var variable in _artBoardEvents)
            {
                variable.ManageEvent(reportedEvent);
            }
        }
    }
    
    private void TrackAdditionalObjects(Vector2 mouseRiveScreenPosition)
    {
        var newPosition = new Vector2(mouseRiveScreenPosition.x / 100 * _percentageInfluence, 
            mouseRiveScreenPosition.y / 100 * -_percentageInfluence);
        foreach (var variable in _objectsToMatch)
            variable.anchoredPosition = newPosition;
    }
    #endregion
    
    #endregion
    
    #region Art board Function Control

    public virtual void ControlClicking(bool x)
    {
        _allowClicking = x;
    }
    public void ControlMouseTracking(bool x)
    {
        _allowMouseTracking = x;
    }
    
    public void ResetEvents()
    {
        if (_mArtBoard == null) return;
        foreach (var variable in _resetEvents)
            variable.ManageEvent(MStateMachine);
    }

    #endregion
    
    #region Art board Variable Controls
    /// <summary>
    /// Use to call a trigger in a rive art board
    /// </summary>
    /// <param name="variableName">Name of Trigger in the rive art board</param>
    public void TriggerVariable(string variableName)
    {
        _triggersInWaiting.Enqueue(variableName);
        _triggerCoroutine ??= StartCoroutine(ActivateTrigger());
    }
    private IEnumerator ActivateTrigger()
    {
        var variableName = _triggersInWaiting.Dequeue();
        if (MStateMachine is not null)
        {
            var smiTrigger = MStateMachine.GetTrigger(variableName);
            if (smiTrigger is not null)
            {
                smiTrigger.Fire();
                if (_debugTriggeredVariables)
                    Debug.Log($"{_asset.name} :: Trigger {variableName} fired");
            }
            else
            {
                Debug.LogWarning($"Trigger ({variableName}) does not exist on Rive State machine on ({gameObject.name})!", this);
            }
        }
        else
        {
            Debug.LogWarning($"Rive State machine on ({gameObject.name}) is null, can't set trigger!", this);
        }

        yield return new WaitForSeconds(_queueTriggerDelay);
        if (_triggersInWaiting.Count > 0)
        {
            _triggerCoroutine = StartCoroutine(ActivateTrigger());
        }
        else
        {
            _triggerCoroutine = null;
        }
    }
    /// <summary>
    /// Use to set a number variable in a rive art board
    /// </summary>
    /// <param name="variableName">Name of Number variable in the rive art board</param>
    /// <param name="newValue">What the Number variable must be set to</param>
    public void TriggerVariable(string variableName, float newValue)
    {
        if (MStateMachine is not null)
        {
            var smiNumber = MStateMachine.GetNumber(variableName);
            if (smiNumber is not null)
            {
                smiNumber.Value = newValue;
                if (_debugTriggeredVariables)
                    Debug.Log($"{_asset.name} :: Number {variableName} set to {smiNumber.Value}");
            }
            else
            {
                Debug.LogWarning($"Number ({variableName}) does not exist on Rive State machine on ({gameObject.name})!", this);
            }
        }
        else
        {
            Debug.LogWarning($"Rive State machine on ({gameObject.name}) is null, can't change int!", this);
        }
    }
    /// <summary>
    /// Use to set a number variable in a rive art board
    /// </summary>
    /// <param name="variableName">Name of Number variable in the rive art board</param>
    /// <param name="newValue">What the Number variable must be set to</param>
    public void TriggerVariable(string variableName, int newValue)
    {
        if (MStateMachine is not null)
        {
            var smiNumber = MStateMachine.GetNumber(variableName);
            if (smiNumber is not null)
            {
                smiNumber.Value = newValue;
                if (_debugTriggeredVariables)
                    Debug.Log($"{_asset.name} :: Number {variableName} set to {smiNumber.Value}");
            }
            else
            {
                Debug.LogWarning($"Number ({variableName}) does not exist on Rive State machine on ({gameObject.name})!", this);
            }
        }
        else
        {
            Debug.LogWarning($"Rive State machine on ({gameObject.name}) is null, can't change int!", this);
        }
    }
    /// <summary>
    /// Use to set a bool variable in a rive art board
    /// </summary>
    /// <param name="variableName">Name of bool variable in the rive art board</param>
    /// <param name="newValue">What the bool variable must be set to</param>
    public void TriggerVariable(string variableName, bool newValue)
    {
        if (MStateMachine is not null)
        {
            var inputs = MStateMachine.Inputs();
            var smiBool = MStateMachine.GetBool(variableName);
            if (smiBool is not null)
            {
                smiBool.Value = newValue;
                if (_debugTriggeredVariables)
                    Debug.Log($"{_asset.name} :: Bool {variableName} set to {smiBool.Value}");
            }
            else
            {
                Debug.LogWarning($"Bool ({variableName}) does not exist on Rive State machine on ({gameObject.name})!", this);
            }
        }
        else
        {
            Debug.LogWarning($"Rive State machine on ({gameObject.name}) is null, can't trigger bool!", this);
        }
    }

    /// <summary>
    /// Set a text field on the art board to a desired value
    /// </summary>
    /// <param name="variableName">Name of text run in the rive art board</param>
    /// <param name="newValue">The string that will be displayed</param>
    public void SetTextRun(string variableName, string newValue)
    {
        _mArtBoard?.SetTextRun(variableName, newValue);
    }

    #endregion
    
    private void OnDisable()
    {
        if (_mCamera != null && _mCommandBuffer != null)
        {
            _mCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, _mCommandBuffer);
        }
    }

    private void OnDestroy()
    {
        // Release the RenderTexture when it's no longer needed
        if (_mRenderTexture != null)
            _mRenderTexture.Release();
    }
}
