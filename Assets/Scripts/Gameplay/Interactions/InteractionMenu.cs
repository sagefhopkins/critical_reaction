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
            CloseAll();

            if (storageRackMenu == null || rack == null || carry == null)
            {
                AnyMenuOpen = false;
                return;
            }

            AnyMenuOpen = true;
            storageRackMenu.Open(rack, carry);
        }

        public void CloseAll()
        {
            if (storageRackMenu != null)
                storageRackMenu.Close();

            AnyMenuOpen = false;
        }
    }
}