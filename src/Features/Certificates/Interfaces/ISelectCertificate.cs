using System.Security.Cryptography.X509Certificates;

namespace Conesoft.Server_Host.Features.Certificates.Interfaces;

interface ISelectCertificate
{
    X509Certificate2 CertificateFor(string domain);
}