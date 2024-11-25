using System;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class InputProxy : MonoBehaviour
{
    public Aurdino Ardino;
    
    public static InputProxy Instance;
    
    public bool LeftDown { get; private set; }
    public bool RightDown { get; private set; }
    public bool SubmitDown { get; private set; }
    public bool ResetDown { get; private set; }
    
    private bool _serialLeftDown;
    private bool _serialRightDown;
    private bool _serialSubmitDown;
    private bool _serialResetDown;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!Ardino)
        {
            return;
        }
        //update order -400 will run before proxy update (-200) then game logic
        Ardino.OnButtonPressed += button =>
        {
            switch (button)
            {
                case Aurdino.ButtonPress.Left:
                    _serialLeftDown = true;
                    break;
                case Aurdino.ButtonPress.Right:
                    _serialRightDown = true;
                    break;
                case Aurdino.ButtonPress.Submit:
                    _serialSubmitDown = true;
                    break;
                case Aurdino.ButtonPress.Reset:
                    _serialResetDown = true;
                    break;
            }
        };
    }
    
    void Update()
    {
        LeftDown = Input.GetKeyDown(KeyCode.LeftArrow) || _serialLeftDown;
        RightDown = Input.GetKeyDown(KeyCode.RightArrow) || _serialRightDown;
        SubmitDown = Input.GetKeyDown(KeyCode.Space) || _serialSubmitDown;
        ResetDown = Input.GetKeyDown(KeyCode.Escape) || _serialResetDown;
        
        _serialLeftDown = false;
        _serialRightDown = false;
        _serialSubmitDown = false;
        _serialResetDown = false;
    }
}
