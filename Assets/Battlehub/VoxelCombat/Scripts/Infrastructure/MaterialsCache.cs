﻿using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IMaterialsCache
    {
        Color GetPrimaryColor(int player);

        Color GetSecondaryColor(int player);

        Material GetPrimaryMaterial(int player);

        Material GetSecondaryMaterial(int player);
    }

    public class MaterialsCache : MonoBehaviour, IMaterialsCache
    {
        private IVoxelGame m_gameState;

        [SerializeField]
        private Color[] m_playerColors;
        [SerializeField]
        private Color[] m_secondaryColors;

        [SerializeField]
        private Material m_primaryMaterial;
        [SerializeField]
        private Material m_secondaryMaterial;

        private Material[] m_primaryMaterials;
        private Material[] m_secondaryMaterials;

        private const float m_alpha = 0.4f;

        public Color GetPrimaryColor(int player)
        {
            return m_playerColors[player];
        }

        public Color GetSecondaryColor(int player)
        {
            if(m_secondaryColors.Length > player)
            {
                return m_secondaryColors[player];
            }
            Color secondaryColor = m_playerColors[player];
            secondaryColor.a = m_alpha;
            return secondaryColor;
        }

        public Material GetPrimaryMaterial(int player)
        {
            return m_primaryMaterials[player];
        }

        public Material GetSecondaryMaterial(int player)
        {
            return m_secondaryMaterials[player];
        }

        private void Awake()
        {
            m_gameState = Dependencies.GameState;

            if(m_playerColors.Length != m_gameState.MaxPlayersCount)
            {
                Debug.LogError("not all m_playerColors defined");
                return;
            }

            m_primaryMaterials = new Material[m_gameState.MaxPlayersCount];
            CreateMaterials(m_primaryMaterial, m_primaryMaterials, true);

            m_secondaryMaterials = new Material[m_gameState.MaxPlayersCount];
            CreateMaterials(m_secondaryMaterial, m_secondaryMaterials, false, m_alpha);
        }

        private void CreateMaterials(Material material, Material[] materials, bool isPrimary, float alpha = 1)
        {
            for(int i = 0; i < materials.Length; ++i)
            {
                materials[i] = Instantiate(material);
                Color color;
                if (isPrimary)
                {
                    color = GetPrimaryColor(i);
                }
                else
                {
                    color = GetSecondaryColor(i);
                }
                materials[i].color = color;
                materials[i].name = material.name;

                
            }

            for (int i = 1; i < materials.Length; ++i)
            {
                materials[i].SetFloat("_FogOfWarCutoff", 0.8f);
            }
        }
    }

}

