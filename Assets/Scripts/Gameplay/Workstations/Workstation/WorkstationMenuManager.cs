using System;
using Gameplay.Player;
using UnityEngine;

namespace Gameplay.Workstations
{
    [Serializable]
    public class WorkstationMenuEntry
    {
        public WorkstationType type;
        public GameObject menuRoot;
        public WorkstationMenuBase menuScript;
    }

    public class WorkstationMenuManager : MonoBehaviour
    {
        public static WorkstationMenuManager Instance { get; private set; }

        [SerializeField] private WorkstationMenuEntry[] menus;

        private WorkstationMenuBase activeMenu;
        private Workstation activeWorkstation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            HideAllMenus();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OpenMenu(Workstation workstation, PlayerCarry localCarry)
        {
        }

        public void CloseCurrentMenu()
        {
        }

        public bool IsMenuOpen()
        {
            return activeWorkstation != null;
        }

        public bool IsMenuOpenFor(Workstation workstation)
        {
            return activeWorkstation == workstation;
        }

        private WorkstationMenuEntry FindEntry(WorkstationType type)
        {
            if (menus == null) return null;

            for (int i = 0; i < menus.Length; i++)
            {
                if (menus[i] != null && menus[i].type == type)
                    return menus[i];
            }

            return null;
        }

        private void HideAllMenus()
        {
            if (menus == null) return;

            for (int i = 0; i < menus.Length; i++)
            {
                if (menus[i] != null && menus[i].menuRoot != null)
                    menus[i].menuRoot.SetActive(false);
            }
        }
    }
}
