# Compliance and Data Protection

This document outlines the compliance measures and data protection practices implemented in the Live Event Service to ensure adherence to GDPR and other relevant regulations.

## Table of Contents
- [Data Collection and Processing](#data-collection-and-processing)
- [User Rights](#user-rights)
- [Data Retention](#data-retention)
- [Security Measures](#security-measures)
- [Incident Response](#incident-response)
- [Third-Party Processors](#third-party-processors)
- [Contact Information](#contact-information)

## Data Collection and Processing

### Personal Data Collected
- **User Information**: Name, email, phone number, authentication identifiers
- **Event Registration Data**: Event preferences, attendance records, waitlist status
- **Technical Data**: IP addresses, device information, access logs
- **Communication Data**: Emails, notifications, support tickets

### Legal Basis for Processing
- **Consent**: For marketing communications and non-essential cookies
- **Contractual Necessity**: For providing the core event registration service
- **Legal Obligation**: For tax, accounting, and regulatory requirements
- **Legitimate Interest**: For service improvement, security, and fraud prevention

## User Rights

Under GDPR, users have the following rights regarding their personal data:

1. **Right to Access**: Users can request a copy of their personal data. Implemented via `GET /api/users/{id}/export` (returns JSON export). Self-access allowed; administrators can export for any user.
2. **Right to Rectification**: Users can update or correct their information.
3. **Right to Erasure**: Users can request deletion of their personal data. Implemented via `DELETE /api/users/{id}`. Current behavior performs deactivation and anonymization by default; hard delete is a privileged administrative operation.
4. **Right to Restrict Processing**: Users can limit how their data is used.
5. **Right to Data Portability**: Users can request their data in a structured format.
6. **Right to Object**: Users can object to certain types of processing.
7. **Rights Related to Automated Decision Making**: Users can request human intervention.

## Data Retention

### Retention Periods

| Data Type | Retention Period | Legal Basis |
|-----------|------------------|-------------|
| User Account Data | 3 years after last activity | Contractual necessity |
| Event Registration Data | 5 years after event completion | Legal obligation |
| Financial Records | 7 years | Legal requirement |
| System Logs | 1 year | Legitimate interest |
| Marketing Data | Until consent is withdrawn | Consent |

### Data Deletion
- Automatic deletion/anonymization is available via an opt-in background retention service (disabled by default). Enable via configuration under `Security:Retention` to:
  - Anonymize inactive users beyond configured retention days
  - Delete cancelled registrations beyond configured retention days
- Users can request early deletion via the service interface or by contacting support
- API support: `DELETE /api/users/{id}` (admin-only) performs deactivation + anonymization to preserve event integrity while honoring privacy.
- Backups retention is managed by RDS/Backup plans; production default is 35 days (see CDK stack)

#### Example configuration (appsettings.Production.json)
```json
{
  "Security": {
    "Encryption": {
      "Key": "{Injected from Secrets Manager}",
      "IV": "{Injected from Secrets Manager}"
    },
    "Retention": {
      "Enabled": true,
      "IntervalHours": 6,
      "UsersRetentionDays": 1095,
      "CancelledRegistrationsRetentionDays": 365
    }
  }
}
```

## Security Measures

### Technical Measures
- Data encryption in transit (TLS 1.2+)
- Data encryption at rest at the storage level (RDS encryption via KMS)
 - Field-level encryption for PII: Implemented via EF Core value converters. Keys are provided via Secrets Manager (KMS-backed) and injected into the API/Worker tasks as secrets (`Security:Encryption:Key` and `Security:Encryption:IV`). If secrets are not set, encryption converters operate in pass-through mode (development/testing only).
- Regular security audits and penetration testing
- Access controls and principle of least privilege
- Multi-factor authentication for administrative access

### Organizational Measures
- Data protection impact assessments for new features
- Regular staff training on data protection
- Data processing agreements with third-party providers
- Documented data protection policies and procedures

## Incident Response

### Breach Notification
- Users will be notified within 72 hours of becoming aware of a data breach
- Regulatory authorities will be notified where required by law
- Incident response plan includes containment, assessment, notification, and review phases

### Response Team
- Designated Data Protection Officer (DPO)
- Security incident response team
- Legal and PR teams for communication

## Third-Party Processors

Current infrastructure provider:

| Processor | Purpose | Data Processed | Location |
|-----------|---------|----------------|----------|
| AWS | Cloud Infrastructure | Service data (compute, storage, networking) | Multiple regions |

Note: Email and payment providers (e.g., SendGrid, Stripe) are not currently integrated. If added in the future, they will be vetted for GDPR compliance and DPAs will be established prior to use.

## Contact Information

For any data protection inquiries or to exercise your rights:

- **Data Protection Officer**: [DPO Email]
- **Mailing Address**: [Company Address]
- **Phone**: [Contact Number]

## Policy Review

This policy is reviewed annually or when significant changes to our processing activities occur. Last updated: [Current Date]

## Implementation Details

### Technical Implementation
- Data retention is implemented using AWS RDS automated backups and snapshots (retention tuned per environment). Application-level purging jobs can be enabled via `Security:Retention` options.
- Audit logs for admin actions are written to a dedicated CloudWatch Logs group
- All data processing activities are logged in AWS CloudTrail
- Regular data protection impact assessments are conducted

### Monitoring and Compliance
- Automated monitoring of data access and modifications
- Regular compliance audits
- Employee training on data protection policies
- Documentation of all data processing activities
