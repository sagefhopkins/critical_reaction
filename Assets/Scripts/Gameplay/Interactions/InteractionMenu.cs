using Gameplay.Player;
using Gameplay.Workstations;
using UnityEngine;

namespace UX
{
    public class InteractionMenus : MonoBehaviour
    {
        public static InteractionMenus Instance { get; private set; }

        [Header("Menus")]
        [SerializeField] private StorageRackMenu storageRackMenu;
        [SerializeField] private WorkstationMenuManager workstationMenuManager;

        public bool AnyMenuOpen { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OpenStorageRack(StorageRack rack, PlayerCarry carry)
        {
            Debug.Log($"InteractionMenus.OpenStorageRack called for rack: {rack?.name}");
            CloseAll();

            if (storageRackMenu == null || rack == null || carry == null)
            {
                AnyMenuOpen = false;
                return;
            }

            AnyMenuOpen = true;
            storageRackMenu.Open(rack, carry);
        }

        public void OpenWorkstation(Workstation workstation, PlayerCarry carry)
        {
            Debug.Log($"InteractionMenus.OpenWorkstation called for workstation: {workstation?.name}, type: {workstation?.Type}");
            CloseAll();

            if (workstationMenuManager == null || workstation == null || carry == null)
            {
                Debug.LogWarning($"InteractionMenus.OpenWorkstation: Cannot open - manager={workstationMenuManager != null}, workstation={workstation != null}, carry={carry != null}");
                AnyMenuOpen = false;
                return;
            }

            AnyMenuOpen = true;
            workstationMenuManager.OpenMenu(workstation, carry);
        }

        public void CloseAll()
        {
            if (storageRackMenu != null)
                storageRackMenu.Close();

            if (workstationMenuManager != null)
                workstationMenuManager.CloseCurrentMenu();

            AnyMenuOpen = false;
        }
    }
}
