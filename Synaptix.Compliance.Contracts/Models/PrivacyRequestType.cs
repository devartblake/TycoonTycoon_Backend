namespace Synaptix.Compliance.Contracts.Models;

public enum PrivacyRequestType
{
    Know,            // CCPA: right to know what data is collected
    Delete,          // CCPA + COPPA: right to erasure
    OptOut,          // CCPA: do not sell / do not share
    DataPortability  // CCPA/CPRA: structured data export
}
