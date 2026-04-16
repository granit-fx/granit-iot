# Security Policy

## Supported Versions

| Version | Supported |
| ------- | --------- |
| latest  | Yes       |

Only the latest released version receives security updates.

## Reporting a Vulnerability

**DO NOT open a public GitHub issue for security vulnerabilities.**

### Preferred: GitHub Private Security Advisory

Use [GitHub Private Security Advisories](https://github.com/granit-fx/granit-iot/security/advisories/new)
to report vulnerabilities confidentially. This enables coordinated disclosure
and automatic CVE assignment through MITRE.

### Alternative: Email

Send a detailed report to **<security@granit-fx.dev>**.

### What to include

- Steps to reproduce (proof of concept if possible)
- Potential impact and attack scenario
- Any suggested mitigations

## Response SLA

| Severity     | Acknowledgment | Patch target | Public disclosure   |
| ------------ | -------------- | ------------ | ------------------- |
| Critical     | 24 hours       | 7 days       | 14 days after patch |
| High         | 48 hours       | 30 days      | 30 days after patch |
| Medium / Low | 48 hours       | 90 days      | 90 days after patch |

## Out of scope

- Issues requiring physical access to infrastructure
- Social engineering attacks
- Denial-of-service through resource exhaustion

## Security Design

This module family is built on the [Granit framework](https://github.com/granit-fx/granit-dotnet),
which provides security primitives (OIDC/OpenIddict, RBAC, encryption, audit trail, GDPR).
For framework-level security issues, refer to the
[Granit security policy](https://github.com/granit-fx/granit-dotnet/security/policy).

## Recognition

Reporters of valid vulnerabilities will be credited in the release notes
(unless they prefer to remain anonymous).
