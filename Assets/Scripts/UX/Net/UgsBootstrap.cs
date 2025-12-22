using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace UX.Net
{
    public class UgsBootstrap : MonoBehaviour
    {
        public static bool Ready { get; private set; }
        public static event Action ReadyChanged;

        private async void Awake()
        {
            if (Ready) return;

            DontDestroyOnLoad(gameObject);

            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Ready = true;
            ReadyChanged?.Invoke();
        }

        public static async Task EnsureReady()
        {
            if (Ready) return;

            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Ready = true;
            ReadyChanged?.Invoke();
        }
    }
}