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
            if (workstation == null) return;

            CloseCurrentMenu();

            WorkstationMenuEntry entry = FindEntry(workstation.Type);
            if (entry == null || entry.menuRoot == null)
            {
                Debug.LogWarning($"No menu configured for workstation type: {workstation.Type}");
                return;
            }

            activeWorkstation = workstation;
            activeMenu = entry.menuScript;

            entry.menuRoot.SetActive(true);

            if (activeMenu != null)
                activeMenu.Open(workstation, localCarry);
        }

        public void CloseCurrentMenu()
        {
            if (activeMenu != null)
            {
                activeMenu.Close();
                activeMenu = null;
            }

            if (activeWorkstation != null)
            {
                WorkstationMenuEntry entry = FindEntry(activeWorkstation.Type);
                if (entry != null && entry.menuRoot != null)
                    entry.menuRoot.SetActive(false);

                activeWorkstation = null;
            }
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
