using System.Net;
using System.Net.Sockets;

namespace Fan_Website.Infrastructure
{
    public static class PrivateNetworkGuard
    {
        public static bool IsPublicAddress(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();

            if (IPAddress.IsLoopback(address))
                return false;

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = address.GetAddressBytes();
                if (b[0] == 0) return false;                                   // 0.0.0.0/8
                if (b[0] == 10) return false;                                  // 10.0.0.0/8
                if (b[0] == 127) return false;                                 // 127.0.0.0/8 loopback
                if (b[0] == 169 && b[1] == 254) return false;                  // 169.254.0.0/16 link-local / cloud metadata
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;      // 172.16.0.0/12
                if (b[0] == 192 && b[1] == 168) return false;                  // 192.168.0.0/16
                return true;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
                    return false;

                var b = address.GetAddressBytes();
                if ((b[0] & 0xfe) == 0xfc) return false;                       // fc00::/7 unique local

                return true;
            }

            // Anything else (unknown address family) - refuse rather than guess.
            return false;
        }

        // Resolves the host and returns the first address that's safe to connect to,
        // or null if none of the resolved addresses are public.
        public static async Task<IPAddress?> ResolvePublicAddressAsync(string host, CancellationToken cancellationToken = default)
        {
            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            }
            catch
            {
                return null;
            }

            return addresses.FirstOrDefault(IsPublicAddress);
        }
    }
}
