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

    public List<SelectionOption> Options;

    private bool _submit;
    private bool _left;
    private bool _right;
    
    private int _menuIndex = 0;

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
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _menuIndex--;
            if (_menuIndex < 0)
            {
                _menuIndex = 0;
            }

            DoSelect();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _menuIndex++;
            if (_menuIndex > Options.Count - 1)
            {
                _menuIndex = Options.Count -1;
            }

            DoSelect();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Submit(Options[_menuIndex]);
        }
    }

    private void DoSelect()
    {
        for (var i = 0; i < Options.Count; i++)
        {
            Unselect(Options[i]);
            if (i == _menuIndex)
            {
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

        option.OnSubmitAction.Invoke();
            
        if (option.CloseOnSubmit)
        {
            Close();
        }
    }
    
    private bool open;
    
    public void Open()
    {
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
