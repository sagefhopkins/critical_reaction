using Gameplay.Player;
using UnityEngine;

namespace Gameplay.Workstations
{
    public abstract class WorkstationMenuBase : MonoBehaviour
    {
        protected Workstation workstation;
        protected PlayerCarry localCarry;

        public virtual void Open(Workstation targetWorkstation, PlayerCarry carry)
        {
            Unsubscribe();

            workstation = targetWorkstation;
            localCarry = carry;

            if (workstation == null || localCarry == null)
            {
                Close();
                return;
            }

            Subscribe();
            OnOpened();
        }

        public virtual void Close()
        {
            Unsubscribe();
            OnClosed();
            workstation = null;
            localCarry = null;
        }

        protected virtual void OnDisable()
        {
            Unsubscribe();
        }

        protected virtual void Subscribe()
        {
            if (workstation != null)
            {
                workstation.OnWorkStateChanged += OnWorkStateChanged;
                workstation.OnProgressChanged += OnProgressChanged;
                workstation.OnInventoryChanged += OnInventoryChanged;
            }

            if (localCarry != null)
                localCarry.HeldItemChanged += OnHeldItemChanged;
        }

        protected virtual void Unsubscribe()
        {
            if (workstation != null)
            {
                workstation.OnWorkStateChanged -= OnWorkStateChanged;
                workstation.OnProgressChanged -= OnProgressChanged;
                workstation.OnInventoryChanged -= OnInventoryChanged;
            }

            if (localCarry != null)
                localCarry.HeldItemChanged -= OnHeldItemChanged;
        }

        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
        protected virtual void OnWorkStateChanged() { }
        protected virtual void OnProgressChanged() { }
        protected virtual void OnInventoryChanged() { }
        protected virtual void OnHeldItemChanged() { }

        protected void RequestClose()
        {
            if (UX.InteractionMenus.Instance != null)
                UX.InteractionMenus.Instance.CloseAll();
            else if (WorkstationMenuManager.Instance != null)
                WorkstationMenuManager.Instance.CloseCurrentMenu();
        }
    }
}
