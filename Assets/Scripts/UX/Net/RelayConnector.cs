using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace UX.Net
{
    public class RelayConnector : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private string connectionType = "dtls";
        [SerializeField] private int maxClientsDefault = 3;

        public string LastJoinCode { get; private set; }

        private void Awake()
        {
            if (networkManager == null)
                networkManager = NetworkManager.Singleton;

            if (transport == null && networkManager != null)
                transport = networkManager.GetComponent<UnityTransport>();
        }

        public async Task<string> HostAsync(int maxClientCount = -1)
        {
            await UgsBootstrap.EnsureReady();

            if (networkManager == null || transport == null)
                throw new InvalidOperationException("NetworkManager or UnityTransport not found.");

            if (networkManager.IsListening)
                networkManager.Shutdown();

            int maxClients = maxClientCount >= 0 ? maxClientCount : maxClientsDefault;

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxClients);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            ApplyAllocationToTransport(allocation);

            if (!networkManager.StartHost())
                throw new InvalidOperationException("Failed to start host.");

            LastJoinCode = joinCode;
            return joinCode;
        }

        public async Task JoinAsync(string joinCode)
        {
            await UgsBootstrap.EnsureReady();

            if (networkManager == null || transport == null)
                throw new InvalidOperationException("NetworkManager or UnityTransport not found.");

            if (string.IsNullOrWhiteSpace(joinCode))
                throw new ArgumentException("Join code is empty.");

            if (networkManager.IsListening)
                networkManager.Shutdown();

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

            ApplyJoinAllocationToTransport(joinAllocation);

            if (!networkManager.StartClient())
                throw new InvalidOperationException("Failed to start client.");

            LastJoinCode = joinCode.Trim();
        }

        public void Shutdown()
        {
            LastJoinCode = null;

            if (networkManager != null && networkManager.IsListening)
                networkManager.Shutdown();
        }

        private void ApplyAllocationToTransport(Allocation allocation)
        {
            RelayServerEndpoint endpoint = PickEndpoint(allocation.ServerEndpoints, allocation.RelayServer);
            transport.SetRelayServerData(
                endpoint.Host,
                (ushort)endpoint.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                null,
                endpoint.Secure
            );
        }

        private void ApplyJoinAllocationToTransport(JoinAllocation joinAllocation)
        {
            RelayServerEndpoint endpoint = PickEndpoint(joinAllocation.ServerEndpoints, joinAllocation.RelayServer);
            transport.SetRelayServerData(
                endpoint.Host,
                (ushort)endpoint.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                endpoint.Secure
            );
        }

        private RelayServerEndpoint PickEndpoint(System.Collections.Generic.IReadOnlyList<RelayServerEndpoint> endpoints, RelayServer fallback)
        {
            string desired = string.IsNullOrWhiteSpace(connectionType) ? RelayServerEndpoint.ConnectionTypeDtls : connectionType;

            if (endpoints != null)
            {
                for (int i = 0; i < endpoints.Count; i++)
                {
                    if (string.Equals(endpoints[i].ConnectionType, desired, StringComparison.OrdinalIgnoreCase))
                        return endpoints[i];
                }
            }

            if (fallback != null)
            {
                bool secure = string.Equals(desired, RelayServerEndpoint.ConnectionTypeDtls, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(desired, RelayServerEndpoint.ConnectionTypeWss, StringComparison.OrdinalIgnoreCase);

                return new RelayServerEndpoint(
                    desired,
                    RelayServerEndpoint.NetworkOptions.Udp,
                    reliable: false,
                    secure: secure,
                    host: fallback.IpV4,
                    port: fallback.Port
                );
            }

            throw new InvalidOperationException($"No Relay endpoint found for connectionType '{desired}'.");
        }
    }
}
