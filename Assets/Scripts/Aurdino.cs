using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-400)]
public class Aurdino : MonoBehaviour
{
    public static Aurdino Instance;
    
    public enum GameState
    {
        Egg = 1,
        AwakeTimer = 2,
        SleepTimer = 3,
        Evolving = 4,
        Dead = 5
    }

    public enum ButtonPress
    {
        None = -1,
        Left = 0,
        Right = 1,
        Submit = 2,
        Reset = 3
    }

    public event Action<ButtonPress> OnButtonPressed = delegate {  };

    [Serializable]
    class SerialSettings
    {
        public string PortName = "COM3";
        public int BaudRate = 9600;
    }

    private SerialSettings _settings;

    private SerialPort _serialPort;

    private bool _gameStateDirty = false;
    private GameState _currentGameState;
    private GameState _sentState;

    private bool _timerStateDirty = false;
    private float _normalizedTimer;
    
    private Thread _serialThread;
    private object _mutex;

    private bool _isRunning;

    private string _lastWrittenValue;

    public void UpdateState(GameState state)
    {
        lock (_mutex)
        {
            Debug.Log($"Updating state: {state}");
            _currentGameState = state;
            _gameStateDirty = true;
        }
    }
    
    public void UpdateTimerState(float normalizedTime)
    {
        lock (_mutex)
        {
            _normalizedTimer = Mathf.Clamp01(normalizedTime);
            _timerStateDirty = true;
        }
    }

    
    private void Awake()
    {
        Instance = this;

        string path = $"{Application.streamingAssetsPath}/SerialSettings.json";

        if (!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
        }
        
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            _settings = JsonUtility.FromJson<SerialSettings>(json);
        }
        else
        {
            _settings = new SerialSettings();
            string json = JsonUtility.ToJson(_settings);
            File.WriteAllText(path, json);
        }
    }

    private void Start()
    {
        _mutex = new object();
        _serialPort = new SerialPort(_settings.PortName, _settings.BaudRate);
        try 
        {
            _serialPort.Open();
            _isRunning = true;
            _serialThread = new Thread(SerialThread);
            _serialThread.Start();
            
        } catch (System.Exception e) 
        {
            Debug.LogError($"Error opening serial port and starting thread: {e}");
        }
    }

    private ButtonPress _recievedButtonPress = ButtonPress.None;
    private string _threadError;

    void SerialThread()
    {
        bool running;
        do
        {
            Thread.Sleep(10);
            
            bool isOpen;

            lock (_mutex)
            {
                isOpen = _serialPort.IsOpen;
            }

            if (isOpen)
            {
                if (_serialPort.BytesToRead > 0)
                {
                    try
                    {
                        string data = _serialPort.ReadLine();

                        if (Enum.TryParse(data, out ButtonPress result))
                        {
                            lock (_mutex)
                            {
                                _recievedButtonPress = result;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        lock (_mutex)
                        {
                            _threadError = e.ToString();
                        }

                    }
                }
                
                lock (_mutex)
                {
                    if (_gameStateDirty)
                    {
                        _gameStateDirty = false;
                        _serialPort.WriteLine($"{(int)_currentGameState}");
                        _sentState = _currentGameState;
                        _lastWrittenValue = $"{_currentGameState} (state) : {(int)_currentGameState}";
                    }
                    else if (_timerStateDirty)
                    {
                        _timerStateDirty = false;
                        int timeRescaled = (int)((_normalizedTimer * 100f) + 10);
                        _serialPort.WriteLine($"{timeRescaled}");
                        _lastWrittenValue = $"{timeRescaled} (time)";
                    }
                }
            }

            lock (_mutex)
            {
                running = _isRunning;
            }
        } while (running);
    }

    void Update() 
    {
        lock (_mutex)
        {
            if (_serialPort.IsOpen == false)
            {
                return;
            }
            switch (_recievedButtonPress)
            {
                case ButtonPress.Left:
                    OnButtonPressed?.Invoke(ButtonPress.Left);

                    break;
                case ButtonPress.Right:
                    OnButtonPressed?.Invoke(ButtonPress.Right);

                    break;
                case ButtonPress.Submit:
                    OnButtonPressed?.Invoke(ButtonPress.Submit);

                    break;
                case ButtonPress.Reset:
                    OnButtonPressed?.Invoke(ButtonPress.Reset);
                    break;
                default:
                    break;
            }
            _recievedButtonPress = ButtonPress.None;

            if (!string.IsNullOrEmpty(_threadError))
            {
                Debug.LogError(_threadError);
                _threadError = String.Empty;
            }
            
            if (!string.IsNullOrEmpty(_lastWrittenValue))
            {
                Debug.Log($"Wrote: {_lastWrittenValue}");
                _lastWrittenValue = String.Empty;
            }
        }
    }

    void OnApplicationQuit()
    {
        GameState sentState;
        bool open;
        do
        {
            UpdateState(GameState.Dead);
            Thread.Sleep(10);
            lock (_mutex)
            {
                open = _serialPort.IsOpen;
                sentState = _sentState;
            }
        } while (open && sentState != GameState.Dead);

        lock (_mutex)
        {
            _isRunning = false;
        }
        
        Thread.Sleep(10);
        _serialThread?.Abort();
      
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
        }
    }
}
