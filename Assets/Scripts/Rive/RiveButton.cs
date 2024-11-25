using System;
using Rive;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RiveButton : RiveRender, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Serializable]
    private struct ButtonEvents
    {
        [SerializeField] private string _variableName;
        [SerializeField] private ValueTypes _valueType;
        [ShowIf("@this._valueType == RiveRender.ValueTypes.Boolean")] 
        [SerializeField] private bool _boolOn;
        [ShowIf("@this._valueType == RiveRender.ValueTypes.Boolean")] 
        [SerializeField] private bool _boolOff;
        [ShowIf("@this._valueType == RiveRender.ValueTypes.Number")] 
        [SerializeField] private float _numberOn;
        [ShowIf("@this._valueType == RiveRender.ValueTypes.Number")] 
        [SerializeField] private float _numberOff;
        public UnityEvent<bool> _logicEvent;
        
        public void ManageEvent(StateMachine stateMachine, bool on)
        {
            switch (_valueType)
            {
                case ValueTypes.Boolean:
                    var boolToReturn = stateMachine.GetBool(_variableName);
                    boolToReturn.Value = on ? _boolOn : _boolOff;
                    break;
                case ValueTypes.Number:
                    var numberToReturn = stateMachine.GetNumber(_variableName);
                    numberToReturn.Value = on ? _numberOn : _numberOff;
                    break;
                case ValueTypes.Trigger:
                    var triggerToReturn = stateMachine.GetTrigger(_variableName);
                    triggerToReturn.Fire();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _logicEvent?.Invoke(on);
        }
    }
    
    [BoxGroup("Button Events")][SerializeField] private bool _allowButtonInteraction = true;
    [BoxGroup("Button Events")][SerializeField] private ButtonEvents _hoverEvent;
    [BoxGroup("Button Events")][SerializeField] private ButtonEvents _clickEvent;
    [HideInInspector] public UnityEvent<bool> HoverEvent;
    [HideInInspector] public UnityEvent<bool> ClickEvent;

    public override void ControlClicking(bool x)
    {
        _allowButtonInteraction = x;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_allowButtonInteraction) return;
        _hoverEvent.ManageEvent(MStateMachine, true);
        HoverEvent?.Invoke(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_allowButtonInteraction) return;
        _hoverEvent.ManageEvent(MStateMachine, false);
        HoverEvent?.Invoke(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_allowButtonInteraction) return;
        _clickEvent.ManageEvent(MStateMachine, true);
        ClickEvent?.Invoke(true);
    }
}
