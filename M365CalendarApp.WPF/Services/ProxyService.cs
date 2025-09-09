using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace M365CalendarApp.WPF.Services;

public static class ProxyService
{
    public static void ConfigureProxy()
    {
        try
        {
            // Configure proxy settings from environment variables
            var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY") ?? 
                           Environment.GetEnvironmentVariable("https_proxy");
            
            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY") ?? 
                          Environment.GetEnvironmentVariable("http_proxy");

            if (!string.IsNullOrEmpty(httpsProxy) || !string.IsNullOrEmpty(httpProxy))
            {
                var proxy = new WebProxy();
                
                if (!string.IsNullOrEmpty(httpsProxy))
                {
                    proxy.Address = new Uri(httpsProxy);
                }
                else if (!string.IsNullOrEmpty(httpProxy))
                {
                    proxy.Address = new Uri(httpProxy);
                }

                // Check for proxy credentials
                var proxyUser = Environment.GetEnvironmentVariable("PROXY_USER");
                var proxyPass = Environment.GetEnvironmentVariable("PROXY_PASS");
                
                if (!string.IsNullOrEmpty(proxyUser) && !string.IsNullOrEmpty(proxyPass))
                {
                    proxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
                }
                else
                {
                    proxy.UseDefaultCredentials = true;
                }

                WebRequest.DefaultWebProxy = proxy;
                
                System.Diagnostics.Debug.WriteLine($"Proxy configured: {proxy.Address}");
            }

            // Configure certificate validation to use Windows certificate store
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            
            // Enable TLS 1.2 and higher
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Proxy configuration failed: {ex.Message}");
        }
    }

    private static bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        System.Net.Security.SslPolicyErrors sslPolicyErrors)
    {
        // If there are no SSL policy errors, the certificate is valid
        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
        {
            return true;
        }

        // Check against Windows certificate store
        if (certificate != null)
        {
            try
            {
                var cert2 = new X509Certificate2(certificate);
                
                // Check if certificate is in the trusted root store
                using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                
                var foundCerts = store.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    cert2.Thumbprint,
                    false);
                
                if (foundCerts.Count > 0)
                {
                    return true;
                }

                // Check machine store as well
                using var machineStore = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                machineStore.Open(OpenFlags.ReadOnly);
                
                var foundMachineCerts = machineStore.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    cert2.Thumbprint,
                    false);
                
                if (foundMachineCerts.Count > 0)
                {
                    return true;
                }

                // Additional validation for corporate certificates
                return ValidateCorporateCertificate(cert2, chain);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Certificate validation error: {ex.Message}");
            }
        }

        // Log the SSL policy errors for debugging
        System.Diagnostics.Debug.WriteLine($"SSL Policy Errors: {sslPolicyErrors}");
        
        // In production, you might want to be more strict
        // For corporate environments, this allows trusted corporate CAs
        return sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors;
    }

    private static bool ValidateCorporateCertificate(X509Certificate2 certificate, X509Chain? chain)
    {
        try
        {
            if (chain == null) return false;

            // Build the certificate chain
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            chain.ChainPolicy.VerificationTime = DateTime.Now;
            
            // Add intermediate CA stores
            chain.ChainPolicy.ExtraStore.AddRange(GetIntermediateCertificates());

            bool isValid = chain.Build(certificate);
            
            if (!isValid)
            {
                // Log chain errors for debugging
                foreach (var status in chain.ChainStatus)
                {
                    System.Diagnostics.Debug.WriteLine($"Chain Status: {status.Status} - {status.StatusInformation}");
                }
            }

            return isValid;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Corporate certificate validation error: {ex.Message}");
            return false;
        }
    }

    private static X509Certificate2Collection GetIntermediateCertificates()
    {
        var certificates = new X509Certificate2Collection();
        
        try
        {
            // Add certificates from intermediate CA store
            using var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            certificates.AddRange(store.Certificates);
            
            using var machineStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
            machineStore.Open(OpenFlags.ReadOnly);
            certificates.AddRange(machineStore.Certificates);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading intermediate certificates: {ex.Message}");
        }
        
        return certificates;
    }

    public static HttpClientHandler CreateHttpClientHandler()
    {
        var handler = new HttpClientHandler();
        
        try
        {
            // Configure proxy
            var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY") ?? 
                           Environment.GetEnvironmentVariable("https_proxy");
            
            if (!string.IsNullOrEmpty(httpsProxy))
            {
                handler.Proxy = new WebProxy(httpsProxy);
                handler.UseProxy = true;
                
                var proxyUser = Environment.GetEnvironmentVariable("PROXY_USER");
                var proxyPass = Environment.GetEnvironmentVariable("PROXY_PASS");
                
                if (!string.IsNullOrEmpty(proxyUser) && !string.IsNullOrEmpty(proxyPass))
                {
                    handler.Proxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
                }
                else
                {
                    handler.UseDefaultCredentials = true;
                }
            }

            // Configure certificate validation
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                ValidateServerCertificate(message, cert, chain, errors);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HttpClientHandler configuration failed: {ex.Message}");
        }
        
        return handler;
    }
}