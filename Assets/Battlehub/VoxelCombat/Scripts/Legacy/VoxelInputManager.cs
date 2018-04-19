﻿using SubjectNerd.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public enum InputAction
    {
        //Infrastructure
        ToggleConsole = 0,
        Quit = 2,
        SaveReplay = 3,

        //Game
        MoveForward = 10,
        MoveSide = 15,
        DPadLeft = 16,
        DPadRight = 17,
        DPadUp = 18,
        DPadDown = 19,

        MouseX = 90,
        MouseY = 91,

        CursorX = 100,
        CursorY = 101,

        Zoom = 102,
        LB = 103,
        RB = 104,
        A = 105,
        X = 106,
        B = 107,
        Action6 = 108,
        Action7 = 109,
        Y = 110,
        Start = 111,
        Back = 112,
        Cancel = 113,
        Submit = 114,
        LT = 115,
        RT = 116,
        RMB = 117,
        LMB = 118,
        MMB = 119,
        LeftStickButton = 120,
        RightStickButton = 121,

        //MapEditor
        EditorCreate = 500,
        EditorDestroy = 510,
        EditorPan = 520,
        EditorRotate = 530,

        //Debug
        ToggleCursor = 1000,
    }

    [Serializable]
    public class InputBinding
    {
        public string Name;
        public InputAction Action;
        public string AxisName;
        public string AltAxisName;
        public bool isMaskedByUI = true;
        public int Player;
    }

    public delegate void InputEventHandler<T>(T arg);

    public interface IVoxelInputManager
    {
        event InputEventHandler<int> DeviceEnabled;
        event InputEventHandler<int> DeviceDisabled;
     
        bool IsInInitializationState
        {
            get;
            set;
        }
           
        int DeviceCount
        {
            get;
        }

        Vector3 MousePosition
        {
            get;
        }

        int GetDeviceIndex(object device);

        bool IsSuspended(int index);

        void Resume(int index);

        void Suspend(int index);

        void ResumeAll();

        void SuspendAll();

        void ActivateAll();

        void DeactivateDevice(int index);

        bool IsKeyboardAndMouse(int index);

        bool IsAnyButtonDown(int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);

        float GetAxisRaw(InputAction action, int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);

        bool GetButtonDown(InputAction action, int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);

        bool GetButton(InputAction action, int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);

        bool GetButtonUp(InputAction action, int player, bool isMaskedBySelectedUI = true, bool isMaskedByPointerOverUI = true);
    }

    /*
    public class VoxelInputManager : MonoBehaviour, IVoxelInputManager
    {
        public event InputEventHandler<int> DeviceEnabled;
        public event InputEventHandler<int> DeviceDisabled;

        public int DeviceCount
        {
            get { return 1; }
        }

        public bool IsInInitializationState
        {
            get;
            set; 
        }

        [Reorderable]
        [SerializeField]
        private InputBinding[] m_bindings;

        public InputBinding[] Bindings
        {
            get { return m_bindings; }
            set
{
                m_bindings = value;
                CreateBuffers();
            }
        }

        private float[][] m_values;
        private bool[][] m_downButtons;
        private bool[][] m_buttons;
        private bool[][] m_upButtons;


        private GameObject m_selectedGameObject;
        private bool m_isInputFieldSelected;
        private static VoxelInputManager m_instance;

        public Vector3 MousePosition
        {
            get { return Input.mousePosition; }
        }

        private Dictionary<int, int> m_commandToIndex;

        private void Awake()
        {
            if (m_instance != null)
            {
                Debug.LogError("Another instance of VoxelInputManager exists");
            }

            m_instance = this;

            CreateBuffers();

            if (DeviceDisabled != null)
            {
                DeviceDisabled(0);
            }

            if (DeviceEnabled != null)
            {
                DeviceEnabled(0);
            }
        }

        private void CreateBuffers()
        {
            m_commandToIndex = new Dictionary<int, int>();
            Array commandValues = Enum.GetValues(typeof(InputAction));
            for(int i = 0; i < commandValues.Length; ++i)
            {
                m_commandToIndex.Add((int)commandValues.GetValue(i), i);
            }

            int commandsCount = Enum.GetNames(typeof(InputAction)).Length;
            const int playersCount = GameConstants.MaxLocalPlayers; //maximum local players

            m_values = new float[playersCount][];
            m_downButtons = new bool[playersCount][];
            m_buttons = new bool[playersCount][];
            m_upButtons = new bool[playersCount][];

            for (int i = 0; i < playersCount; ++i)
            {
                m_values[i] = new float[commandsCount];
                m_downButtons[i] = new bool[commandsCount];
                m_buttons[i] = new bool[commandsCount];
                m_upButtons[i] = new bool[commandsCount];
            }
        }

        //private void LateUpdate() //Because PlayerCommandsPanel and PlayerUnitController does not work properly with LateUpdate here
        private void Update()
        {
            bool isPointerOverGameObject = false;
            if (EventSystem.current != null)
            {
                isPointerOverGameObject = EventSystem.current.IsPointerOverGameObject();
                if (EventSystem.current.currentSelectedGameObject != m_selectedGameObject)
                {
                    m_selectedGameObject = EventSystem.current.currentSelectedGameObject;
                    m_isInputFieldSelected = m_selectedGameObject != null && m_selectedGameObject.GetComponent<InputField>() != null;
                }
            }

            if(m_isInputFieldSelected || isPointerOverGameObject)
            {
                for (int i = 0; i < Bindings.Length; ++i)
                {
                    InputBinding binding = Bindings[i];
                    if(binding.isMaskedByUI)
                    {
                        m_values[binding.Player][m_commandToIndex[(int)binding.Action]] = 0;
                        m_downButtons[binding.Player][m_commandToIndex[(int)binding.Action]] = false;
                        m_buttons[binding.Player][m_commandToIndex[(int)binding.Action]] = false;
                        m_upButtons[binding.Player][m_commandToIndex[(int)binding.Action]] = false;
                    }
                    else
                    {
                        GetInput(binding);
                    }
                }
            }
            else
            {
                for (int i = 0; i < Bindings.Length; ++i)
                {
                    InputBinding binding = Bindings[i];

                    GetInput(binding);
                }
            } 
        }

        private void GetInput(InputBinding binding)
        {
            if (string.IsNullOrEmpty(binding.AltAxisName))
            {
                m_values[binding.Player][m_commandToIndex[(int)binding.Action]] = Input.GetAxisRaw(binding.AxisName);
                m_downButtons[binding.Player][m_commandToIndex[(int)binding.Action]] = Input.GetButtonDown(binding.AxisName);
                m_buttons[binding.Player][m_commandToIndex[(int)binding.Action]] = Input.GetButton(binding.AxisName);
                m_upButtons[binding.Player][m_commandToIndex[(int)binding.Action]] = Input.GetButtonUp(binding.AxisName);
            }
            else
            {
                m_values[binding.Player][m_commandToIndex[(int)binding.Action]] = Mathf.Clamp(Input.GetAxisRaw(binding.AxisName) + Input.GetAxisRaw(binding.AltAxisName), -1, 1);
                m_downButtons[binding.Player][m_commandToIndex[(int)binding.Action]] = Input.GetButtonDown(binding.AxisName) || Input.GetButtonDown(binding.AltAxisName);
                m_buttons[binding.Player][m_commandToIndex[(int)binding.Action]] = Input.GetButton(binding.AxisName) || Input.GetButton(binding.AltAxisName);
                m_upButtons[binding.Player][m_commandToIndex[(int)binding.Action]] = Input.GetButtonUp(binding.AxisName) || Input.GetButtonUp(binding.AltAxisName);
            }
        }

        public int GetDeviceIndex(object device)
        {
            return -1;
        }

        public bool IsSuspended(int index)
        {
            throw new NotImplementedException();
        }

        public void ResumeAll()
        {
            throw new NotImplementedException();
        }

        public void SuspendAll()
        {
            throw new NotImplementedException();
        }

        public void Resume(int index)
        {
            throw new NotImplementedException();
        }

        public void Suspend(int index)
        {
            throw new NotImplementedException();
        }

        public void DeactivateDevice(int index)
        {
            throw new NotImplementedException();
        }

        public bool IsKeyboardAndMouse(int index)
        {
            throw new NotImplementedException();
        }

        public bool IsAnyButtonDown(int player, bool isMaskedByUI = true)
        {
            throw new NotImplementedException();
        }

        public void ActivateAll()
        {
            throw new NotImplementedException();
        }

        public float GetAxisRaw(InputAction action, int player, bool isMaskedByUI)
        {
            return m_values[player][m_commandToIndex[(int)action]];
        }

        public bool GetButtonDown(InputAction action, int player, bool isMaskedByUI)
        {
            return m_downButtons[player][m_commandToIndex[(int)action]];
        }

        public bool GetButton(InputAction action, int player, bool isMaskedByUI)
        {
            return m_buttons[player][m_commandToIndex[(int)action]];
        }

        public bool GetButtonUp(InputAction action, int player, bool isMaskedByUI)
        {
            return m_upButtons[player][m_commandToIndex[(int)action]];
        }
    }
    */
}
