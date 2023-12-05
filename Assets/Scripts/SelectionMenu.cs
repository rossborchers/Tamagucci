using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SelectionMenu : MonoBehaviour
{
    [Serializable]
    public class SelectionOption
    {
        public bool CloseOnSubmit;
        public GameObject[] Selected;

        public SelectionMenu OnSubmitMenu;
        public UnityEvent OnSubmitAction;
    }
    
    public bool StartClosed;

    private float _openTime;

    public GameObject Root;
    public int StartIndex = 0;

    public int CurrentIndex
    {
        set
        {
            _menuIndex = value;
            if (_menuIndex < 0)
            {
                _menuIndex = 0;
            }
            if (_menuIndex > Options.Count - 1)
            {
                _menuIndex = Options.Count -1;
            }
            DoSelect();
        }
    }
    public List<SelectionOption> Options;

    private bool _submit;
    private bool _left;
    private bool _right;
    
    private int _menuIndex = 0;

    public bool InvertDir;

    private void Start()
    {
        if (StartClosed)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    void Update()
    {
        if (!open || Time.time - _openTime < 0.5)
        {
            return;
        }
        bool subtractIdxDown = InvertDir ? InputProxy.Instance.RightDown : InputProxy.Instance.LeftDown;
        if (subtractIdxDown)
        {
            _menuIndex--;
            if (_menuIndex < 0)
            {
                _menuIndex = 0;
            }

            DoSelect();
        }

        bool addIdxDown = InvertDir ? InputProxy.Instance.LeftDown : InputProxy.Instance.RightDown;
        if (addIdxDown)
        {
            _menuIndex++;
            if (_menuIndex > Options.Count - 1)
            {
                _menuIndex = Options.Count -1;
            }

            DoSelect();
        }

        if (InputProxy.Instance.SubmitDown)
        {
            Submit(Options[_menuIndex]);
        }
    }

    private int _lastSelected;
    
    private void DoSelect()
    {
        for (var i = 0; i < Options.Count; i++)
        {
            Unselect(Options[i]);
            if (i == _menuIndex)
            {
                if (_lastSelected != i)
                {
                    GameManager.Instance.SFXHighlight.Play();
                    _lastSelected = i;
                }

                Select(Options[i]);
            }
        }
    }

    public void Select(SelectionOption option)
    {
        foreach (var selected in option.Selected)
        {
            selected.gameObject.SetActive(true);
        }
    }

    public void Unselect(SelectionOption option)
    {
        foreach (var selected in option.Selected)
        {
            selected.gameObject.SetActive(false);
        }
    }
    
    public void Submit(SelectionOption option)
    {
        if (option.OnSubmitMenu != null)
        {
            option.OnSubmitMenu.Open();
        }

        GameManager.Instance.SFXSelect.Play();
        option.OnSubmitAction.Invoke();
            
        if (option.CloseOnSubmit)
        {
            Close();
        }
    }
    
    private bool open;
    
    public void Open()
    {
        //_menuIndex = StartIndex;
        open = true;
        _openTime = Time.time;
        Root.SetActive(true);

        DoSelect();
    }

    public void Close()
    {
        open = false;
        Root.SetActive(false);
    }
}
