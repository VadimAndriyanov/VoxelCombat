﻿using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IGameView
    {
        bool IsOn
        {
            get;
            set;
        }

        void Initialize(int viewportsCount, bool isOn);

        IGameViewport GetViewport(int index);

        IPlayerUnitController GetUnitController(int index);

        IPlayerSelectionController GetSelectionController(int index);

        IPlayerCameraController GetCameraController(int index);

        ITargetSelectionController GetTargetSelectionController(int index);
    }

    public class GameView : MonoBehaviour, IGameView
    {
        [SerializeField]
        private GameViewport m_gameViewportPrefab;

        [SerializeField]
        private Transform[] m_viewportPlaceholders;

        private GameViewport[] m_gameViewports;

        [SerializeField]
        private GameObject m_secondRow;

        [SerializeField]
        private GameObject m_secondCol;

        [SerializeField]
        private int m_viewportCount = 1;

        private bool m_isInitialized = false;

        //This camera will be destroyed when game will be started
        [SerializeField]
        private Camera m_initializationCamera;

        [SerializeField]
        private GameObject m_menuOverlay;
        
        [SerializeField]
        private bool m_isOn = false;
        public bool IsOn
        {
            get { return m_isOn; }
            set
            {
                if(!m_isInitialized && value)
                {
                    throw new InvalidOperationException("Call Initialize method first");
                }

                if (m_initializationCamera != null && value)
                {
                    Destroy(m_initializationCamera.gameObject);
                }

                if (m_isOn != value)
                {
                    m_isOn = value;
                    UpdateGameViewMode();
                }

                if (m_menuOverlay != null)
                {
                    m_menuOverlay.SetActive(m_isOn);
                }
            }
        }

        public void Initialize(int viewportsCount, bool isOn)
        {
            if(m_initializationCamera && isOn)
            {
                Destroy(m_initializationCamera.gameObject);
            }

            if (m_menuOverlay != null)
            {
                m_menuOverlay.SetActive(isOn);
            }

            m_isInitialized = true;
            m_viewportCount = viewportsCount;
            m_isOn = isOn;

            InitViewports();
            UpdateGameViewMode();
        }


        private void UpdateGameViewMode()
        {
            UpdateCursorMode();

            if (m_gameViewports != null)
            {
                for (int i = 0; i < m_gameViewports.Length; ++i)
                {
                    GameViewport gameViewport = m_gameViewports[i];
                    gameViewport.Camera.gameObject.SetActive(m_isOn);

                    PlayerCameraController cameraController = gameViewport.GetComponent<PlayerCameraController>();
                    if (cameraController != null)
                    {
                        cameraController.gameObject.SetActive(m_isOn);
                    }
                }
            }
        }

        private void UpdateCursorMode()
        {
            Cursor.visible = !m_isOn;
            Cursor.lockState = m_isOn ? CursorLockMode.Locked : CursorLockMode.None;
        }


        public IGameViewport GetViewport(int index)
        {
            return m_viewportPlaceholders[index].GetComponentInChildren<GameViewport>(true);
        }

        private void Awake()
        {
            if(m_isOn)
            {
                Initialize(m_viewportCount, m_isOn);
            }
            else
            {
                if (m_menuOverlay != null)
                {
                    m_menuOverlay.SetActive(false);
                }
            }
        }
#if UNITY_EDITOR
        private void Update()
        {

            if (Dependencies.InputManager.GetButtonDown(InputAction.ToggleCursor, -1, false))
            {
                Cursor.visible = !Cursor.visible;
                if(Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                } 
            }

            if (!Cursor.visible)
            {
                if(Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
#endif


        private void InitViewports()
        {
            if (m_gameViewports != null)
            {
                for (int i = 0; i < m_gameViewports.Length; ++i)
                {
                    Destroy(m_gameViewports[i].gameObject);
                }
            }

            if (m_viewportPlaceholders.Length < m_viewportCount)
            {
                Debug.LogError("m_viewports.Length < m_playerCount");
            }
            else
            {
                m_gameViewports = new GameViewport[m_viewportCount];

                m_secondRow.SetActive(m_viewportCount > 2);
                m_secondCol.SetActive(m_viewportCount > 1);

                IEventSystemManager eventSystemMan = Dependencies.EventSystemManager;

                for (int i = 0; i < m_viewportCount; ++i)
                {
                    GameViewport gameViewport = Instantiate(m_gameViewportPrefab, m_viewportPlaceholders[i]);
                    gameViewport.Camera.gameObject.SetActive(m_isOn);
                    gameViewport.name = "Viewport" + i;
                    gameViewport.LocalPlayerIndex = i;
                    m_gameViewports[i] = gameViewport;

                    eventSystemMan.Apply(gameViewport.gameObject, i);
                    
                    PlayerCameraController cameraController = gameViewport.GetComponent<PlayerCameraController>();
                    if(cameraController != null)
                    {
                        cameraController.LocalPlayerIndex = i;
                    }

                    PlayerSelectionController playerSelectionController = gameViewport.GetComponent<PlayerSelectionController>();
                    if(playerSelectionController != null)
                    {
                        playerSelectionController.LocalPlayerIndex = i;
                    }

                    PlayerUnitController playerUnitController = gameViewport.GetComponent<PlayerUnitController>();
                    if(playerUnitController != null)
                    {
                        playerUnitController.LocalPlayerIndex = i;
                    }

                    TargetSelectionController targetSelectionController = gameViewport.GetComponent<TargetSelectionController>();
                    if(targetSelectionController != null)
                    {
                        targetSelectionController.LocalPlayerIndex = i;
                    }

                    PlayerMenu playerMenu = gameViewport.GetComponent<PlayerMenu>();
                    if(playerMenu != null)
                    {
                        playerMenu.LocalPlayerIndex = i;
                    }
                }
            }
        }

        public IPlayerUnitController GetUnitController(int index)
        {
            return m_gameViewports[index].GetComponent<PlayerUnitController>();
        }

        public IPlayerSelectionController GetSelectionController(int index)
        {
            return m_gameViewports[index].GetComponent<PlayerSelectionController>();
        }

        public IPlayerCameraController GetCameraController(int index)
        {
            return m_gameViewports[index].GetComponent<PlayerCameraController>();
        }

        public ITargetSelectionController GetTargetSelectionController(int index)
        {
            return m_gameViewports[index].GetComponent<TargetSelectionController>();
        }
    }
}
